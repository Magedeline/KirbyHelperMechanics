# Builds KirbyHelperMechanics and packages it into an Everest-installable zip
# under dist/. Run from anywhere; paths below are relative to this script.
#
# Usage:
#   pwsh ./publish.ps1
#   pwsh ./publish.ps1 -Configuration Debug
#
# The zip contains exactly what Everest needs at Mods/KirbyHelperMechanics/:
# everest.yaml, bin/, Graphics/, Loenn/, Audio/, README.md -- with everest.yaml
# sitting at the ZIP ROOT (not inside a wrapping folder), since Everest expects
# to find it there. Dev-only content (Source/, lib-stripped/, .git/, .vs/,
# build intermediates) is deliberately left out.

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$distDir = Join-Path $root "dist"
$stageDir = Join-Path $distDir "KirbyHelperMechanics"
$zipPath = Join-Path $distDir "KirbyHelperMechanics.zip"

Write-Host "Building ($Configuration)..."
dotnet build (Join-Path $root "Source/KirbyHelperMechanics.csproj") -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

$builtDll = Join-Path $root "bin/KirbyHelperMechanics.dll"
if (-not (Test-Path $builtDll)) {
    throw "Expected $builtDll to exist after build -- the csproj's post-build copy step didn't run or failed."
}

if (Test-Path $distDir) {
    Remove-Item $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stageDir | Out-Null

$includePaths = @("everest.yaml", "bin", "Graphics", "Loenn", "Audio", "README.md")
foreach ($item in $includePaths) {
    $src = Join-Path $root $item
    if (-not (Test-Path $src)) {
        Write-Warning "Skipping missing path: $item"
        continue
    }
    Copy-Item -Path $src -Destination (Join-Path $stageDir $item) -Recurse
}

Write-Host "Zipping to $zipPath..."
Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -Force

Write-Host "Done: $zipPath"
