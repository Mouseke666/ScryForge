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

:: Nieuwe versies
set "new_tag=v%major%.%minor%.%patch%"
set "new_version=%major%.%minor%.%patch%"

echo.
echo Nieuwe release versie: %new_tag%
echo (dit wordt ook lokaal in de app getoond)

:: ========================================
:: 1. Update de <Version> in ScryForge.csproj (super robuust)
:: ========================================

set "csproj=ScryForge.csproj"

if not exist "%csproj%" (
    echo FOUT: %csproj% niet gevonden in de huidige map!
    pause
    exit /b 1
)

echo Updaten van versie in %csproj% naar %new_version% ...

powershell -Command ^
"$xml = [xml](Get-Content '%csproj%' -Encoding UTF8); ^
$ns = New-Object Xml.XmlNamespaceManager $xml.NameTable; ^
$ns.AddNamespace('msb', 'http://schemas.microsoft.com/developer/msbuild/2003'); ^
$versionNode = $xml.SelectSingleNode('//msb:Version', $ns); ^
if ($versionNode) { ^
    $versionNode.InnerText = '%new_version%'; ^
    Write-Host 'Version bijgewerkt naar %new_version%' ^
} else { ^
    $pg = $xml.SelectSingleNode('//msb:PropertyGroup[not(msb:Version)]', $ns); ^
    if (-not $pg) { ^
        $pg = $xml.CreateElement('PropertyGroup', 'http://schemas.microsoft.com/developer/msbuild/2003'); ^
        $xml.Project.AppendChild($pg) ^
    }; ^
    $newNode = $xml.CreateElement('Version', 'http://schemas.microsoft.com/developer/msbuild/2003'); ^
    $newNode.InnerText = '%new_version%'; ^
    $pg.AppendChild($newNode); ^
    Write-Host 'Version toegevoegd: %new_version%' ^
}; ^
$xml.Save('%csproj%')"

if errorlevel 1 (
    echo FOUT: Kon de versie niet bijwerken in %csproj%!
    pause
    exit /b 1
)

echo Versie succesvol bijgewerkt in %csproj%
echo.

:: ========================================
:: 2. Commit de wijziging
:: ========================================

git add ScryForge.csproj
git commit -m "chore: bump version to %new_version%" --no-verify || echo Geen wijziging in csproj (mogelijk al up-to-date)

:: ========================================
:: 3. Tag aanmaken en pushen
:: ========================================

git tag -a %new_tag% -m "Versie %new_tag% release"

echo Push tag %new_tag%...
git push origin %new_tag%

echo Push main branch...
git push origin main
:: Als je branch 'master' heet, vervang 'main' door 'master'

echo.
echo ========================================
echo KLAAR!
echo Nieuwe versie: %new_tag%
echo Lokale app toont nu: %new_version%
echo GitHub Actions bouwt de release...
echo ========================================
pause