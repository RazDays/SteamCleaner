# Configurar protocolo de seguridad para la descarga
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Usamos /releases/latest/download/ para obtener SIEMPRE la versión mas reciente
$exeUrl = "https://github.com/RazDays/SteamCleaner/releases/latest/download/SteamCleaner.exe"
$tempPath = "$env:TEMP\SteamCleaner.exe"

Write-Host "Verificando entorno de ejecucion..." -ForegroundColor Cyan

# 1. Comprobar si .NET 8 Desktop Runtime esta instalado
$dotnetInstalled = $false
try {
    $runtimes = dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft.WindowsDesktop.App 8\.") {
        $dotnetInstalled = $true
    }
} catch {
    $dotnetInstalled = $false
}

# 2. Si no esta instalado, descargarlo e instalarlo de forma silenciosa
if (-not $dotnetInstalled) {
    Write-Host ".NET 8 Runtime no detectado. Descargando e instalarlo requisito..." -ForegroundColor Yellow
    $dotnetUrl = "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"
    $dotnetInstaller = "$env:TEMP\dotnet_installer.exe"
    
    Invoke-WebRequest -Uri $dotnetUrl -OutFile $dotnetInstaller
    Start-Process -FilePath $dotnetInstaller -ArgumentList "/passive /norestart" -Wait
    Remove-Item $dotnetInstaller -ErrorAction SilentlyContinue
}

# 3. Limpiar ejecutable antiguo de la carpeta Temp (si existe)
if (Test-Path $tempPath) {
    Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
}

# 4. Descargar SteamCleaner.exe (Ultima versión)
Write-Host "Descargando SteamCleaner..." -ForegroundColor Green
Invoke-WebRequest -Uri $exeUrl -OutFile $tempPath

# 5. Iniciar tu programa
Start-Process -FilePath $tempPath

# 6. Cerrar PowerShell inmediatamente
[System.Environment]::Exit(0)
