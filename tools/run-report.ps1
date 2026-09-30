<#
.SYNOPSIS
  Tong hop moi run cua tools/run-task.ps1 (token, tien, thoi gian, trang thai) + tin hieu harvest -> 1 trang HTML.

.DESCRIPTION
  Doc handoff/**/runs/<task>/<stamp>/ (progress.json; ban cu: status.json + r*.result/review.json)
  va handoff/**/interventions.jsonl. Khong goi AI, khong goi mang.
  Ghi handoff/_reports/run-report.json + run-report.html, in tom tat ra console.

  Dung cuoi moi WP (workflow/harvest.md) hoac bat cu luc nao muon biet "tool dang ton bao nhieu".

.EXAMPLE
  .\tools\run-report.ps1
  .\tools\run-report.ps1 -Since 2026-09-01 -Wp wp-004
  .\tools\run-report.ps1 -NoOpen
#>
[CmdletBinding()]
param(
    [string]$Since,
    [string]$Wp,
    [string]$OutDir,
    [switch]$NoOpen,
    [switch]$NoLint,
    [string]$Root
)

$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1: $PSScriptRoot rong trong gia tri mac dinh cua param -> tinh o day.
if (-not $Root) { $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
$Root = [System.IO.Path]::GetFullPath($Root)
if (-not $OutDir) { $OutDir = Join-Path $Root 'handoff/_reports' }

function Write-Utf8NoBom([string]$Path, [string]$Text) {
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding $false))
}

function Read-JsonSafe([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    try { return ([System.IO.File]::ReadAllText($Path).TrimStart([char]0xFEFF) | ConvertFrom-Json) } catch { return $null }
}

function Get-RelPath([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $base = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if ($full.StartsWith($base, [System.StringComparison]::OrdinalIgnoreCase)) { $full = $full.Substring($base.Length) }
    return ($full -replace '\\', '/')
}

function Get-Num($Value) { if ($null -eq $Value -or $Value -eq '') { return 0 } return [double]$Value }

function Get-Median([double[]]$Values) {
    $v = @($Values | Sort-Object)
    if ($v.Count -eq 0) { return [double]0 }
    if ($v.Count % 2) { return $v[[int][math]::Floor($v.Count / 2)] }
    return ($v[$v.Count / 2 - 1] + $v[$v.Count / 2]) / 2
}

$sinceDate = $null
if ($Since) { $sinceDate = [datetime]::ParseExact($Since, 'yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture) }

# ---------------------------------------------------------------- runs

$handoff = Join-Path $Root 'handoff'
$runList = New-Object System.Collections.Generic.List[object]
$planOnly = 0
foreach ($runsDir in @(Get-ChildItem -Path $handoff -Directory -Recurse -Filter 'runs' -ErrorAction SilentlyContinue)) {
    $wpName = Split-Path -Leaf (Split-Path -Parent $runsDir.FullName)
    if ($Wp -and $wpName -ne $Wp) { continue }
    foreach ($taskDir in @(Get-ChildItem -LiteralPath $runsDir.FullName -Directory)) {
        foreach ($runDir in @(Get-ChildItem -LiteralPath $taskDir.FullName -Directory | Where-Object { $_.Name -match '^\d{8}-\d{6}$' })) {
            $stamp = [datetime]::ParseExact($runDir.Name, 'yyyyMMdd-HHmmss', [System.Globalization.CultureInfo]::InvariantCulture)
            if ($sinceDate -and $stamp -lt $sinceDate) { continue }
            $files = @(Get-ChildItem -LiteralPath $runDir.FullName -File | ForEach-Object { $_.Name })
            if ($files.Count -eq 0 -or (@($files | Where-Object { $_ -ne 'plan.html' }).Count -eq 0)) { $planOnly++; continue }

            $p = Read-JsonSafe (Join-Path $runDir.FullName 'progress.json')
            $st = Read-JsonSafe (Join-Path $runDir.FullName 'status.json')
            $src = if ($p) { $p } else { $st }
            $status = if ($src -and $src.status) { [string]$src.status } else { 'INCOMPLETE' }
            $rounds = if ($src -and $null -ne $src.rounds) { [int]$src.rounds } else { @($files | Where-Object { $_ -match '^r[1-9]\d*\.result\.json$' }).Count }

            $verdicts = @($files | Where-Object { $_ -match '^r\d+\.review\.json$' } | Sort-Object | ForEach-Object {
                    $rv = Read-JsonSafe (Join-Path $runDir.FullName $_); if ($rv -and $rv.verdict) { [string]$rv.verdict } })
            $u = if ($p -and $p.usage) { $p.usage } else { $null }
            $implTok = if ($u -and $u.implementer) { Get-Num $u.implementer.tokens } else { 0 }
            $revTok = if ($u -and $u.reviewer) { Get-Num $u.reviewer.tokens } else { 0 }
            $cost = if ($u -and $u.total) { Get-Num $u.total.costUsd } else { 0 }
            $dur = if ($u -and $u.total) { Get-Num $u.total.durationMs } else { 0 }
            $scriptRan = [bool]($files -contains 'r0.verify.md')
            $models = New-Object System.Collections.Generic.List[string]
            $resumed = 0
            if ($p -and $p.steps) {
                foreach ($s in @($p.steps)) {
                    if ($s.resumed) { $resumed++ }
                    if ($s.usage -and $s.usage.model) {
                        $m = "$($s.role):$($s.usage.model)"; if ($s.usage.effort) { $m += " $($s.usage.effort)" }
                        if (-not $models.Contains($m)) { $models.Add($m) }
                    }
                }
            }
            $aiUsed = ($implTok + $revTok) -gt 0 -or @($files | Where-Object { $_ -match '^r[1-9]\d*\.(implementer|reviewer)' }).Count -gt 0
            $kind = if ($scriptRan -and -not $aiUsed) { 'script' } elseif ($aiUsed) { 'ai' } else { 'none' }
            $runList.Add([pscustomobject][ordered]@{
                    wp = $wpName; task = $taskDir.Name; stamp = $runDir.Name; at = $stamp.ToString('yyyy-MM-dd HH:mm')
                    status = $status; reason = $(if ($src -and $src.reason) { ([string]$src.reason).Substring(0, [math]::Min(300, ([string]$src.reason).Length)) } else { '' })
                    rounds = $rounds; kind = $kind; host = $(if ($src -and $src.host) { [string]$src.host } else { '' })
                    profile = $(if ($src -and $src.profile) { [string]$src.profile } else { '' })
                    machine = $(if ($src -and $src.machine) { [string]$src.machine } else { '' })
                    models = @($models); verdicts = @($verdicts); resumed = $resumed
                    implTokens = $implTok; reviewTokens = $revTok; tokens = $implTok + $revTok
                    costUsd = [math]::Round([double]$cost, 4); durationMs = $dur; usageKnown = [bool]$u
                    dir = (Get-RelPath $runDir.FullName)
                })
        }
    }
}
$runs = @($runList | Sort-Object { $_.stamp })

# ---------------------------------------------------------------- interventions

$ivList = New-Object System.Collections.Generic.List[object]
foreach ($f in @(Get-ChildItem -Path $handoff -File -Recurse -Filter 'interventions.jsonl' -ErrorAction SilentlyContinue)) {
    $wpName = Split-Path -Leaf (Split-Path -Parent $f.FullName)
    if ($Wp -and $wpName -ne $Wp) { continue }
    foreach ($line in [System.IO.File]::ReadAllLines($f.FullName)) {
        if (-not $line.Trim()) { continue }
        try { $o = $line.TrimStart([char]0xFEFF) | ConvertFrom-Json } catch { continue }
        # Giu gio nhu luc ghi (offset may chay), khong doi sang mui gio may doc report.
        $at = $null; try { $at = ([datetimeoffset]::Parse([string]$o.at, [System.Globalization.CultureInfo]::InvariantCulture)).DateTime } catch { }
        if ($sinceDate -and $at -and $at -lt $sinceDate) { continue }
        $ivList.Add([pscustomobject][ordered]@{ wp = $wpName; at = $(if ($at) { $at.ToString('yyyy-MM-dd HH:mm') } else { [string]$o.at }); task = [string]$o.taskId; state = [string]$o.state; reason = [string]$o.reason; run = [string]$o.run })
    }
}

$interventions = $ivList.ToArray()

# ---------------------------------------------------------------- aggregates

function Group-Count($Items, [scriptblock]$Key) {
    $h = [ordered]@{}
    foreach ($i in $Items) { $k = [string](& $Key $i); if (-not $k) { $k = '?' }; if ($h.Contains($k)) { $h[$k]++ } else { $h[$k] = 1 } }
    return $h
}

$aiRuns = @($runs | Where-Object { $_.kind -eq 'ai' -and $_.usageKnown })
$scriptRuns = @($runs | Where-Object { $_.kind -eq 'script' })
$scriptPass = @($scriptRuns | Where-Object { $_.status -eq 'DONE_PENDING_FEEL' })
$medianAiTokens = Get-Median @($aiRuns | ForEach-Object { [double]$_.tokens })
$medianAiCost = Get-Median @($aiRuns | ForEach-Object { [double]$_.costUsd })
$medianAiMs = Get-Median @($aiRuns | ForEach-Object { [double]$_.durationMs })

# Uoc tinh tiet kiem: moi run script PASS thay cho 1 run AI cung task (median cung task, thieu thi median chung).
$savedTokens = 0.0; $savedCost = 0.0; $savedMs = 0.0
foreach ($r in $scriptPass) {
    $same = @($aiRuns | Where-Object { $_.task -eq $r.task })
    $t = if ($same.Count) { Get-Median @($same | ForEach-Object { [double]$_.tokens }) } else { $medianAiTokens }
    $c = if ($same.Count) { Get-Median @($same | ForEach-Object { [double]$_.costUsd }) } else { $medianAiCost }
    $ms = if ($same.Count) { Get-Median @($same | ForEach-Object { [double]$_.durationMs }) } else { $medianAiMs }
    $savedTokens += $t; $savedCost += $c; $savedMs += [math]::Max([double]0, [double]($ms - $r.durationMs))
}

$models = [ordered]@{}
foreach ($r in $runs) {
    foreach ($m in $r.models) {
        $role = ($m -split ':')[0]
        $tok = if ($role -eq 'implementer') { $r.implTokens } else { $r.reviewTokens }
        if (-not $models.Contains($m)) { $models[$m] = [ordered]@{ runs = 0; tokens = 0 } }
        $models[$m].runs++; $models[$m].tokens += $tok
    }
}

$allVerdicts = @($runs | ForEach-Object { $_.verdicts } | Where-Object { $_ })
$harvest = [ordered]@{
    loopCap = @($runs | Where-Object { $_.status -like 'ESCALATE*' }).Count
    blockedToolchain = @($runs | Where-Object { $_.status -eq 'BLOCKED_TOOLCHAIN' }).Count
    blockedQuota = @($runs | Where-Object { $_.status -eq 'BLOCKED_QUOTA' }).Count
    blockedBudget = @($runs | Where-Object { $_.status -eq 'BLOCKED_BUDGET' }).Count
    reviewOverrides = @($interventions | Where-Object { $_.state -eq 'REVIEW_OVERRIDE' }).Count
    patchRuns = @($runs | Where-Object { $_.verdicts -contains 'PATCH' }).Count
    targetReconsider = @($allVerdicts | Where-Object { $_ -eq 'TARGET_RECONSIDER' }).Count
    incomplete = @($runs | Where-Object { $_.status -eq 'INCOMPLETE' }).Count
    planOnly = $planOnly
}

$lint = $null
$lintScript = Join-Path $PSScriptRoot 'template-lint.ps1'
if (-not $NoLint -and (Test-Path $lintScript)) {
    try {
        $lintOut = & $lintScript -Json 2>$null
        $lintObj = (@($lintOut) -join "`n") | ConvertFrom-Json
        $lint = [ordered]@{ fail = @($lintObj.findings | Where-Object { $_.severity -eq 'FAIL' }).Count; warn = @($lintObj.findings | Where-Object { $_.severity -eq 'WARN' }).Count; findings = @($lintObj.findings) }
    } catch { $lint = [ordered]@{ error = $_.Exception.Message } }
}

$report = [ordered]@{
    schema = 'run-report/v1'
    generatedAt = (Get-Date).ToString('s')
    root = (Split-Path -Leaf $Root)
    filter = [ordered]@{ since = $Since; wp = $Wp }
    totals = [ordered]@{
        runs = $runs.Count; aiRuns = @($runs | Where-Object { $_.kind -eq 'ai' }).Count; scriptRuns = $scriptRuns.Count
        pass = @($runs | Where-Object { $_.status -eq 'DONE_PENDING_FEEL' }).Count
        tokens = [double](($runs | Measure-Object -Property tokens -Sum).Sum)
        implTokens = [double](($runs | Measure-Object -Property implTokens -Sum).Sum)
        reviewTokens = [double](($runs | Measure-Object -Property reviewTokens -Sum).Sum)
        costUsd = [math]::Round([double](($runs | Measure-Object -Property costUsd -Sum).Sum), 3)
        durationMs = [double](($runs | Measure-Object -Property durationMs -Sum).Sum)
        medianAiTokens = $medianAiTokens; medianAiCostUsd = [math]::Round([double]$medianAiCost, 3)
        savedTokensEst = [math]::Round([double]$savedTokens); savedCostUsdEst = [math]::Round([double]$savedCost, 3); savedMsEst = [math]::Round([double]$savedMs)
        usageUnknownRuns = @($runs | Where-Object { $_.kind -eq 'ai' -and -not $_.usageKnown }).Count
    }
    byStatus = (Group-Count $runs { param($r) $r.status })
    byTask = (Group-Count $runs { param($r) $r.task })
    verdicts = (Group-Count $allVerdicts { param($v) $v })
    models = $models
    interventionsByState = (Group-Count $interventions { param($i) $i.state })
    harvest = $harvest
    lint = $lint
    runs = @($runs)
    interventions = @($interventions)
}

# ---------------------------------------------------------------- output

$json = $report | ConvertTo-Json -Depth 12
Write-Utf8NoBom (Join-Path $OutDir 'run-report.json') ($json + "`n")
$tpl = Join-Path $PSScriptRoot 'run-report.html'
$htmlPath = Join-Path $OutDir 'run-report.html'
if (Test-Path $tpl) {
    $html = [System.IO.File]::ReadAllText($tpl)
    # '</' trong JSON (vd '</script>' trong reason) se dong the script som.
    $html = $html.Replace('/*__DATA__*/null', ($report | ConvertTo-Json -Depth 12 -Compress).Replace('</', '<\/'))
    Write-Utf8NoBom $htmlPath $html
}

$t = $report.totals
Write-Host ("run-report: {0} run ({1} AI, {2} script), {3} PASS" -f $t.runs, $t.aiRuns, $t.scriptRuns, $t.pass)
Write-Host ("  token: {0:N0} (implementer {1:N0} + reviewer {2:N0}), tien reviewer: `${3}, thoi gian: {4:N1} phut" -f $t.tokens, $t.implTokens, $t.reviewTokens, $t.costUsd, ($t.durationMs / 60000))
if ($scriptPass.Count) { Write-Host ("  script verify PASS {0} lan -> uoc tiet kiem ~{1:N0} token, ~`${2}" -f $scriptPass.Count, $t.savedTokensEst, $t.savedCostUsdEst) }
Write-Host ("  trang thai: " + (($report.byStatus.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ', '))
if ($interventions.Count) { Write-Host ("  can nguoi: " + (($report.interventionsByState.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ', ')) }
if ($t.usageUnknownRuns) { Write-Host "  ($($t.usageUnknownRuns) run AI cu chua ghi usage - runner truoc 2026-09-30)" }
Write-Host "  -> $(Get-RelPath $htmlPath)"
if (-not $NoOpen -and (Test-Path $htmlPath) -and $env:OS -eq 'Windows_NT') { Start-Process $htmlPath | Out-Null }
