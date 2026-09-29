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
  Runner khong commit, khong push.

.EXAMPLE
  .\tools\doctor.ps1
  .\tools\run-task.ps1 -Task handoff\wp-004\tasks\WP004-B-VERIFY.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Task,
    [int]$MaxPatchRounds = 2,
    [string]$Implementer = '',
    [int]$CapabilityMaxAgeMinutes = 480,
    [ValidateSet('read-only', 'workspace-write', 'danger-full-access')][string]$CodexSandbox = 'workspace-write'
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
            rounds = $Rounds; host = $ImplHost; finishedAt = (Get-Date).ToString('o')
        })
    Write-Host ''
    Write-Host "[$($TaskObj.id)] $Status $(if ($Reason) { "- $Reason" })"
    Write-Host "run dir: $(Get-RelPath $RunDir)"
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

# ---------------------------------------------------------------- load task + capabilities

$TaskPath = (Resolve-Path $Task).Path
$TaskObj = Read-Json $TaskPath
foreach ($f in @('id', 'packet', 'implementers', 'requires', 'writeSet', 'acceptance')) {
    if ($null -eq $TaskObj.$f) { throw "task thieu field bat buoc '$f' ($TaskPath)" }
}
if (-not (Test-Path $TaskObj.packet)) { throw "packet khong ton tai: $($TaskObj.packet)" }

$WpDir = Split-Path -Parent (Split-Path -Parent $TaskPath)
$RunDir = Join-Path $WpDir ("runs/{0}/{1}" -f $TaskObj.id, (Get-Date).ToString('yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $RunDir -Force | Out-Null
$InterventionsPath = Join-Path $WpDir 'interventions.jsonl'
$RunsRel = (Get-RelPath (Join-Path $WpDir 'runs')) + '/'
$ImplHost = $null

$capPath = Join-Path $ProjectRoot '.toolchain/capabilities.json'
if (-not (Test-Path $capPath)) {
    Add-Intervention 'BLOCKED_TOOLCHAIN' 'chua co .toolchain/capabilities.json'
    Complete-Run 'BLOCKED_TOOLCHAIN' 'chay tools/doctor.ps1 truoc' 0
}
$Cap = Read-Json $capPath
$age = ((Get-Date) - [DateTime]::Parse($Cap.generatedAt)).TotalMinutes
if ($age -gt $CapabilityMaxAgeMinutes) {
    Add-Intervention 'BLOCKED_TOOLCHAIN' ("capabilities cu {0:N0} phut" -f $age)
    Complete-Run 'BLOCKED_TOOLCHAIN' 'capabilities qua cu, chay lai tools/doctor.ps1' 0
}

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
if (-not $ImplHost) {
    $why = "khong host nao trong [$($candidates -join ', ')] PASS [$(@($TaskObj.requires) -join ', ')]"
    Add-Intervention 'BLOCKED_TOOLCHAIN' $why
    Complete-Run 'BLOCKED_TOOLCHAIN' $why 0
}
if (-not (Test-HostLevels 'claude' @('INSTALLED', 'REVIEWER_READONLY'))) {
    Add-Intervention 'BLOCKED_TOOLCHAIN' 'reviewer claude chua PASS REVIEWER_READONLY'
    Complete-Run 'BLOCKED_TOOLCHAIN' 'reviewer claude chua PASS REVIEWER_READONLY' 0
}
$ImplExe = $Cap.hosts.$ImplHost.exe
$ImplMcp = $Cap.hosts.$ImplHost.unityMcpServer
$ReviewerExe = $Cap.hosts.claude.exe

$BaseCommit = (Invoke-Git @('rev-parse', 'HEAD')).Trim()
$Snapshot = Get-TreeSnapshot
$Ignore = @($RunsRel, '.toolchain/', (Get-RelPath $InterventionsPath))

Write-Host "[$($TaskObj.id)] implementer=$ImplHost reviewer=claude base=$($BaseCommit.Substring(0, 8)) run=$(Get-RelPath $RunDir)"

# ---------------------------------------------------------------- prompts

$writeSetText = (@($TaskObj.writeSet) | ForEach-Object { "- $_" }) -join "`n"
$acceptText = (@($TaskObj.acceptance) | ForEach-Object { "- [$($_.kind)] $($_.text)" }) -join "`n"
$reuseText = if ($TaskObj.mustReuse) { (@($TaskObj.mustReuse) | ForEach-Object { "- $_" }) -join "`n" } else { '- (none)' }
$targetText = if ($TaskObj.targetRef) { $TaskObj.targetRef } else { '(none)' }

function New-ImplementerPrompt([int]$Round, [string]$PrevReviewPath) {
    $patchBlock = ''
    if ($PrevReviewPath) {
        $patchBlock = @"

## Round $Round - apply review findings

An independent reviewer returned PATCH. Read ``$(Get-RelPath $PrevReviewPath)`` and fix every finding.
Do not re-litigate the findings; if one is impossible, end with RESULT: BLOCKED and say why.
"@
    }
    return @"
# Runner task $($TaskObj.id) - implementer (round $Round)

You are the IMPLEMENTER, running headless under tools/run-task.ps1. No human is watching.
Follow AGENTS.md / CLAUDE.md of this repo.

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

## Output
Reply with ONLY one JSON object, no prose, no code fence:
{"verdict":"PASS|PATCH|TARGET_RECONSIDER|BLOCKED","summary":"...","findings":[{"area":"...","observed":"...","expected":"...","evidence":"...","patch":{"field":"...","from":null,"to":null,"instruction":"..."}}],"implHypothesesRuledOut":[]}
"@
}

# ---------------------------------------------------------------- host invocation

function Invoke-Implementer([string]$PromptPath, [string]$LogPath, [string]$LastMsgPath) {
    $instruction = "Read the file $(Get-RelPath $PromptPath) and follow it exactly."
    if ($ImplHost -eq 'claude') {
        $tools = @('Read', 'Grep', 'Glob', 'Edit', 'Write')
        if ($ImplMcp) { $tools += "mcp__$ImplMcp" }
        $r = Invoke-Native $ImplExe (@('-p', $instruction, '--output-format', 'json', '--permission-mode', 'acceptEdits', '--allowedTools') + $tools) $LogPath
        $obj = Get-JsonObjectFromText $r.Text
        $text = if ($obj -and $obj.result) { [string]$obj.result } else { $r.Text }
    } else {
        $r = Invoke-Native $ImplExe @('exec', '-s', $CodexSandbox, '-o', $LastMsgPath, $instruction) $LogPath
        $text = if (Test-Path $LastMsgPath) { [System.IO.File]::ReadAllText($LastMsgPath) } else { $r.Text }
    }
    Write-Text $LastMsgPath $text
    return $text
}

function Invoke-Reviewer([string]$PromptPath, [string]$LogPath) {
    $instruction = "Read the file $(Get-RelPath $PromptPath) and follow it exactly. Output only the JSON object."
    $r = Invoke-Native $ReviewerExe @('-p', $instruction, '--output-format', 'json', '--allowedTools', 'Read', 'Grep', 'Glob') $LogPath
    $outer = Get-JsonObjectFromText $r.Text
    $session = if ($outer -and $outer.session_id) { [string]$outer.session_id } else { 'unknown' }
    $inner = $null
    if ($outer -and $outer.result) { $inner = Get-JsonObjectFromText ([string]$outer.result) }
    return [pscustomobject]@{ Review = $inner; Session = $session }
}

# ---------------------------------------------------------------- loop

$prevReview = $null
$maxRounds = $MaxPatchRounds + 1
for ($round = 1; $round -le $maxRounds; $round++) {
    $p = Join-Path $RunDir "r$round"
    Write-Host "[$($TaskObj.id)] round $round - implementer ($ImplHost) ..."
    Write-Text "$p.implementer.prompt.md" (New-ImplementerPrompt $round $prevReview)
    $msg = Invoke-Implementer "$p.implementer.prompt.md" "$p.implementer.out.txt" "$p.implementer.last.md"

    $status = 'BLOCKED'; $blocker = 'implementer khong in dong RESULT:'
    if ($msg -match '(?m)^\s*RESULT:\s*IMPLEMENTED\b') { $status = 'IMPLEMENTED'; $blocker = $null }
    elseif ($msg -match '(?m)^\s*RESULT:\s*BLOCKED:?\s*(.*)$') { $blocker = $Matches[1].Trim() }
    $evidence = @()
    if ($msg -match '(?m)^\s*EVIDENCE:\s*(.*)$') { $evidence = @($Matches[1] -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }
    $summary = ''
    if ($msg -match '(?m)^\s*SUMMARY:\s*(.*)$') { $summary = $Matches[1].Trim() }

    $changed = Get-ChangedSince $Snapshot $Ignore
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
    }
    Write-Json "$p.result.json" $result
    Write-Host "[$($TaskObj.id)] round $round - $status, $($changed.Count) file doi$(if ($blocker) { ", $blocker" })"

    if ($status -eq 'BLOCKED') {
        Add-Intervention 'BLOCKED' $blocker
        Complete-Run 'BLOCKED' $blocker $round
    }

    Write-Host "[$($TaskObj.id)] round $round - reviewer (claude, read-only) ..."
    Write-Text "$p.reviewer.prompt.md" (New-ReviewerPrompt $round "$p.result.json" "$p.diff")
    $rv = Invoke-Reviewer "$p.reviewer.prompt.md" "$p.reviewer.out.txt"
    if (-not $rv.Review -or @('PASS', 'PATCH', 'TARGET_RECONSIDER', 'BLOCKED') -notcontains [string]$rv.Review.verdict) {
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
    }
    Write-Json "$p.review.json" $review
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

Add-Intervention 'LOOP_CAP' "PATCH sau $MaxPatchRounds vong sua"
Complete-Run 'ESCALATE:LOOP_CAP' "PATCH sau $MaxPatchRounds vong sua" $maxRounds
