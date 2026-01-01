@echo off
setlocal enabledelayedexpansion

echo ========================================
echo ScryForge Release Script
echo ========================================

:: Haal de laatste tag op
set "last_tag="
for /f "delims=" %%a in ('git tag --sort=-v:refname 2^>nul') do (
    set "last_tag=%%a"
    goto :found
)

:found
if "%last_tag%"=="" (
    echo Geen tags gevonden, start bij v0.0.0
    set "last_tag=v0.0.0"
) else (
    echo Laatste tag: %last_tag%
)

:: Verwijder de 'v'
set "version_number=%last_tag:~1%"

:: Split major.minor.patch
for /f "tokens=1-3 delims=." %%a in ("%version_number%") do (
    set "major=%%a"
    set "minor=%%b"
    set "patch=%%c"
)

if "!patch!"=="" set "patch=0"

:: Verhoog patch
set /a patch+=1

:: Nieuwe versies samenstellen
set "new_tag=v%major%.%minor%.%patch%"
set "new_version=%major%.%minor%.%patch%"

echo.
echo Nieuwe release versie: %new_tag%
echo (dit wordt ook lokaal in de app getoond)

:: ========================================
:: 1. Update de <Version> in ScryForge.csproj
:: ========================================

set "csproj=ScryForge.csproj"

:: Controleer of het bestand bestaat
if not exist "%csproj%" (
    echo FOUT: %csproj% niet gevonden!
    pause
    exit /b 1
)

:: Gebruik PowerShell om de <Version> tag te vervangen of toe te voegen
powershell -Command ^
"$xml = [xml](Get-Content '%csproj%'); ^
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable); ^
$ns.AddNamespace('ns', 'http://schemas.microsoft.com/developer/msbuild/2003'); ^
$node = $xml.SelectSingleNode('//ns:Version', $ns); ^
if ($node) { $node.InnerText = '%new_version%' } ^
else { ^
  $pg = $xml.SelectSingleNode('//ns:PropertyGroup[not(*)]'); ^
  if (-not $pg) { $pg = $xml.Project.PropertyGroup[0] } ^
  $ver = $xml.CreateElement('Version'); ^
  $ver.InnerText = '%new_version%'; ^
  $pg.AppendChild($ver) | Out-Null ^
}; ^
$xml.Save('%csproj%'); ^
Write-Host 'Version in %csproj% bijgewerkt naar %new_version%'"

echo.

:: ========================================
:: 2. Commit de wijziging in csproj (optioneel maar aanbevolen)
:: ========================================

git add ScryForge.csproj
git commit -m "chore: update project version to %new_version%" --no-verify

:: ========================================
:: 3. Maak tag en push alles
:: ========================================

git tag -a %new_tag% -m "Versie %new_tag% release"
git push origin %new_tag%
git push origin main  :: of master, afhankelijk van je branch

echo.
echo ========================================
echo Klaar!
echo - Nieuwe tag: %new_tag%
echo - Lokale versie: %new_version%
echo - GitHub Actions is gestart voor de build
echo ========================================
pause