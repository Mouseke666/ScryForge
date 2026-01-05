Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   ScryForge Release Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get the latest tag
$lastTag = git tag --sort=-v:refname | Select-Object -First 1

if (-not $lastTag) {
    Write-Host "No tags found → starting from v0.0.0" -ForegroundColor Yellow
    $lastTag = "v0.0.0"
}
else {
    Write-Host "Latest tag: $lastTag" -ForegroundColor Green
}

# Calculate new version
$version = $lastTag.Substring(1)
$parts = $version.Split('.')
$patch = [int]$parts[2] + 1

$newTag = "v$($parts[0]).$($parts[1]).$patch"
$newVersion = "$($parts[0]).$($parts[1]).$patch"

Write-Host ""
Write-Host "New release: $newTag" -ForegroundColor Magenta
Write-Host "Local app version will show: $newVersion" -ForegroundColor Gray

# Update ScryForge.csproj
$csprojPath = "ScryForge.csproj"
if (-not (Test-Path $csprojPath)) {
    Write-Error "ERROR: $csprojPath not found!"
    exit 1
}

Write-Host ""
Write-Host "Cleaning and updating version in $csprojPath..." -ForegroundColor Yellow

[xml]$xml = Get-Content $csprojPath -Encoding UTF8

# 1. Remove ALL existing <Version> nodes (cleanup)
$versionNodes = $xml.SelectNodes("//Version")
foreach ($node in $versionNodes) {
    $node.ParentNode.RemoveChild($node) | Out-Null
}

# 2. Add one clean <Version> node to the first PropertyGroup
$firstPropertyGroup = $xml.Project.PropertyGroup[0]
if (-not $firstPropertyGroup) {
    $firstPropertyGroup = $xml.CreateElement("PropertyGroup")
    $xml.Project.AppendChild($firstPropertyGroup) | Out-Null
}

$newVersionNode = $xml.CreateElement("Version")
$newVersionNode.InnerText = $newVersion
$firstPropertyGroup.AppendChild($newVersionNode) | Out-Null

# Save changes
$xml.Save($csprojPath)

Write-Host "Version successfully set to $newVersion" -ForegroundColor Green

# Git: add, commit, tag, push
git add $csprojPath
git commit -m "chore: bump version to $newVersion" --no-verify

git tag -a $newTag -m "Release $newTag"

Write-Host ""
Write-Host "Pushing tag and main branch..." -ForegroundColor Yellow
git push origin $newTag
git push origin main  # change to 'master' if your default branch has a different name

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DONE! New version: $newTag" -ForegroundColor Green
Write-Host "Local app now shows: $newVersion" -ForegroundColor Green
Write-Host "GitHub Actions is building the release..." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

# No pause - script ends automatically