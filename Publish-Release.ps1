$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   ScryForge Release Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Forceer dat we in de repository root staan
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Test-Path (Join-Path $scriptDir "ScryForge.sln"))) {
    Write-Error "FOUT: Dit script moet uitgevoerd worden vanuit de repository root (waar ScryForge.sln staat)!"
    Write-Host "Huidige map: $(Get-Location)" -ForegroundColor Red
    Write-Host "Verwachte map: $scriptDir" -ForegroundColor Red
    exit 1
}

# Correct pad naar csproj
$csprojPath = Join-Path $scriptDir "ScryForge\ScryForge.csproj"

if (-not (Test-Path $csprojPath)) {
    Write-Error "FOUT: Kan ScryForge.csproj niet vinden op $csprojPath"
    exit 1
}

Write-Host "Script draait vanuit: $scriptDir" -ForegroundColor Green
Write-Host "Gevonden csproj: $csprojPath" -ForegroundColor Green

# Haal laatste git tag op
$lastTag = git tag --sort=-v:refname | Select-Object -First 1

if (-not $lastTag) {
    Write-Host "No tags found → starting from v0.0.0" -ForegroundColor Yellow
    $lastTag = "v0.0.0"
}
else {
    Write-Host "Latest tag: $lastTag" -ForegroundColor Green
}

# Bepaal nieuwe versie (patch bump)
$version = $lastTag.Substring(1)
$parts = $version.Split('.')

if ($parts.Count -ne 3) {
    throw "Invalid tag format '$lastTag' (expected vX.Y.Z)"
}

$patch = [int]$parts[2] + 1
$newVersion = "$($parts[0]).$($parts[1]).$patch"
$newTag = "v$newVersion"

Write-Host ""
Write-Host "New release: $newTag" -ForegroundColor Magenta
Write-Host "Local app version will show: $newVersion" -ForegroundColor Gray

Write-Host ""
Write-Host "Cleaning and updating version in csproj..." -ForegroundColor Yellow

# Laad csproj als XML
[xml]$xml = Get-Content $csprojPath -Raw -Encoding UTF8

# Zoek een onvoorwaardelijke PropertyGroup
$targetGroup = $xml.Project.PropertyGroup |
    Where-Object { -not $_.Condition -or $_.Condition.Trim() -eq '' } |
    Select-Object -First 1

if (-not $targetGroup) {
    throw "No unconditional <PropertyGroup> found in csproj"
}

# Zoek of maak <Version>
$versionNode = $targetGroup.Version

if ($versionNode) {
    Write-Host "Updating existing <Version> from '$($versionNode.InnerText)' to '$newVersion'" -ForegroundColor Cyan
    $versionNode.InnerText = $newVersion
}
else {
    $versionNode = $xml.CreateElement("Version")
    $versionNode.InnerText = $newVersion
    $targetGroup.AppendChild($versionNode) | Out-Null
    Write-Host "Added new <Version>$newVersion</Version>" -ForegroundColor Green
}

# Opslaan als UTF-8 zonder BOM (MSBuild-safe)
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($csprojPath, $xml.OuterXml, $utf8NoBom)

Write-Host "Version successfully set to $newVersion" -ForegroundColor Green

# Git: commit, tag, push
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
