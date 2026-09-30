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
    # Log nhieu dong: MCP for Unity chi dat dong dau vao 'message', phan con lai nam o stack trace
    # (thay tren Unity that: '[MotionParity]' mat dong '=> 38/38 (fail 0)'). Ghep lai de 'expect' khop ca log.
    return @($list | ForEach-Object {
            if ($_ -is [string]) { $_ }
            elseif ($_.message -or $_.text) {
                $parts = @([string]$(if ($_.message) { $_.message } else { $_.text }))
                foreach ($k in @('stackTrace', 'stacktrace', 'stack_trace', 'details')) { if ($_.PSObject.Properties[$k] -and $_.$k) { $parts += [string]$_.$k } }
                $parts -join "`n"
            }
            else { ($_ | ConvertTo-Json -Compress) }
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
# Doc mot MCP resource (JSON trong contents[0].text). Loi / khong ho tro -> $null (khong chan).
function Read-McpResource([string]$Uri) {
    try {
        $id = $script:McpNextId++
        $r = Send-McpMessage @{ jsonrpc = '2.0'; id = $id; method = 'resources/read'; params = @{ uri = $Uri } } 30
        if ($r.error -or -not $r.result.contents) { return $null }
        $text = (@($r.result.contents | ForEach-Object { $_.text }) -join "`n")
        return ($text | ConvertFrom-Json)
    } catch { return $null }
}

function ConvertTo-ComparablePath([string]$Path) {
    return ($Path -replace '\\', '/').TrimEnd('/').ToLowerInvariant()
}

# Chot chan: script phai noi chuyen voi Unity dang mo DUNG repo nay.
# Bai hoc 2026-09-30: Unity mo Template_001 giu cong 8080, verify cua Demo chay tren Template (7 test thay vi 79).
# Nhieu Editor cung noi vao server: chon instance trung ten thu muc repo (set_active_instance).
function Assert-UnityProject([string]$Root) {
    $name = Split-Path -Leaf ([System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/'))
    $inst = Read-McpResource 'mcpforunity://instances'
    $list = if ($inst) { @($inst.instances) } else { @() }
    if ($list.Count -gt 1) {
        $match = @($list | Where-Object { [string]$_.name -eq $name })
        if ($match.Count -eq 1) {
            $null = Invoke-UnityTool -Name 'set_active_instance' -Arguments @{ instance = [string]$match[0].id } -RetryForSec 30
        } else {
            return "co $($list.Count) Unity dang noi MCP ($((@($list | ForEach-Object { $_.id })) -join ', ')), khong chon duoc instance ten '$name'"
        }
    }
    $info = Read-McpResource 'mcpforunity://project/info'
    $projRoot = if ($info -and $info.data) { [string]$info.data.projectRoot } elseif ($info) { [string]$info.projectRoot } else { '' }
    if (-not $projRoot) { return $null }  # server cu khong co resource nay: bo qua, cac buoc sau van co minTotal
    $want = ConvertTo-ComparablePath ([System.IO.Path]::GetFullPath($Root))
    if ((ConvertTo-ComparablePath $projRoot) -ne $want) {
        return "Unity MCP dang noi voi project '$projRoot', khong phai repo nay ($Root). Mo dung project trong Unity (hoac tat Editor kia) roi chay lai."
    }
    return $null
}

function Invoke-VerifySteps {
    param([Parameter(Mandatory = $true)]$Steps, [Parameter(Mandatory = $true)][string]$Root, [int]$TestTimeoutMin = 25,
        [string]$BlenderExe, [switch]$SkipProjectCheck)
    $lines = New-Object System.Collections.Generic.List[string]
    $fails = New-Object System.Collections.Generic.List[string]
    $wrong = if ($SkipProjectCheck) { $null } else { Assert-UnityProject $Root }
    if ($wrong) {
        $lines.Add("[0] project: SAI $wrong")
        $fails.Add("[0] project: $wrong")
        return [pscustomobject]@{ Pass = $false; Lines = @($lines); Failures = @($fails) }
    }
    if (-not $SkipProjectCheck) { $lines.Add('[0] project: OK') }
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
                    # waitSec: viec bat dong bo (capture trong Play Mode...) -> doc lai moi 5 s toi khi du minCount hoac het gio.
                    $waitUntil = (Get-Date).AddSeconds([int]$(if ($s.waitSec) { $s.waitSec } else { 0 }))
                    while ($true) {
                        $payload = Invoke-UnityTool -Name 'read_console' -Arguments $a -RetryForSec 300
                        $entries = Get-ConsoleEntries $payload
                        if ($null -eq $s.minCount -or $entries.Count -ge [int]$s.minCount -or (Get-Date) -ge $waitUntil) { break }
                        Start-Sleep -Seconds 5
                    }
                    $text = ($entries -join "`n")
                    $ok = $true
                    $why = @()
                    if ($null -ne $s.maxCount -and $entries.Count -gt [int]$s.maxCount) { $ok = $false; $why += "$($entries.Count) dong > toi da $($s.maxCount)" }
                    if ($null -ne $s.minCount -and $entries.Count -lt [int]$s.minCount) { $ok = $false; $why += "$($entries.Count) dong < toi thieu $($s.minCount)" }
                    if ($s.expect -and $text -notmatch [string]$s.expect) {
                        # Truong ten khac / log bi tach: thu khop tren JSON tho, va ghi JSON tho vao file de soi.
                        $raw = $payload | ConvertTo-Json -Compress -Depth 8
                        $text += "`n`n--- raw read_console ---`n" + $raw
                        if ($raw -notmatch [string]$s.expect) { $ok = $false; $why += "khong khop '$($s.expect)'" }
                    }
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
                    if (-not $sum) {
                        # Job 'failed' khong co result (bai hoc 2026-09-30): ly do o data.error, test fail o progress.failures_so_far.
                        $pr = $job.data.progress
                        $md += "`n## Job khong co result`n- error: $($job.data.error)`n- progress: $($pr.completed)/$($pr.total)`n"
                        if ($pr.current_test_full_name) { $md += "- dang chay: $($pr.current_test_full_name)`n" }
                        if ($pr.blocked_reason) { $md += "- blocked: $($pr.blocked_reason)`n" }
                        $sof = @($pr.failures_so_far | Where-Object { $_ })
                        if ($sof.Count) { $md += "`n## Failures so far`n" + (($sof | ForEach-Object { "- $($_.full_name): $($_.message)" }) -join "`n") + "`n" }
                        $md += "`n## Raw`n``````json`n" + ($job | ConvertTo-Json -Depth 8 -Compress) + "`n```````n"
                        $sum = [pscustomobject]@{ total = $pr.total; passed = $null; failed = $sof.Count; skipped = $null; durationSeconds = $null }
                    }
                    Write-VerifyFile $Root $s.out $md
                    $allPass = ($job.data.status -eq 'succeeded') -and ([int]$sum.failed -eq 0) -and ([int]$sum.total -gt 0)
                    # minTotal: so test it bat thuong = test assembly khong compile / sai project.
                    if ($null -ne $s.minTotal -and [int]$sum.total -lt [int]$s.minTotal) {
                        $fails.Add("${label}: chi $($sum.total) test < minTotal $($s.minTotal) (job $($job.data.status)$(if ($job.data.error) { ": $($job.data.error)" }))")
                        $lines.Add("${label}: FAIL $($sum.passed)/$($sum.total) < minTotal $($s.minTotal)")
                    }
                    elseif (($s.expectAllPass -eq $false) -or $allPass) { $lines.Add("${label}: OK $($sum.passed)/$($sum.total) ($($sum.durationSeconds) s)") }
                    else {
                        $why = if ($job.data.error) { "; $($job.data.error)" } else { '' }
                        $fails.Add("${label}: $($sum.failed) fail / $($sum.total) (job $jobId $($job.data.status)$why)"); $lines.Add("${label}: FAIL $($sum.passed)/$($sum.total)$why")
                    }
                }
                'menu' {
                    $r = Invoke-UnityTool -Name 'execute_menu_item' -Arguments @{ menu_path = [string]$s.path } -TimeoutSec 300
                    if ($r -and $r.success -eq $false) { throw "menu '$($s.path)': $($r.message) $($r.error)" }
                    $lines.Add("${label}: OK '$($s.path)'")
                }
                'blender' {
                    # Blender headless (0 token): tools/blender/mesh_report.py -> nguong trong step. Exe tu doctor (capabilities.blender).
                    if (-not $BlenderExe) { throw 'chua co Blender (doctor -Blender; Docs/asset-pipeline.json)' }
                    $script = if ($s.script) { [string]$s.script } else { 'tools/blender/mesh_report.py' }
                    $bargs = @('-b', '--factory-startup', '-P', (Join-Path $Root $script), '--') + @($s.args | ForEach-Object { if ($_ -match '^[\w./-]+\.(fbx|glb|gltf|obj|blend)$') { Join-Path $Root $_ } else { [string]$_ } })
                    $prevEap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
                    try { $bout = (& $BlenderExe @bargs 2>&1 | ForEach-Object { "$_" }) -join "`n" } finally { $ErrorActionPreference = $prevEap }
                    $bline = @($bout -split "`n" | Where-Object { $_ -match '^AGENTPACK_[A-Z]+ ' }) | Select-Object -Last 1
                    if (-not $bline) { throw "blender khong in dong AGENTPACK_*: $(($bout -replace '\s+', ' ').Substring(0, [Math]::Min(200, ($bout -replace '\s+', ' ').Length)))" }
                    $rep = $bline.Substring($bline.IndexOf(' ') + 1) | ConvertFrom-Json
                    $why = @()
                    if (-not $rep.ok) { $why += "report loi: $($rep.error)" }
                    if ($null -ne $s.maxTris -and [int]$rep.tris -gt [int]$s.maxTris) { $why += "$($rep.tris) tris > $($s.maxTris)" }
                    if ($null -ne $s.maxMaterials -and [int]$rep.materials -gt [int]$s.maxMaterials) { $why += "$($rep.materials) material > $($s.maxMaterials)" }
                    if ($s.pivotBottom -eq $true -and -not $rep.pivotBottom) { $why += 'pivot khong o day (min z != 0)' }
                    if ($s.maxSize -and $rep.bounds) {
                        for ($k = 0; $k -lt 3; $k++) { if ([double]$rep.bounds.size[$k] -gt [double]@($s.maxSize)[$k] + 1e-4) { $why += "bounds $($rep.bounds.size -join 'x') > $(@($s.maxSize) -join 'x')"; break } }
                    }
                    Write-VerifyFile $Root $s.out ($bline + "`n")
                    $sum = if ($rep.ok) { "$($rep.tris) tris, $($rep.materials) mat, size $($rep.bounds.size -join 'x')" } else { '' }
                    if ($why.Count) { $fails.Add("${label}: " + ($why -join '; ')); $lines.Add("${label}: FAIL " + ($why -join '; ')) }
                    else { $lines.Add("${label}: OK $sum") }
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

Export-ModuleMember -Function Connect-UnityMcp, Invoke-UnityTool, Invoke-VerifySteps, ConvertFrom-McpBody, Assert-UnityProject
