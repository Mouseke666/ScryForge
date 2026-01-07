# ============================
# ScryForge Release Script
# ============================

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   ScryForge Release Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Bepaal script- en repo-pad
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Write-Host "DEBUG: Repository root = $scriptDir"

if (-not (Test-Path (Join-Path $scriptDir "ScryForge.sln"))) {
    Write-Error "FOUT: Script moet worden uitgevoerd vanuit de repository root!"
    Write-Host "Huidige map: $(Get-Location)" -ForegroundColor Red
    exit 1
}

# Correct pad naar csproj
$csprojPath = Join-Path $scriptDir "ScryForge\ScryForge.csproj"
if (-not (Test-Path $csprojPath)) {
    Write-Error "FOUT: Kan csproj niet vinden op $csprojPath"
    exit 1
}

Write-Host "Script draait vanuit: $scriptDir" -ForegroundColor Green
Write-Host "Gevonden csproj: $csprojPath" -ForegroundColor Green

# ============================
# Haal laatste git tag op
# ============================
$lastTag = git tag --sort=-creatordate | Select-Object -First 1
if (-not $lastTag) {
    Write-Host "Geen tags gevonden → start vanaf v0.0.0" -ForegroundColor Yellow
    $lastTag = "v0.0.0"
} else {
    Write-Host "Latest tag: $lastTag" -ForegroundColor Green
}

# Bepaal nieuwe patch-versie
$versionParts = $lastTag.Substring(1).Split('.')
if ($versionParts.Count -ne 3) {
    throw "Ongeldig tag-formaat '$lastTag', verwacht vX.Y.Z"
}

$patch = [int]$versionParts[2] + 1
$newVersion = "$($versionParts[0]).$($versionParts[1]).$patch"
$newTag = "v$newVersion"

Write-Host ""
Write-Host "New release: $newTag" -ForegroundColor Magenta
Write-Host "Local app version will show: $newVersion" -ForegroundColor Gray
Write-Host ""

# ============================
# Laad csproj als XML
# ============================
try {
    $reader = [System.IO.StreamReader]::new($csprojPath,[System.Text.Encoding]::UTF8)
    $xml = New-Object System.Xml.XmlDocument
    $xml.Load($reader)
    $reader.Close()
    Write-Host "DEBUG: XML loaded successfully. Root element: $($xml.DocumentElement.Name)"
} catch {
    Write-Error "Failed to load XML from $csprojPath"
    Write-Error $_.Exception.Message
    exit 1
}

# ============================
# Pak eerste PropertyGroup via SelectNodes
# ============================
$propertyGroups = $xml.SelectNodes("/Project/PropertyGroup")
if ($propertyGroups.Count -eq 0) {
    throw "Geen <PropertyGroup> gevonden in csproj"
}
$targetGroup = $propertyGroups[0]

# ============================
# Update of voeg <Version> toe
# ============================
$versionNode = $targetGroup.SelectSingleNode("Version")
if ($versionNode) {
    Write-Host "Updating existing <Version> from '$($versionNode.InnerText)' to '$newVersion'" -ForegroundColor Cyan
    $versionNode.InnerText = $newVersion
} else {
    $versionNode = $xml.CreateElement("Version")
    $versionNode.InnerText = $newVersion
    $targetGroup.AppendChild($versionNode) | Out-Null
    Write-Host "Added new <Version>$newVersion</Version>" -ForegroundColor Green
}

# ============================
# Opslaan als UTF-8 zonder BOM, netjes geformatteerd
# ============================
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$writer = [System.Xml.XmlWriter]::Create($csprojPath, $settings)
$xml.Save($writer)
$writer.Close()

Write-Host "Version successfully set to $newVersion" -ForegroundColor Green

# ============================
# Git: commit, tag, push
# ============================
git add $csprojPath
git commit -m "chore: bump version to $newVersion" --no-verify
git tag -a $newTag -m "Release $newTag"

Write-Host ""
Write-Host "Pushing tag and branch..." -ForegroundColor Yellow
git push origin $newTag
git push origin HEAD

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DONE! New version: $newTag" -ForegroundColor Green
Write-Host "Local app now shows: $newVersion" -ForegroundColor Green
Write-Host "GitHub Actions is building the release..." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
