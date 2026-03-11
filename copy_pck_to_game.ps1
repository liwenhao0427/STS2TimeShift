param(
    [switch]$Deploy,
    [switch]$Backup,
    [switch]$Restore,
    [string]$BackupName,
    [string]$BackupSource,
    [string]$RestoreTo,
    [switch]$Interactive
)

$ErrorActionPreference = "Stop"

$localPckPath = Join-Path $PSScriptRoot "TimeShift.pck"
$defaultModTarget = "E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\TimeShift\TimeShift.pck"
$backupRoot = Join-Path $PSScriptRoot "backups"
$indexPath = Join-Path $backupRoot "index.json"

function Ensure-ParentDirectory {
    param([string]$FilePath)

    $parent = Split-Path -Parent $FilePath
    if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
}

function Ensure-BackupRoot {
    if (-not (Test-Path -LiteralPath $backupRoot)) {
        New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    }
}

function Get-ProfileName {
    param([string]$Path)

    if ($Path -match "\\mods\\") {
        return "mod"
    }

    return "nonmod"
}

function Read-BackupIndex {
    Ensure-BackupRoot

    if (-not (Test-Path -LiteralPath $indexPath)) {
        return @()
    }

    $raw = Get-Content -LiteralPath $indexPath -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return @()
    }

    $data = $raw | ConvertFrom-Json
    if ($data -is [System.Array]) {
        return @($data)
    }

    return @($data)
}

function Write-BackupIndex {
    param([System.Object[]]$Items)

    Ensure-BackupRoot
    $Items | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $indexPath -Encoding UTF8
}

function New-Backup {
    param(
        [string]$SourcePath,
        [string]$Name
    )

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        throw "Backup source does not exist: $SourcePath"
    }

    Ensure-BackupRoot

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $safeName = $Name
    if ([string]::IsNullOrWhiteSpace($safeName)) {
        $safeName = "backup-$stamp"
    }

    $safeName = ($safeName -replace "[^a-zA-Z0-9._-]", "_").Trim("_")
    if ([string]::IsNullOrWhiteSpace($safeName)) {
        $safeName = "backup-$stamp"
    }

    $backupFile = Join-Path $backupRoot "$stamp-$safeName.pck"
    Copy-Item -LiteralPath $SourcePath -Destination $backupFile -Force

    $items = Read-BackupIndex
    $entry = [PSCustomObject]@{
        id = [guid]::NewGuid().ToString("N")
        name = $safeName
        createdAt = (Get-Date).ToString("s")
        sourcePath = $SourcePath
        sourceProfile = (Get-ProfileName -Path $SourcePath)
        backupPath = $backupFile
    }

    $newItems = @($entry) + @($items)
    Write-BackupIndex -Items $newItems

    Write-Host "Backup created: $backupFile"
    Write-Host "Backup name:    $safeName"
    Write-Host "Source profile: $($entry.sourceProfile)"
}

function Get-LatestBackup {
    $items = Read-BackupIndex
    if ($items.Count -eq 0) {
        return $null
    }

    return $items[0]
}

function Get-BackupByName {
    param([string]$Name)

    $items = Read-BackupIndex
    foreach ($item in $items) {
        if ($item.name -eq $Name) {
            return $item
        }
    }

    return $null
}

function Invoke-Deploy {
    if (-not (Test-Path -LiteralPath $localPckPath)) {
        throw "Source file not found: $localPckPath. Export TimeShift.pck from Godot first."
    }

    Ensure-ParentDirectory -FilePath $defaultModTarget
    Copy-Item -LiteralPath $localPckPath -Destination $defaultModTarget -Force

    Write-Host "Copied from: $localPckPath"
    Write-Host "Copied to:   $defaultModTarget"
}

function Invoke-Restore {
    param(
        [string]$Name,
        [string]$TargetPath
    )

    $backupEntry = $null
    if ([string]::IsNullOrWhiteSpace($Name)) {
        $backupEntry = Get-LatestBackup
    }
    else {
        $backupEntry = Get-BackupByName -Name $Name
    }

    if ($null -eq $backupEntry) {
        if ([string]::IsNullOrWhiteSpace($Name)) {
            throw "No backup found. Create one first."
        }

        throw "Backup not found by name: $Name"
    }

    if (-not (Test-Path -LiteralPath $backupEntry.backupPath)) {
        throw "Backup file is missing: $($backupEntry.backupPath)"
    }

    $destination = $TargetPath
    if ([string]::IsNullOrWhiteSpace($destination)) {
        $destination = $backupEntry.sourcePath
    }

    Ensure-ParentDirectory -FilePath $destination
    Copy-Item -LiteralPath $backupEntry.backupPath -Destination $destination -Force

    Write-Host "Restored from:  $($backupEntry.backupPath)"
    Write-Host "Restore target: $destination"
    Write-Host "Default target: $($backupEntry.sourcePath)"
    Write-Host "Source profile: $($backupEntry.sourceProfile)"
}

function Show-InteractiveMenu {
    while ($true) {
        Write-Host ""
        Write-Host "==== TimeShift PCK Utility ===="
        Write-Host "1) Deploy local PCK to game mods"
        Write-Host "2) Backup current game PCK"
        Write-Host "3) Restore backup"
        Write-Host "4) Exit"

        $choice = Read-Host "Select"

        switch ($choice) {
            "1" {
                Invoke-Deploy
            }
            "2" {
                $name = Read-Host "Backup name (optional)"
                $source = Read-Host "Backup source path (Enter for default game mods path)"
                if ([string]::IsNullOrWhiteSpace($source)) {
                    $source = $defaultModTarget
                }

                New-Backup -SourcePath $source -Name $name
            }
            "3" {
                $name = Read-Host "Backup name (Enter for latest)"
                $backupEntry = $null
                if ([string]::IsNullOrWhiteSpace($name)) {
                    $backupEntry = Get-LatestBackup
                }
                else {
                    $backupEntry = Get-BackupByName -Name $name
                }

                if ($null -eq $backupEntry) {
                    Write-Host "Backup not found."
                    break
                }

                Write-Host "Default restore target: $($backupEntry.sourcePath)"
                Write-Host "Source profile:          $($backupEntry.sourceProfile)"
                $target = Read-Host "Restore target path (Enter for default)"
                Invoke-Restore -Name $name -TargetPath $target
            }
            "4" {
                return
            }
            default {
                Write-Host "Invalid choice."
            }
        }
    }
}

$selectedActions = @($Deploy, $Backup, $Restore) | Where-Object { $_ }
if ($Interactive -or $selectedActions.Count -eq 0) {
    Show-InteractiveMenu
    return
}

if ($selectedActions.Count -gt 1) {
    throw "Use only one action at a time: -Deploy or -Backup or -Restore."
}

if ($Deploy) {
    Invoke-Deploy
    return
}

if ($Backup) {
    $source = $BackupSource
    if ([string]::IsNullOrWhiteSpace($source)) {
        $source = $defaultModTarget
    }

    New-Backup -SourcePath $source -Name $BackupName
    return
}

if ($Restore) {
    Invoke-Restore -Name $BackupName -TargetPath $RestoreTo
    return
}
