<#
.SYNOPSIS
  Chay nhieu task song song co gioi han (Phase 4, luat D.9 trong handoff/ROADMAP.md).

.DESCRIPTION
  Moi task van chay bang tools/run-task.ps1 (implementer -> reviewer -> PATCH). Batch chi quyet dinh THU TU va SONG SONG:
    - dependsOn: task chi bat dau khi moi dependency da DONE (trong batch, hoac run gan nhat da DONE_PENDING_FEEL).
      Dependency fail -> task SKIPPED.
    - writeSet: hai task co writeSet giao nhau khong chay cung luc (so theo tien to truoc ky tu glob dau tien, than trong).
    - tai nguyen doc quyen: 'unity' (resources co 'unity', requires UNITY_*, hoac buoc verify can Unity) -> toi da 1 luong Unity;
      'blender' (requires BLENDER_*, buoc verify blender) -> toi da 1 luong Blender.
    - toi da -MaxParallel task cung luc (mac dinh 2).
  Cung working tree: moi runner nhan -ExternalWriteSet = writeSet cua cac task khac, de khong tinh file cua task kia.
  Khong commit giua chung (git index.lock); -Commit commit tung task PASS sau khi ca batch xong.
  Output tung task: handoff/_batch/<stamp>/<id>.out.txt; tong ket: handoff/_batch/<stamp>/batch.json.

.EXAMPLE
  .\tools\run-batch.ps1 -Tasks handoff\wp-005\tasks\A.json,handoff\wp-005\tasks\B.json -Plan
  .\tools\run-batch.ps1 -Wp wp-005 -MaxParallel 2 -Commit
#>
[CmdletBinding()]
param(
    [string[]]$Tasks,
    [string]$Wp,
    [int]$MaxParallel = 2,
    [string]$RunProfile,
    [switch]$VerifyOnly,
    [switch]$Plan,
    [switch]$Yes,
    [switch]$Commit,
    [int]$PollSec = 5,
    # V2: task KHONG dung Unity chay trong git worktree rieng (.worktrees/), ket qua cherry-pick ve branch hien tai.
    # Tat bang -NoIsolate (quay lai V1: cung working tree + -ExternalWriteSet).
    [switch]$NoIsolate,
    [switch]$KeepWorktrees,
    [string]$Root
)

$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1: $PSScriptRoot rong trong gia tri mac dinh cua param -> tinh o day.
if (-not $Root) { $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
$Root = [System.IO.Path]::GetFullPath($Root)
$Utf8NoBom = New-Object System.Text.UTF8Encoding $false
$RunTask = Join-Path $Root 'tools/run-task.ps1'

function Get-RelPath([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $base = $Root.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if ($full.StartsWith($base, [System.StringComparison]::OrdinalIgnoreCase)) { $full = $full.Substring($base.Length) }
    return ($full -replace '\\', '/')
}

function Read-JsonFile([string]$Path) { return ([System.IO.File]::ReadAllText($Path).TrimStart([char]0xFEFF) | ConvertFrom-Json) }

# Tien to co dinh cua glob (truoc * ? [ {): 'Assets/_Core/4_Scripts/Board/**' -> 'Assets/_Core/4_Scripts/Board/'.
function Get-GlobPrefix([string]$Glob) {
    $g = $Glob -replace '\\', '/'
    $i = $g.IndexOfAny([char[]]'*?[{')
    if ($i -lt 0) { return $g }
    return $g.Substring(0, $i)
}

# Than trong: giao nhau neu tien to nay la tien to cua tien to kia (cung file, hoac thu muc chua nhau).
function Test-GlobOverlap([string]$A, [string]$B) {
    $pa = Get-GlobPrefix $A; $pb = Get-GlobPrefix $B
    return $pa.StartsWith($pb, [System.StringComparison]::OrdinalIgnoreCase) -or $pb.StartsWith($pa, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-WriteSetOverlap($TaskA, $TaskB) {
    foreach ($a in $TaskA.writeSet) { foreach ($b in $TaskB.writeSet) { if (Test-GlobOverlap $a $b) { return "$a ~ $b" } } }
    return $null
}

function Get-LatestRun([string]$TaskId, [string]$Base = $Root) {
    $dirs = @(Get-ChildItem -Path (Join-Path $Base 'handoff') -Directory -Recurse -Filter $TaskId -ErrorAction SilentlyContinue |
            Where-Object { (Split-Path -Leaf (Split-Path -Parent $_.FullName)) -eq 'runs' } |
            ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -Directory | Where-Object { $_.Name -match '^\d{8}-\d{6}$' } } |
            Sort-Object Name)
    if (-not $dirs.Count) { return $null }
    return $dirs[-1]
}

function Get-RunStatus($RunDir) {
    if (-not $RunDir) { return $null }
    foreach ($f in @('status.json', 'progress.json')) {
        $p = Join-Path $RunDir.FullName $f
        if (Test-Path -LiteralPath $p) { try { return [string](Read-JsonFile $p).status } catch { } }
    }
    return $null
}

# ---------------------------------------------------------------- load tasks

$paths = @()
if ($Tasks) { $paths = @($Tasks | ForEach-Object { $_ -split ',' } | Where-Object { $_ }) }
if ($Wp) { $paths += @(Get-ChildItem -Path (Join-Path $Root "handoff/$Wp/tasks") -Filter '*.json' -File | ForEach-Object { $_.FullName }) }
if (-not $paths.Count) { throw 'can -Tasks <file.json,...> hoac -Wp <wp>' }

$items = New-Object System.Collections.Generic.List[object]
foreach ($p in $paths) {
    $full = if ([System.IO.Path]::IsPathRooted($p)) { $p } else { Join-Path $Root $p }
    $t = Read-JsonFile $full
    if ($t.schema -ne 'task/v1') { Write-Warning "bo qua $(Get-RelPath $full): schema '$($t.schema)'"; continue }
    $res = @($t.resources | Where-Object { $_ })
    $unitySteps = @($t.verify.steps | Where-Object { $_ -and @('blender', 'files') -notcontains [string]$_.do })
    $usesUnity = ($res -contains 'unity') -or $unitySteps.Count -gt 0 -or (@($t.requires | Where-Object { $_ -like 'UNITY_*' }).Count -gt 0)
    if ($usesUnity -and $res -notcontains 'unity') { $res += 'unity' }
    # Blender: mot phien MCP / mot pipeline tren may -> doc quyen nhu Unity.
    $usesBlender = ($res -contains 'blender') -or @($t.verify.steps | Where-Object { $_ -and [string]$_.do -eq 'blender' }).Count -gt 0 -or (@($t.requires | Where-Object { $_ -like 'BLENDER_*' }).Count -gt 0)
    if ($usesBlender -and $res -notcontains 'blender') { $res += 'blender' }
    $items.Add([pscustomobject]@{
            id = [string]$t.id; path = (Get-RelPath $full); writeSet = @($t.writeSet); dependsOn = @($t.dependsOn | Where-Object { $_ })
            resources = @($res); state = 'PENDING'; status = $null; reason = $null; proc = $null; out = $null; started = $null; finished = $null; runDir = $null
            isolated = $false; wt = $null; branch = $null; base = $null
        })
}
$byId = @{}
foreach ($it in $items) {
    if ($byId.ContainsKey($it.id)) { throw "trung task id '$($it.id)'" }
    $byId[$it.id] = $it
}

# Dependency ngoai batch: phai DONE san.
foreach ($it in $items) {
    foreach ($d in $it.dependsOn) {
        if ($byId.ContainsKey($d)) { continue }
        $st = Get-RunStatus (Get-LatestRun $d)
        if ($st -ne 'DONE_PENDING_FEEL') { $it.state = 'SKIPPED'; $it.reason = "dependsOn $d chua DONE (run gan nhat: $(if ($st) { $st } else { 'chua chay' }))" }
    }
}

# Vong phu thuoc trong batch.
$visiting = @{}; $done = @{}
function Test-Cycle([string]$Id) {
    if ($done[$Id]) { return $false }
    if ($visiting[$Id]) { return $true }
    $visiting[$Id] = $true
    foreach ($d in $byId[$Id].dependsOn) { if ($byId.ContainsKey($d) -and (Test-Cycle $d)) { return $true } }
    $visiting[$Id] = $false; $done[$Id] = $true
    return $false
}
foreach ($it in $items) { if (Test-Cycle $it.id) { throw "dependsOn co vong quanh '$($it.id)'" } }

# Cap xung dot (khong duoc chay cung luc).
$conflicts = @{}
foreach ($a in $items) {
    foreach ($b in $items) {
        if ($a.id -ge $b.id) { continue }
        $why = @()
        $ov = Test-WriteSetOverlap $a $b
        if ($ov) { $why += "writeSet $ov" }
        $shared = @($a.resources | Where-Object { $b.resources -contains $_ })
        if ($shared.Count) { $why += "doc quyen $($shared -join ',')" }
        if ($why.Count) { $conflicts["$($a.id)|$($b.id)"] = ($why -join '; '); $conflicts["$($b.id)|$($a.id)"] = ($why -join '; ') }
    }
}

# ---------------------------------------------------------------- plan

Write-Host "BATCH $($items.Count) task, toi da $MaxParallel song song, toi da 1 luong Unity"
foreach ($it in $items) {
    $dep = if ($it.dependsOn.Count) { " sau: $($it.dependsOn -join ', ')" } else { '' }
    $res = if ($it.resources.Count) { " [$($it.resources -join ',')]" } else { '' }
    $skip = if ($it.state -eq 'SKIPPED') { "  SKIP: $($it.reason)" } else { '' }
    $it.isolated = (-not $NoIsolate) -and ($it.resources -notcontains 'unity')
    $iso = if ($it.isolated) { ' {worktree}' } else { '' }
    Write-Host ("  {0,-28}{1}{2}{3}{4}" -f $it.id, $res, $iso, $dep, $skip)
}
$pairs = @($conflicts.Keys | Where-Object { ($_ -split '\|')[0] -lt ($_ -split '\|')[1] })
if ($pairs.Count) {
    Write-Host '  khong chay cung luc:'
    foreach ($k in $pairs) { Write-Host "    $($k -replace '\|', ' x ')  ($($conflicts[$k]))" }
}
if ($Plan) { exit 0 }
if (-not $Yes) {
    $ans = Read-Host 'Chay batch nay? [y/N]'
    if ($ans -notmatch '^(y|yes|c|co)$') { Write-Host 'Da huy.'; exit 0 }
}

# ---------------------------------------------------------------- run

$stamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$BatchDir = Join-Path $Root "handoff/_batch/$stamp"
New-Item -ItemType Directory -Path $BatchDir -Force | Out-Null
$psExe = (Get-Process -Id $PID).Path
# .worktrees/ phai bi git bo qua, neu khong runner tren working tree chinh thay file la -> 'ghi ngoai writeSet'.
if (-not $NoIsolate -and @($items | Where-Object { $_.isolated }).Count) {
    & git -C $Root check-ignore -q '.worktrees/x' 2>$null
    if ($LASTEXITCODE -ne 0) {
        $exclude = Join-Path (& git -C $Root rev-parse --git-common-dir).Trim() 'info/exclude'
        if (-not [System.IO.Path]::IsPathRooted($exclude)) { $exclude = Join-Path $Root $exclude }
        New-Item -ItemType Directory -Path (Split-Path -Parent $exclude) -Force | Out-Null
        [System.IO.File]::AppendAllText($exclude, "`n/.worktrees/`n")
        Write-Host '  (them /.worktrees/ vao .git/info/exclude; nen them vao .gitignore)'
    }
}
# File chung ma moi runner deu ghi: khong phai thay doi cua task nao.
$sharedGlobs = @('handoff/**/runs/**', 'handoff/**/interventions.jsonl', 'handoff/_batch/**', 'handoff/_reports/**')

function Format-Arg([string]$Value) { return '"' + $Value.Replace('"', '\"') + '"' }

function Test-CanStart($It, $Running) {
    if ($It.state -ne 'PENDING') { return $false }
    foreach ($d in $It.dependsOn) {
        if (-not $byId.ContainsKey($d)) { continue }
        if ($byId[$d].state -ne 'DONE') { return $false }
    }
    # Task dang cho cherry-pick cung tinh: ket qua cua no chua ve HEAD, worktree moi se thieu no.
    foreach ($r in @($items | Where-Object { $_.state -eq 'RUNNING' -or $_.state -eq 'MERGING' })) {
        if ($conflicts.ContainsKey("$($It.id)|$($r.id)")) { return $false }
    }
    return $true
}

# Worktree rieng tu HEAD hien tai (da gom ket qua cac task DONE truoc do). File gitignored can cho runner
# (.toolchain/capabilities.json, config/*.local.json) duoc chep sang.
function New-TaskWorktree($It) {
    $It.base = (& git -C $Root rev-parse HEAD).Trim()
    $It.branch = "batch/$stamp/$($It.id)"
    $It.wt = Join-Path $Root ".worktrees/$stamp-$($It.id)"
    & git -C $Root worktree add -q -b $It.branch $It.wt $It.base 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $It.wt)) { throw "git worktree add that bai cho $($It.id)" }
    foreach ($rel in @('.toolchain', 'config/run-profiles.local.json', '.toolchain.local.json')) {
        $src = Join-Path $Root $rel
        if (Test-Path -LiteralPath $src) { Copy-Item -LiteralPath $src -Destination (Join-Path $It.wt $rel) -Recurse -Force }
    }
}

function Start-TaskProcess($It) {
    $workDir = $Root
    if ($It.isolated) {
        New-TaskWorktree $It
        $workDir = $It.wt
        # Tach biet: chi co thay doi cua chinh no; commit trong branch rieng de cherry-pick ve.
        $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Format-Arg (Join-Path $It.wt 'tools/run-task.ps1')), '-Task', (Format-Arg $It.path), '-Yes', '-NoOpen', '-Commit')
    } else {
        $others = @($items | Where-Object { $_.id -ne $It.id -and -not $_.isolated } | ForEach-Object { $_.writeSet }) + $sharedGlobs
        # Mot chuoi lenh, tu boc nhay: path co dau cach (vd C:\... co khoang trang) va glob co ';'.
        $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Format-Arg $RunTask), '-Task', (Format-Arg $It.path), '-Yes', '-NoOpen',
            '-ExternalWriteSet', (Format-Arg ($others -join ';')))
    }
    if ($RunProfile) { $argList += @('-RunProfile', (Format-Arg $RunProfile)) }
    if ($VerifyOnly) { $argList += '-VerifyOnly' }
    $It.out = Join-Path $BatchDir "$($It.id).out.txt"
    $It.started = Get-Date
    $It.proc = Start-Process -FilePath $psExe -ArgumentList ($argList -join ' ') -WorkingDirectory $workDir -NoNewWindow -PassThru `
        -RedirectStandardOutput $It.out -RedirectStandardError (Join-Path $BatchDir "$($It.id).err.txt")
    $null = $It.proc.Handle   # PS 5.1: can giu handle thi ExitCode moi co gia tri
    $It.state = 'RUNNING'
    Write-Host ("[{0}] START {1}{2}" -f (Get-Date).ToString('HH:mm:ss'), $It.id, $(if ($It.isolated) { " (worktree $(Get-RelPath $It.wt))" } else { '' }))
}

function Complete-Task($It) {
    $It.finished = Get-Date
    $run = Get-LatestRun $It.id $(if ($It.isolated) { $It.wt } else { $Root })
    # Ten thu muc run = gio bat dau (yyyyMMdd-HHmmss); CreationTime khong dang tin tren moi filesystem.
    $runStamp = if ($run) { [datetime]::ParseExact($run.Name, 'yyyyMMdd-HHmmss', [System.Globalization.CultureInfo]::InvariantCulture) } else { $null }
    if ($run -and $runStamp -ge $It.started.AddSeconds(-5)) {
        $It.runDir = if ($It.isolated) { $run.FullName.Substring($It.wt.TrimEnd('\', '/').Length + 1) -replace '\\', '/' } else { Get-RelPath $run.FullName }
        $It.status = Get-RunStatus $run
    }
    if (-not $It.status) { $It.status = "EXIT_$($It.proc.ExitCode)" }
    if ($It.isolated) {
        # Chua DONE cho toi khi ket qua ve branch hien tai (task phu thuoc can thay no).
        $It.state = 'MERGING'
        Write-Host ("[{0}] XONG   {1} ({2}) -> cho cherry-pick" -f (Get-Date).ToString('HH:mm:ss'), $It.id, $It.status)
        return
    }
    Set-FinalState $It
}

function Set-FinalState($It) {
    $It.state = if ($It.status -eq 'DONE_PENDING_FEEL' -and $It.state -ne 'FAILED') { 'DONE' } else { 'FAILED' }
    Write-Host ("[{0}] {1,-6} {2} ({3}, {4:N1} phut)" -f (Get-Date).ToString('HH:mm:ss'), $It.state, $It.id, $It.status, ($It.finished - $It.started).TotalMinutes)
    if ($It.state -eq 'FAILED') {
        # Task phu thuoc (truc tiep hoac gian tiep) khong chay nua.
        $queue = New-Object System.Collections.Generic.Queue[string]
        $queue.Enqueue($It.id)
        while ($queue.Count) {
            $cur = $queue.Dequeue()
            foreach ($x in $items) {
                if ($x.state -eq 'PENDING' -and $x.dependsOn -contains $cur) { $x.state = 'SKIPPED'; $x.reason = "dependsOn $cur khong DONE"; $queue.Enqueue($x.id) }
            }
        }
    }
}

# Cherry-pick commit cua worktree ve branch hien tai (ca code lan evidence; task BLOCKED chi co evidence).
function Merge-Isolated($It) {
    $commits = @(& git -C $Root rev-list --reverse "$($It.base)..$($It.branch)")
    $ok = $true
    if ($commits.Count) {
        & git -C $Root cherry-pick $commits 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            & git -C $Root cherry-pick --abort 2>&1 | Out-Null
            $ok = $false
            $It.reason = "cherry-pick xung dot; ket qua giu o branch $($It.branch)"
        }
    }
    if (-not $ok) { $It.state = 'FAILED' }
    Write-Host ("[{0}] MERGE  {1}: {2} commit{3}" -f (Get-Date).ToString('HH:mm:ss'), $It.id, $commits.Count, $(if ($ok) { '' } else { ' - XUNG DOT' }))
    if (-not $KeepWorktrees) {
        & git -C $Root worktree remove --force $It.wt 2>&1 | Out-Null
        if ($ok) { & git -C $Root branch -D $It.branch 2>&1 | Out-Null }
    }
    Set-FinalState $It
}

while ($true) {
    # Cherry-pick doi luc khong co task nao dang chay tren working tree chinh (tranh doi file duoi chan runner).
    if (-not @($items | Where-Object { $_.state -eq 'RUNNING' -and -not $_.isolated }).Count) {
        foreach ($m in @($items | Where-Object { $_.state -eq 'MERGING' })) { Merge-Isolated $m }
    }
    $running = @($items | Where-Object { $_.state -eq 'RUNNING' })
    foreach ($r in $running) { if ($r.proc.HasExited) { Complete-Task $r } }
    $running = @($items | Where-Object { $_.state -eq 'RUNNING' })
    foreach ($it in @($items | Where-Object { $_.state -eq 'PENDING' })) {
        if ($running.Count -ge $MaxParallel) { break }
        if (Test-CanStart $it $running) { Start-TaskProcess $it; $running = @($items | Where-Object { $_.state -eq 'RUNNING' }) }
    }
    if (-not @($items | Where-Object { $_.state -eq 'RUNNING' -or $_.state -eq 'MERGING' }).Count) {
        $stuck = @($items | Where-Object { $_.state -eq 'PENDING' })
        foreach ($s in $stuck) { $s.state = 'SKIPPED'; $s.reason = 'khong the bat dau (phu thuoc chua DONE)' }
        break
    }
    Start-Sleep -Seconds $PollSec
}

# ---------------------------------------------------------------- summary + commit

$summary = @($items | ForEach-Object {
        [ordered]@{ id = $_.id; state = $_.state; status = $_.status; reason = $_.reason; runDir = $_.runDir
            started = $(if ($_.started) { $_.started.ToString('s') }); minutes = $(if ($_.finished) { [math]::Round(($_.finished - $_.started).TotalMinutes, 1) }) }
    })
[System.IO.File]::WriteAllText((Join-Path $BatchDir 'batch.json'), (([ordered]@{ schema = 'batch/v1'; stamp = $stamp; maxParallel = $MaxParallel; tasks = $summary } | ConvertTo-Json -Depth 6) + "`n"), $Utf8NoBom)

Write-Host ''
Write-Host "BATCH xong -> $(Get-RelPath $BatchDir)"
foreach ($it in $items) { Write-Host ("  {0,-8} {1,-28} {2}" -f $it.state, $it.id, $(if ($it.reason) { $it.reason } else { $it.status })) }

if ($Commit) {
    foreach ($it in @($items | Where-Object { $_.state -eq 'DONE' -and $_.runDir -and -not $_.isolated })) {
        $result = @(Get-ChildItem -LiteralPath (Join-Path $Root $it.runDir) -Filter 'r*.result.json' | Sort-Object Name)
        # @(if ...) ngoai: gan truc tiep tu if boc mang 1 phan tu thanh chuoi, roi chuoi + mang = noi chuoi.
        $changed = @(if ($result.Count) { (Read-JsonFile $result[-1].FullName).changedFiles })
        $addPaths = @(@($changed) + @($it.runDir) | Where-Object { $_ -and (Test-Path -LiteralPath (Join-Path $Root $_)) })
        if (-not $addPaths.Count) { Write-Warning "commit $($it.id): khong co file de add"; continue }
        & git -C $Root add -- $addPaths
        & git -C $Root commit -q -m "$($it.id): DONE_PENDING_FEEL ($($it.runDir)) [batch $stamp]"
        if ($LASTEXITCODE -ne 0) { Write-Warning "commit $($it.id) that bai (git exit $LASTEXITCODE); file van tren working tree"; continue }
        Write-Host "  committed $($it.id)"
    }
    & git -C $Root add -- (Get-RelPath $BatchDir)
    & git -C $Root commit -q -m "batch $stamp summary"
    if ($LASTEXITCODE -ne 0) { Write-Warning "commit tong ket batch that bai (git exit $LASTEXITCODE)" }
}
if (@($items | Where-Object { $_.state -ne 'DONE' }).Count) { exit 1 }
exit 0
