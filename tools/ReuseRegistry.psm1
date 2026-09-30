# Reuse registry: he thong co san ma agent phai dung lai, va mau code "viet lai" can bat.
# Dung chung cho tools/template-lint.ps1 (quet ca project) va tools/run-task.ps1 (quet dong moi them cua mot round).
# Bai hoc pilot WP004: worker tu viet 4 AudioSource thay vi AudioController co san; reviewer khong bat, sua muon.
# File: Docs/reuse-registry.json (schema reuse-registry/v1). ASCII-only (Windows PowerShell 5.1).

function Read-ReuseRegistry([string]$Root) {
    $path = Join-Path $Root 'Docs/reuse-registry.json'
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    $reg = [System.IO.File]::ReadAllText($path).TrimStart([char]0xFEFF) | ConvertFrom-Json
    if ($reg.schema -ne 'reuse-registry/v1') { throw "Docs/reuse-registry.json: schema '$($reg.schema)' khac 'reuse-registry/v1'" }
    return $reg
}

# Glob kieu gitignore don gian: ** = moi thu muc, * = trong mot doan, ? = mot ky tu.
function ConvertTo-GlobRegex([string]$Glob) {
    $g = $Glob -replace '\\', '/'
    $sb = New-Object System.Text.StringBuilder '^'
    for ($i = 0; $i -lt $g.Length; $i++) {
        $c = $g[$i]
        if ($c -eq '*' -and $i + 1 -lt $g.Length -and $g[$i + 1] -eq '*') {
            if ($i + 2 -lt $g.Length -and $g[$i + 2] -eq '/') { [void]$sb.Append('(?:.*/)?'); $i += 2 } else { [void]$sb.Append('.*'); $i++ }
        } elseif ($c -eq '*') { [void]$sb.Append('[^/]*') }
        elseif ($c -eq '?') { [void]$sb.Append('[^/]') }
        else { [void]$sb.Append([regex]::Escape([string]$c)) }
    }
    [void]$sb.Append('$')
    return $sb.ToString()
}

function Test-GlobAny([string]$Path, $Globs) {
    $p = $Path -replace '\\', '/'
    foreach ($g in @($Globs)) { if ($g -and $p -match (ConvertTo-GlobRegex ([string]$g))) { return $true } }
    return $false
}

function Test-InReuseScope($Registry, [string]$RelPath) {
    $scan = $Registry.scan
    $include = if ($scan -and $scan.include) { @($scan.include) } else { @('Assets/**/*.cs') }
    $exclude = if ($scan -and $scan.exclude) { @($scan.exclude) } else { @() }
    return (Test-GlobAny $RelPath $include) -and -not (Test-GlobAny $RelPath $exclude)
}

# Dong code (bo comment // dau dong va phan sau //) co khop mau "avoid" cua he thong nao khong.
function Test-ReuseLine($Registry, [string]$RelPath, [string]$Line, [int]$LineNo, $Hits) {
    $code = $Line
    $trim = $code.TrimStart()
    if ($trim.StartsWith('//') -or $trim.StartsWith('*') -or $trim.StartsWith('/*')) { return }
    $cut = $code.IndexOf('//')
    if ($cut -ge 0) { $code = $code.Substring(0, $cut) }
    foreach ($sys in @($Registry.systems)) {
        if ($sys.allowIn -and (Test-GlobAny $RelPath $sys.allowIn)) { continue }
        foreach ($a in @($sys.avoid)) {
            if (-not $a.pattern) { continue }
            if ($a.allowIn -and (Test-GlobAny $RelPath $a.allowIn)) { continue }
            if ($code -match [string]$a.pattern) {
                $Hits.Add([pscustomobject]@{ path = $RelPath; line = $LineNo; system = [string]$sys.name; why = [string]$a.why; text = $Line.Trim() })
            }
        }
    }
}

# Quet toan bo file (lint). $Files = duong dan tuong doi; bo trong = moi file trong scan.include.
function Find-ReuseBypass($Registry, [string]$Root, [string[]]$Files) {
    $hits = New-Object System.Collections.Generic.List[object]
    if (-not $Registry) { return $hits.ToArray() }
    if (-not $Files) {
        $Files = @(Get-ChildItem -Path (Join-Path $Root 'Assets') -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue | ForEach-Object {
                $_.FullName.Substring($Root.TrimEnd('\', '/').Length + 1) -replace '\\', '/' })
    }
    foreach ($rel in $Files) {
        if (-not (Test-InReuseScope $Registry $rel)) { continue }
        $full = Join-Path $Root $rel
        if (-not (Test-Path -LiteralPath $full)) { continue }
        $n = 0
        foreach ($line in [System.IO.File]::ReadAllLines($full)) { $n++; Test-ReuseLine $Registry $rel $line $n $hits }
    }
    return $hits.ToArray()
}

# Quet chi dong MOI them trong unified diff (git diff) + toan bo file moi chua track. Dung cho mot round cua runner.
function Find-ReuseBypassInChange($Registry, [string]$Root, [string]$DiffText, [string[]]$NewFiles) {
    $hits = New-Object System.Collections.Generic.List[object]
    if (-not $Registry) { return $hits.ToArray() }
    $file = $null; $lineNo = 0
    foreach ($line in ($DiffText -split "`r?`n")) {
        if ($line -match '^\+\+\+ b/(.+)$') { $file = $Matches[1]; continue }
        if ($line -match '^\+\+\+ ') { $file = $null; continue }
        if ($line -match '^@@ -\d+(?:,\d+)? \+(\d+)') { $lineNo = [int]$Matches[1] - 1; continue }
        if (-not $file) { continue }
        if ($line.StartsWith('+')) {
            $lineNo++
            if (Test-InReuseScope $Registry $file) { Test-ReuseLine $Registry $file $line.Substring(1) $lineNo $hits }
        } elseif (-not $line.StartsWith('-')) { $lineNo++ }
    }
    foreach ($h in @(Find-ReuseBypass $Registry $Root @($NewFiles | Where-Object { $_ }))) { $hits.Add($h) }
    return $hits.ToArray()
}

Export-ModuleMember -Function Read-ReuseRegistry, Find-ReuseBypass, Find-ReuseBypassInChange, Test-GlobAny, ConvertTo-GlobRegex
