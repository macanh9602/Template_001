param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectRoot,

    [Parameter(Mandatory=$true)]
    [string]$ManifestPath,

    [string]$ReportPath,

    [switch]$DryRun,
    [switch]$Force,
    [switch]$NoBackup
)

$ErrorActionPreference = "Stop"

function Get-Sha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Write-Utf8Text([string]$Path, [string]$Text) {
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path -LiteralPath $dir)) {
        [IO.Directory]::CreateDirectory($dir) | Out-Null
    }
    $enc = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($Path, $Text, $enc)
}

function Write-BytesAtomic([string]$Path, [byte[]]$Bytes) {
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path -LiteralPath $dir)) {
        [IO.Directory]::CreateDirectory($dir) | Out-Null
    }
    $tmp = $Path + ".bootstrap-tmp-" + [Guid]::NewGuid().ToString("N")
    try {
        [IO.File]::WriteAllBytes($tmp, $Bytes)
        if (Test-Path -LiteralPath $Path) {
            Remove-Item -LiteralPath $Path -Force
        }
        Move-Item -LiteralPath $tmp -Destination $Path
    }
    finally {
        if (Test-Path -LiteralPath $tmp) { Remove-Item -LiteralPath $tmp -Force }
    }
}

$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
$manifestFile = (Resolve-Path -LiteralPath $ManifestPath).Path

if (-not (Test-Path (Join-Path $root "Assets"))) {
    throw "ProjectRoot does not look like a Unity project: $root"
}

$manifest = Get-Content -LiteralPath $manifestFile -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.manifestVersion -ne 1) {
    throw "Unsupported manifestVersion: $($manifest.manifestVersion)"
}

Push-Location $root
try {
    $isGit = Test-Path ".git"
    $dirtyPaths = @()
    if ($isGit -and -not $Force) {
        $dirty = @(& git status --porcelain)
        if ($LASTEXITCODE -ne 0) { throw "git status failed" }
        # DryRun writes nothing: report dirty files instead of refusing, so the PO can review the plan first.
        if ($dirty.Count -and -not $DryRun) {
            throw "Working tree is dirty. Commit/stash first or re-run with -Force after review."
        }
        $dirtyPaths = @($dirty | ForEach-Object { ($_.Substring(3) -replace '^"|"$', '') -replace '\\', '/' })
        if ($dirtyPaths.Count) {
            Write-Host "WARNING: working tree is dirty ($($dirtyPaths.Count) path(s)). Apply will refuse until it is clean:"
            $dirtyPaths | ForEach-Object { Write-Host "  dirty: $_" }
        }
    }

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupRoot = Join-Path (Split-Path -Parent $root) ((Split-Path -Leaf $root) + "_bootstrap_backup_" + $stamp)
    $report = New-Object System.Collections.Generic.List[string]

    if ([string]::IsNullOrWhiteSpace($ReportPath)) {
        $reportFile = Join-Path $root ("handoff/bootstrap/bootstrap-report-" + $stamp + ".md")
    }
    elseif ([IO.Path]::IsPathRooted($ReportPath)) {
        $reportFile = $ReportPath
    }
    else {
        $reportFile = Join-Path $root $ReportPath
    }

    foreach ($op in $manifest.operations) {
        $relative = [string]$op.path
        if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::IsPathRooted($relative)) {
            throw "Manifest path must be relative: $relative"
        }

        $target = Join-Path $root $relative
        $operation = ([string]$op.operation).ToLowerInvariant()
        $beforeSha = Get-Sha256 $target
        $expected = [string]$op.expectedBeforeSha256

        if ($expected -and $beforeSha -and $expected.ToLowerInvariant() -ne $beforeSha -and -not $Force) {
            throw "SHA mismatch before $operation : $relative"
        }

        $norm = ($relative -replace '\\', '/').TrimEnd('/')
        $touchesDirty = $dirtyPaths | Where-Object { $_.TrimEnd('/') -eq $norm }
        $wouldFail = ''
        if (-not $Force) {
            if ($operation -eq 'add' -and $beforeSha) { $wouldFail = "`tWOULD-FAIL(add target exists)" }
            elseif ($operation -eq 'replace' -and -not $beforeSha) { $wouldFail = "`tWOULD-FAIL(replace target missing)" }
        }
        $report.Add("$operation`t$relative`tbefore=$beforeSha" + $(if ($touchesDirty) { "`tTOUCHES-DIRTY" } else { "" }) + $wouldFail)

        if ($DryRun) { continue }

        if ((Test-Path -LiteralPath $target) -and -not $NoBackup) {
            $backupPath = Join-Path $backupRoot $relative
            $backupDir = Split-Path -Parent $backupPath
            if (-not (Test-Path -LiteralPath $backupDir)) {
                [IO.Directory]::CreateDirectory($backupDir) | Out-Null
            }
            Copy-Item -LiteralPath $target -Destination $backupPath -Force
        }

        switch ($operation) {
            "add" {
                if ((Test-Path -LiteralPath $target) -and -not $Force) {
                    throw "ADD target already exists: $relative"
                }
                $bytes = [Convert]::FromBase64String([string]$op.contentBase64)
                Write-BytesAtomic $target $bytes
            }
            "replace" {
                if (-not (Test-Path -LiteralPath $target) -and -not $Force) {
                    throw "REPLACE target missing: $relative"
                }
                $bytes = [Convert]::FromBase64String([string]$op.contentBase64)
                Write-BytesAtomic $target $bytes
            }
            "delete" {
                if (Test-Path -LiteralPath $target) {
                    Remove-Item -LiteralPath $target -Recurse -Force
                }
            }
            default {
                throw "Unknown operation: $operation"
            }
        }
    }

    if ($DryRun) {
        Write-Host "DRY RUN - no files changed."
        $report | ForEach-Object { Write-Host $_ }
        exit 0
    }

    $manifestSha = (Get-FileHash -LiteralPath $manifestFile -Algorithm SHA256).Hash.ToLowerInvariant()
    $reportLines = New-Object System.Collections.Generic.List[string]
    $reportLines.Add("# Bootstrap Report")
    $reportLines.Add("")
    $reportLines.Add("- Applied: " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
    $reportLines.Add('- Project root: `' + $root + '`')
    $reportLines.Add('- Manifest: `' + $manifestFile + '`')
    $reportLines.Add('- Manifest SHA256: `' + $manifestSha + '`')
    $reportLines.Add('- Backup: ' + $(if ($NoBackup) { 'disabled' } else { '`' + $backupRoot + '`' }))
    $reportLines.Add("")
    $reportLines.Add("## Operations")
    $reportLines.Add("")
    $reportLines.Add('```text')
    foreach ($line in $report) { $reportLines.Add($line) }
    $reportLines.Add('```')
    Write-Utf8Text $reportFile ($reportLines -join "`n")

    if ($isGit) {
        & git diff --check
        if ($LASTEXITCODE -ne 0) { throw "git diff --check failed" }
        Write-Host ""
        Write-Host "Git status:"
        & git status --short
    }

    Write-Host ""
    Write-Host "Bootstrap patch applied."
    if (-not $NoBackup) { Write-Host "Backup: $backupRoot" }
    Write-Host "Report: $reportFile"
    Write-Host "Next: compile/test/review diff. Keep manifest/report; generated one-off wrappers may be deleted after closure."
}
finally {
    Pop-Location
}
