<#
.SYNOPSIS
  Dong bo skills/ (canonical) sang thu muc skill native cua host agent. Khong sua ban sinh ra; sua skills/ roi chay lai.

.DESCRIPTION
  skills/<name>/SKILL.md (+ refs/) la nguon duy nhat (skills/README.md). Host nao can thu muc rieng thi sinh tu day:
    claude  -> .claude/skills/<name>/   (Claude Code doc project skill o day; frontmatter name/description giu nguyen)
  Codex doc AGENTS.md -> skills/README.md truc tiep, khong can ban sao.
  Moi thu muc sinh ra co file .generated (nguon + hash) de lint/-Check biet ban sao cu hay bi sua tay.

.EXAMPLE
  .\tools\sync-skills.ps1            # sinh / cap nhat
  .\tools\sync-skills.ps1 -Check     # chi bao lech (exit 1 neu lech), dung trong CI / doctor
#>
[CmdletBinding()]
param(
    [ValidateSet('claude')][string[]]$Hosts = @('claude'),
    [switch]$Check,
    [string]$Root
)

$ErrorActionPreference = 'Stop'
if (-not $Root) { $Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
$Root = [System.IO.Path]::GetFullPath($Root)
$Src = Join-Path $Root 'skills'
$Targets = @{ claude = '.claude/skills' }

function Get-TreeHash([string]$Dir) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $sb = New-Object System.Text.StringBuilder
    foreach ($f in @(Get-ChildItem -LiteralPath $Dir -Recurse -File | Where-Object { $_.Name -ne '.generated' } | Sort-Object FullName)) {
        $rel = $f.FullName.Substring($Dir.TrimEnd('\', '/').Length + 1) -replace '\\', '/'
        $h = [BitConverter]::ToString($sha.ComputeHash([System.IO.File]::ReadAllBytes($f.FullName))) -replace '-', ''
        [void]$sb.Append("$rel $h`n")
    }
    return ([BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($sb.ToString()))) -replace '-', '').Substring(0, 16)
}

$skills = @(Get-ChildItem -LiteralPath $Src -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'SKILL.md') })
$drift = New-Object System.Collections.Generic.List[string]

foreach ($h in $Hosts) {
    $dest = Join-Path $Root $Targets[$h]
    foreach ($sk in $skills) {
        $want = Get-TreeHash $sk.FullName
        $out = Join-Path $dest $sk.Name
        $marker = Join-Path $out '.generated'
        $have = $null; $edited = $false
        if (Test-Path -LiteralPath $marker) {
            $m = [System.IO.File]::ReadAllText($marker)
            if ($m -match 'hash=(\w+)') { $have = $Matches[1] }
            $edited = ($have -and (Get-TreeHash $out) -ne $have)
        } elseif (Test-Path -LiteralPath $out) { $edited = $true }
        if ($have -eq $want -and -not $edited) { continue }
        $why = if ($edited) { 'ban sao bi sua tay / khong do tool sinh' } elseif ($have) { 'skills/ da doi' } else { 'chua sinh' }
        $drift.Add("${h}: $($Targets[$h])/$($sk.Name) ($why)")
        if ($Check) { continue }
        if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out -Recurse -Force }
        Copy-Item -LiteralPath $sk.FullName -Destination $out -Recurse
        [System.IO.File]::WriteAllText($marker, "source=skills/$($sk.Name)`nhash=$want`nDO NOT EDIT: sua skills/$($sk.Name) roi chay tools/sync-skills.ps1`n", (New-Object System.Text.UTF8Encoding $false))
    }
    # Skill da bi xoa khoi skills/ thi xoa ban sao (chi ban do tool sinh).
    if (Test-Path -LiteralPath $dest) {
        foreach ($d in @(Get-ChildItem -LiteralPath $dest -Directory)) {
            if (-not (Test-Path -LiteralPath (Join-Path $Src $d.Name)) -and (Test-Path -LiteralPath (Join-Path $d.FullName '.generated'))) {
                $drift.Add("${h}: $($Targets[$h])/$($d.Name) (skill da bi xoa khoi skills/)")
                if (-not $Check) { Remove-Item -LiteralPath $d.FullName -Recurse -Force }
            }
        }
    }
}

if ($Check) {
    if ($drift.Count) { $drift | ForEach-Object { Write-Host "LECH $_" }; Write-Host 'Chay: .\tools\sync-skills.ps1'; exit 1 }
    Write-Host "sync-skills: $($skills.Count) skill khop ($($Hosts -join ', '))"
    exit 0
}
if ($drift.Count) { $drift | ForEach-Object { Write-Host "  ~ $_" } }
Write-Host "sync-skills: $($skills.Count) skill -> $(($Hosts | ForEach-Object { $Targets[$_] }) -join ', ') ($($drift.Count) cap nhat)"
