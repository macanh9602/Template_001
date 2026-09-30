<#
.SYNOPSIS
  Mo game moi tu Template_001 bang mot lenh: dien danh tinh project (ten, code, namespace, bundle id), don phan chi cua Template.

.DESCRIPTION
  Chay MOT lan, ngay sau khi copy/clone Template sang repo game moi. Chi lam viec co hoc, khong quyet dinh thiet ke:
    - ProjectSettings.asset: productName, companyName, applicationIdentifier (Android / iPhone / Standalone).
    - Docs/*.md, handoff/ROADMAP.md, handoff/START-PROMPT.md: '[TEN GAME]' -> ten game.
    - Docs/project-context.md: Internal code, Namespace root, Unity version (doc tu ProjectVersion.txt).
    - Xoa handoff/template-v2/ (task verify cua chinh Template) va handoff/visual/CURRENT neu lo co.
  Sau do chay template-lint (va doctor neu co -Doctor). Phan con lai cua Docs/project-context.md
  (genre, authoring mode, script root...) van do nguoi + playbooks/p1-bootstrap.md chot.

.EXAMPLE
  .\tools\new-game.ps1 -Name "Block Home" -Code in042-blockhome -Namespace BlockHome -Company AMZG -BundleId com.amzg.blockhome -DryRun
  .\tools\new-game.ps1 -Name "Block Home" -Code in042-blockhome -Namespace BlockHome -Company AMZG -BundleId com.amzg.blockhome -Doctor
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [Parameter(Mandatory = $true)][string]$Code,
    [Parameter(Mandatory = $true)][string]$Namespace,
    [Parameter(Mandatory = $true)][string]$BundleId,
    [string]$Company,
    [switch]$DryRun,
    [switch]$Doctor,
    [string]$Root
)

$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1: $PSScriptRoot rong trong gia tri mac dinh cua param -> tinh o day.
if (-not $Root) { $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
$Root = [System.IO.Path]::GetFullPath($Root)

if ($Namespace -notmatch '^[A-Z][A-Za-z0-9]*(\.[A-Z][A-Za-z0-9]*)*$') { throw "Namespace '$Namespace' phai PascalCase (vd BlockHome)" }
if ($BundleId -notmatch '^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*){2,}$') { throw "BundleId '$BundleId' phai dang com.company.game (chu thuong)" }
if ($Code -notmatch '^[a-z0-9][a-z0-9-]*$') { throw "Code '$Code' chi gom chu thuong, so, '-' (vd in042-blockhome)" }

# Placeholder co dau tieng Viet; dung ma ky tu de file .ps1 van ASCII (Windows PowerShell 5.1).
$TitlePlaceholder = '[T' + [char]0x00CA + 'N GAME]'
$CodePlaceholder = '`[in0xx-t' + [char]0x00EA + 'n]`'

$utf8 = New-Object System.Text.UTF8Encoding $false
$changes = New-Object System.Collections.Generic.List[string]

function Update-File([string]$Rel, [scriptblock]$Transform) {
    $path = Join-Path $Root $Rel
    if (-not (Test-Path -LiteralPath $path)) { return }
    $old = [System.IO.File]::ReadAllText($path)
    $new = & $Transform $old
    if ($new -ne $old) {
        if (-not $changes.Contains($Rel)) { $changes.Add($Rel) }
        if (-not $DryRun) { [System.IO.File]::WriteAllText($path, $new, $utf8) }
    }
}

# ---------------------------------------------------------------- ProjectSettings

$companyValue = if ($Company) { $Company } else { $null }
Update-File 'ProjectSettings/ProjectSettings.asset' {
    param($t)
    $t = [regex]::Replace($t, '(?m)^(  productName: ).*$', { param($m) $m.Groups[1].Value + $Name })
    if ($companyValue) { $t = [regex]::Replace($t, '(?m)^(  companyName: ).*$', { param($m) $m.Groups[1].Value + $companyValue }) }
    # Chi sua trong khoi applicationIdentifier.
    # Khong dung (?s): '.' khong duoc vuot dong, neu khong se an sang khoi buildNumber / scriptingDefineSymbols.
    $t = [regex]::Replace($t, '(?m)^(  applicationIdentifier:\r?\n)((?:    [^\s:]+: [^\r\n]*\r?\n)+)', {
            param($m)
            $body = [regex]::Replace($m.Groups[2].Value, '(?m)^(    (?:Android|iPhone|Standalone): )[^\r\n]*', { param($x) $x.Groups[1].Value + $BundleId })
            $m.Groups[1].Value + $body
        })
    return $t
}

# ---------------------------------------------------------------- Docs + prompts

$titleFiles = @(Get-ChildItem -Path (Join-Path $Root 'Docs') -Filter '*.md' -File | ForEach-Object { "Docs/$($_.Name)" }) + @('handoff/ROADMAP.md', 'handoff/START-PROMPT.md')
foreach ($rel in $titleFiles) { Update-File $rel { param($t) $t.Replace($TitlePlaceholder, $Name) } }

$unityVersion = $null
$pv = Join-Path $Root 'ProjectSettings/ProjectVersion.txt'
if (Test-Path $pv) { $m = [regex]::Match([System.IO.File]::ReadAllText($pv), '(?m)^m_EditorVersion:\s*(\S+)'); if ($m.Success) { $unityVersion = $m.Groups[1].Value } }

Update-File 'Docs/project-context.md' {
    param($t)
    $t = $t.Replace($CodePlaceholder, '`' + $Code + '`')
    $t = [regex]::Replace($t, '(?m)^(\| Namespace root `\[GameRoot\]` \|)\s*\|', { param($m) $m.Groups[1].Value + ' `' + $Namespace + '` |' })
    if ($unityVersion) { $t = [regex]::Replace($t, '(?m)^(\| Unity version \|)\s*\|', { param($m) $m.Groups[1].Value + ' ' + $unityVersion + ' |' }) }
    return $t
}

# ---------------------------------------------------------------- template-only files

foreach ($rel in @('handoff/template-v2', 'handoff/visual/CURRENT', 'handoff/_reports/run-report.html', 'handoff/_reports/run-report.json')) {
    $path = Join-Path $Root $rel
    if (Test-Path -LiteralPath $path) {
        $changes.Add("xoa $rel")
        if (-not $DryRun) { Remove-Item -LiteralPath $path -Recurse -Force }
    }
}

# ---------------------------------------------------------------- report

Write-Host "new-game: '$Name' ($Code), namespace $Namespace, bundle $BundleId$(if ($unityVersion) { ", Unity $unityVersion" })"
if ($changes.Count -eq 0) { Write-Host '  khong co gi de doi (da chay roi?)' }
$changes | ForEach-Object { Write-Host "  ~ $_" }
if ($DryRun) { Write-Host '(dry run: chua ghi gi)'; exit 0 }

$lint = Join-Path $PSScriptRoot 'template-lint.ps1'
if (Test-Path $lint) { Write-Host ''; & $lint | Out-Host }
if ($Doctor) {
    $doc = Join-Path $PSScriptRoot 'doctor.ps1'
    if (Test-Path $doc) { Write-Host ''; & $doc -SkipSmoke | Out-Host }
}

Write-Host ''
Write-Host 'Tiep theo:'
Write-Host '  1. Mo project trong Unity (lan dau import lau); Window > MCP for Unity > Start Server.'
Write-Host '  2. powershell -ExecutionPolicy Bypass -File .\tools\doctor.ps1   (lan dau tren may nay: bo -SkipSmoke)'
Write-Host '  3. Dien not Docs/project-context.md theo playbooks/p1-bootstrap.md (genre, authoring mode, script root).'
Write-Host '  4. Them he thong rieng cua game vao Docs/reuse-registry.json khi co.'
Write-Host '  5. git add -A; git commit -m "Init <game> from Template_001"'
