$ErrorActionPreference = "Stop"

$sourcePath = Join-Path $PSScriptRoot "DevDeckTools.pck"
$targetDir = "E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\DevDeckTools"
$targetPath = Join-Path $targetDir "DevDeckTools.pck"

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "Source file not found: $sourcePath. Export DevDeckTools.pck from Godot first."
}

if (-not (Test-Path -LiteralPath $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force

Write-Host "Copied from: $sourcePath"
Write-Host "Copied to:   $targetPath"
