# Publish-Release.ps1 (definitieve versie zonder xmlns-probleem)

$lastTag = git tag --sort=-v:refname | Select-Object -First 1
if (-not $lastTag) { $lastTag = "v0.0.0" }

$version = $lastTag.Substring(1)
$parts = $version.Split('.')
$patch = [int]$parts[2] + 1
$newTag = "v$($parts[0]).$($parts[1]).$patch"
$newVersion = "$($parts[0]).$($parts[1]).$patch"

[xml]$xml = Get-Content "ScryForge.csproj" -Encoding UTF8

$versionNode = $xml.Project.PropertyGroup.Version
if ($versionNode) {
    $versionNode.InnerText = $newVersion
}
else {
    $pg = $xml.Project.PropertyGroup[0]
    $newNode = $xml.CreateElement("Version")
    $newNode.InnerText = $newVersion
    $pg.AppendChild($newNode)
}

$xml.Save("ScryForge.csproj")

git add ScryForge.csproj
git commit -m "chore: bump version to $newVersion" --no-verify

git tag -a $newTag -m "Versie $newTag release"
git push origin $newTag
git push origin main