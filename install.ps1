# Configurar protocolo de seguridad para la descarga
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$exeUrl = "https://github.com/RazDays/SteamCleaner/releases/download/v1.0.0/SteamCleaner.exe"
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
    Write-Host ".NET 8 Runtime no detectado. Descargando e instalando requisito..." -ForegroundColor Yellow
    $dotnetUrl = "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"
    $dotnetInstaller = "$env:TEMP\dotnet_installer.exe"
    
    Invoke-WebRequest -Uri $dotnetUrl -OutFile $dotnetInstaller
    Start-Process -FilePath $dotnetInstaller -ArgumentList "/passive /norestart" -Wait
    Remove-Item $dotnetInstaller -ErrorAction SilentlyContinue
}

# 3. Descargar SteamCleaner.exe
Write-Host "Descargando SteamCleaner..." -ForegroundColor Green
Invoke-WebRequest -Uri $exeUrl -OutFile $tempPath

# 4. Ocultar la ventana de PowerShell mientras corre tu herramienta
$windowCode = '[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);'
$asyncType = Add-Type -MemberDefinition $windowCode -Name "Win32ShowWindow" -Namespace Win32Functions -PassThru
$null = $asyncType::ShowWindow((Get-Process -Id $PID).MainWindowHandle, 0) # 0 = Ocultar ventana

# 5. Ejecutar SteamCleaner y ESPERAR a que termine (incluso si cierran con la X)
$process = Start-Process -FilePath $tempPath -PassThru
$process.WaitForExit()

# 6. Dar 1 segundo a Windows para liberar el ejecutable y eliminarlo
Start-Sleep -Seconds 1
Remove-Item -Path $tempPath -Force -ErrorAction SilentlyContinue

# 7. Salir
[System.Environment]::Exit(0)
