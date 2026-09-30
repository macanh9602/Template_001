<#
.SYNOPSIS
  Chay mot task end-to-end: implementer headless -> git diff -> reviewer read-only -> PATCH retry.

.DESCRIPTION
  V1 runner. Khong co DAG, khong chay song song. Mot task, mot Unity lane.

    task.json (handoff/<wp>/tasks/<id>.json)
      -> capabilities (.toolchain/capabilities.json tu tools/doctor.ps1)
      -> chon implementer host thoa `requires`
      -> implementer (claude -p | codex exec) doc prompt file, lam viec, in RESULT:
      -> runner tinh changedFiles tu git (khong tin worker tu khai), kiem writeSet
      -> result.json
      -> reviewer: claude -p moi, chi Read/Grep/Glob, tra JSON review/v1
      -> PASS            => DONE_PENDING_FEEL
         PATCH           => lap lai implementer voi findings (toi da -MaxPatchRounds)
         PATCH vuot cap  => ESCALATE:LOOP_CAP
         TARGET_RECONSIDER (co implHypothesesRuledOut) => BLOCKED_ON_TARGET
         BLOCKED / loi   => BLOCKED
      Moi lan can nguoi: them 1 dong vao handoff/<wp>/interventions.jsonl.

  Output: handoff/<wp>/runs/<id>/<timestamp>/  (prompt, *.out.txt, diff, rN.result.json, rN.review.json,
          status.json, unity-editor-log-tail.txt). Khong dung duoi .log: .gitignore cua Unity bo qua *.log.
  Runner khong push. -Commit: task PASS -> commit code + evidence; chua PASS -> chi commit evidence (handoff/).

.EXAMPLE
  .\tools\run-task.ps1 -Task handoff\wp-004\tasks\WP004-B-VERIFY.json -Plan
      Chi xem ke hoach (agent, model, effort, uoc tinh usage) tren trang HTML; khong chay gi.
  .\tools\run-task.ps1 -Task handoff\wp-004\tasks\WP004-B-VERIFY.json -Profile economy -Confirm -Commit
      Hien ke hoach, hoi y/N truoc khi giao; profile lay tu config/run-profiles.json.

  Tien do: handoff/<wp>/runs/dashboard.html (tu lam moi moi 5 giay khi dang chay).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Task,
    # -1 = lay tu profile (config/run-profiles.json).
    [int]$MaxPatchRounds = -1,
    # Profile trong config/run-profiles.json (balanced / economy / fast / quality ...).
    [Alias('Profile')][string]$RunProfile = '',
    [string]$ImplementerModel = '',
    [string]$ImplementerEffort = '',
    [string]$ReviewerModel = '',
    [string]$ReviewerEffort = '',
    # Codex service tier: priority = nhanh hon, ton hon. 'default' = bo ghi de.
    [string]$CodexServiceTier = '',
    # Chi ghi ke hoach ra plan.html roi thoat; khong goi agent nao.
    [switch]$Plan,
    # Hien ke hoach va hoi y/N truoc khi giao (mac dinh theo confirmBeforeDispatch trong config).
    [switch]$Confirm,
    # Bo qua buoc hoi du config bat confirmBeforeDispatch.
    [switch]$Yes,
    # Khong tu mo trang HTML.
    [switch]$NoOpen,
    # Vong sua (PATCH) mo phien implementer moi thay vi tiep tuc phien vong truoc.
    [switch]$NoResume,
    # Chi chay buoc 'verify' bang script; fail thi dung (BLOCKED), khong bao gio goi AI.
    [switch]$VerifyOnly,
    [string]$Implementer = '',
    [int]$CapabilityMaxAgeMinutes = 480,
    [ValidateSet('read-only', 'workspace-write', 'danger-full-access')][string]$CodexSandbox = 'workspace-write',
    # Commit changedFiles + run dir khi task PASS (chua PASS: chi evidence). Mac dinh tat: runner chi in lenh git.
    [switch]$Commit,
    # Khong tu chay doctor khi capabilities thieu/qua han.
    [switch]$NoAutoDoctor
)

$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false } catch { }
$Utf8NoBom = New-Object System.Text.UTF8Encoding $false
$ProjectRoot = (Get-Location).Path

# ---------------------------------------------------------------- helpers

function Write-Text([string]$Path, [string]$Text) {
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Text, $Utf8NoBom)
}

function Write-Json([string]$Path, $Object) { Write-Text $Path ($Object | ConvertTo-Json -Depth 10) }

function Read-Json([string]$Path) {
    # Chap nhan ca file co BOM (bai hoc A-02.review.json).
    $text = [System.IO.File]::ReadAllText($Path)
    return ($text.TrimStart([char]0xFEFF) | ConvertFrom-Json)
}

function Get-RelPath([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if ($full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) { $full = $full.Substring($root.Length) }
    return ($full -replace '\\', '/')
}

function Invoke-Native([string]$Exe, [string[]]$Arguments, [string]$LogPath) {
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $out = & $Exe @Arguments 2>&1 | ForEach-Object { "$_" } | Out-String
        $code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $prev }
    if ($LogPath) { Write-Text $LogPath $out }
    return [pscustomobject]@{ Code = $code; Text = $out }
}

function Invoke-Git([string[]]$Arguments) {
    $r = Invoke-Native 'git' (@('-c', 'core.quotePath=false') + $Arguments) $null
    if ($r.Code -ne 0) { throw "git $($Arguments -join ' ') failed: $($r.Text)" }
    return $r.Text
}

function Convert-GlobToRegex([string]$Glob) {
    $g = $Glob -replace '\\', '/'
    $sb = New-Object System.Text.StringBuilder
    $i = 0
    while ($i -lt $g.Length) {
        $c = $g[$i]
        if ($c -eq '*') {
            if ($i + 1 -lt $g.Length -and $g[$i + 1] -eq '*') {
                [void]$sb.Append('.*'); $i += 2
                if ($i -lt $g.Length -and $g[$i] -eq '/') { $i++ }
                continue
            }
            [void]$sb.Append('[^/]*')
        } elseif ($c -eq '?') { [void]$sb.Append('[^/]') }
        else { [void]$sb.Append([regex]::Escape([string]$c)) }
        $i++
    }
    return '^' + $sb.ToString() + '$'
}

function Test-InWriteSet([string]$RelPath, [string[]]$Globs) {
    foreach ($g in $Globs) { if ($RelPath -match (Convert-GlobToRegex $g)) { return $true } }
    return $false
}

# Snapshot trang thai working tree: path -> hash (hoac '<deleted>'). Dung de tach thay doi cua task
# khoi file da dirty tu truoc (Unity project hiem khi sach).
function Get-TreeSnapshot {
    $map = @{}
    $text = Invoke-Git @('status', '--porcelain=v1', '--untracked-files=all')
    foreach ($line in ($text -split "`r?`n")) {
        if ($line.Length -lt 4) { continue }
        $path = $line.Substring(3)
        if ($path -match ' -> ') { $path = ($path -split ' -> ')[-1] }
        $path = $path.Trim('"')
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            $map[$path] = (Invoke-Git @('hash-object', '--', $path)).Trim()
        } elseif (Test-Path -LiteralPath $path -PathType Container) {
            continue
        } else {
            $map[$path] = '<deleted>'
        }
    }
    return $map
}

function Get-ChangedSince($Before, [string[]]$IgnorePrefixes) {
    $after = Get-TreeSnapshot
    $changed = New-Object System.Collections.Generic.List[string]
    foreach ($k in $after.Keys) {
        if (-not $Before.ContainsKey($k) -or $Before[$k] -ne $after[$k]) { $changed.Add($k) }
    }
    foreach ($k in $Before.Keys) {
        # File dirty truoc task nhung gio da ve dung HEAD (task revert) cung la thay doi.
        if (-not $after.ContainsKey($k)) { $changed.Add($k) }
    }
    $filtered = $changed | Where-Object {
        $p = $_; -not ($IgnorePrefixes | Where-Object { $p.StartsWith($_) })
    } | Sort-Object -Unique
    return @($filtered)
}

function Add-Intervention([string]$State, [string]$Reason) {
    $line = [ordered]@{ at = (Get-Date).ToString('o'); taskId = $TaskObj.id; state = $State; reason = $Reason; run = (Get-RelPath $RunDir) } | ConvertTo-Json -Compress
    [System.IO.File]::AppendAllText($InterventionsPath, $line + "`n", $Utf8NoBom)
}

# Unity khong ghi timestamp vao Console qua MCP; Editor.log la evidence duy nhat cho su co Editor/MCP.
# Luu duoi .txt vi .gitignore cua Unity bo qua *.log.
function Save-EditorLogTail {
    $candidates = @()
    if ($env:LOCALAPPDATA) { $candidates += (Join-Path $env:LOCALAPPDATA 'Unity/Editor/Editor.log') }
    if ($HOME) { $candidates += (Join-Path $HOME 'Library/Logs/Unity/Editor.log'); $candidates += (Join-Path $HOME '.config/unity3d/Editor.log') }
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) {
            try {
                $tail = Get-Content -LiteralPath $c -Tail 800 -ErrorAction Stop | Out-String
                Write-Text (Join-Path $RunDir 'unity-editor-log-tail.txt') $tail
            } catch { }
            return
        }
    }
}

function Complete-Run([string]$Status, [string]$Reason, [int]$Rounds) {
    Save-EditorLogTail
    Write-Json (Join-Path $RunDir 'status.json') ([ordered]@{
            schema = 'run-status/v1'; taskId = $TaskObj.id; status = $Status; reason = $Reason
            rounds = $Rounds; host = $ImplHost; profile = $(if ($Exec) { $Exec.profile } else { $null })
            exec = $Exec; usage = $(if ($Progress) { $Progress.usage } else { $null })
            finishedAt = (Get-Date).ToString('o')
        })
    if ($Progress) {
        $Progress.status = $Status; $Progress.reason = $Reason; $Progress.rounds = $Rounds
        Save-Progress $false
    } else { Update-Dashboard $false }
    Write-Host ''
    Write-Host "[$($TaskObj.id)] $Status $(if ($Reason) { "- $Reason" })"
    Write-Host "run dir: $(Get-RelPath $RunDir)"
    # Bai hoc pilot run 3: code task sua ma chi add evidence thi doi may la mat.
    $paths = @(@($LastChanged) + @((Get-RelPath $RunDir), (Get-RelPath $InterventionsPath)) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Sort-Object -Unique)
    if ($Commit -and $Status -ne 'DONE_PENDING_FEEL') {
        # Chua PASS: chi commit evidence (handoff/), khong commit code chua review.
        $paths = @($paths | Where-Object { $_.StartsWith('handoff/') })
    }
    if ($Commit -and $paths.Count) {
        $null = Invoke-Git (@('add', '--') + $paths)
        $null = Invoke-Git @('commit', '-m', "$($TaskObj.id): $Status ($(Get-RelPath $RunDir))")
        Write-Host "committed: $((Invoke-Git @('rev-parse', '--short', 'HEAD')).Trim()) ($($paths.Count) path)"
    } else {
        if ($Status -ne 'DONE_PENDING_FEEL' -and @($LastChanged | Where-Object { -not $_.StartsWith('handoff/') }).Count) {
            Write-Host 'Chu y: task chua PASS; code da doi van nam tren working tree (chua review xong):'
            @($LastChanged | Where-Object { -not $_.StartsWith('handoff/') }) | ForEach-Object { Write-Host "  $_" }
        }
        if ($Status -ne 'DONE_PENDING_FEEL') {
            # Chua PASS: chi commit evidence; code chua review khong duoc de xuat commit.
            $paths = @($paths | Where-Object { $_.StartsWith('handoff/') })
            Write-Host 'Commit (chi evidence):'
        } else {
            Write-Host 'Commit (code + evidence):'
        }
        Write-Host ("  git add -- " + (($paths | ForEach-Object { '"' + $_ + '"' }) -join ' '))
        Write-Host ("  git commit -m `"$($TaskObj.id): $Status`"")
    }
    $code = 1
    if ($Status -eq 'DONE_PENDING_FEEL') { $code = 0 }
    exit $code
}

function Get-JsonObjectFromText([string]$Text) {
    $start = $Text.IndexOf('{')
    $end = $Text.LastIndexOf('}')
    if ($start -lt 0 -or $end -le $start) { return $null }
    try { return ($Text.Substring($start, $end - $start + 1) | ConvertFrom-Json) } catch { return $null }
}

# ---------------------------------------------------------------- usage + progress + dashboard

# Claude -p --output-format json: token (khong tinh cache read, re), cost, thoi gian, model.
function Get-ClaudeUsage([string]$Text) {
    $o = Get-JsonObjectFromText $Text
    if (-not $o) { return $null }
    $u = $o.usage
    $in = [long]0
    foreach ($k in @('input_tokens', 'cache_creation_input_tokens')) { if ($u -and $u.$k) { $in += [long]$u.$k } }
    $out = if ($u -and $u.output_tokens) { [long]$u.output_tokens } else { [long]0 }
    $cacheRead = if ($u -and $u.cache_read_input_tokens) { [long]$u.cache_read_input_tokens } else { [long]0 }
    $models = $null
    if ($o.modelUsage) { $models = (@($o.modelUsage.PSObject.Properties | ForEach-Object { $_.Name }) -join ',') }
    $cost = $null
    if ($null -ne $o.total_cost_usd) { $cost = [double]$o.total_cost_usd }
    $ms = $null
    if ($null -ne $o.duration_ms) { $ms = [long]$o.duration_ms }
    return [ordered]@{ model = $models; tokens = $in + $out; cacheReadTokens = $cacheRead; costUsd = $cost; durationMs = $ms }
}

# codex exec in header 'model: ...', 'reasoning effort: ...' va cuoi 'tokens used <n>'.
function Get-CodexUsage([string]$Text, [long]$Ms) {
    $model = $null; $effort = $null; $tier = $null; $tokens = $null
    if ($Text -match '(?m)^model:\s*(\S+)') { $model = $Matches[1] }
    if ($Text -match '(?m)^reasoning effort:\s*(\S+)') { $effort = $Matches[1] }
    if ($Text -match '(?m)^service tier:\s*(\S+)') { $tier = $Matches[1] }
    $m = [regex]::Matches($Text, '(?m)^tokens used\s*\r?\n?\s*([\d][\d,\.]*)')
    if ($m.Count) { $tokens = [long](($m[$m.Count - 1].Groups[1].Value) -replace '[,\.]', '') }
    return [ordered]@{ model = $model; effort = $effort; serviceTier = $tier; tokens = $tokens; costUsd = $null; durationMs = $Ms }
}

function Format-RunStamp([string]$Name) {
    if ($Name -match '^(\d{4})(\d{2})(\d{2})-(\d{2})(\d{2})(\d{2})$') { return "$($Matches[1])-$($Matches[2])-$($Matches[3]) $($Matches[4]):$($Matches[5]):$($Matches[6])" }
    return $Name
}

function Get-Interventions {
    if (-not (Test-Path $InterventionsPath)) { return @() }
    $list = @()
    foreach ($line in [System.IO.File]::ReadAllLines($InterventionsPath)) {
        if ($line.Trim()) { try { $list += ($line.TrimStart([char]0xFEFF) | ConvertFrom-Json) } catch { } }
    }
    return , $list
}

# Moi run: progress.json (runner moi) hoac status.json (run cu) -> mot dong cho dashboard.
function Get-RunsData {
    $list = @()
    $runsRoot = Join-Path $WpDir 'runs'
    if (-not (Test-Path $runsRoot)) { return , $list }
    foreach ($taskDir in Get-ChildItem -LiteralPath $runsRoot -Directory) {
        foreach ($r in Get-ChildItem -LiteralPath $taskDir.FullName -Directory) {
            if ($r.Name -eq '_plan') { continue }
            $prog = Join-Path $r.FullName 'progress.json'
            $st = Join-Path $r.FullName 'status.json'
            try {
                if (Test-Path $prog) { $list += (Read-Json $prog) }
                elseif (Test-Path $st) {
                    $x = Read-Json $st
                    $list += [pscustomobject][ordered]@{
                        taskId = $x.taskId; status = $x.status; reason = $x.reason; rounds = $x.rounds; host = $x.host
                        profile = $x.profile; exec = $x.exec; usage = $x.usage; observedModel = $null
                        startedAt = (Format-RunStamp $r.Name); steps = @()
                    }
                }
            } catch { }
        }
    }
    return , $list
}

function Write-DashboardHtml([string]$OutPath, $Data, [bool]$Live) {
    $tpl = Join-Path $PSScriptRoot 'run-dashboard.html'
    if (-not (Test-Path $tpl)) { return }
    $html = [System.IO.File]::ReadAllText($tpl)
    $json = ($Data | ConvertTo-Json -Depth 12 -Compress) -replace '</', '<\/'
    $html = $html.Replace('/*__DATA__*/null', $json)
    if ($Live) { $html = $html.Replace('<!--__REFRESH__-->', '<meta http-equiv="refresh" content="5">') }
    Write-Text $OutPath $html
}

function Update-Dashboard([bool]$Live) {
    try {
        $data = [ordered]@{
            mode = 'dashboard'; wp = (Split-Path -Leaf $WpDir); generatedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
            live = $Live; runs = (Get-RunsData); interventions = (Get-Interventions)
        }
        Write-DashboardHtml $DashboardPath $data $Live
    } catch { Write-Host "[runner] khong ghi duoc dashboard: $($_.Exception.Message)" }
}

function Open-Page([string]$Path) {
    if ($NoOpen) { return }
    if ($env:OS -eq 'Windows_NT') { try { Start-Process -FilePath $Path } catch { } }
}

$Progress = $null
function Save-Progress([bool]$Live) {
    if (-not $Progress) { return }
    Write-Json (Join-Path $RunDir 'progress.json') $Progress
    Update-Dashboard $Live
}

function Add-Usage($Bucket, $U) {
    if (-not $U) { return }
    foreach ($k in @('tokens', 'costUsd', 'durationMs')) {
        if ($null -ne $U.$k) {
            if ($null -eq $Bucket[$k]) { $Bucket[$k] = 0 }
            $Bucket[$k] += $U.$k
        }
    }
}

function Start-Step([string]$Role, [int]$Round, [string]$HostName) {
    $step = [ordered]@{ round = $Round; role = $Role; host = $HostName; state = 'running'; startedAt = (Get-Date).ToString('HH:mm:ss'); outcome = $null; verdict = $null; usage = $null; resumed = $false }
    $Progress.steps += , $step
    Save-Progress $true
    return $step
}

function Stop-Step($Step, [string]$Outcome, [string]$Verdict, $U) {
    $Step.state = 'done'; $Step.outcome = $Outcome; $Step.verdict = $Verdict; $Step.usage = $U
    Add-Usage $Progress.usage[$Step.role] $U
    Add-Usage $Progress.usage.total $U
    if ($Step.role -eq 'implementer' -and $U -and $U.model) { $Progress.observedModel = $U.model }
    Save-Progress $true
}

# ---------------------------------------------------------------- load task + capabilities

$TaskPath = (Resolve-Path $Task).Path
$TaskObj = Read-Json $TaskPath
foreach ($f in @('id', 'packet', 'implementers', 'requires', 'writeSet', 'acceptance')) {
    if ($null -eq $TaskObj.$f) { throw "task thieu field bat buoc '$f' ($TaskPath)" }
}
if (-not (Test-Path $TaskObj.packet)) { throw "packet khong ton tai: $($TaskObj.packet)" }

$WpDir = Split-Path -Parent (Split-Path -Parent $TaskPath)
$RunStamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$RunDir = Join-Path $WpDir ("runs/{0}/{1}" -f $TaskObj.id, $RunStamp)
if ($Plan) { $RunDir = Join-Path $WpDir ("runs/{0}/_plan" -f $TaskObj.id) }
$DashboardPath = Join-Path $WpDir 'runs/dashboard.html'
New-Item -ItemType Directory -Path $RunDir -Force | Out-Null
$InterventionsPath = Join-Path $WpDir 'interventions.jsonl'
$RunsRel = (Get-RelPath (Join-Path $WpDir 'runs')) + '/'
$ImplHost = $null
# File task da doi o round cuoi; Complete-Run dung de commit/in lenh commit.
$LastChanged = @()

$capPath = Join-Path $ProjectRoot '.toolchain/capabilities.json'
function Get-CapabilityAgeMinutes {
    if (-not (Test-Path $capPath)) { return [double]::MaxValue }
    try { return ((Get-Date) - [DateTime]::Parse((Read-Json $capPath).generatedAt)).TotalMinutes } catch { return [double]::MaxValue }
}
# Capabilities thieu/qua han: tu chay doctor (deterministic) thay vi bat nguoi chay. Pilot run 4 bi chan chi vi qua dem.
if ((Get-CapabilityAgeMinutes) -gt $CapabilityMaxAgeMinutes) {
    $doctor = Join-Path $PSScriptRoot 'doctor.ps1'
    if (-not $NoAutoDoctor -and (Test-Path $doctor)) {
        Write-Host '[runner] capabilities thieu/qua han -> chay tools/doctor.ps1 ...'
        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        # -SkipSmoke: khong mo phien Codex/Claude that (ton quota); doctor giu ket qua smoke PASS <= 7 ngay.
        try { & $doctor -SkipSmoke | Out-Host } finally { $ErrorActionPreference = $prevEap }
    }
}
$age = Get-CapabilityAgeMinutes
if ($age -gt $CapabilityMaxAgeMinutes) {
    $why = if ($age -eq [double]::MaxValue) { 'chua co .toolchain/capabilities.json' } else { "capabilities cu {0:N0} phut" -f $age }
    Add-Intervention 'BLOCKED_TOOLCHAIN' $why
    Complete-Run 'BLOCKED_TOOLCHAIN' "$why; chay tools/doctor.ps1" 0
}
$Cap = Read-Json $capPath

function Test-HostLevels([string]$HostName, [string[]]$Levels) {
    foreach ($lvl in $Levels) {
        $hit = @($Cap.checks | Where-Object { $_.host -eq $HostName -and $_.level -eq $lvl -and $_.status -eq 'PASS' })
        if ($hit.Count -eq 0) { return $false }
    }
    return [bool]($Cap.hosts -and $Cap.hosts.$HostName -and $Cap.hosts.$HostName.exe)
}

$candidates = @($TaskObj.implementers)
if ($Implementer) { $candidates = @($Implementer) }
foreach ($h in $candidates) {
    if (Test-HostLevels $h @($TaskObj.requires)) { $ImplHost = $h; break }
}
# -VerifyOnly khong goi AI: khong can host nao PASS smoke/reviewer (repo moi chua chay doctor day du van verify duoc).
if (-not $ImplHost -and ($Plan -or ($VerifyOnly -and $TaskObj.verify))) { $ImplHost = $candidates[0] }
if (-not $ImplHost) {
    $why = "khong host nao trong [$($candidates -join ', ')] PASS [$(@($TaskObj.requires) -join ', ')]"
    Add-Intervention 'BLOCKED_TOOLCHAIN' $why
    Complete-Run 'BLOCKED_TOOLCHAIN' $why 0
}
if (-not $Plan -and -not $VerifyOnly -and -not (Test-HostLevels 'claude' @('INSTALLED', 'REVIEWER_READONLY'))) {
    Add-Intervention 'BLOCKED_TOOLCHAIN' 'reviewer claude chua PASS REVIEWER_READONLY'
    Complete-Run 'BLOCKED_TOOLCHAIN' 'reviewer claude chua PASS REVIEWER_READONLY' 0
}
$ImplExe = $Cap.hosts.$ImplHost.exe
$ImplMcp = $Cap.hosts.$ImplHost.unityMcpServer
$ReviewerExe = $Cap.hosts.claude.exe

# ---------------------------------------------------------------- profile -> exec plan


# Ghi de theo may (gitignored): model kha dung khac nhau theo tai khoan (pilot: may cong ty co Luna, may nha chi co Sol).
function Merge-Json($Base, $Over) {
    foreach ($prop in $Over.PSObject.Properties) {
        $cur = $Base.PSObject.Properties[$prop.Name]
        if ($cur -and $cur.Value -is [pscustomobject] -and $prop.Value -is [pscustomobject]) { Merge-Json $cur.Value $prop.Value | Out-Null }
        else { $Base | Add-Member -NotePropertyName $prop.Name -NotePropertyValue $prop.Value -Force }
    }
    return $Base
}
function Get-RunProfiles {
    $pp = Join-Path $ProjectRoot 'config/run-profiles.json'
    $lp = Join-Path $ProjectRoot 'config/run-profiles.local.json'
    if (Test-Path $pp) {
        $base = Read-Json $pp
        if (Test-Path $lp) { $base = Merge-Json $base (Read-Json $lp) }
        return $base
    }
    return ('{"default":"balanced","confirmBeforeDispatch":false,"profiles":{"balanced":{"label":"balanced","maxPatchRounds":2}}}' | ConvertFrom-Json)
}
function Select-Value($Override, $FromProfile) { if ($Override) { return $Override } return $FromProfile }

$Profiles = Get-RunProfiles
$ProfileName = $RunProfile
if (-not $ProfileName -and $TaskObj.profile) { $ProfileName = [string]$TaskObj.profile }
if (-not $ProfileName) { $ProfileName = [string]$Profiles.default }
$Prof = $Profiles.profiles.$ProfileName
if (-not $Prof) { throw "profile '$ProfileName' khong co trong config/run-profiles.json" }
$ip = $null; $rp = $null
if ($Prof.implementer) { $ip = $Prof.implementer.$ImplHost }
if ($Prof.reviewer) { $rp = $Prof.reviewer.claude }
# serviceTier: 'default' = x1 (ep ro, khong phu thuoc config.toml), 'priority' = x1.5, null/'none' = de host tu chon.
$tier = Select-Value $CodexServiceTier $(if ($ip) { $ip.serviceTier } else { $null })
if ($tier -eq 'none') { $tier = $null }
$rounds = 2
if ($PSBoundParameters.ContainsKey('MaxPatchRounds') -and $MaxPatchRounds -ge 0) { $rounds = $MaxPatchRounds }
elseif ($null -ne $Prof.maxPatchRounds) { $rounds = [int]$Prof.maxPatchRounds }
$autoHost = $null
foreach ($h in @($TaskObj.implementers)) { if (Test-HostLevels $h @($TaskObj.requires)) { $autoHost = $h; break } }
# Model/effort/tier host se dung khi profile de null: doc config cua host truoc khi chay,
# de trang ke hoach va dashboard hien dung model (Sol/Luna, phien ban) ngay tu dau.
function Get-CodexConfigDefaults {
    $home2 = $env:CODEX_HOME
    if (-not $home2) { $home2 = Join-Path $HOME '.codex' }
    $cfgPath = Join-Path $home2 'config.toml'
    $r = [ordered]@{ model = $null; effort = $null; serviceTier = $null }
    if (-not (Test-Path $cfgPath)) { return $r }
    foreach ($line in [System.IO.File]::ReadAllLines($cfgPath)) {
        $t = $line.Trim()
        if ($t -match '^\[') { break }  # chi doc phan top-level, truoc [section] dau tien
        if ($t -match '^model\s*=\s*"([^"]+)"') { $r.model = $Matches[1] }
        elseif ($t -match '^model_reasoning_effort\s*=\s*"([^"]+)"') { $r.effort = $Matches[1] }
        elseif ($t -match '^service_tier\s*=\s*"([^"]+)"') { $r.serviceTier = $Matches[1] }
    }
    return $r
}
function Get-ClaudeSettingsDefaults {
    $r = [ordered]@{ model = $null; effort = $null; serviceTier = $null }
    $sp = Join-Path $HOME '.claude/settings.json'
    if (Test-Path $sp) { try { $j = Read-Json $sp; if ($j.model) { $r.model = [string]$j.model } } catch { } }
    return $r
}
function Resolve-Effective($Cfg, [string]$HostName) {
    $d = if ($HostName -eq 'codex') { Get-CodexConfigDefaults } else { Get-ClaudeSettingsDefaults }
    $src = if ($HostName -eq 'codex') { 'config.toml' } else { 'settings.json' }
    $out = [ordered]@{}
    foreach ($k in @('model', 'effort', 'serviceTier')) {
        if ($Cfg.$k) { $out[$k] = [string]$Cfg.$k; $out[$k + 'Source'] = 'profile' }
        elseif ($d[$k]) { $out[$k] = [string]$d[$k]; $out[$k + 'Source'] = $src }
        else { $out[$k] = $null; $out[$k + 'Source'] = 'account' }
    }
    return $out
}

$Exec = [ordered]@{
    profile = $ProfileName; maxPatchRounds = $rounds; implementerDefault = $autoHost
    implementer = [ordered]@{
        host = $ImplHost
        model = (Select-Value $ImplementerModel $(if ($ip) { $ip.model } else { $null }))
        effort = (Select-Value $ImplementerEffort $(if ($ip) { $ip.effort } else { $null }))
        serviceTier = $(if ($ImplHost -eq 'codex') { $tier } else { $null })
    }
    reviewer = [ordered]@{
        host = 'claude'
        model = (Select-Value $ReviewerModel $(if ($rp) { $rp.model } else { $null }))
        effort = (Select-Value $ReviewerEffort $(if ($rp) { $rp.effort } else { $null }))
    }
}

$Exec.implementer.effective = (Resolve-Effective $Exec.implementer $ImplHost)
$Exec.reviewer.effective = (Resolve-Effective $Exec.reviewer 'claude')
# Vong sua tiep tuc phien implementer cua vong truoc (codex exec resume / claude --resume): khong doc lai
# packet, code, AGENTS tu dau. Pilot: vong 2 phien moi ton 132k token chi de sua 1 dong. Reviewer luon phien moi.
$Exec.resumeOnPatch = (-not $NoResume) -and ($null -eq $Prof.resumeOnPatch -or [bool]$Prof.resumeOnPatch)
$Exec.budget = [ordered]@{ maxTokensPerRun = $null; implementerMaxUsd = $null; reviewerMaxUsd = $null }
if ($Prof.budget) { foreach ($k in @('maxTokensPerRun', 'implementerMaxUsd', 'reviewerMaxUsd')) { if ($null -ne $Prof.budget.$k) { $Exec.budget[$k] = $Prof.budget.$k } } }
$Machine = if ($env:COMPUTERNAME) { $env:COMPUTERNAME } else { [System.Net.Dns]::GetHostName() }

# Model khong chi dinh => host dung mac dinh tai khoan; lay model do duoc o lan truoc (uu tien cung may) de canh bao.
$PlanWarnings = @()
if (-not $Exec.implementer.effective.model) {
    $allRuns = Get-RunsData
    $hist = @($allRuns | Where-Object { $_.observedModel -and $_.exec -and $_.exec.implementer.host -eq $ImplHost } | Sort-Object { [string]$_.startedAt } -Descending)
    $same = @($hist | Where-Object { $_.machine -eq $Machine })
    $last = if ($same.Count) { $same[0] } elseif ($hist.Count) { $hist[0] } else { $null }
    if ($last) {
        $Exec.implementer.effective.lastObserved = [string]$last.observedModel
        $Exec.implementer.effective.lastObservedOn = $(if ($same.Count) { 'may nay' } else { 'may khac' })
    }
    $PlanWarnings += ("Chua chi dinh model cho $ImplHost - se dung mac dinh tai khoan" + $(if ($last) { " (lan truoc tren $($Exec.implementer.effective.lastObservedOn): $($last.observedModel))" } else { '' }) + '. Chi dinh model de kiem soat chi phi.')
}
if (-not $Exec.budget.maxTokensPerRun) { $PlanWarnings += 'Khong co tran token cho run nay (budget.maxTokensPerRun).' }

function Show-PlanConsole {
    $i = $Exec.implementer; $r = $Exec.reviewer
    $dash = '(mac dinh)'
    Write-Host ''
    Write-Host "[$($TaskObj.id)] KE HOACH  profile=$($Exec.profile)  vong sua toi da=$($Exec.maxPatchRounds)"
    $ie = $i.effective; $re = $r.effective
    $fmt = { param($v, $src) if ($v) { if ($src -eq 'profile') { $v } else { "$v ($src)" } } else { $dash } }
    Write-Host ("  implementer : {0,-7} model={1} effort={2}{3}" -f $i.host, (& $fmt $ie.model $ie.modelSource), (& $fmt $ie.effort $ie.effortSource), $(if ($i.host -eq 'codex') { " tier=$(if ($ie.serviceTier) { & $fmt $ie.serviceTier $ie.serviceTierSource } else { 'standard' })" } else { '' }))
    Write-Host ("  reviewer    : claude  model={0} effort={1} (chi doc)" -f (& $fmt $re.model $re.modelSource), (& $fmt $re.effort $re.effortSource))
    if ($Exec.budget.maxTokensPerRun) { Write-Host "  tran token  : $($Exec.budget.maxTokensPerRun) / run" }
    Write-Host "  vong sua    : $(if ($Exec.resumeOnPatch) { 'tiep tuc phien implementer cu' } else { 'phien moi moi vong' })"
    if ($TaskObj.verify -and $TaskObj.verify.steps) { Write-Host "  xac minh    : script Unity MCP truoc ($(@($TaskObj.verify.steps).Count) buoc, 0 token); fail -> $(if ([string]$TaskObj.verify.onFail -eq 'stop') { 'dung' } else { 'giao implementer' })" }
    foreach ($w in $PlanWarnings) { Write-Host "  CANH BAO    : $w" -ForegroundColor Yellow }
}

function Write-PlanPage([string]$OutPath) {
    $hosts = [ordered]@{}
    foreach ($h in @('codex', 'claude')) {
        $levels = @($Cap.checks | Where-Object { $_.host -eq $h -and $_.status -eq 'PASS' } | ForEach-Object { $_.level })
        $hosts[$h] = [ordered]@{ ok = (Test-HostLevels $h @($TaskObj.requires)); levels = $levels }
    }
    $runs = Get-RunsData
    $codexModels = @(@($runs | Where-Object { $_.observedModel -and $_.exec -and $_.exec.implementer.host -eq 'codex' } | ForEach-Object { $_.observedModel }) + @((Get-CodexConfigDefaults).model) | Where-Object { $_ } | Sort-Object -Unique)
    $data = [ordered]@{
        mode = 'plan'; wp = (Split-Path -Leaf $WpDir); generatedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        runs = $runs; interventions = (Get-Interventions)
        plan = [ordered]@{
            taskPath = ((Get-RelPath $TaskPath) -replace '/', '\')
            task = [ordered]@{ id = $TaskObj.id; packet = $TaskObj.packet; acceptance = @($TaskObj.acceptance); writeSet = @($TaskObj.writeSet); requires = @($TaskObj.requires) }
            exec = $Exec; profiles = $Profiles.profiles; hosts = $hosts; knownModels = [ordered]@{ codex = $codexModels }
            warnings = @($PlanWarnings); machine = $Machine
        }
    }
    Write-DashboardHtml $OutPath $data $false
}

# -VerifyOnly khong ton token nen khong hoi (tru khi -Confirm).
$needConfirm = $Confirm -or ([bool]$Profiles.confirmBeforeDispatch -and -not $Yes -and -not $VerifyOnly)
if ($Plan -or $needConfirm) {
    Show-PlanConsole
    $planPage = Join-Path $RunDir 'plan.html'
    Write-PlanPage $planPage
    Write-Host "  trang ke hoach: $(Get-RelPath $planPage)"
    Open-Page $planPage
    if ($Plan) { exit 0 }
    $ans = Read-Host 'Giao viec voi cau hinh nay? [y/N]'
    if ($ans -notmatch '^(y|yes|c|co)$') {
        Remove-Item -LiteralPath $RunDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host 'Da huy, khong giao. Chinh cau hinh trong trang ke hoach roi chay lenh no tao ra.'
        exit 0
    }
}

$BaseCommit = (Invoke-Git @('rev-parse', 'HEAD')).Trim()
$Snapshot = Get-TreeSnapshot
$Ignore = @($RunsRel, '.toolchain/', (Get-RelPath $InterventionsPath))

Write-Host "[$($TaskObj.id)] implementer=$ImplHost reviewer=claude profile=$($Exec.profile) base=$($BaseCommit.Substring(0, 8)) run=$(Get-RelPath $RunDir)"
$Progress = [ordered]@{
    schema = 'run-progress/v1'; taskId = $TaskObj.id; status = 'RUNNING'; reason = $null; rounds = 0
    host = $ImplHost; profile = $Exec.profile; exec = $Exec; observedModel = $null; machine = $Machine
    startedAt = (Format-RunStamp $RunStamp); baseCommit = $BaseCommit
    usage = [ordered]@{ script = [ordered]@{}; implementer = [ordered]@{}; reviewer = [ordered]@{}; total = [ordered]@{} }
    steps = @()
}
Save-Progress $true
Write-Host "[$($TaskObj.id)] tien do: $(Get-RelPath $DashboardPath)"
Open-Page $DashboardPath

# ---------------------------------------------------------------- prompts

$writeSetText = (@($TaskObj.writeSet) | ForEach-Object { "- $_" }) -join "`n"
$acceptText = (@($TaskObj.acceptance) | ForEach-Object { "- [$($_.kind)] $($_.text)" }) -join "`n"
$reuseText = if ($TaskObj.mustReuse) { (@($TaskObj.mustReuse) | ForEach-Object { "- $_" }) -join "`n" } else { '- (none)' }
$targetText = if ($TaskObj.targetRef) { $TaskObj.targetRef } else { '(none)' }
# context minimal: task xac minh khong doc chuoi doc bat buoc cua AGENTS.md (pilot: 16 file ~117 KB, ~135k token).
$contextRule = if ([string]$TaskObj.context -eq 'minimal') {
    "CONTEXT: minimal (AGENTS.md section 2 exception). Read ONLY the packet below and files it names explicitly.`nDo NOT read the AGENTS.md reading order (Docs/*, standards/*), skills/*, or other handoff files. Hard rules of AGENTS.md section 5 still apply."
} else {
    'Follow AGENTS.md / CLAUDE.md of this repo.'
}
$baselineText = if ($TaskObj.baselineRef) { "baselineRef = $($TaskObj.baselineRef)" } else { 'no baselineRef set' }

function New-ImplementerPrompt([int]$Round, [string]$PrevReviewPath) {
    $patchBlock = ''
    if ($ScriptVerifyReport -and $Round -eq 1) {
        $patchBlock += @"

## Deterministic verification failed first
tools/run-task.ps1 ran the task's verify steps through Unity MCP (no AI) and they failed. Report:
``$(Get-RelPath $ScriptVerifyReport)``. Fix what failed; the evidence files it names are already written.
"@
    }
    if ($PrevReviewPath) {
        $patchBlock = @"

## Round $Round - apply review findings

An independent reviewer returned PATCH. Read ``$(Get-RelPath $PrevReviewPath)`` and fix every finding.
If this conversation already contains your previous round, do not re-read files you already know; only
re-check what the findings touch.
Do not re-litigate the findings; if one is impossible, end with RESULT: BLOCKED and say why.
"@
    }
    return @"
# Runner task $($TaskObj.id) - implementer (round $Round)

You are the IMPLEMENTER, running headless under tools/run-task.ps1. No human is watching.
$contextRule

## Packet
Read and execute: ``$($TaskObj.packet)``

## Rules
- Only create/modify files matching the write set below. The runner diffs git afterwards; anything outside => task BLOCKED.
- Do not edit approved visual target files. Target ref: $targetText
- Must reuse (do not re-implement):
$reuseText
- Unity MCP server name: $ImplMcp
- Do not commit, push, or rewrite git history.
- You do NOT judge the result. Do not write review files. Do not claim PASS or DONE.
- Never ask the user a question: if blocked, stop and report.
- Only call a failure pre-existing if you show it failing at the task baselineRef ($baselineText).
  Never check out, stash or reset to establish a baseline in this working tree.
- Unity may stop answering MCP (ping not answered, session not ready, poll timeout) while it compiles,
  reloads the domain or runs tests: its main thread is busy. That alone is NOT a blocker. Wait 30 s and
  poll again; keep polling for up to 20 minutes per long operation before reporting BLOCKED, and include
  the last MCP error text and how long you waited.

## Write set
$writeSetText

## Acceptance (for your self-check; a separate reviewer decides)
$acceptText
$patchBlock

## Final output (required, last lines of your final message)
RESULT: IMPLEMENTED
or
RESULT: BLOCKED: <exact blocker + evidence>
EVIDENCE: <semicolon-separated repo-relative paths of evidence files you produced>
SUMMARY: <one line of what you did and the observed numbers>
"@
}

function New-ReviewerPrompt([int]$Round, [string]$ResultPath, [string]$DiffPath) {
    return @"
# Runner task $($TaskObj.id) - independent reviewer (round $Round)

You are the REVIEWER, a fresh read-only invocation. You did not implement this. You can only read files.
Judge the implementer's work against the packet and acceptance. Be concrete; cite evidence files.

## Read
- Packet: ``$($TaskObj.packet)``
- Result (runner-computed changed files, implementer summary): ``$(Get-RelPath $ResultPath)``
- Diff of this round: ``$(Get-RelPath $DiffPath)``
- Evidence files listed in the result.
- Target ref: $targetText

## Acceptance
$acceptText

## Verdict rules
- PASS: every [auto] and [review] acceptance item is satisfied with evidence. [manual] items are not yours; ignore them.
- PATCH: something the implementer can fix. Each finding needs a concrete fix: patch.field + from/to when it is a profile/data value, else patch.instruction.
- TARGET_RECONSIDER: only if implementation matches the target/acceptance and the result is still wrong, AND you list in implHypothesesRuledOut the implementation causes you ruled out with evidence. Otherwise use PATCH or BLOCKED.
- BLOCKED: cannot be judged or fixed by the implementer (missing evidence you cannot obtain, contradictory packet).
- Never propose changing an approved target as a PATCH.
- "Pre-existing failure" is accepted ONLY with evidence that it fails at the task's baselineRef
  ($baselineText), a commit from before the work under test. The runner's base commit is the state
  WITH the work under test, so failing there proves nothing. No baselineRef, or no evidence at it
  => treat the failure as caused by the work under test (PATCH), never as pre-existing.
- A failing test whose assertion contradicts a locked rule of the packet is a stale test: PATCH with the
  instruction to update the test to the locked rule, citing the rule.

## Output
Reply with ONLY one JSON object, no prose, no code fence:
{"verdict":"PASS|PATCH|TARGET_RECONSIDER|BLOCKED","summary":"...","findings":[{"area":"...","observed":"...","expected":"...","evidence":"...","patch":{"field":"...","from":null,"to":null,"instruction":"..."}}],"implHypothesesRuledOut":[]}
"@
}

# ---------------------------------------------------------------- host invocation

function Get-ClaudeExecArgs($Cfg, $MaxUsd) {
    $a = @()
    if ($Cfg.model) { $a += @('--model', [string]$Cfg.model) }
    if ($Cfg.effort) { $a += @('--effort', [string]$Cfg.effort) }
    if ($MaxUsd) { $a += @('--max-budget-usd', ([string]$MaxUsd)) }
    return $a
}

function Invoke-Implementer([string]$PromptPath, [string]$LogPath, [string]$LastMsgPath, [string]$ResumeId) {
    $instruction = "Read the file $(Get-RelPath $PromptPath) and follow it exactly."
    $cfg = $Exec.implementer
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $session = $null
    if ($ImplHost -eq 'claude') {
        $tools = @('Read', 'Grep', 'Glob', 'Edit', 'Write')
        if ($ImplMcp) { $tools += "mcp__$ImplMcp" }
        $resumeArgs = if ($ResumeId) { @('--resume', $ResumeId) } else { @() }
        $r = Invoke-Native $ImplExe (@('-p', $instruction, '--output-format', 'json', '--permission-mode', 'acceptEdits') + $resumeArgs + (Get-ClaudeExecArgs $cfg $Exec.budget.implementerMaxUsd) + @('--allowedTools') + $tools) $LogPath
        $obj = Get-JsonObjectFromText $r.Text
        $text = if ($obj -and $obj.result) { [string]$obj.result } else { '' }
        if ($obj -and $obj.session_id) { $session = [string]$obj.session_id }
        $usage = Get-ClaudeUsage $r.Text
    } else {
        # -c value khong bao nhay: PS 5.1 lam hong dau nhay trong tham so native; codex doc gia tri tran la chuoi.
        # 'exec resume' khong co -s: sandbox truyen qua -c sandbox_mode.
        $cx = if ($ResumeId) { @('exec', 'resume', '-c', "sandbox_mode=$CodexSandbox") } else { @('exec', '-s', $CodexSandbox) }
        if ($cfg.model) { $cx += @('-m', [string]$cfg.model) }
        if ($cfg.effort) { $cx += @('-c', "model_reasoning_effort=$($cfg.effort)") }
        if ($cfg.serviceTier) { $cx += @('-c', "service_tier=$($cfg.serviceTier)") }
        if (Test-Path $LastMsgPath) { Remove-Item -LiteralPath $LastMsgPath -Force }
        $tail = if ($ResumeId) { @('-o', $LastMsgPath, $ResumeId, $instruction) } else { @('-o', $LastMsgPath, $instruction) }
        $r = Invoke-Native $ImplExe ($cx + $tail) $LogPath
        if ($r.Text -match '(?m)^session id:\s*([0-9a-fA-F-]{36})') { $session = $Matches[1] }
        # Chi tin tin nhan cuoi (-o). Log tho co ca noi dung prompt Codex da doc (co dong RESULT: mau);
        # pilot economy: Codex het quota giua chung, runner doc nham 'RESULT: IMPLEMENTED' tu log.
        $text = if (Test-Path $LastMsgPath) { [System.IO.File]::ReadAllText($LastMsgPath) } else { '' }
        $usage = Get-CodexUsage $r.Text $sw.ElapsedMilliseconds
    }
    if ($usage -and $null -eq $usage.durationMs) { $usage.durationMs = $sw.ElapsedMilliseconds }
    Write-Text $LastMsgPath $text
    if (-not $session -and $ResumeId) { $session = $ResumeId }
    return [pscustomobject]@{ Text = $text; Usage = $usage; Code = $r.Code; Raw = $r.Text; Session = $session; Resumed = [bool]$ResumeId }
}

# Het quota/rate limit cua host: khong phai loi task; bao ro thoi diem thu lai, khong goi reviewer.
function Get-QuotaHint([string]$Raw) {
    $m = [regex]::Match($Raw, "(?i)(you've hit your usage limit|usage limit|rate limit|quota exceeded|insufficient_quota)[^\r\n]*")
    if (-not $m.Success) { return $null }
    $t = $m.Value
    if ($t -match '(?i)try again (at|in) ([^.\r\n]+)') { return "het quota host, thu lai $($Matches[1]) $($Matches[2].Trim())" }
    return "het quota host: $($t.Substring(0, [Math]::Min(160, $t.Length)))"
}

# Implementer ket thuc ma khong co RESULT: thuong la host loi ngay (het quota, mang, sandbox, auth).
# Dua exit code + vai dong loi cuoi vao ly do de dashboard noi duoc nguyen nhan, khong phai mo log.
function Get-HostFailureHint($Impl) {
    $lines = @(($Impl.Raw -split "`r?`n") | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $err = @($lines | Where-Object { $_ -match '(?i)error|limit|quota|denied|unauthori|forbidden|failed|timeout|rate' } | Select-Object -Last 2)
    if ($err.Count -eq 0) { $err = @($lines | Select-Object -Last 2) }
    $hint = ($err -join ' | ')
    if ($hint.Length -gt 300) { $hint = $hint.Substring(0, 300) + '...' }
    return "exit=$($Impl.Code); $hint"
}

function Invoke-Reviewer([string]$PromptPath, [string]$LogPath) {
    $instruction = "Read the file $(Get-RelPath $PromptPath) and follow it exactly. Output only the JSON object."
    $r = Invoke-Native $ReviewerExe (@('-p', $instruction, '--output-format', 'json') + (Get-ClaudeExecArgs $Exec.reviewer $Exec.budget.reviewerMaxUsd) + @('--allowedTools', 'Read', 'Grep', 'Glob')) $LogPath
    $outer = Get-JsonObjectFromText $r.Text
    $session = if ($outer -and $outer.session_id) { [string]$outer.session_id } else { 'unknown' }
    $inner = $null
    if ($outer -and $outer.result) { $inner = Get-JsonObjectFromText ([string]$outer.result) }
    return [pscustomobject]@{ Review = $inner; Session = $session; Usage = (Get-ClaudeUsage $r.Text) }
}

# ---------------------------------------------------------------- loop

# ---------------------------------------------------------------- round 0: xac minh bang script (0 token)

$ScriptVerifyReport = $null
if ($TaskObj.verify -and $TaskObj.verify.steps) {
    $vp = Join-Path $RunDir 'r0'
    Write-Host "[$($TaskObj.id)] round 0 - xac minh bang script (Unity MCP, 0 token) ..."
    $vStep = Start-Step 'script' 0 'unity-mcp'
    $mcpUrl = if ($Cap.unity -and $Cap.unity.mcpUrl) { [string]$Cap.unity.mcpUrl } else { 'http://127.0.0.1:8080/mcp' }
    $vsw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        Import-Module (Join-Path $PSScriptRoot 'UnityMcp.psm1') -Force
        Connect-UnityMcp -Url $mcpUrl | Out-Null
        $vr = Invoke-VerifySteps -Steps $TaskObj.verify.steps -Root $ProjectRoot
    } catch {
        $vr = [pscustomobject]@{ Pass = $false; Lines = @("MCP $mcpUrl : $($_.Exception.Message)"); Failures = @("MCP: $($_.Exception.Message)") }
    }
    $vUsage = [ordered]@{ model = 'script'; tokens = 0; costUsd = 0; durationMs = $vsw.ElapsedMilliseconds }
    Write-Text "$vp.verify.md" ("# Script verify ($($TaskObj.id))`n`n" + ((@($vr.Lines) | ForEach-Object { "- $_" }) -join "`n") + "`n")
    $changed = Get-ChangedSince $Snapshot $Ignore
    $script:LastChanged = @($changed)
    $outOfScope = @($changed | Where-Object { -not (Test-InWriteSet $_ @($TaskObj.writeSet)) })
    $evidence = @(@($TaskObj.verify.steps) | Where-Object { $_.out } | ForEach-Object { [string]$_.out })
    $vStatus = if ($vr.Pass -and -not $outOfScope.Count) { 'IMPLEMENTED' } else { 'BLOCKED' }
    $vBlocker = if ($outOfScope.Count) { "script ghi ngoai writeSet: $($outOfScope -join ', ')" } elseif (-not $vr.Pass) { (@($vr.Failures) -join ' | ') } else { $null }
    Write-Json "$vp.result.json" ([ordered]@{
            schema = 'result/v1'; taskId = $TaskObj.id; round = 0; host = 'script'; status = $vStatus; blocker = $vBlocker
            baseCommit = $BaseCommit; targetRef = $TaskObj.targetRef; changedFiles = @($changed); outOfScope = $outOfScope
            evidence = $evidence; summary = (@($vr.Lines) -join ' | '); log = (Get-RelPath "$vp.verify.md"); usage = $vUsage
        })
    Stop-Step $vStep $(if ($vStatus -eq 'IMPLEMENTED') { 'PASS' } else { 'FAIL' }) $(if ($vStatus -eq 'IMPLEMENTED') { 'PASS' } else { 'FAIL' }) $vUsage
    foreach ($line in @($vr.Lines)) { Write-Host "  $line" }
    if ($vStatus -eq 'IMPLEMENTED') {
        # Acceptance la so do duoc (compile/test/parity): khong can reviewer AI.
        Complete-Run 'DONE_PENDING_FEEL' "script verify PASS, 0 token ($(@($vr.Lines).Count) buoc)" 0
    }
    if ($VerifyOnly -or [string]$TaskObj.verify.onFail -eq 'stop' -or $outOfScope.Count) {
        Add-Intervention 'BLOCKED' "script verify: $vBlocker"
        Complete-Run 'BLOCKED' "script verify: $vBlocker" 0
    }
    $ScriptVerifyReport = "$vp.verify.md"
    Write-Host "[$($TaskObj.id)] script verify FAIL -> giao implementer ($ImplHost)"
}

if ($VerifyOnly) {
    Complete-Run 'BLOCKED' 'task khong co khoi verify; -VerifyOnly khong goi AI' 0
}

$prevReview = $null
$ImplSession = $null
$maxRounds = $Exec.maxPatchRounds + 1
for ($round = 1; $round -le $maxRounds; $round++) {
    $p = Join-Path $RunDir "r$round"
    Write-Text "$p.implementer.prompt.md" (New-ImplementerPrompt $round $prevReview)
    $spent = $Progress.usage.total['tokens']
    if ($Exec.budget.maxTokensPerRun -and $spent -and $spent -ge [long]$Exec.budget.maxTokensPerRun) {
        $why = "da dung $spent token >= tran $($Exec.budget.maxTokensPerRun); khong mo vong $round"
        Add-Intervention 'BLOCKED_BUDGET' $why
        Complete-Run 'BLOCKED_BUDGET' $why ($round - 1)
    }
    Write-Host "[$($TaskObj.id)] round $round - implementer ($ImplHost) ..."
    $Progress.rounds = $round
    $implStep = Start-Step 'implementer' $round $ImplHost
    $resumeId = $null
    if ($round -gt 1 -and $Exec.resumeOnPatch -and $ImplSession) { $resumeId = $ImplSession }
    $impl = Invoke-Implementer "$p.implementer.prompt.md" "$p.implementer.out.txt" "$p.implementer.last.md" $resumeId
    if ($resumeId -and -not $impl.Text -and -not (Get-QuotaHint $impl.Raw)) {
        # Phien cu khong mo lai duoc (het han, bi xoa, loi CLI): chay lai vong nay bang phien moi.
        Write-Host "[$($TaskObj.id)] round $round - khong tiep tuc duoc phien $resumeId, mo phien moi"
        Move-Item -LiteralPath "$p.implementer.out.txt" -Destination "$p.implementer.resume-failed.out.txt" -Force -ErrorAction SilentlyContinue
        $impl = Invoke-Implementer "$p.implementer.prompt.md" "$p.implementer.out.txt" "$p.implementer.last.md" $null
    }
    if ($impl.Session) { $script:ImplSession = $impl.Session }
    $implStep.resumed = $impl.Resumed
    $msg = $impl.Text

    $status = 'BLOCKED'; $blocker = "implementer khong in dong RESULT: ($(Get-HostFailureHint $impl))"
    if ($msg -match '(?m)^\s*RESULT:\s*IMPLEMENTED\b') { $status = 'IMPLEMENTED'; $blocker = $null }
    elseif ($msg -match '(?m)^\s*RESULT:\s*BLOCKED:?\s*(.*)$') { $blocker = $Matches[1].Trim() }
    # Dong mau cua prompt (<...>) khong phai ket qua that.
    if ($status -eq 'IMPLEMENTED' -and $msg -match '(?m)^\s*SUMMARY:\s*<one line') {
        $status = 'BLOCKED'; $blocker = 'implementer tra ve dong mau cua prompt, khong phai ket qua'
    }
    $quota = Get-QuotaHint $impl.Raw
    $evidence = @()
    if ($msg -match '(?m)^\s*EVIDENCE:\s*(.*)$') { $evidence = @($Matches[1] -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }
    $summary = ''
    if ($msg -match '(?m)^\s*SUMMARY:\s*(.*)$') { $summary = $Matches[1].Trim() }

    $changed = Get-ChangedSince $Snapshot $Ignore
    $script:LastChanged = @($changed)
    $outOfScope = @($changed | Where-Object { -not (Test-InWriteSet $_ @($TaskObj.writeSet)) })
    $tracked = @($changed | Where-Object { (Invoke-Native 'git' @('ls-files', '--error-unmatch', '--', $_) $null).Code -eq 0 })
    $diffText = if ($tracked.Count) { Invoke-Git (@('diff', '--stat', '--patch', '--') + $tracked) } else { '' }
    $untracked = @($changed | Where-Object { $tracked -notcontains $_ })
    if ($untracked.Count) { $diffText += "`n# untracked/new files:`n" + (($untracked | ForEach-Object { "+ $_" }) -join "`n") }
    Write-Text "$p.diff" $diffText

    if ($status -eq 'IMPLEMENTED' -and $outOfScope.Count) {
        $status = 'BLOCKED'; $blocker = "ghi ngoai writeSet: $($outOfScope -join ', ')"
    }
    $result = [ordered]@{
        schema = 'result/v1'; taskId = $TaskObj.id; round = $round; host = $ImplHost; status = $status; blocker = $blocker
        baseCommit = $BaseCommit; targetRef = $TaskObj.targetRef; changedFiles = @($changed); outOfScope = $outOfScope
        evidence = $evidence; summary = $summary; log = (Get-RelPath "$p.implementer.out.txt"); diff = (Get-RelPath "$p.diff")
        exec = $Exec.implementer; usage = $impl.Usage; session = $impl.Session; resumed = $impl.Resumed
    }
    Write-Json "$p.result.json" $result
    Stop-Step $implStep $status $null $impl.Usage
    Write-Host "[$($TaskObj.id)] round $round - $status, $($changed.Count) file doi$(if ($blocker) { ", $blocker" })"

    if ($quota -and $status -ne 'IMPLEMENTED') {
        Add-Intervention 'BLOCKED_QUOTA' $quota
        Complete-Run 'BLOCKED_QUOTA' $quota $round
    }
    if ($status -eq 'BLOCKED') {
        Add-Intervention 'BLOCKED' $blocker
        Complete-Run 'BLOCKED' $blocker $round
    }

    Write-Host "[$($TaskObj.id)] round $round - reviewer (claude, read-only) ..."
    Write-Text "$p.reviewer.prompt.md" (New-ReviewerPrompt $round "$p.result.json" "$p.diff")
    $revStep = Start-Step 'reviewer' $round 'claude'
    $rv = Invoke-Reviewer "$p.reviewer.prompt.md" "$p.reviewer.out.txt"
    if (-not $rv.Review -or @('PASS', 'PATCH', 'TARGET_RECONSIDER', 'BLOCKED') -notcontains [string]$rv.Review.verdict) {
        Stop-Step $revStep 'INVALID' $null $rv.Usage
        Add-Intervention 'BLOCKED' 'reviewer khong tra JSON hop le'
        Complete-Run 'BLOCKED' "reviewer khong tra JSON hop le (xem $(Get-RelPath "$p.reviewer.out.txt"))" $round
    }
    $verdict = [string]$rv.Review.verdict
    $ruledOut = @($rv.Review.implHypothesesRuledOut | Where-Object { $_ })
    if ($verdict -eq 'TARGET_RECONSIDER' -and $ruledOut.Count -eq 0) { $verdict = 'BLOCKED' }
    $review = [ordered]@{
        schema = 'review/v1'; taskId = $TaskObj.id; round = $round; reviewer = "claude:$($rv.Session)"
        reviewedBase = $BaseCommit; targetRef = $TaskObj.targetRef; verdict = $verdict
        verdictRaw = [string]$rv.Review.verdict; summary = [string]$rv.Review.summary
        findings = @($rv.Review.findings); implHypothesesRuledOut = $ruledOut
        exec = $Exec.reviewer; usage = $rv.Usage
    }
    Write-Json "$p.review.json" $review
    Stop-Step $revStep 'done' $verdict $rv.Usage
    Write-Host "[$($TaskObj.id)] round $round - verdict ${verdict}: $($review.summary)"

    switch ($verdict) {
        'PASS' { Complete-Run 'DONE_PENDING_FEEL' $review.summary $round }
        'TARGET_RECONSIDER' {
            Add-Intervention 'TARGET_RECONSIDER' $review.summary
            Complete-Run 'BLOCKED_ON_TARGET' $review.summary $round
        }
        'BLOCKED' {
            $why = if ($review.verdictRaw -eq 'TARGET_RECONSIDER') { 'impl-unknown: TARGET_RECONSIDER thieu implHypothesesRuledOut' } else { $review.summary }
            Add-Intervention 'BLOCKED' $why
            Complete-Run 'BLOCKED' $why $round
        }
        'PATCH' { $prevReview = "$p.review.json" }
    }
}

Add-Intervention 'LOOP_CAP' "PATCH sau $($Exec.maxPatchRounds) vong sua"
Complete-Run 'ESCALATE:LOOP_CAP' "PATCH sau $($Exec.maxPatchRounds) vong sua" $maxRounds
