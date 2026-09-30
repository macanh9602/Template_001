<#
.SYNOPSIS
  Target delta (V2): direction vN -> vN+1 doi gi, Profile nao can Import lai, task nao STALE can chay lai.

.DESCRIPTION
  So hai file visual-direction/v1 (mac dinh: CURRENT voi ban no supersedes):
    - moi section: status doi, key them / doi / bo trong values (kem profileMap -> asset.field), metrics motion doi;
    - asset can Import lai (VisualProfile, MotionProfile...) va Mesh.* can lam lai asset;
    - task co targetRef trong handoff/visual/: lay run DONE gan nhat, xem no da chay voi direction nao.
      Direction do khac dich va phan task quan tam (#section, khong co = moi section) co doi => STALE.
  Khong goi AI, khong ghi gi tru -Out. In lenh run-batch cho task STALE.

.EXAMPLE
  .\tools\direction-delta.ps1
  .\tools\direction-delta.ps1 -From handoff\visual\direction.v001.json -To handoff\visual\direction.v002.json
  .\tools\direction-delta.ps1 -Out handoff\visual\delta-latest.json
#>
[CmdletBinding()]
param(
    [string]$From,
    [string]$To,
    [string]$Out,
    [string]$Root
)

$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1: $PSScriptRoot rong trong gia tri mac dinh cua param -> tinh o day.
if (-not $Root) { $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
$Root = [System.IO.Path]::GetFullPath($Root)
$Pointer = Join-Path $Root 'handoff/visual/CURRENT'

function Get-RelPath([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $base = $Root.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if ($full.StartsWith($base, [System.StringComparison]::OrdinalIgnoreCase)) { $full = $full.Substring($base.Length) }
    return ($full -replace '\\', '/')
}

function Read-JsonFile([string]$Path) { return ([System.IO.File]::ReadAllText($Path).TrimStart([char]0xFEFF) | ConvertFrom-Json) }

function Resolve-Direction([string]$Ref) {
    if (-not $Ref) { return $null }
    $r = ($Ref -split '#')[0]
    if ($r -match '^v?(\d+)$') { $r = 'handoff/visual/direction.v{0:D3}.json' -f [int]$Matches[1] }
    if ($r -eq 'handoff/visual/CURRENT' -or $r -eq 'CURRENT') { $r = ([System.IO.File]::ReadAllText($Pointer)).Trim().TrimStart([char]0xFEFF) }
    $full = if ([System.IO.Path]::IsPathRooted($r)) { $r } else { Join-Path $Root $r }
    if (-not (Test-Path -LiteralPath $full)) { throw "khong thay direction '$Ref' ($full)" }
    return $full
}

function Get-Props($Obj) {
    if ($null -eq $Obj) { return @() }
    return @($Obj.PSObject.Properties | ForEach-Object { $_.Name })
}

function Get-Json($Value) { if ($null -eq $Value) { return 'null' } return ($Value | ConvertTo-Json -Depth 20 -Compress) }

# Delta giua hai direction: list cac muc doi, moi muc {section, kind, key, old, new, target}.
function Compare-Directions($A, $B) {
    $changes = New-Object System.Collections.Generic.List[object]
    $map = @{}
    foreach ($d in @($A, $B)) { foreach ($k in (Get-Props $d.profileMap)) { $map[$k] = [string]$d.profileMap.$k } }
    $sections = @((Get-Props $A.sections) + (Get-Props $B.sections) | Sort-Object -Unique)
    foreach ($sec in $sections) {
        $sa = $A.sections.$sec; $sb = $B.sections.$sec
        if ([string]$sa.status -ne [string]$sb.status) {
            $changes.Add([pscustomobject]@{ section = $sec; kind = 'status'; key = '-'; old = [string]$sa.status; new = [string]$sb.status; target = '' })
        }
        $va = $sa.values; $vb = $sb.values
        foreach ($k in @((Get-Props $va) + (Get-Props $vb) | Sort-Object -Unique)) {
            $ja = if ((Get-Props $va) -contains $k) { Get-Json $va.$k } else { $null }
            $jb = if ((Get-Props $vb) -contains $k) { Get-Json $vb.$k } else { $null }
            if ($ja -eq $jb) { continue }
            $kind = if ($null -eq $ja) { 'added' } elseif ($null -eq $jb) { 'removed' } else { 'changed' }
            $changes.Add([pscustomobject]@{ section = $sec; kind = $kind; key = $k; old = $ja; new = $jb; target = $(if ($map.ContainsKey($k)) { $map[$k] } else { '(khong co trong profileMap)' }) })
        }
        $ma = $sa.metrics; $mb = $sb.metrics
        foreach ($demo in @((Get-Props $ma) + (Get-Props $mb) | Sort-Object -Unique)) {
            $ja = Get-Json $ma.$demo; $jb = Get-Json $mb.$demo
            if ($ja -ne $jb) { $changes.Add([pscustomobject]@{ section = $sec; kind = 'metrics'; key = $demo; old = ''; new = ''; target = 'MotionParity' }) }
        }
        # Cac khoi khac cua section (fx cues/effects/budget...): chi bao co doi.
        foreach ($k in @((Get-Props $sa) + (Get-Props $sb) | Sort-Object -Unique)) {
            if (@('status', 'values', 'metrics', 'approvedBy', 'approvedAt', 'lockedGroups') -contains $k) { continue }
            if ((Get-Json $sa.$k) -ne (Get-Json $sb.$k)) { $changes.Add([pscustomobject]@{ section = $sec; kind = 'block'; key = $k; old = ''; new = ''; target = '' }) }
        }
    }
    return $changes.ToArray()
}

# ---------------------------------------------------------------- delta

if (-not $To -and -not (Test-Path -LiteralPath $Pointer)) {
    Write-Host 'Chua co direction nao duoc promote (handoff/visual/CURRENT chua co): chua co gi de so.'
    Write-Host 'Promote lan dau: .\tools\promote-direction.ps1 -Proposal handoff\visual\proposals\<id>.json -Sections look'
    exit 0
}
$toPath = Resolve-Direction $(if ($To) { $To } else { 'CURRENT' })
$toDoc = Read-JsonFile $toPath
$fromPath = if ($From) { Resolve-Direction $From } elseif ($toDoc.supersedes) { Resolve-Direction ([string]$toDoc.supersedes) } else { $null }
if (-not $fromPath) { Write-Host "$(Get-RelPath $toPath) la direction dau tien (khong co supersedes): chua co gi de so."; exit 0 }
$fromDoc = Read-JsonFile $fromPath

$delta = @(Compare-Directions $fromDoc $toDoc)
Write-Host "DELTA $(Get-RelPath $fromPath) (v$($fromDoc.version)) -> $(Get-RelPath $toPath) (v$($toDoc.version)): $($delta.Count) thay doi"
foreach ($c in $delta) {
    $detail = switch ($c.kind) {
        'status' { "$($c.old) -> $($c.new)" }
        'changed' { "$($c.old) -> $($c.new)  => $($c.target)" }
        'added' { "+ $($c.new)  => $($c.target)" }
        'removed' { "- $($c.old)" }
        'metrics' { 'metrics doi => MotionParity can chay lai' }
        default { 'doi' }
    }
    Write-Host ("  {0,-7} {1,-8} {2,-14} {3}" -f $c.section, $c.kind, $c.key, $detail)
}

$assets = @($delta | Where-Object { $_.target -match '^[A-Za-z_]\w*\.' -and $_.target -notmatch '^Mesh\.' } | ForEach-Object { ($_.target -split '\.')[0] } | Sort-Object -Unique)
$meshes = @($delta | Where-Object { $_.target -match '^Mesh\.' } | ForEach-Object { $_.target } | Sort-Object -Unique)
if ($assets.Count) { Write-Host "Import lai (Tools/Visual Direction/Import approved sections): $($assets -join ', ')" }
if ($meshes.Count) { Write-Host "Asset can lam lai (Blender -> asset-intake): $($meshes -join ', ')" }

# ---------------------------------------------------------------- tasks

$tasks = New-Object System.Collections.Generic.List[object]
$toRel = Get-RelPath $toPath
foreach ($tf in @(Get-ChildItem -Path (Join-Path $Root 'handoff') -Recurse -File -Filter '*.json' -ErrorAction SilentlyContinue |
            Where-Object { (Split-Path -Leaf (Split-Path -Parent $_.FullName)) -eq 'tasks' })) {
    try { $t = Read-JsonFile $tf.FullName } catch { continue }
    if ($t.schema -ne 'task/v1' -or -not ([string]$t.targetRef).StartsWith('handoff/visual/')) { continue }
    $frag = if ([string]$t.targetRef -match '#(.+)$') { $Matches[1] } else { $null }
    $runsDir = Join-Path (Split-Path -Parent (Split-Path -Parent $tf.FullName)) "runs/$($t.id)"
    $used = $null; $usedRun = $null
    if (Test-Path -LiteralPath $runsDir) {
        foreach ($rd in @(Get-ChildItem -LiteralPath $runsDir -Directory | Where-Object { $_.Name -match '^\d{8}-\d{6}$' } | Sort-Object Name -Descending)) {
            $st = $null
            foreach ($f in @('status.json', 'progress.json')) { $p = Join-Path $rd.FullName $f; if (Test-Path $p) { try { $st = Read-JsonFile $p; break } catch { } } }
            if (-not $st -or $st.status -ne 'DONE_PENDING_FEEL') { continue }
            $ref = [string]$st.targetRef
            if (-not $ref) {
                $res = @(Get-ChildItem -LiteralPath $rd.FullName -Filter 'r*.result.json' | Sort-Object Name)
                if ($res.Count) { $ref = [string](Read-JsonFile $res[-1].FullName).targetRef }
            }
            $used = $ref; $usedRun = Get-RelPath $rd.FullName
            break
        }
    }
    $state = 'CHUA PASS'
    $why = 'chua co run DONE'
    if ($usedRun) {
        $usedFile = ($used -split '#')[0]
        if (-not $usedFile -or $usedFile -eq 'handoff/visual/CURRENT') {
            $state = 'KHONG RO'; $why = "run $usedRun khong ghi direction cu the (runner cu); chay lai de chac"
        } elseif ($usedFile -eq $toRel) {
            $state = 'OK'; $why = "da chay voi $toRel"
        } else {
            $usedPath = Join-Path $Root $usedFile
            $rel = if (Test-Path -LiteralPath $usedPath) { @(Compare-Directions (Read-JsonFile $usedPath) $toDoc) } else { @() }
            $hit = @($rel | Where-Object { -not $frag -or $_.section -eq $frag })
            if ($hit.Count) { $state = 'STALE'; $why = "chay voi $usedFile; $($hit.Count) thay doi o $(if ($frag) { $frag } else { 'direction' })" }
            else { $state = 'OK'; $why = "chay voi $usedFile; phan '$frag' khong doi" }
        }
    }
    $tasks.Add([pscustomobject]@{ id = [string]$t.id; path = (Get-RelPath $tf.FullName); targetRef = [string]$t.targetRef; state = $state; why = $why; lastRun = $usedRun })
}

if ($tasks.Count) {
    Write-Host 'Task dung visual direction:'
    foreach ($t in $tasks) { Write-Host ("  {0,-9} {1,-28} {2}" -f $t.state, $t.id, $t.why) }
    $stale = @($tasks | Where-Object { $_.state -in @('STALE', 'KHONG RO') })
    if ($stale.Count) {
        Write-Host 'Chay lai:'
        Write-Host ("  .\tools\run-batch.ps1 -Tasks " + (($stale | ForEach-Object { $_.path -replace '/', '\' }) -join ',') + " -Plan")
    }
}

if ($Out) {
    $obj = [ordered]@{ schema = 'direction-delta/v1'; from = (Get-RelPath $fromPath); to = $toRel; changes = $delta; reimport = $assets; meshes = $meshes; tasks = $tasks.ToArray() }
    $outPath = if ([System.IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $Root $Out }
    [System.IO.File]::WriteAllText($outPath, (($obj | ConvertTo-Json -Depth 8) + "`n"), (New-Object System.Text.UTF8Encoding $false))
    Write-Host "-> $(Get-RelPath $outPath)"
}
