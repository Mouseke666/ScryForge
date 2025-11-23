@echo off
setlocal enabledelayedexpansion

:: Haal de laatste tag op
for /f "delims=" %%a in ('git tag --sort=-creatordate') do (
    set last_tag=%%a
    goto :found
)
:found

:: Controleer of er überhaupt een tag is
if "%last_tag%"=="" (
    echo Geen bestaande tags gevonden, begin bij v0.0.0
    set last_tag=v0.0.0
)

:: Verwijder de 'v'
set version_number=%last_tag:~1%

:: Split in major.minor.patch
for /f "tokens=1-3 delims=." %%a in ("%version_number%") do (
    set major=%%a
    set minor=%%b
    set patch=%%c
)

:: Controleer of patch leeg is
if "!patch!"=="" set patch=0

:: Verhoog patch
set /a patch+=1

:: Nieuwe tag samenstellen
set new_tag=v%major%.%minor%.%patch%

:: Tag aanmaken en pushen
git tag -a %new_tag% -m "Versie %new_tag% release"
git push origin %new_tag%

echo Nieuwe tag %new_tag% gepusht naar GitHub!
pause
