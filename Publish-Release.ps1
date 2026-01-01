# Publish-Release.ps1
# ScryForge automatische release script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   ScryForge Release Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Haal de laatste tag op
$lastTag = git tag --sort=-v:refname | Select-Object -First 1

if (-not $lastTag) {
    Write-Host "Geen tags gevonden, start bij v0.0.0" -ForegroundColor Yellow
    $lastTag = "v0.0.0"
}
else {
    Write-Host "Laatste tag: $lastTag" -ForegroundColor Green
}

# Verwijder 'v' en split versie
$version = $lastTag.Substring(1)
$parts = $version.Split('.')
$major = $parts[0]
$minor = $parts[1]
$patch = [int]$parts[2]

$patch += 1

$newTag = "v$major.$minor.$patch"
$newVersion = "$major.$minor.$patch"

Write-Host ""
Write-Host "Nieuwe release versie: $newTag" -ForegroundColor Magenta
Write-Host "(deze versie wordt ook lokaal getoond)" -ForegroundColor Gray

# Update ScryForge.csproj
$csproj = "ScryForge.csproj"

if (-not (Test-Path $csproj)) {
    Write-Error "FOUT: $csproj niet gevonden!"
    pause
    exit 1
}

Write-Host ""
Write-Host "Updaten versie in $csproj naar $newVersion..." -ForegroundColor Yellow

[xml]$xml = Get-Content $csproj -Encoding UTF8
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")

$versionNode = $xml.SelectSingleNode("//msb:Version", $ns)

if ($versionNode) {
    $versionNode.InnerText = $newVersion
    Write-Host "Version bijgewerkt naar $newVersion" -ForegroundColor Green
}
else {
    # Zoek een PropertyGroup zonder Version, of maak nieuwe
    $pg = $xml.SelectSingleNode("//msb:PropertyGroup[not(msb:Version)]", $ns)
    if (-not $pg) {
        $pg = $xml.CreateElement("PropertyGroup", "http://schemas.microsoft.com/developer/msbuild/2003")
        $xml.Project.AppendChild($pg) | Out-Null
    }
    $newNode = $xml.CreateElement("Version", "http://schemas.microsoft.com/developer/msbuild/2003")
    $newNode.InnerText = $newVersion
    $pg.AppendChild($newNode) | Out-Null
    Write-Host "Version toegevoegd: $newVersion" -ForegroundColor Green
}

$xml.Save($csproj)

# Commit de wijziging
git add $csproj
git commit -m "chore: bump version to $newVersion" --no-verify

# Maak tag en push alles
git tag -a $newTag -m "Versie $newTag release"

Write-Host ""
Write-Host "Push tag $newTag..." -ForegroundColor Yellow
git push origin $newTag

Write-Host "Push main branch..." -ForegroundColor Yellow
git push origin main  # verander naar 'master' als nodig

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "KLAAR!" -ForegroundColor Green
Write-Host "Nieuwe versie: $newTag" -ForegroundColor White
Write-Host "Lokale app toont nu: $newVersion" -ForegroundColor White
Write-Host "GitHub Actions bouwt de release..." -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan

pause