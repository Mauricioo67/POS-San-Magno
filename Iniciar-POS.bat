@echo off
setlocal
cd /d "%~dp0"

echo ================================================
echo        APP POS SAN MAGNO - INICIO LOCAL
echo ================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: No se encontro .NET SDK. Instala el SDK 10.0.401 indicado en global.json y vuelve a ejecutar.
  pause
  exit /b 1
)

echo Verificando certificado HTTPS de desarrollo...
dotnet dev-certs https --trust

echo.
echo Iniciando POS.Server en https://localhost:7008 ...
start "POS.Server - API" powershell.exe -NoExit -ExecutionPolicy Bypass -Command "Set-Location '%~dp0'; dotnet run --project 'POS.Server\POS.Server.csproj' --launch-profile https"

echo Esperando que el servidor inicie...
timeout /t 8 /nobreak >nul

echo Iniciando POS.Client en https://localhost:7282 ...
start "POS.Client - Interfaz" powershell.exe -NoExit -ExecutionPolicy Bypass -Command "Set-Location '%~dp0'; dotnet run --project 'POS.Client\POS.Client.csproj' --launch-profile https"

echo.
echo Se abrieron dos ventanas: API y cliente.
echo No cierres la ventana POS.Server mientras uses el sistema.
echo Usuario administrador: admin@pos.local
echo Clave local inicial: SanMagno#2026
echo Direccion: https://localhost:7282/login
echo.
pause
