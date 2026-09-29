<#
.SYNOPSIS
  Kiem tra (va tu sua khi co the) capability cua agent host truoc khi dispatch task.

.DESCRIPTION
  Chay o root Unity project, Unity Editor dang mo va MCP for Unity da Start Server.
  Moi host (claude, codex) duoc kiem theo chuoi:
    INSTALLED -> AUTHENTICATED -> UNITY_MCP_CONFIGURED -> UNITY_MCP_SMOKE_PASS
  Reviewer read-only (claude, chi Read/Grep/Glob) duoc kiem la khong ghi duoc file.

  -Repair chi lam buoc deterministic:
    - cai Codex CLI qua npm neu thieu;
    - dang ky Unity MCP cho Claude Code bang chinh entry panel Unity da ghi vao ~/.codex/config.toml
      (hoac -UnityMcpUrl neu truyen vao).
  Dang nhap (OAuth trinh duyet) la buoc tay mot lan/may: script chi in huong dan.

  Output: bang tren console + .toolchain/capabilities.json (gitignored, UTF-8 khong BOM).

.EXAMPLE
  .\tools\doctor.ps1                 # chi kiem
  .\tools\doctor.ps1 -Repair         # kiem + tu sua phan tu dong duoc
  .\tools\doctor.ps1 -SkipSmoke      # khong goi model (khong ton token)
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
# Native output (git, claude, codex) la UTF-8; PS 5.1 mac dinh doc theo OEM code page.
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false } catch { }
$ProjectRoot = (Get-Location).Path
$Results = New-Object System.Collections.Generic.List[object]
$ManualSteps = New-Object System.Collections.Generic.List[string]
# Runner doc exe + ten Unity MCP server cua tung host tu day.
$HostInfo = [ordered]@{}

function Add-Check([string]$HostName, [string]$Level, [string]$Status, [string]$Detail) {
    $Results.Add([pscustomobject]@{ host = $HostName; level = $Level; status = $Status; detail = $Detail })
}

function Invoke-Native([string]$Exe, [string[]]$Arguments) {
    # Gom stdout+stderr thanh text; khong de stderr cua native thanh ErrorRecord do tren PS 5.1.
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

# Doc [mcp_servers.<name>] co ten chua "unity" trong config.toml cua Codex (panel MCP for Unity ghi vao day).
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
    Add-Check 'unity' 'MCP_ENTRY_KNOWN' 'PASS' "tu $(if ($UnityMcpUrl) { '-UnityMcpUrl' } else { $codexConfig }): $how"
} else {
    Add-Check 'unity' 'MCP_ENTRY_KNOWN' 'FAIL' "khong thay [mcp_servers.*unity*] trong $codexConfig"
    $ManualSteps.Add('Unity: panel MCP for Unity -> Client: Codex -> Configure (hoac truyen -UnityMcpUrl http://127.0.0.1:8080/mcp).')
}

if ($unityEntry -and $unityEntry.url) {
    if (Test-TcpPort $unityEntry.url) {
        Add-Check 'unity' 'MCP_SERVER_REACHABLE' 'PASS' $unityEntry.url
    } else {
        Add-Check 'unity' 'MCP_SERVER_REACHABLE' 'FAIL' "khong ket noi duoc $($unityEntry.url)"
        $ManualSteps.Add('Unity: mo Unity Editor, panel MCP for Unity -> Start Server.')
    }
}

$manifest = Join-Path $ProjectRoot 'Packages/manifest.json'
if (Test-Path $manifest) {
    $m = [regex]::Match((Get-Content $manifest -Raw), '"com\.coplaydev\.unity-mcp"\s*:\s*"([^"]+)"')
    if ($m.Success) {
        $ref = $m.Groups[1].Value
        if ($ref -match '#(main|master)$' -or ($ref -match '\.git' -and $ref -notmatch '#')) {
            Add-Check 'unity' 'MCP_PACKAGE_PINNED' 'WARN' "$ref troi theo branch; pin theo tag (vd #v10.0.0)"
        } else {
            Add-Check 'unity' 'MCP_PACKAGE_PINNED' 'PASS' $ref
        }
    }
}

# ---------------------------------------------------------------- Claude

function Test-ClaudeHost {
    $exe = Find-Exe 'claude'
    if (-not $exe) {
        Add-Check 'claude' 'INSTALLED' 'FAIL' 'khong co claude tren PATH'
        $ManualSteps.Add('Claude: cai Claude Code CLI (https://code.claude.com/docs) roi chay lai doctor.')
        return
    }
    Add-Check 'claude' 'INSTALLED' 'PASS' $exe
    $HostInfo['claude'] = [ordered]@{ exe = $exe; unityMcpServer = $null }

    # `claude auth status` khong ton token nhung co the van bao loggedIn khi OAuth het han va khong refresh duoc,
    # nen khi khong -SkipSmoke thi ping them mot lan -p.
    $st = Invoke-Native $exe @('auth', 'status', '--json')
    $loggedIn = $false
    try { $loggedIn = [bool](($st.Text | ConvertFrom-Json).loggedIn) } catch { $loggedIn = $false }
    $authFail = -not $loggedIn
    if (-not $authFail -and -not $SkipSmoke) {
        $r = Invoke-Native $exe @('-p', 'Reply with exactly: OK', '--output-format', 'json')
        $authFail = ($r.Text -match 'authenticate|OAuth|login' -and $r.Text -match '"is_error"\s*:\s*true')
    }
    if ($authFail) {
        Add-Check 'claude' 'AUTHENTICATED' 'FAIL' 'OAuth het han / chua login'
        $ManualSteps.Add('Claude: chay `claude auth login` (mo trinh duyet, mot lan/may).')
        return
    }
    Add-Check 'claude' 'AUTHENTICATED' 'PASS' ''

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
        Add-Check 'claude' 'UNITY_MCP_CONFIGURED' 'FAIL' 'claude mcp list khong co Unity'
        $ManualSteps.Add('Claude: chay lai voi -Repair, hoac panel MCP for Unity -> Client: Claude Code -> Configure.')
        return
    }
    $serverName = ($unityLine -split ':')[0].Trim()
    if ($unityLine -match 'Connected') {
        $HostInfo['claude'].unityMcpServer = $serverName
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

    # Reviewer read-only: chi Read/Grep/Glob thi khong duoc tao file.
    $probe = Join-Path ([System.IO.Path]::GetTempPath()) ("doctor-readonly-" + [guid]::NewGuid().ToString('N') + '.txt')
    $null = Invoke-Native $exe @('-p', "Create the file $probe with content x. If you cannot, reply CANNOT.", '--allowedTools', 'Read,Grep,Glob', '--output-format', 'json')
    if (Test-Path $probe) {
        Remove-Item $probe -Force
        Add-Check 'claude' 'REVIEWER_READONLY' 'FAIL' 'reviewer toolset van ghi duoc file'
    } else {
        Add-Check 'claude' 'REVIEWER_READONLY' 'PASS' 'Read,Grep,Glob khong ghi duoc file'
    }
}

# ---------------------------------------------------------------- Codex

function Find-CodexExe {
    $exe = Find-Exe 'codex'
    if ($exe) { return $exe }
    # Binary di kem extension VS Code khong nam tren PATH; chi dung de bao, runner van nen dung CLI cai qua npm.
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
            $ManualSteps.Add('Codex: can Node.js/npm de cai Codex CLI (npm install -g @openai/codex).')
        }
    }
    if (-not $exe) {
        Add-Check 'codex' 'INSTALLED' 'FAIL' 'khong co codex tren PATH'
        $ManualSteps.Add('Codex: chay doctor voi -Repair (npm install -g @openai/codex), mo terminal moi.')
        return
    }
    $onPath = [bool](Find-Exe 'codex')
    $HostInfo['codex'] = [ordered]@{ exe = $exe; unityMcpServer = $null }
    Add-Check 'codex' 'INSTALLED' ($(if ($onPath) { 'PASS' } else { 'WARN' })) ($(if ($onPath) { $exe } else { "chi co binary cua VS Code extension: $exe (khong tren PATH)" }))

    $st = Invoke-Native $exe @('login', 'status')
    if ($st.Code -eq 0 -and $st.Text -notmatch '(?i)not logged in') {
        Add-Check 'codex' 'AUTHENTICATED' 'PASS' (($st.Text -replace '\s+', ' ').Trim())
    } else {
        Add-Check 'codex' 'AUTHENTICATED' 'FAIL' (($st.Text -replace '\s+', ' ').Trim())
        $ManualSteps.Add('Codex: chay `codex login` (mot lan/may).')
        return
    }

    $list = Invoke-Native $exe @('mcp', 'list')
    if ($list.Text -match '(?i)unity') {
        if ($unityEntry) { $HostInfo['codex'].unityMcpServer = $unityEntry.name }
        Add-Check 'codex' 'UNITY_MCP_CONFIGURED' 'PASS' 'codex mcp list co Unity'
    } else {
        Add-Check 'codex' 'UNITY_MCP_CONFIGURED' 'FAIL' 'codex mcp list khong co Unity'
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
    hosts       = $HostInfo
    checks      = $Results
}
$outPath = Join-Path $ProjectRoot '.toolchain/capabilities.json'
Write-Utf8NoBom $outPath ($capabilities | ConvertTo-Json -Depth 5)
Write-Host "capabilities -> $outPath"

if ($ManualSteps.Count -gt 0) {
    Write-Host ''
    Write-Host 'Buoc lam tay con lai:' -ForegroundColor Yellow
    $ManualSteps | Select-Object -Unique | ForEach-Object { Write-Host "  - $_" }
}

$failed = @($Results | Where-Object { $_.status -eq 'FAIL' }).Count
exit ([int]($failed -gt 0))
