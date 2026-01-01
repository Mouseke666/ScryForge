# Publish-Release.ps1 - Definitieve robuuste versie

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   ScryForge Release Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Laatste tag ophalen
$lastTag = git tag --sort=-v:refname | Select-Object -First 1
if (-not $lastTag) {
    Write-Host "Geen tags gevonden → start bij v0.0.0" -ForegroundColor Yellow
    $lastTag = "v0.0.0"
}
else {
    Write-Host "Laatste tag: $lastTag" -ForegroundColor Green
}

# Nieuwe versie berekenen
$version = $lastTag.Substring(1)
$parts = $version.Split('.')
$patch = [int]$parts[2] + 1
$newTag = "v$($parts[0]).$($parts[1]).$patch"
$newVersion = "$($parts[0]).$($parts[1]).$patch"

Write-Host "Nieuwe release: $newTag" -ForegroundColor Magenta
Write-Host "Lokale versie wordt: $newVersion" -ForegroundColor Gray

# csproj bijwerken
$csprojPath = "ScryForge.csproj"
if (-not (Test-Path $csprojPath)) {
    Write-Error "FOUT: $csprojPath niet gevonden!"
    pause
    exit 1
}

Write-Host "Schoonmaken en bijwerken van versie in $csprojPath..." -ForegroundColor Yellow

[xml]$xml = Get-Content $csprojPath -Encoding UTF8

# 1. Verwijder ALLE bestaande <Version> nodes (schoonmaak)
$versionNodes = $xml.SelectNodes("//Version")
foreach ($node in $versionNodes) {
    $node.ParentNode.RemoveChild($node) | Out-Null
}

# 2. Voeg één schone <Version> toe in het eerste PropertyGroup
$firstPropertyGroup = $xml.Project.PropertyGroup[0]
if (-not $firstPropertyGroup) {
    # Als er echt geen PropertyGroup is (zeer onwaarschijnlijk), maak er een
    $firstPropertyGroup = $xml.CreateElement("PropertyGroup")
    $xml.Project.AppendChild($firstPropertyGroup) | Out-Null
}

$newVersionNode = $xml.CreateElement("Version")
$newVersionNode.InnerText = $newVersion
$firstPropertyGroup.AppendChild($newVersionNode) | Out-Null

# Opslaan
$xml.Save($csprojPath)

Write-Host "Versie succesvol ingesteld op $newVersion" -ForegroundColor Green

# Git: commit, tag, push
git add $csprojPath
git commit -m "chore: bump version to $newVersion" --no-verify

git tag -a $newTag -m "Versie $newTag release"

Write-Host "Push tag en main branch..." -ForegroundColor Yellow
git push origin $newTag
git push origin main  # verander naar 'master' als nodig

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "KLAAR! Nieuwe versie: $newTag" -ForegroundColor Green
Write-Host "Lokale app toont nu: $newVersion" -ForegroundColor Green
Write-Host "GitHub Actions bouwt de release..." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

pause