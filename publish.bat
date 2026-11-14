@echo off
chcp 65001 >nul
echo Збірка Ліфре: StartLifre.exe + StopLifre.exe для x64 і x86...
set FLAGS=--self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true -p:DebugType=None -p:DebugSymbols=false

dotnet publish src\Lifre.Start\Lifre.Start.csproj -c Release -r win-x64 %FLAGS% -o dist\x64
if errorlevel 1 goto err
dotnet publish src\Lifre.Stop\Lifre.Stop.csproj -c Release -r win-x64 %FLAGS% -o dist\x64
if errorlevel 1 goto err
dotnet publish src\Lifre.Start\Lifre.Start.csproj -c Release -r win-x86 %FLAGS% -o dist\x86
if errorlevel 1 goto err
dotnet publish src\Lifre.Stop\Lifre.Stop.csproj -c Release -r win-x86 %FLAGS% -o dist\x86
if errorlevel 1 goto err

echo Готово:
echo   dist\x64\StartLifre.exe  dist\x64\StopLifre.exe
echo   dist\x86\StartLifre.exe  dist\x86\StopLifre.exe
pause
exit /b 0
:err
echo ПОМИЛКА збірки
pause
exit /b 1
