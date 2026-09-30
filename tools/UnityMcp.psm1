<#
  Client Unity MCP (MCP for Unity, HTTP transport cua FastMCP) + buoc xac minh bang script.

  Muc dich: task xac minh (compile, console, test, menu, file) chay KHONG can AI -> 0 token.
  Pilot WP004-B-VERIFY: agent ton ~110k token Codex + ~$0.20 Claude chi de goi 5 tool MCP va ghi 3 file.

  Tool dung (MCP for Unity v10): refresh_unity, read_console, run_tests, get_test_job, execute_menu_item.
  Giao thuc: JSON-RPC qua POST <url>; initialize tra header Mcp-Session-Id; tra loi co the la JSON hoac SSE.
  Unity khong tra loi ping khi dang compile/chay test (main thread ban) -> moi call deu retry toi deadline.
#>

$script:McpUrl = $null
$script:McpSession = $null
$script:McpProtocol = $null
$script:McpNextId = 1

function ConvertFrom-McpBody([string]$Body) {
    $t = $Body.Trim()
    if ($t.StartsWith('{') -or $t.StartsWith('[')) { return , @($t | ConvertFrom-Json) }
    # SSE: moi su kien co dong 'data: {...}'
    $out = @()
    $buf = ''
    foreach ($line in ($Body -split "`r?`n")) {
        if ($line.StartsWith('data:')) { $buf += $line.Substring(5).TrimStart() }
        elseif ($line.Trim() -eq '' -and $buf) { try { $out += ($buf | ConvertFrom-Json) } catch { } ; $buf = '' }
    }
    if ($buf) { try { $out += ($buf | ConvertFrom-Json) } catch { } }
    return , $out
}

function Send-McpMessage([hashtable]$Message, [int]$TimeoutSec = 120) {
    $headers = @{ 'Accept' = 'application/json, text/event-stream' }
    if ($script:McpSession) { $headers['Mcp-Session-Id'] = $script:McpSession }
    if ($script:McpProtocol) { $headers['MCP-Protocol-Version'] = $script:McpProtocol }
    $json = $Message | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $resp = Invoke-WebRequest -Uri $script:McpUrl -Method Post -Headers $headers -ContentType 'application/json' -Body $bytes -UseBasicParsing -TimeoutSec $TimeoutSec
    $sid = $resp.Headers['Mcp-Session-Id']
    if (-not $sid) { $sid = $resp.Headers['mcp-session-id'] }
    if ($sid) { $script:McpSession = (@($sid) -join '') }
    $body = ''
    if ($resp.RawContentStream) { $body = [System.Text.Encoding]::UTF8.GetString($resp.RawContentStream.ToArray()) }
    elseif ($resp.Content -is [byte[]]) { $body = [System.Text.Encoding]::UTF8.GetString($resp.Content) }
    else { $body = [string]$resp.Content }
    if (-not $Message.ContainsKey('id')) { return $null }
    foreach ($m in (ConvertFrom-McpBody $body)) {
        if ($null -ne $m.id -and [string]$m.id -eq [string]$Message.id) { return $m }
    }
    throw "MCP: khong co tra loi cho id $($Message.id)"
}

function Connect-UnityMcp {
    param([Parameter(Mandatory = $true)][string]$Url)
    $script:McpUrl = $Url
    $script:McpSession = $null
    $script:McpProtocol = $null
    $id = $script:McpNextId++
    $init = Send-McpMessage @{
        jsonrpc = '2.0'; id = $id; method = 'initialize'
        params = @{ protocolVersion = '2025-06-18'; capabilities = @{}; clientInfo = @{ name = 'run-task.ps1'; version = '1' } }
    } 30
    if ($init.error) { throw "MCP initialize: $($init.error.message)" }
    if ($init.result -and $init.result.protocolVersion) { $script:McpProtocol = [string]$init.result.protocolVersion }
    $null = Send-McpMessage @{ jsonrpc = '2.0'; method = 'notifications/initialized' } 30
    return $init.result
}

# Goi mot tool; tra ve object ket qua (structuredContent, hoac JSON trong content[0].text).
function Invoke-UnityTool {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [hashtable]$Arguments = @{},
        [int]$TimeoutSec = 120,
        [int]$RetryForSec = 600
    )
    $deadline = (Get-Date).AddSeconds($RetryForSec)
    $lastError = $null
    while ($true) {
        try {
            $id = $script:McpNextId++
            $r = Send-McpMessage @{ jsonrpc = '2.0'; id = $id; method = 'tools/call'; params = @{ name = $Name; arguments = $Arguments } } $TimeoutSec
            if ($r.error) { throw "MCP $Name loi: $($r.error.message)" }
            $res = $r.result
            $payload = $null
            if ($res.structuredContent) { $payload = $res.structuredContent }
            elseif ($res.content) {
                $text = (@($res.content | Where-Object { $_.type -eq 'text' } | ForEach-Object { $_.text }) -join "`n")
                try { $payload = $text | ConvertFrom-Json } catch { $payload = [pscustomobject]@{ text = $text } }
            }
            # FastMCP boc gia tri tra ve trong {"result": {...}} (thay tren Unity that, run 20260930-134739).
            if ($payload -and $payload.PSObject.Properties['result'] -and -not $payload.PSObject.Properties['data'] -and $payload.result -isnot [string]) { $payload = $payload.result }
            # Unity ban (dang compile / chay test): server tra success=false kem 'busy' / 'not ready' -> thu lai.
            $msg = [string]$(if ($payload) { @($payload.error, $payload.message) -join ' ' } else { '' })
            if ($res.isError -or ($payload -and $payload.success -eq $false -and $msg -match '(?i)busy|not ready|compil|reload|ping|timeout|retry')) {
                throw "MCP $Name tam thoi chua san sang: $msg"
            }
            return $payload
        } catch {
            $lastError = $_.Exception.Message
            if ((Get-Date) -gt $deadline) { throw "MCP $Name that bai sau $RetryForSec s: $lastError" }
            Start-Sleep -Seconds 10
            # Phien co the mat khi Unity reload domain / server restart: mo lai.
            try { Connect-UnityMcp -Url $script:McpUrl | Out-Null } catch { }
        }
    }
}

function Get-ConsoleEntries($Payload) {
    # Khong hieu dinh dang -> loi, khong duoc coi la '0 dong' (neu khong buoc 'maxCount 0' PASS gia).
    if (-not $Payload -or -not $Payload.PSObject.Properties['data']) { throw "read_console tra dinh dang la: $($Payload | ConvertTo-Json -Compress -Depth 6)" }
    if ($Payload.success -eq $false) { throw "read_console loi: $($Payload.message) $($Payload.error)" }
    $data = $Payload.data
    $list = @()
    if ($data -is [System.Array]) { $list = $data }
    elseif ($data -is [string]) { $list = @($data -split "`r?`n" | Where-Object { $_ }) }
    elseif ($data) {
        $found = $false
        foreach ($k in @('lines', 'items', 'entries', 'messages')) { if ($data.PSObject.Properties[$k]) { $list = @($data.$k); $found = $true; break } }
        if (-not $found) { throw "read_console: khong tim thay danh sach log trong data: $($data | ConvertTo-Json -Compress -Depth 6)" }
    }
    return @($list | ForEach-Object {
            if ($_ -is [string]) { $_ } elseif ($_.message) { [string]$_.message } elseif ($_.text) { [string]$_.text } else { ($_ | ConvertTo-Json -Compress) }
        })
}

function Write-VerifyFile([string]$Root, [string]$Rel, [string]$Text) {
    if (-not $Rel) { return }
    $full = Join-Path $Root $Rel
    $dir = Split-Path -Parent $full
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($full, $Text, (New-Object System.Text.UTF8Encoding $false))
}

<#
  Chay cac buoc 'verify' cua task. Moi buoc: { "do": ..., ... }
    console-clear                         xoa Console
    refresh   { compile: true }           refresh asset + yeu cau compile, cho editor san sang
    console   { types:["error"], maxCount:0, filter:"", expect:"regex", out:"path" }
    tests     { mode:"EditMode", out:"path", expectAllPass:true }
    menu      { path:"Menu/Item" }
    files     { exist:["path", ...] }
  Tra ve @{ Pass; Lines (bao cao); Failures }.
#>
function Invoke-VerifySteps {
    param([Parameter(Mandatory = $true)]$Steps, [Parameter(Mandatory = $true)][string]$Root, [int]$TestTimeoutMin = 25)
    $lines = New-Object System.Collections.Generic.List[string]
    $fails = New-Object System.Collections.Generic.List[string]
    $i = 0
    foreach ($s in @($Steps)) {
        $i++
        $label = "[$i] $($s.do)"
        try {
            switch ([string]$s.do) {
                'console-clear' {
                    $null = Invoke-UnityTool -Name 'read_console' -Arguments @{ action = 'clear' }
                    $lines.Add("${label}: OK")
                }
                'refresh' {
                    $args2 = @{ mode = 'force'; scope = 'all'; wait_for_ready = $true }
                    if ($s.compile) { $args2['compile'] = 'request' }
                    $null = Invoke-UnityTool -Name 'refresh_unity' -Arguments $args2 -TimeoutSec 300
                    $lines.Add("${label}: OK (compile=$([bool]$s.compile))")
                }
                'console' {
                    # @(...) ngoai if: PowerShell boc mang 1 phan tu thanh chuoi; server doi 'types' la list.
                    [string[]]$types = @(if ($s.types) { @($s.types) } else { 'error' })
                    $a = @{ action = 'get'; types = [object[]]$types; count = 200; format = 'json' }
                    if ($s.filter) { $a['filter_text'] = [string]$s.filter }
                    $entries = Get-ConsoleEntries (Invoke-UnityTool -Name 'read_console' -Arguments $a)
                    $text = ($entries -join "`n")
                    $ok = $true
                    $why = @()
                    if ($null -ne $s.maxCount -and $entries.Count -gt [int]$s.maxCount) { $ok = $false; $why += "$($entries.Count) dong > toi da $($s.maxCount)" }
                    if ($s.expect -and $text -notmatch [string]$s.expect) { $ok = $false; $why += "khong khop '$($s.expect)'" }
                    Write-VerifyFile $Root $s.out ("types=" + ($types -join ',') + $(if ($s.filter) { " filter=$($s.filter)" } else { '' }) + "`ncount=$($entries.Count)`n`n" + $text + "`n")
                    if ($ok) { $lines.Add("${label}: OK ($($entries.Count) dong)") } else { $fails.Add("${label}: " + ($why -join '; ')); $lines.Add("${label}: FAIL " + ($why -join '; ')) }
                }
                'tests' {
                    $mode = if ($s.mode) { [string]$s.mode } else { 'EditMode' }
                    $start = Invoke-UnityTool -Name 'run_tests' -Arguments @{ mode = $mode; include_failed_tests = $true } -TimeoutSec 120
                    $jobId = [string]$start.data.job_id
                    if (-not $jobId) { throw "run_tests khong tra job_id: $($start | ConvertTo-Json -Compress -Depth 6)" }
                    $deadline = (Get-Date).AddMinutes($TestTimeoutMin)
                    $job = $null
                    while ($true) {
                        $job = Invoke-UnityTool -Name 'get_test_job' -Arguments @{ job_id = $jobId; include_failed_tests = $true; wait_timeout = 50 } -TimeoutSec 120 -RetryForSec 900
                        $st = [string]$job.data.status
                        if (@('succeeded', 'failed', 'cancelled') -contains $st) { break }
                        if ((Get-Date) -gt $deadline) { throw "test job $jobId qua $TestTimeoutMin phut (status=$st)" }
                    }
                    $sum = $job.data.result.summary
                    $failed = @($job.data.result.results | Where-Object { $_.state -and $_.state -notmatch '(?i)pass' })
                    $md = "# Tests ($mode)`n`n- job: ``$jobId```n- status: $($job.data.status)`n- total: $($sum.total)`n- passed: $($sum.passed)`n- failed: $($sum.failed)`n- skipped: $($sum.skipped)`n- duration: $($sum.durationSeconds) s`n"
                    if ($failed.Count) { $md += "`n## Failures`n" + (($failed | ForEach-Object { "- $($_.fullName): $($_.message)" }) -join "`n") + "`n" }
                    Write-VerifyFile $Root $s.out $md
                    $allPass = ($job.data.status -eq 'succeeded') -and ([int]$sum.failed -eq 0) -and ([int]$sum.total -gt 0)
                    if (($s.expectAllPass -eq $false) -or $allPass) { $lines.Add("${label}: OK $($sum.passed)/$($sum.total) ($($sum.durationSeconds) s)") }
                    else { $fails.Add("${label}: $($sum.failed) fail / $($sum.total) (job $jobId)"); $lines.Add("${label}: FAIL $($sum.passed)/$($sum.total)") }
                }
                'menu' {
                    $r = Invoke-UnityTool -Name 'execute_menu_item' -Arguments @{ menu_path = [string]$s.path } -TimeoutSec 300
                    if ($r -and $r.success -eq $false) { throw "menu '$($s.path)': $($r.message) $($r.error)" }
                    $lines.Add("${label}: OK '$($s.path)'")
                }
                'files' {
                    $missing = @(@($s.exist) | Where-Object { -not (Test-Path -LiteralPath (Join-Path $Root $_)) })
                    if ($missing.Count) { $fails.Add("${label}: thieu " + ($missing -join ', ')); $lines.Add("${label}: FAIL thieu $($missing.Count) file") }
                    else { $lines.Add("${label}: OK $(@($s.exist).Count) file") }
                }
                default { throw "buoc khong ho tro: '$($s.do)'" }
            }
        } catch {
            $fails.Add("${label}: LOI $($_.Exception.Message)")
            $lines.Add("${label}: LOI $($_.Exception.Message)")
        }
    }
    return [pscustomobject]@{ Pass = ($fails.Count -eq 0); Lines = @($lines); Failures = @($fails) }
}

Export-ModuleMember -Function Connect-UnityMcp, Invoke-UnityTool, Invoke-VerifySteps, ConvertFrom-McpBody
