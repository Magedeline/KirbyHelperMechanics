# Builds KirbyHelperMechanics and packages it into an Everest-installable zip
# under dist/. Run from anywhere; paths below are relative to this script.
#
# Usage:
#   pwsh ./publish.ps1
#   pwsh ./publish.ps1 -Configuration Debug
#
# The zip contains exactly what Everest needs at Mods/KirbyHelperMechanics/:
# everest.yaml, bin/, Graphics/, Loenn/, Audio/, Dialog/, Maps/, README.md --
# with everest.yaml sitting at the ZIP ROOT (not inside a wrapping folder),
# since Everest expects to find it there. Dev-only content (Source/,
# lib-stripped/, .git/, .vs/, build intermediates) is deliberately left out.

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

$includePaths = @("everest.yaml", "bin", "Graphics", "Loenn", "Audio", "Dialog", "Maps", "README.md")
foreach ($item in $includePaths) {
    $src = Join-Path $root $item
    if (-not (Test-Path $src)) {
        Write-Warning "Skipping missing path: $item"
        continue
    }
    Copy-Item -Path $src -Destination (Join-Path $stageDir $item) -Recurse
}

Write-Host "Zipping to $zipPath..."
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
# Both Compress-Archive AND [System.IO.Compression.ZipFile]::CreateFromDirectory
# write zip entries using the OS path separator -- on Windows that means every
# entry ends up as "bin\KirbyHelperMechanics.dll" instead of
# "bin/KirbyHelperMechanics.dll" (verified directly; CreateFromDirectory does
# NOT normalize this the way its docs might imply). The ZIP spec requires
# forward slashes, and Everest's mod loader (which has to work identically on
# Windows/Linux/Mac) reads entries expecting "/" -- with backslash entries it
# can read everest.yaml (a lone root file) but can't resolve "DLL: bin/..."
# to anything inside the archive, so the mod shows up but its assembly "fails
# to load". Build the archive entry-by-entry instead, explicitly forcing "/"
# in every entry name ourselves.
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -Path $stageDir -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($stageDir.Length + 1) -replace '\\', '/'
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $_.FullName, $relativePath, [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
    }
} finally {
    $zip.Dispose()
}

Write-Host "Done: $zipPath"
