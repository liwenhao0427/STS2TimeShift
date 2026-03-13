$ErrorActionPreference = "Stop"

$pckSourcePath = Join-Path $PSScriptRoot "TimeShift.pck"
$dllSearchRoot = Join-Path $PSScriptRoot "build"
$dllSourcePath = $null
$targetDir = "E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\TimeShift"
$pckTargetPath = Join-Path $targetDir "TimeShift.pck"
$dllTargetPath = Join-Path $targetDir "TimeShift.dll"

if (-not (Test-Path -LiteralPath $pckSourcePath)) {
    throw "Source file not found: $pckSourcePath. Export TimeShift.pck from Godot first."
}

if (-not (Test-Path -LiteralPath $dllSearchRoot)) {
    throw "Build output directory not found: $dllSearchRoot. Please build TimeShift first."
}

$dllCandidate = Get-ChildItem -Path $dllSearchRoot -Filter "TimeShift.dll" -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $dllCandidate) {
    throw "TimeShift.dll not found under: $dllSearchRoot. Please build TimeShift first."
}

$dllSourcePath = $dllCandidate.FullName

if (-not (Test-Path -LiteralPath $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

Copy-Item -LiteralPath $pckSourcePath -Destination $pckTargetPath -Force
Copy-Item -LiteralPath $dllSourcePath -Destination $dllTargetPath -Force

Write-Host "Copied PCK from: $pckSourcePath"
Write-Host "Copied PCK to:   $pckTargetPath"
Write-Host "Copied DLL from: $dllSourcePath"
Write-Host "Copied DLL to:   $dllTargetPath"
