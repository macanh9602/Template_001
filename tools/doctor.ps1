<#
.SYNOPSIS
  Kiểm tra (và tự sửa khi có thể) capability của agent host trước khi dispatch task.

.DESCRIPTION
  Chạy ở root Unity project, Unity Editor đang mở và MCP for Unity đã Start Server.
  Mỗi host (claude, codex) được kiểm theo chuỗi:
    INSTALLED -> AUTHENTICATED -> UNITY_MCP_CONFIGURED -> UNITY_MCP_SMOKE_PASS
  Reviewer read-only (claude, chỉ Read/Grep/Glob) được kiểm là không ghi được file.

  -Repair chỉ làm bước deterministic:
    - cài Codex CLI qua npm nếu thiếu;
    - đăng ký Unity MCP cho Claude Code bằng chính entry panel Unity đã ghi vào ~/.codex/config.toml
      (hoặc -UnityMcpUrl nếu truyền vào).
  Đăng nhập (OAuth trình duyệt) là bước tay một lần/máy: script chỉ in hướng dẫn.

  Output: bảng trên console + .toolchain/capabilities.json (gitignored, UTF-8 không BOM).

.EXAMPLE
  .\tools\doctor.ps1                 # chỉ kiểm
  .\tools\doctor.ps1 -Repair         # kiểm + tự sửa phần tự động được
  .\tools\doctor.ps1 -SkipSmoke      # không gọi model (không tốn token)
#>
[CmdletBinding()]
param(
    [switch]$Repair,
    [switch]$SkipSmoke,
    [string[]]$Hosts = @('claude', 'codex'),
    [string]$UnityMcpName = 'UnityMCP',
    [string]$UnityMcpUrl = ''
)

$ErrorActionPreference = 'Continue'
$ProjectRoot = (Get-Location).Path
$Results = New-Object System.Collections.Generic.List[object]
$ManualSteps = New-Object System.Collections.Generic.List[string]

function Add-Check([string]$HostName, [string]$Level, [string]$Status, [string]$Detail) {
    $Results.Add([pscustomobject]@{ host = $HostName; level = $Level; status = $Status; detail = $Detail })
}

function Invoke-Native([string]$Exe, [string[]]$Arguments) {
    # Gom stdout+stderr thành text; không để stderr của native thành ErrorRecord đỏ trên PS 5.1.
    $out = & $Exe @Arguments 2>&1 | ForEach-Object { "$_" } | Out-String
    return [pscustomobject]@{ Code = $LASTEXITCODE; Text = $out }
}

function Find-Exe([string]$Name) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cmd) { return $cmd.Source }
    return $null
}

function Write-Utf8NoBom([string]$Path, [string]$Text) {
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding $false))
}

# ---------------------------------------------------------------- Unity MCP discovery

function Get-CodexConfigPath {
    $codexHome = $env:CODEX_HOME
    if (-not $codexHome) { $codexHome = Join-Path $HOME '.codex' }
    return (Join-Path $codexHome 'config.toml')
}

# Đọc [mcp_servers.<name>] có tên chứa "unity" trong config.toml của Codex (panel MCP for Unity ghi vào đây).
function Get-UnityEntryFromCodexConfig([string]$Path) {
    if (-not (Test-Path $Path)) { return $null }
    $lines = Get-Content -Path $Path -Encoding UTF8
    $entry = $null
    $inArgs = $false
    $argsText = ''
    foreach ($line in $lines) {
        $trim = $line.Trim()
        if ($trim -match '^\[mcp_servers\.("?)([^\]"]+)\1\]$') {
            if ($entry) { break }
            if ($Matches[2] -match 'unity') {
                $entry = [ordered]@{ name = $Matches[2]; url = $null; command = $null; args = @() }
            }
            continue
        }
        if (-not $entry) { continue }
        if ($trim -match '^\[') { break }
        if ($inArgs) {
            $argsText += ' ' + $trim
            if ($trim -match '\]') { $inArgs = $false }
            continue
        }
        if ($trim -match '^url\s*=\s*"([^"]+)"') { $entry.url = $Matches[1]; continue }
        if ($trim -match '^command\s*=\s*"([^"]+)"') { $entry.command = $Matches[1] -replace '\\\\', '\'; continue }
        if ($trim -match '^args\s*=\s*(.*)$') {
            $argsText = $Matches[1]
            if ($argsText -notmatch '\]') { $inArgs = $true }
        }
    }
    if ($entry -and $argsText) {
        $entry.args = @([regex]::Matches($argsText, '"((?:[^"\\]|\\.)*)"') | ForEach-Object { $_.Groups[1].Value -replace '\\\\', '\' })
    }
    return $entry
}

function Test-TcpPort([string]$Url) {
    try {
        $uri = [Uri]$Url
        $client = New-Object System.Net.Sockets.TcpClient
        $task = $client.ConnectAsync($uri.Host, $uri.Port)
        $ok = $task.Wait(1500) -and $client.Connected
        $client.Close()
        return $ok
    } catch { return $false }
}

$SmokePrompt = 'Use the Unity MCP tools to read the current Unity Editor console. Reply with exactly one line and nothing else: errors=N warnings=N'
$SmokePattern = 'errors=\d+\s+warnings=\d+'

# ---------------------------------------------------------------- Unity side

$codexConfig = Get-CodexConfigPath
$unityEntry = Get-UnityEntryFromCodexConfig $codexConfig
if ($UnityMcpUrl) { $unityEntry = [ordered]@{ name = $UnityMcpName; url = $UnityMcpUrl; command = $null; args = @() } }

if ($unityEntry) {
    $how = if ($unityEntry.url) { "url $($unityEntry.url)" } else { "stdio $($unityEntry.command) $($unityEntry.args -join ' ')" }
    Add-Check 'unity' 'MCP_ENTRY_KNOWN' 'PASS' "từ $(if ($UnityMcpUrl) { '-UnityMcpUrl' } else { $codexConfig }): $how"
} else {
    Add-Check 'unity' 'MCP_ENTRY_KNOWN' 'FAIL' "không thấy [mcp_servers.*unity*] trong $codexConfig"
    $ManualSteps.Add('Unity: panel MCP for Unity -> Client: Codex -> Configure (hoặc truyền -UnityMcpUrl http://127.0.0.1:8080/mcp).')
}

if ($unityEntry -and $unityEntry.url) {
    if (Test-TcpPort $unityEntry.url) {
        Add-Check 'unity' 'MCP_SERVER_REACHABLE' 'PASS' $unityEntry.url
    } else {
        Add-Check 'unity' 'MCP_SERVER_REACHABLE' 'FAIL' "không kết nối được $($unityEntry.url)"
        $ManualSteps.Add('Unity: mở Unity Editor, panel MCP for Unity -> Start Server.')
    }
}

$manifest = Join-Path $ProjectRoot 'Packages/manifest.json'
if (Test-Path $manifest) {
    $m = [regex]::Match((Get-Content $manifest -Raw), '"com\.coplaydev\.unity-mcp"\s*:\s*"([^"]+)"')
    if ($m.Success) {
        $ref = $m.Groups[1].Value
        if ($ref -match '#(main|master)$' -or ($ref -match '\.git' -and $ref -notmatch '#')) {
            Add-Check 'unity' 'MCP_PACKAGE_PINNED' 'WARN' "$ref trôi theo branch; pin theo tag (vd #v10.0.0)"
        } else {
            Add-Check 'unity' 'MCP_PACKAGE_PINNED' 'PASS' $ref
        }
    }
}

# ---------------------------------------------------------------- Claude

function Test-ClaudeHost {
    $exe = Find-Exe 'claude'
    if (-not $exe) {
        Add-Check 'claude' 'INSTALLED' 'FAIL' 'không có claude trên PATH'
        $ManualSteps.Add('Claude: cài Claude Code CLI (https://code.claude.com/docs) rồi chạy lại doctor.')
        return
    }
    Add-Check 'claude' 'INSTALLED' 'PASS' $exe

    if (-not $SkipSmoke) {
        $r = Invoke-Native $exe @('-p', 'Reply with exactly: OK', '--output-format', 'json')
        if ($r.Text -match 'authenticate|OAuth|login' -and $r.Text -match '"is_error"\s*:\s*true') {
            Add-Check 'claude' 'AUTHENTICATED' 'FAIL' 'OAuth hết hạn / chưa login'
            $ManualSteps.Add('Claude: chạy `claude`, gõ /login, xong /exit (một lần/máy).')
            return
        }
        Add-Check 'claude' 'AUTHENTICATED' 'PASS' ''
    }

    $list = Invoke-Native $exe @('mcp', 'list')
    $unityLine = ($list.Text -split "`r?`n") | Where-Object { $_ -match '(?i)unity' } | Select-Object -First 1
    if (-not $unityLine -and $Repair -and $unityEntry) {
        if ($unityEntry.url) {
            $add = Invoke-Native $exe @('mcp', 'add', '--transport', 'http', '--scope', 'user', $UnityMcpName, $unityEntry.url)
        } else {
            $add = Invoke-Native $exe (@('mcp', 'add', '--scope', 'user', $UnityMcpName, '--', $unityEntry.command) + $unityEntry.args)
        }
        Write-Host "[repair] claude mcp add $UnityMcpName -> exit $($add.Code)"
        $list = Invoke-Native $exe @('mcp', 'list')
        $unityLine = ($list.Text -split "`r?`n") | Where-Object { $_ -match '(?i)unity' } | Select-Object -First 1
    }
    if (-not $unityLine) {
        Add-Check 'claude' 'UNITY_MCP_CONFIGURED' 'FAIL' 'claude mcp list không có Unity'
        $ManualSteps.Add('Claude: chạy lại với -Repair, hoặc panel MCP for Unity -> Client: Claude Code -> Configure.')
        return
    }
    $serverName = ($unityLine -split ':')[0].Trim()
    if ($unityLine -match 'Connected') {
        Add-Check 'claude' 'UNITY_MCP_CONFIGURED' 'PASS' $unityLine.Trim()
    } else {
        Add-Check 'claude' 'UNITY_MCP_CONFIGURED' 'FAIL' $unityLine.Trim()
        return
    }

    if ($SkipSmoke) { return }

    $smoke = Invoke-Native $exe @('-p', $SmokePrompt, '--allowedTools', "mcp__$serverName", '--output-format', 'json')
    $answer = ''
    try { $answer = ($smoke.Text | ConvertFrom-Json).result } catch { $answer = $smoke.Text }
    if ($answer -match $SmokePattern) {
        Add-Check 'claude' 'UNITY_MCP_SMOKE_PASS' 'PASS' $Matches[0]
    } else {
        Add-Check 'claude' 'UNITY_MCP_SMOKE_PASS' 'FAIL' (($answer -replace '\s+', ' ').Trim() | ForEach-Object { $_.Substring(0, [Math]::Min(200, $_.Length)) })
    }

    # Reviewer read-only: chỉ Read/Grep/Glob thì không được tạo file.
    $probe = Join-Path ([System.IO.Path]::GetTempPath()) ("doctor-readonly-" + [guid]::NewGuid().ToString('N') + '.txt')
    $null = Invoke-Native $exe @('-p', "Create the file $probe with content x. If you cannot, reply CANNOT.", '--allowedTools', 'Read,Grep,Glob', '--output-format', 'json')
    if (Test-Path $probe) {
        Remove-Item $probe -Force
        Add-Check 'claude' 'REVIEWER_READONLY' 'FAIL' 'reviewer toolset vẫn ghi được file'
    } else {
        Add-Check 'claude' 'REVIEWER_READONLY' 'PASS' 'Read,Grep,Glob không ghi được file'
    }
}

# ---------------------------------------------------------------- Codex

function Find-CodexExe {
    $exe = Find-Exe 'codex'
    if ($exe) { return $exe }
    # Binary đi kèm extension VS Code không nằm trên PATH; chỉ dùng để báo, runner vẫn nên dùng CLI cài qua npm.
    $bundled = Get-ChildItem -Path (Join-Path $HOME '.vscode\extensions') -Filter 'codex.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($bundled) { return $bundled.FullName }
    return $null
}

function Test-CodexHost {
    $exe = Find-CodexExe
    if (-not $exe -and $Repair) {
        $npm = Find-Exe 'npm'
        if ($npm) {
            $r = Invoke-Native $npm @('install', '-g', '@openai/codex')
            Write-Host "[repair] npm install -g @openai/codex -> exit $($r.Code)"
            $exe = Find-Exe 'codex'
        } else {
            $ManualSteps.Add('Codex: cần Node.js/npm để cài Codex CLI (npm install -g @openai/codex).')
        }
    }
    if (-not $exe) {
        Add-Check 'codex' 'INSTALLED' 'FAIL' 'không có codex trên PATH'
        $ManualSteps.Add('Codex: chạy doctor với -Repair (npm install -g @openai/codex), mở terminal mới.')
        return
    }
    $onPath = [bool](Find-Exe 'codex')
    Add-Check 'codex' 'INSTALLED' ($(if ($onPath) { 'PASS' } else { 'WARN' })) ($(if ($onPath) { $exe } else { "chỉ có binary của VS Code extension: $exe (không trên PATH)" }))

    $st = Invoke-Native $exe @('login', 'status')
    if ($st.Code -eq 0 -and $st.Text -notmatch '(?i)not logged in') {
        Add-Check 'codex' 'AUTHENTICATED' 'PASS' (($st.Text -replace '\s+', ' ').Trim())
    } else {
        Add-Check 'codex' 'AUTHENTICATED' 'FAIL' (($st.Text -replace '\s+', ' ').Trim())
        $ManualSteps.Add('Codex: chạy `codex login` (một lần/máy).')
        return
    }

    $list = Invoke-Native $exe @('mcp', 'list')
    if ($list.Text -match '(?i)unity') {
        Add-Check 'codex' 'UNITY_MCP_CONFIGURED' 'PASS' 'codex mcp list có Unity'
    } else {
        Add-Check 'codex' 'UNITY_MCP_CONFIGURED' 'FAIL' 'codex mcp list không có Unity'
        $ManualSteps.Add('Codex: panel MCP for Unity -> Client: Codex -> Configure.')
        return
    }

    if ($SkipSmoke) { return }
    $smoke = Invoke-Native $exe @('exec', $SmokePrompt)
    if ($smoke.Text -match $SmokePattern) {
        Add-Check 'codex' 'UNITY_MCP_SMOKE_PASS' 'PASS' $Matches[0]
    } else {
        $tail = (($smoke.Text -replace '\s+', ' ').Trim())
        Add-Check 'codex' 'UNITY_MCP_SMOKE_PASS' 'FAIL' $tail.Substring([Math]::Max(0, $tail.Length - 200))
    }
}

if ($Hosts -contains 'claude') { Test-ClaudeHost }
if ($Hosts -contains 'codex') { Test-CodexHost }

# ---------------------------------------------------------------- Report

$Results | Format-Table host, level, status, detail -AutoSize -Wrap | Out-String -Width 200 | Write-Host

$capabilities = [ordered]@{
    schema      = 'capabilities/v1'
    generatedAt = (Get-Date).ToString('o')
    machine     = $env:COMPUTERNAME
    projectRoot = $ProjectRoot
    checks      = $Results
}
$outPath = Join-Path $ProjectRoot '.toolchain/capabilities.json'
Write-Utf8NoBom $outPath ($capabilities | ConvertTo-Json -Depth 5)
Write-Host "capabilities -> $outPath"

if ($ManualSteps.Count -gt 0) {
    Write-Host ''
    Write-Host 'Bước làm tay còn lại:' -ForegroundColor Yellow
    $ManualSteps | Select-Object -Unique | ForEach-Object { Write-Host "  - $_" }
}

$failed = @($Results | Where-Object { $_.status -eq 'FAIL' }).Count
exit ([int]($failed -gt 0))
