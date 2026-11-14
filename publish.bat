@echo off
chcp 65001 >nul
echo Збірка автономного StartLifre.exe (один файл, без .NET, без папок у Документах)...
dotnet publish src\Lifre.Start\Lifre.Start.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
if errorlevel 1 (echo ПОМИЛКА збірки & pause & exit /b 1)
echo Готово: dist\StartLifre.exe
pause
