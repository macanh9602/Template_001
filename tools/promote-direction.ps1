<#
.SYNOPSIS
  Promote visual direction theo section (quyet dinh cua PO): proposal -> handoff/visual/direction.vNNN.json + CURRENT.

.DESCRIPTION
  Lab / agent chi duoc xuat proposal (status DRAFT/CANDIDATE) vao handoff/visual/proposals/.
  Script nay la con duong DUY NHAT tao section APPROVED:
    - lay direction hien hanh (CURRENT) lam nen, thay cac section duoc chon bang section cua proposal (status APPROVED),
    - section khong chon: giu nguyen tu nen; nen chua co thi lay tu proposal voi status CANDIDATE,
    - profileMap gop (proposal ghi de), version +1, file cu giu nguyen lam lich su,
    - ghi handoff/visual/CURRENT va mot dong vao handoff/visual/promotions.jsonl.
  Sau do trong Unity: Tools/Visual Direction/Dry Run (xem drift) -> Import approved sections.

.EXAMPLE
  .\tools\promote-direction.ps1 -Status
  .\tools\promote-direction.ps1 -Proposal handoff\visual\proposals\cozy-b.json -Sections look -DryRun
  .\tools\promote-direction.ps1 -Proposal handoff\visual\proposals\cozy-b.json -Sections look,motion -By "Ducan"
  .\tools\promote-direction.ps1 -Migrate handoff\wp-004\visual-target.json -Out handoff\visual\proposals\wp004-legacy.json
#>
[CmdletBinding(DefaultParameterSetName = 'Status')]
param(
    [Parameter(ParameterSetName = 'Promote', Mandatory = $true)][string]$Proposal,
    [Parameter(ParameterSetName = 'Promote', Mandatory = $true)][string[]]$Sections,
    [Parameter(ParameterSetName = 'Promote')][string]$By = 'PO',
    [Parameter(ParameterSetName = 'Promote')][switch]$DryRun,
    [Parameter(ParameterSetName = 'Migrate', Mandatory = $true)][string]$Migrate,
    [Parameter(ParameterSetName = 'Migrate')][string]$Out,
    [Parameter(ParameterSetName = 'Status')][switch]$Status,
    [string]$Root
)

$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1: $PSScriptRoot rong trong gia tri mac dinh cua param -> tinh o day.
if (-not $Root) { $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
$Root = [System.IO.Path]::GetFullPath($Root)
$Schema = 'visual-direction/v1'
$VisualDir = Join-Path $Root 'handoff/visual'
$PointerPath = Join-Path $VisualDir 'CURRENT'
$AllowedStatus = @('DRAFT', 'CANDIDATE', 'APPROVED', 'SUPERSEDED')

function Write-Utf8NoBom([string]$Path, [string]$Text) {
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding $false))
}

function Resolve-RepoPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return (Join-Path $Root $Path)
}

function Get-RelPath([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $base = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if ($full.StartsWith($base, [System.StringComparison]::OrdinalIgnoreCase)) { $full = $full.Substring($base.Length) }
    return ($full -replace '\\', '/')
}

# PSCustomObject (ConvertFrom-Json) -> [ordered] de sua va ghi lai giu thu tu khoa.
function ConvertTo-Ordered($Value) {
    if ($null -eq $Value) { return $null }
    if ($Value -is [System.Management.Automation.PSCustomObject]) {
        $h = [ordered]@{}
        foreach ($p in $Value.PSObject.Properties) { $h[$p.Name] = ConvertTo-Ordered $p.Value }
        return $h
    }
    if ($Value -is [System.Collections.IDictionary]) {
        $h = [ordered]@{}
        foreach ($k in $Value.Keys) { $h[$k] = ConvertTo-Ordered $Value[$k] }
        return $h
    }
    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) {
        return , @($Value | ForEach-Object { ConvertTo-Ordered $_ })
    }
    return $Value
}

function Read-JsonFile([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { throw "khong thay file: $Path" }
    $text = [System.IO.File]::ReadAllText($Path).TrimStart([char]0xFEFF)
    return (ConvertTo-Ordered ($text | ConvertFrom-Json))
}

function Write-JsonFile([string]$Path, $Object) { Write-Utf8NoBom $Path (($Object | ConvertTo-Json -Depth 30) + "`n") }

function Get-CurrentDirection {
    if (-not (Test-Path -LiteralPath $PointerPath)) { return $null }
    $rel = ([System.IO.File]::ReadAllText($PointerPath)).Trim().TrimStart([char]0xFEFF)
    if (-not $rel) { return $null }
    $path = Resolve-RepoPath $rel
    if (-not (Test-Path -LiteralPath $path)) { throw "CURRENT tro toi '$rel' nhung file khong ton tai" }
    $doc = Read-JsonFile $path
    if ($doc['schema'] -ne $Schema) { throw "$rel co schema '$($doc['schema'])', can '$Schema'" }
    return [pscustomobject]@{ Path = $path; Doc = $doc }
}

function Get-SectionValueCount($Section) {
    if ($Section -and $Section['values'] -is [System.Collections.IDictionary]) { return $Section['values'].Count }
    return 0
}

function Compare-SectionValues($Old, $New) {
    # So key/gia tri cua values: tra ve so key them / doi / bo.
    $o = if ($Old -and $Old['values']) { $Old['values'] } else { [ordered]@{} }
    $n = if ($New -and $New['values']) { $New['values'] } else { [ordered]@{} }
    $added = 0; $changed = 0; $removed = 0
    foreach ($k in $n.Keys) {
        if (-not $o.Contains($k)) { $added++ }
        elseif ((ConvertTo-Json $o[$k] -Compress) -ne (ConvertTo-Json $n[$k] -Compress)) { $changed++ }
    }
    foreach ($k in $o.Keys) { if (-not $n.Contains($k)) { $removed++ } }
    return "+$added ~$changed -$removed"
}

function Show-Status {
    $cur = Get-CurrentDirection
    if (-not $cur) {
        Write-Host "Chua co direction nao duoc promote ($(Get-RelPath $PointerPath) chua co)."
        Write-Host 'Lab xuat proposal vao handoff/visual/proposals/, roi: promote-direction.ps1 -Proposal <file> -Sections look'
        return
    }
    $d = $cur.Doc
    Write-Host "CURRENT -> $(Get-RelPath $cur.Path)  (v$($d['version']), proposal: $($d['proposal']))"
    foreach ($name in $d['sections'].Keys) {
        $s = $d['sections'][$name]
        $who = if ($s['approvedAt']) { "  $($s['approvedBy']) $($s['approvedAt'])" } else { '' }
        Write-Host ("  {0,-8} {1,-10} {2,3} gia tri{3}" -f $name, $s['status'], (Get-SectionValueCount $s), $who)
    }
}

# ---------------------------------------------------------------- Migrate (schema cu *visual-target/v1 -> proposal)

function Convert-LegacyTarget($Legacy, [string]$SourceRel) {
    $known = @('schema', 'source', 'look', 'motion', 'motionMetrics', 'fx', 'lockedGroups', 'referenceLevel', 'notes', 'profileMap', 'meshBrief')
    $look = [ordered]@{ status = 'CANDIDATE' }
    if ($Legacy['lockedGroups']) { $look['lockedGroups'] = $Legacy['lockedGroups'] }
    $look['values'] = if ($Legacy['look']) { $Legacy['look'] } else { [ordered]@{} }
    $motion = [ordered]@{ status = 'CANDIDATE'; values = $(if ($Legacy['motion']) { $Legacy['motion'] } else { [ordered]@{} }) }
    if ($Legacy['motionMetrics']) { $motion['metrics'] = $Legacy['motionMetrics'] }
    $fx = [ordered]@{ status = 'CANDIDATE'; values = [ordered]@{} }
    if ($Legacy['fx'] -is [System.Collections.IDictionary]) { foreach ($k in $Legacy['fx'].Keys) { $fx[$k] = $Legacy['fx'][$k] } }
    $legacyRest = [ordered]@{ schema = $Legacy['schema'] }
    foreach ($k in $Legacy.Keys) { if ($known -notcontains $k) { $legacyRest[$k] = $Legacy[$k] } }
    $doc = [ordered]@{
        schema = $Schema
        version = 0
        source = $(if ($Legacy['source']) { $Legacy['source'] } else { $SourceRel })
        migratedFrom = $SourceRel
    }
    if ($Legacy['referenceLevel']) { $doc['referenceLevel'] = $Legacy['referenceLevel'] }
    if ($Legacy['notes']) { $doc['notes'] = $Legacy['notes'] }
    $doc['sections'] = [ordered]@{ look = $look; motion = $motion; fx = $fx }
    $doc['profileMap'] = if ($Legacy['profileMap']) { $Legacy['profileMap'] } else { [ordered]@{} }
    if ($Legacy['meshBrief']) { $doc['meshBrief'] = $Legacy['meshBrief'] }
    $doc['legacy'] = $legacyRest
    return $doc
}

# ---------------------------------------------------------------- main

if ($PSCmdlet.ParameterSetName -eq 'Status') { Show-Status; exit 0 }

if ($PSCmdlet.ParameterSetName -eq 'Migrate') {
    $src = Resolve-RepoPath $Migrate
    $legacy = Read-JsonFile $src
    if ([string]$legacy['schema'] -notmatch 'visual-target/v\d+$') { throw "schema '$($legacy['schema'])' khong phai *visual-target/vN" }
    $doc = Convert-LegacyTarget $legacy (Get-RelPath $src)
    if (-not $Out) { $Out = Join-Path $VisualDir ("proposals/" + [System.IO.Path]::GetFileNameWithoutExtension($src) + '-migrated.json') }
    $outPath = Resolve-RepoPath $Out
    Write-JsonFile $outPath $doc
    Write-Host "proposal -> $(Get-RelPath $outPath)  (moi section = CANDIDATE; lockedGroups cu nam o look.lockedGroups)"
    Write-Host "Section nao PO da chot thi promote: .\tools\promote-direction.ps1 -Proposal $(Get-RelPath $outPath) -Sections look"
    exit 0
}

# Promote
$propPath = Resolve-RepoPath $Proposal
$prop = Read-JsonFile $propPath
if ($prop['schema'] -ne $Schema) {
    throw "proposal co schema '$($prop['schema'])', can '$Schema'. File schema cu: chay -Migrate truoc."
}
if (-not ($prop['sections'] -is [System.Collections.IDictionary])) { throw 'proposal thieu sections' }
$Sections = @($Sections | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
foreach ($name in $Sections) {
    if (-not $prop['sections'].Contains($name)) { throw "proposal khong co section '$name' (co: $(@($prop['sections'].Keys) -join ', '))" }
}
foreach ($name in $prop['sections'].Keys) {
    $st = [string]$prop['sections'][$name]['status']
    if ($AllowedStatus -notcontains $st) { throw "section '$name' co status '$st' khong hop le" }
    if ($st -eq 'APPROVED') { Write-Warning "proposal tu ghi '$name' = APPROVED; lab/agent khong duoc duyet thay PO. Coi nhu CANDIDATE." }
}

$cur = Get-CurrentDirection
$base = if ($cur) { $cur.Doc } else { $null }
$version = if ($base) { [int]$base['version'] + 1 } else { 1 }
$today = (Get-Date).ToString('yyyy-MM-dd')

$new = [ordered]@{ schema = $Schema }
if ($prop['project']) { $new['project'] = $prop['project'] } elseif ($base -and $base['project']) { $new['project'] = $base['project'] }
$new['version'] = $version
$new['proposal'] = Get-RelPath $propPath
$new['supersedes'] = if ($cur) { Get-RelPath $cur.Path } else { $null }
foreach ($k in @('source', 'referenceLevel', 'intent', 'notes')) {
    if ($prop.Contains($k) -and $null -ne $prop[$k]) { $new[$k] = $prop[$k] } elseif ($base -and $base.Contains($k)) { $new[$k] = $base[$k] }
}

$sectionsOut = [ordered]@{}
$names = New-Object System.Collections.Generic.List[string]
if ($base) { foreach ($n in $base['sections'].Keys) { $names.Add($n) } }
foreach ($n in $prop['sections'].Keys) { if (-not $names.Contains($n)) { $names.Add($n) } }
$report = New-Object System.Collections.Generic.List[string]
foreach ($name in $names) {
    $old = if ($base -and $base['sections'].Contains($name)) { $base['sections'][$name] } else { $null }
    $fromProp = if ($prop['sections'].Contains($name)) { $prop['sections'][$name] } else { $null }
    if ($Sections -contains $name) {
        $s = ConvertTo-Ordered $fromProp
        $s['status'] = 'APPROVED'
        $s['approvedBy'] = $By
        $s['approvedAt'] = $today
        $sectionsOut[$name] = $s
        $oldStatus = if ($old) { $old['status'] } else { '-' }
        $report.Add(("  {0,-8} {1} -> APPROVED  values {2}" -f $name, $oldStatus, (Compare-SectionValues $old $s)))
    } elseif ($old) {
        $sectionsOut[$name] = $old
        $report.Add(("  {0,-8} giu nguyen ({1})" -f $name, $old['status']))
    } else {
        $s = ConvertTo-Ordered $fromProp
        if ($s['status'] -eq 'APPROVED') { $s['status'] = 'CANDIDATE' }
        $sectionsOut[$name] = $s
        $report.Add(("  {0,-8} moi, {1} (chua duyet)" -f $name, $s['status']))
    }
}
$new['sections'] = $sectionsOut

$map = [ordered]@{}
if ($base -and $base['profileMap']) { foreach ($k in $base['profileMap'].Keys) { $map[$k] = $base['profileMap'][$k] } }
if ($prop['profileMap']) { foreach ($k in $prop['profileMap'].Keys) { $map[$k] = $prop['profileMap'][$k] } }
$new['profileMap'] = $map
$unmapped = New-Object System.Collections.Generic.List[string]
foreach ($name in $Sections) {
    $vals = $sectionsOut[$name]['values']
    if ($vals -is [System.Collections.IDictionary]) { foreach ($k in $vals.Keys) { if (-not $map.Contains($k)) { $unmapped.Add("$name.$k") } } }
}
foreach ($k in @('tolerance', 'meshBrief')) {
    if ($prop.Contains($k) -and $null -ne $prop[$k]) { $new[$k] = $prop[$k] } elseif ($base -and $base.Contains($k)) { $new[$k] = $base[$k] }
}

$outPath = Join-Path $VisualDir ('direction.v{0:D3}.json' -f $version)
Write-Host "PROMOTE $(Get-RelPath $propPath) -> $(Get-RelPath $outPath)  (v$version, boi $By)"
$report | ForEach-Object { Write-Host $_ }
if ($unmapped.Count) {
    Write-Warning ("khong co trong profileMap (importer se bo qua): " + ($unmapped -join ', '))
}
if ($DryRun) { Write-Host '(dry run: chua ghi gi)'; exit 0 }
if (Test-Path -LiteralPath $outPath) { throw "$(Get-RelPath $outPath) da ton tai; direction vN la bat bien" }

Write-JsonFile $outPath $new
Write-Utf8NoBom $PointerPath ((Get-RelPath $outPath) + "`n")
$log = [ordered]@{ at = (Get-Date).ToString('s'); version = $version; by = $By; proposal = (Get-RelPath $propPath); sections = @($Sections); file = (Get-RelPath $outPath) }
$logPath = Join-Path $VisualDir 'promotions.jsonl'
[System.IO.File]::AppendAllText($logPath, (($log | ConvertTo-Json -Compress) + "`n"), (New-Object System.Text.UTF8Encoding $false))
Write-Host "CURRENT -> $(Get-RelPath $outPath)"
Write-Host 'Tiep theo (Unity): Tools/Visual Direction/Dry Run (log drift) -> Import approved sections.'
if ($cur) { Write-Host 'Task nao can chay lai: .\tools\direction-delta.ps1' }
Write-Host "Ghi decision vao Docs/decision-log.md neu day la quyet dinh cap project (vd khoa art direction)."
