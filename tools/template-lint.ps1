<#
.SYNOPSIS
  Kiem loi cau truc cua Template / project dung Template (khong can Unity).

.DESCRIPTION
  Moi rule sinh ra tu mot loi da tra gia that (review 2026-09-29, pilot WP004):

    PARALLEL_ROOT   Assets/_Core co ca root danh so va root cu (4_Scripts + Scripts, 2_Models + Models...).
    ORPHAN_META     Folder .meta trong Assets/ ma folder khong ton tai (git khong luu folder rong;
                    clone ve Unity bao meta mo coi, tool tao lai root sai ten).
    ZERO_BYTE_CS    File .cs 0 byte duoc track.
    ABS_PATH        Path tuyet doi cua may (C:\..., D:/...) trong file text duoc commit.
    BOM             JSON/JSONL co UTF-8 BOM (json.load fail) - handoff/Docs/templates/tools.
    PS1_NON_ASCII   tools/*.ps1 co ky tu ngoai ASCII (Windows PowerShell 5.1 doc sai, hong parse).
    DANGLING_REF    `path` trong .md tro toi file/folder khong ton tai (tru file README.md cua folder khai la output khi chay).
    PKG_FLOATING    Git package trong Packages/manifest.json troi theo branch (#main, khong #).
    PRODUCT_NAME    Project that van mang productName cua Template.
    GITIGNORE       Thieu ignore cho file machine-local cua toolchain.
    PS1_PARSE       File .ps1/.psm1 co loi cu phap (script khong chay duoc).
    PS1_PARAM_PSSCRIPTROOT  Gia tri mac dinh cua param dung $PSScriptRoot (rong tren Windows PowerShell 5.1).
    SKILL_SYNC      Ban sao skill cua host (.claude/skills) lech skills/ (canonical).
    REUSE_BYPASS    Code khop mau 'avoid' trong Docs/reuse-registry.json (viet lai he thong co san). WARN.

  FAIL -> exit 1. WARN khong chan.

.EXAMPLE
  .\tools\template-lint.ps1
  .\tools\template-lint.ps1 -Json
#>
[CmdletBinding()]
param(
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false } catch { }
$Root = (Get-Location).Path
$Findings = New-Object System.Collections.Generic.List[object]

function Add-Finding([string]$Rule, [string]$Severity, [string]$Path, [string]$Detail) {
    $Findings.Add([pscustomobject]@{ rule = $Rule; severity = $Severity; path = $Path; detail = $Detail })
}

# File duoc track (khong quet Library/, Temp/...). Lich su/evidence khong phai contract hien tai.
$prev = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
$Tracked = @(& git -c core.quotePath=false ls-files 2>$null)
$ErrorActionPreference = $prev
if ($Tracked.Count -eq 0) { throw 'template-lint can chay trong git repo (git ls-files rong).' }
$HistoryPattern = '^(PACK-VERSION\.md|REVIEW-[^/]*\.md|handoff/[^/]+/(runs|captures|reviews)/)'

# ---------------------------------------------------------------- PARALLEL_ROOT / ORPHAN_META

$core = Join-Path $Root 'Assets/_Core'
if (Test-Path $core) {
    $pairs = @(
        @('0_Texture2D', 'Texture2D'), @('1_Materials', 'Materials'), @('2_Models', 'Models'),
        @('3_Prefabs', 'Prefabs'), @('4_Scripts', 'Scripts'), @('5_Shaders', 'Shaders'), @('6_Scenes', 'Scenes')
    )
    foreach ($p in $pairs) {
        $numbered = (Test-Path (Join-Path $core $p[0])) -or (Test-Path (Join-Path $core ($p[0] + '.meta')))
        $legacy = (Test-Path (Join-Path $core $p[1])) -or (Test-Path (Join-Path $core ($p[1] + '.meta')))
        if ($numbered -and $legacy) {
            Add-Finding 'PARALLEL_ROOT' 'FAIL' "Assets/_Core/$($p[1])" "song song voi Assets/_Core/$($p[0]); chon mot root, migrate la story rieng"
        }
    }
}
foreach ($f in $Tracked) {
    if ($f -notmatch '^Assets/.*\.meta$' -or $f -match '^Assets/Plugins/') { continue }  # plugin ben thu ba: khong phai skeleton
    $target = $f.Substring(0, $f.Length - 5)
    if (-not (Test-Path -LiteralPath (Join-Path $Root $target))) {
        # File asset thieu thi git se bao; ORPHAN_META nham vao folder rong khong duoc track.
        $metaText = Get-Content -LiteralPath (Join-Path $Root $f) -TotalCount 5 -ErrorAction SilentlyContinue | Out-String
        if ($metaText -match 'folderAsset:\s*yes') {
            Add-Finding 'ORPHAN_META' 'FAIL' $target 'folder chi co .meta; them .gitkeep vao folder (Unity bo qua dotfile)'
        }
    }
}

# ---------------------------------------------------------------- ZERO_BYTE_CS

foreach ($f in $Tracked) {
    if ($f -notmatch '\.cs$') { continue }
    $full = Join-Path $Root $f
    if ((Test-Path -LiteralPath $full) -and (Get-Item -LiteralPath $full).Length -eq 0) {
        Add-Finding 'ZERO_BYTE_CS' 'FAIL' $f 'script 0 byte: xoa (kem .meta) hoac viet noi dung'
    }
}

# ---------------------------------------------------------------- text-file rules

$textExt = '\.(md|json|jsonl|ps1|psm1|py|toml|ya?ml|txt|cs)$'
$absPattern = '(?<![A-Za-z0-9])[A-Za-z]:[\\/](?:[^\s"''`<>|]*)'
$refRoots = '^(Assets|Docs|handoff|knowledge|playbooks|skills|standards|templates|tools|workflow|config)/'

foreach ($f in $Tracked) {
    if ($f -notmatch $textExt) { continue }
    if ($f -match '^(Assets/(?!_Core/)|Packages/|ProjectSettings/)') { continue }  # plugin ben thu ba
    $full = Join-Path $Root $f
    if (-not (Test-Path -LiteralPath $full)) { continue }
    $bytes = [System.IO.File]::ReadAllBytes($full)
    $isHistory = $f -match $HistoryPattern

    if ($f -match '\.(json|jsonl)$' -and $f -match '^(handoff|Docs|templates|tools|config|skills)/' -and
        $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        Add-Finding 'BOM' 'FAIL' $f 'UTF-8 BOM; ghi lai UTF-8 khong BOM (PS 5.1: [IO.File]::WriteAllText + UTF8Encoding($false))'
    }
    if ($f -match '^(tools|templates)/.*\.ps(m?)1$') {
        foreach ($b in $bytes) { if ($b -gt 0x7F) { Add-Finding 'PS1_NON_ASCII' 'FAIL' $f 'co ky tu ngoai ASCII; PS 5.1 doc .ps1 khong BOM theo ANSI'; break } }
    }
    if ($f -match '\.ps(m?)1$') {
        # Loi cu phap lam script khong bao gio chay duoc (vd backtick markdown trong chuoi "..."); parse khong thuc thi gi.
        $parseErrors = $null
        [void][System.Management.Automation.Language.Parser]::ParseFile($full, [ref]$null, [ref]$parseErrors)
        if ($parseErrors -and $parseErrors.Count) {
            $pe = $parseErrors[0]
            Add-Finding 'PS1_PARSE' 'FAIL' "$f`:$($pe.Extent.StartLineNumber)" "khong parse duoc: $($pe.Message) ($($parseErrors.Count) loi)"
        }
    }
    if ($isHistory) { continue }

    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    $lines = $text -split "`r?`n"
    $inFence = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($f -match '\.md$' -and $line -match '^\s*```') { $inFence = -not $inFence; continue }

        # Windows PowerShell 5.1: $PSScriptRoot rong khi tinh gia tri mac dinh cua param (pwsh 7 thi khong) -> script hong chi tren may Windows.
        if ($f -match '\.ps1$' -and $line -match '^\s*(\[[^\]]+\]\s*)*\$\w+\s*=.*\$PSScriptRoot' -and $line -match '^\s*\[') {
            Add-Finding 'PS1_PARAM_PSSCRIPTROOT' 'FAIL' "$f`:$($i + 1)" 'param mac dinh dung $PSScriptRoot (rong tren PS 5.1); tinh sau param() bang $MyInvocation.MyCommand.Path'
        }
        foreach ($m in [regex]::Matches($line, $absPattern)) {
            if ($m.Value -match '^[A-Za-z]:[\\/](\.\.\.|$)') { continue }  # vi du dang C:\... trong tai lieu
            Add-Finding 'ABS_PATH' 'FAIL' "$f`:$($i + 1)" "path may cu the '$($m.Value)'; dua vao .toolchain.local.json / file *.local gitignored"
        }

        if ($f -notmatch '\.md$' -or $inFence) { continue }
        foreach ($m in [regex]::Matches($line, '`([^`\s]+)`')) {
            $ref = $m.Groups[1].Value.TrimEnd('.', ',', ':', ';', ')')
            if ($ref -notmatch $refRoots) { continue }
            if ($ref -match '[\*\?<>\[\]\{\}]|xxx|XXX|\.\.\.|/N/|<|\$') { continue }  # glob / placeholder
            # handoff/<x>/ ket thuc bang / la noi output sinh ra khi chay (vd handoff/bootstrap/).
            if ($ref -match '^handoff/.*/$') { continue }
            $refPath = ($ref -split '#')[0] -replace '/$', ''
            # File machine-local (gitignored) hop le khi co ban .example di kem.
            $example = [regex]::Replace($refPath, '(\.[^./]+)$', '.example$1')
            # File sinh ra khi chay project (vd handoff/visual/CURRENT) hop le khi README.md cua folder khai ten no trong `...`.
            $declared = $false
            $readme = Join-Path $Root ((Split-Path -Parent $refPath) + '/README.md')
            if (Test-Path -LiteralPath $readme) {
                $leaf = Split-Path -Leaf $refPath
                $pattern = '`' + [regex]::Escape($leaf).Replace('NNN', '[^`]*') + '`'
                $declared = ([System.IO.File]::ReadAllText($readme) -match $pattern)
                if (-not $declared -and $leaf -match 'NNN') { $declared = ([System.IO.File]::ReadAllText($readme) -match [regex]::Escape('`' + $leaf + '`')) }
            }
            if (-not $declared -and -not (Test-Path -LiteralPath (Join-Path $Root $refPath)) -and -not (Test-Path -LiteralPath (Join-Path $Root $example))) {
                Add-Finding 'DANGLING_REF' 'WARN' "$f`:$($i + 1)" "tro toi '$refPath' khong ton tai"
            }
        }
    }
}

# ---------------------------------------------------------------- packages / project settings / gitignore

$manifest = Join-Path $Root 'Packages/manifest.json'
if (Test-Path $manifest) {
    foreach ($m in [regex]::Matches((Get-Content $manifest -Raw), '"([^"]+)"\s*:\s*"([^"]*\.git[^"]*)"')) {
        $ref = $m.Groups[2].Value
        if ($ref -notmatch '#' -or $ref -match '#(main|master|develop)$') {
            Add-Finding 'PKG_FLOATING' 'WARN' "Packages/manifest.json" "$($m.Groups[1].Value) troi theo branch: $ref; pin theo tag/commit"
        }
    }
}

$isTemplateRepo = $false
$ctx = Join-Path $Root 'Docs/project-context.md'
if (Test-Path $ctx) { $isTemplateRepo = (Get-Content $ctx -TotalCount 1) -match '\[T' }
$ps = Join-Path $Root 'ProjectSettings/ProjectSettings.asset'
if ((Test-Path $ps) -and -not $isTemplateRepo) {
    $pm = [regex]::Match((Get-Content $ps -Raw), '(?m)^\s*productName:\s*(.+)$')
    if ($pm.Success -and $pm.Groups[1].Value.Trim() -match '^Template_\d+$') {
        Add-Finding 'PRODUCT_NAME' 'WARN' 'ProjectSettings/ProjectSettings.asset' "productName van la '$($pm.Groups[1].Value.Trim())' cua Template"
    }
}

$gi = Join-Path $Root '.gitignore'
$giText = if (Test-Path $gi) { Get-Content $gi -Raw } else { '' }
foreach ($need in @('/.toolchain/', '/.toolchain.local.json', '/Docs/asset-pipeline.json', '/.worktrees/')) {
    if ($giText -notmatch [regex]::Escape($need)) {
        Add-Finding 'GITIGNORE' 'WARN' '.gitignore' "thieu '$need' (file machine-local)"
    }
}

# ---------------------------------------------------------------- SKILL_SYNC

$syncScript = Join-Path $PSScriptRoot 'sync-skills.ps1'
if ((Test-Path (Join-Path $Root '.claude/skills')) -and (Test-Path $syncScript)) {
    $syncOut = @(& $syncScript -Check -Root $Root *>&1 | ForEach-Object { [string]$_ })
    foreach ($l in @($syncOut | Where-Object { $_ -like 'LECH *' })) {
        Add-Finding 'SKILL_SYNC' 'WARN' '.claude/skills' ($l.Substring(5) + ' -> chay tools/sync-skills.ps1')
    }
}

# ---------------------------------------------------------------- REUSE_BYPASS

$reuseModule = Join-Path $PSScriptRoot 'ReuseRegistry.psm1'
if (Test-Path $reuseModule) {
    Import-Module $reuseModule -Force
    try {
        $registry = Read-ReuseRegistry $Root
        foreach ($h in @(Find-ReuseBypass $registry $Root)) {
            Add-Finding 'REUSE_BYPASS' 'WARN' "$($h.path):$($h.line)" "$($h.system): $($h.why) | $($h.text)"
        }
    } catch {
        Add-Finding 'REUSE_BYPASS' 'FAIL' 'Docs/reuse-registry.json' $_.Exception.Message
    }
}

# ---------------------------------------------------------------- report

$fail = @($Findings | Where-Object { $_.severity -eq 'FAIL' }).Count
$warn = @($Findings | Where-Object { $_.severity -eq 'WARN' }).Count
if ($Json) {
    [ordered]@{ schema = 'lint/v1'; fail = $fail; warn = $warn; findings = $Findings } | ConvertTo-Json -Depth 4
} else {
    if ($Findings.Count) {
        $Findings | Sort-Object severity, rule, path | Format-Table severity, rule, path, detail -AutoSize -Wrap | Out-String -Width 220 | Write-Host
    }
    Write-Host "template-lint: $fail FAIL, $warn WARN$(if ($isTemplateRepo) { ' (template repo)' })"
}
exit ([int]($fail -gt 0))
