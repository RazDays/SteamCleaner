using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Runtime.InteropServices;

class Program
{
    // 0 = English (default), 1 = PT-BR, 2 = ES
    static int currentLang = 0;

    // --- IMPORTACIÓN DE API DE WINDOWS PARA MANEJAR LA 'X' DE LA CONSOLA ---
    [DllImport("Kernel32")]
    private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

    private delegate bool EventHandler(CtrlType sig);
    private static EventHandler? _handler;

    private enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }

    // Método que se ejecuta al presionar la 'X' o cerrar la ventana
    private static bool Handler(CtrlType sig)
    {
        switch (sig)
        {
            case CtrlType.CTRL_CLOSE_EVENT:
            case CtrlType.CTRL_LOGOFF_EVENT:
            case CtrlType.CTRL_SHUTDOWN_EVENT:
                // Forza la muerte inmediata del proceso actual sin dejar nada bloqueado
                Process.GetCurrentProcess().Kill();
                return true;
            default:
                return false;
        }
    }

    [STAThread]
    static void Main()
    {
        // Registrar el manejador del botón 'X'
        _handler += new EventHandler(Handler);
        SetConsoleCtrlHandler(_handler, true);

        // Evento adicional de salida de proceso para asegurar limpieza total
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            try
            {
                Process.GetCurrentProcess().Kill();
            }
            catch { }
        };

        // Configurar la consola para interpretar y mostrar caracteres Unicode/UTF-8
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        Console.Title = "Steam Cleaner - by RazDays._";

        // 1. Selector Inicial de Idioma
        SeleccionarIdioma();

        while (true)
        {
            // 2. Selección de Ruta de Steam
            string? steamPath = null;
            bool rutaConfirmada = false;

            while (!rutaConfirmada)
            {
                Console.Clear();
                MostrarBanner();

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(GetText(
                    "--- STEAM FOLDER SELECTION ---",
                    "--- SELEÇÃO DA PASTA DO STEAM ---",
                    "--- SELECCIÓN DE CARPETA DE STEAM ---"
                ));
                Console.WriteLine(GetText(
                    " [1] Automatically detect main Steam folder",
                    " [1] Detectar pasta principal do Steam automaticamente",
                    " [1] Detectar carpeta principal de Steam automáticamente"
                ));
                Console.WriteLine(GetText(
                    " [2] Select specific folder (File Explorer)",
                    " [2] Selecionar pasta específica (Explorador de Arquivos)",
                    " [2] Seleccionar carpeta específica (Explorador de archivos)"
                ));
                Console.WriteLine(GetText(
                    " [3] Drag and drop or enter path manually",
                    " [3] Arrastar ou inserir o caminho manualmente",
                    " [3] Arrastar o ingresar ruta manualmente"
                ));
                Console.WriteLine(GetText(
                    " [4] Change language / Mudar idioma / Cambiar idioma",
                    " [4] Change language / Mudar idioma / Cambiar idioma",
                    " [4] Change language / Mudar idioma / Cambiar idioma"
                ));

                Console.Write("\n" + GetText("Select an option (1-4): ", "Selecione uma opção (1-4): ", "Seleccione una opción (1-4): "));
                string? opRuta = Console.ReadLine()?.Trim();

                if (opRuta == "1")
                {
                    steamPath = BuscarRutaSteam();
                    if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(GetText("\n[!] Steam was not detected in registry.", "\n[!] Steam não foi detectado no registro.", "\n[!] No se pudo detectar Steam en el registro."));
                        Console.ResetColor();
                        PausarBreve();
                        continue;
                    }
                }
                else if (opRuta == "2")
                {
                    steamPath = SeleccionarCarpeta();
                }
                else if (opRuta == "3")
                {
                    // SECCIÓN INDEPENDIENTE PARA LA OPCIÓN 3 (MANUAL / ARRASTRAR)
                    bool canceladoManual = false;

                    while (true)
                    {
                        Console.Clear();
                        MostrarBanner();

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(GetText(
                            "--- MANUAL STEAM PATH INPUT ---",
                            "--- ENTRADA MANUAL DO CAMINHO DO STEAM ---",
                            "--- INGRESO MANUAL DE RUTA DE STEAM ---"
                        ));
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(GetText(
                            "Drag the Steam folder into this window, paste the path, or type '0' to go back.",
                            "Arraste a pasta do Steam para esta janela, cole o caminho ou digite '0' para voltar.",
                            "Arrastre la carpeta de Steam a esta ventana, pegue la ruta o escriba '0' para regresar."
                        ));
                        Console.ResetColor();

                        Console.Write("\n" + GetText("Steam Path > ", "Caminho do Steam > ", "Ruta de Steam > "));
                        string? rawPath = Console.ReadLine()?.Trim();

                        if (string.IsNullOrEmpty(rawPath) || rawPath == "0")
                        {
                            canceladoManual = true;
                            break;
                        }

                        string pathProcesado = rawPath.Replace("\"", "");

                        if (Directory.Exists(pathProcesado))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n" + GetText("Valid folder detected: ", "Pasta válida detectada: ", "Carpeta válida detectada: ") + pathProcesado);
                            Console.ResetColor();

                            Console.Write("\n" + GetText("Do you want to use this folder? (Y/N) [Y to confirm]: ", "Deseja usar esta pasta? (S/N) [S para confirmar]: ", "¿Desea usar esta carpeta? (S/N) [S para confirmar]: "));
                            string? respManual = Console.ReadLine()?.Trim().ToUpper();

                            if (respManual == "Y" || respManual == "S")
                            {
                                steamPath = pathProcesado;
                                rutaConfirmada = true;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(GetText("\n[!] Invalid or non-existent folder path.", "\n[!] Caminho de pasta inválido ou inexistente.", "\n[!] Ruta de carpeta no válida o inexistente."));
                            Console.ResetColor();
                            PausarBreve();
                        }
                    }

                    if (canceladoManual)
                    {
                        continue;
                    }
                }
                else if (opRuta == "4")
                {
                    SeleccionarIdioma();
                    continue;
                }
                else
                {
                    continue;
                }

                if (opRuta != "3" && !string.IsNullOrEmpty(steamPath) && Directory.Exists(steamPath))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n" + GetText("Selected path: ", "Caminho selecionado: ", "Ruta seleccionada: ") + steamPath);
                    Console.ResetColor();

                    Console.Write(GetText("Do you want to use this folder? (Y/N) [Y to confirm]: ", "Deseja usar esta pasta? (S/N) [S para confirmar]: ", "¿Desea usar esta carpeta? (S/N) [S para confirmar]: "));
                    string? resp = Console.ReadLine()?.Trim().ToUpper();

                    if (resp == "Y" || resp == "S")
                    {
                        rutaConfirmada = true;
                    }
                }
            }

            // 3. Selección del Modo de Limpieza
            int modoSeleccionado = 0;
            bool modoConfirmado = false;

            while (!modoConfirmado)
            {
                Console.Clear();
                MostrarBanner();

                Console.WriteLine("==========================================================================");
                Console.WriteLine(GetText("SELECT CLEANUP MODE:", "SELECIONE O MODO DE LIMPEZA:", "SELECCIONE EL MODO DE LIMPIEZA:"));
                
                Console.WriteLine(GetText(
                    " [1] Normal Cleanup",
                    " [1] Limpeza Normal",
                    " [1] Limpieza Normal"
                ));
                
                Console.WriteLine(GetText(
                    " [2] Full Cleanup (Deep Clean)",
                    " [2] Limpeza Profunda (Full Clean)",
                    " [2] Limpieza Profunda (Full Clean)"
                ));

                // AVISO DESTACADO PARA USUARIOS DE LUATOOLS (BORDES Y SÍMBOLOS ASCII CORREGIDOS)
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n +------------------------------------------------------------------+");
                Console.WriteLine(GetText(
                    " |                    [!] NOTICE FOR LUATOOLS [!]                   |",
                    " |               [!] AVISO PARA USUARIOS DO LUATOOLS [!]            |",
                    " |               [!] AVISO PARA USUARIOS DE LUATOOLS [!]            |"
                ));
                Console.WriteLine(GetText(
                    " | If you use tools like LuaTools and want to clear AppData residue,|",
                    " | Se voce usa ferramentas como LuaTools e quer limpar o AppData,   |",
                    " | Si usas herramientas como LuaTools y deseas limpiar AppData,     |"
                ));
                Console.WriteLine(GetText(
                    " | please select Option 3 below.                                    |",
                    " | por favor selecione a Opcao 3 abaixo.                            |",
                    " | por favor selecciona la Opcion 3 a continuacion.                 |"
                ));
                Console.WriteLine(" +------------------------------------------------------------------+\n");
                Console.ResetColor();

                Console.WriteLine(GetText(
                    " [3] Full Purge (+ LuaTools)",
                    " [3] Purga Completa (+ LuaTools)",
                    " [3] Purga Completa (+ LuaTools)"
                ));

                Console.Write("\n" + GetText("Choose an option (1-3): ", "Escolha uma opção (1-3): ", "Elija una opción (1-3): "));
                string? modoOp = Console.ReadLine()?.Trim();

                if (modoOp == "1") modoSeleccionado = 1;
                else if (modoOp == "2") modoSeleccionado = 2;
                else if (modoOp == "3") modoSeleccionado = 3;
                else continue;

                // PANTALLA DETALLADA DE VISTA PREVIA Y CONFIRMACIÓN
                Console.Clear();
                MostrarBanner();

                MostrarDetallesModo(modoSeleccionado);

                Console.Write("\n" + GetText("Do you want to proceed with this cleanup? (Y/N) [Y to continue]: ", "Deseja prosseguir com esta limpeza? (S/N) [S para continuar]: ", "¿Desea proceder con esta limpieza? (S/N) [S para continuar]: "));
                string? confirmacionModo = Console.ReadLine()?.Trim().ToUpper();

                if (confirmacionModo == "Y" || confirmacionModo == "S")
                {
                    modoConfirmado = true;
                }
            }

            // 4. Ejecución del Proceso
            Console.Clear();
            MostrarBanner();

            Console.WriteLine("[1/4] " + GetText("Closing Steam and background processes...", "Fechando o Steam e processos relacionados...", "Cerrando Steam y procesos relacionados..."));
            CerrarSteamYProcesosRelacionados(steamPath!);
            MostrarBarraProgresoFalsa(GetText("Preparing directory", "Preparando diretório", "Preparando directorio"), 15);

            string[] keepList;

            if (modoSeleccionado == 1)
            {
                keepList = new string[] { 
                    "appcache", "config", "steam", "steamapps", "userdata", 
                    "steam.exe", "opensteamtool.toml", "cloud_redirect.dll" 
                };
            }
            else
            {
                keepList = new string[] { 
                    "steam", "steamapps", "userdata", "steam.exe" 
                };
            }

            Console.WriteLine("\n[2/4] " + GetText("Deleting files from Steam directory...", "Deletando arquivos da pasta do Steam...", "Eliminando archivos de la carpeta de Steam..."));
            DirectoryInfo dir = new DirectoryInfo(steamPath!);

            foreach (FileSystemInfo item in dir.GetFileSystemInfos())
            {
                if (!keepList.Contains(item.Name, StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (item is DirectoryInfo subDir)
                            subDir.Delete(true);
                        else
                            item.Delete();

                        Console.WriteLine($"  - Deleted: {item.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ! Could not delete {item.Name}: {ex.Message}");
                    }
                }
            }

            if (modoSeleccionado == 2 || modoSeleccionado == 3)
            {
                Console.WriteLine("\n[3/4] " + GetText("Performing Deep Full Cleanup (AppData & Registry)...", "Executando limpeza profunda (AppData e Registro)...", "Ejecutando limpieza profunda (AppData y Registro)..."));
                
                EliminarCarpetaAppData("LocalAppData", "Steam");
                EliminarCarpetaAppData("AppData", "Steam");
                EliminarRegistroSteam();

                if (modoSeleccionado == 3)
                {
                    Console.WriteLine("\n  -> " + GetText("Purging LuaTools components...", "Purgando componentes do LuaTools...", "Purgando componentes de LuaTools..."));
                    
                    // Borrado en Roaming (AppData)
                    EliminarCarpetaAppData("AppData", "LuaTools");
                    EliminarCarpetaAppData("AppData", "LuaToolsGui");

                    // AGREGADO: Borrado en Local (AppData\Local)
                    EliminarCarpetaAppData("LOCALAPPDATA", "LuaTools");
                    EliminarCarpetaAppData("LOCALAPPDATA", "LuaToolsGui");
                }

                MostrarBarraProgresoFalsa(GetText("Purging leftover cache & registry keys", "Limpando cache e registros", "Limpiando caché y registros"), 25);
            }
            else
            {
                Console.WriteLine("\n[3/4] " + GetText("Skipping Registry & AppData purge (Normal Mode)...", "Ignorando limpeza de Registro e AppData (Modo Normal)...", "Omitiendo limpieza de Registro y AppData (Modo Normal)..."));
                MostrarBarraProgresoFalsa(GetText("Finalizing file structure", "Finalizando estrutura de arquivos", "Finalizando estructura de archivos"), 10);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[SUCCESS] " + GetText("Cleanup completed successfully!", "Limpeza concluída com sucesso!", "¡Limpieza completada con éxito!"));
            Console.ResetColor();

            // 5. Menú Post-Limpieza
            Console.WriteLine("\n==========================================================================");
            Console.WriteLine(GetText(" [1] Launch Steam now", " [1] Iniciar o Steam agora", " [1] Ejecutar Steam ahora"));
            Console.WriteLine(GetText(" [2] Exit", " [2] Sair", " [2] Salir"));
            Console.WriteLine("==========================================================================");

            Console.Write("\n" + GetText("Choose an option (1-2): ", "Escolha uma opção (1-2): ", "Elija una opción (1-2): "));
            string? postOp = Console.ReadLine()?.Trim();

            if (postOp == "1")
            {
                string steamExe = Path.Combine(steamPath!, "steam.exe");
                if (File.Exists(steamExe))
                {
                    Console.WriteLine("\n" + GetText("Launching Steam executable...", "Iniciando executável do Steam...", "Iniciando ejecutable de Steam..."));
                    Process.Start(steamExe);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n[WARNING] " + GetText("steam.exe was not found.", "steam.exe não foi encontrado.", "No se encontró steam.exe."));
                    Console.ResetColor();
                }
            }

            AutoCloseTimer(5);
            
            // Cierre forzado y limpio al finalizar el programa
            Process.GetCurrentProcess().Kill();
            break;
        }
    }

    static void MostrarDetallesModo(int modo)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        if (modo == 1)
        {
            Console.WriteLine("==========================================================================");
            Console.WriteLine(GetText("MODE 1: NORMAL CLEANUP DETAILS", "MODO 1: DETALHES DA LIMPEZA NORMAL", "MODO 1: DETALLES DE LIMPIEZA NORMAL"));
            Console.WriteLine("==========================================================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(GetText(
                " [+] KEEPS: appcache, config, steam (folder), steamapps, userdata, steam.exe, opensteamtool.toml, cloud_redirect.dll",
                " [+] CONSERVA: appcache, config, steam (pasta), steamapps, userdata, steam.exe, opensteamtool.toml, cloud_redirect.dll",
                " [+] CONSERVA: appcache, config, steam (carpeta), steamapps, userdata, steam.exe, opensteamtool.toml, cloud_redirect.dll"
            ));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(GetText(
                " [-] DELETES: Temporary logs, crash dumps, corrupt caches inside Steam folder.",
                " [-] DELETA: Logs temporários, dumps de erro, caches corrompidos dentro da pasta do Steam.",
                " [-] ELIMINA: Logs temporales, archivos de error y caché corrupto dentro de la carpeta de Steam."
            ));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(GetText(
                " [*] NOTE: Safe light cleanup. Keeps your logged account, settings, and game files untouched.",
                " [*] NOTA: Limpeza leve e segura. Mantém sua conta conectada, configurações e jogos intactos.",
                " [*] NOTA: Limpieza ligera y segura. Mantiene tu cuenta iniciada, configuraciones y juegos intactos."
            ));
        }
        else if (modo == 2)
        {
            Console.WriteLine("==========================================================================");
            Console.WriteLine(GetText("MODE 2: DEEP FULL CLEANUP DETAILS", "MODO 2: DETALHES DA LIMPEZA PROFUNDA", "MODO 2: DETALLES DE LIMPIEZA PROFUNDA"));
            Console.WriteLine("==========================================================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(GetText(
                " [+] KEEPS ONLY: steam (folder), steamapps, userdata, steam.exe",
                " [+] CONSERVA APENAS: steam (pasta), steamapps, userdata, steam.exe",
                " [+] CONSERVA SOLO: steam (carpeta), steamapps, userdata, steam.exe"
            ));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(GetText(
                " [-] DELETES: Everything in Steam folder + AppData cache + HKCU Registry keys.",
                " [-] DELETA: Tudo na pasta do Steam + cache do AppData + chaves do Registro HKCU.",
                " [-] ELIMINA: Todo en la carpeta Steam + caché de AppData + claves del Registro HKCU."
            ));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(GetText(
                " [*] NOTE: Deep reset. Re-login will be required, but installed games & saves ARE SAVED.",
                " [*] NOTA: Reset profundo. Será necessário fazer login novamente, mas jogos e saves ESTÃO SALVOS.",
                " [*] NOTA: Reset profundo. Se requerirá iniciar sesión de nuevo, pero tus juegos y saves ESTÁN A SALVO."
            ));
        }
        else if (modo == 3)
        {
            Console.WriteLine("==========================================================================");
            Console.WriteLine(GetText("MODE 3: FULL PURGE (+ LUATOOLS) DETAILS", "MODO 3: DETALHES DA PURGA COMPLETA (+ LUATOOLS)", "MODO 3: DETALLES DE PURGA COMPLETA (+ LUATOOLS)"));
            Console.WriteLine("==========================================================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(GetText(
                " [+] KEEPS ONLY: steam (folder), steamapps, userdata, steam.exe",
                " [+] CONSERVA APENAS: steam (pasta), steamapps, userdata, steam.exe",
                " [+] CONSERVA SOLO: steam (carpeta), steamapps, userdata, steam.exe"
            ));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(GetText(
                " [-] DELETES: Steam folder + AppData + Registry AND LuaTools / LuaToolsGui AppData folders.",
                " [-] DELETA: Pasta do Steam + AppData + Registro E pastas LuaTools / LuaToolsGui no AppData.",
                " [-] ELIMINA: Carpeta de Steam + AppData + Registro Y las carpetas de LuaTools / LuaToolsGui en AppData."
            ));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(GetText(
                " [*] NOTE: Complete purge. Forces a clean reinstall for both Steam and LuaTools.",
                " [*] NOTA: Purga completa. Força uma reinstalação limpa do Steam e do LuaTools.",
                " [*] NOTA: Purga completa. Fuerza una reinstalación limpia tanto de Steam como de LuaTools."
            ));
        }
        Console.ResetColor();
    }

    static void SeleccionarIdioma()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=================================================");
        Console.WriteLine("          SELECT LANGUAGE / IDIOMA               ");
        Console.WriteLine("=================================================");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" [1] English");
        Console.WriteLine(" [2] Português (Brasil)");
        Console.WriteLine(" [3] Español (Latinoamérica)");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=================================================");
        Console.ResetColor();
        
        Console.Write("\nChoose an option / Elija una opción (1-3) [Default 1]: ");
        string? input = Console.ReadLine()?.Trim();

        if (input == "2") currentLang = 1;      // PT-BR
        else if (input == "3") currentLang = 2; // ES
        else currentLang = 0;                   // EN (Default)
    }

    static string GetText(string en, string pt, string es)
    {
        if (currentLang == 1) return pt;
        if (currentLang == 2) return es;
        return en;
    }

    static void MostrarBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;

        string[] asciiLines = new string[]
        {
            @"  _____ __                          __________                               ",
            @" / ___// /____  ____ _____ ___ / /   / ____/ /__  ____ _____  ___  _____     ",
            @" \__ \/ __/ _ \/ __ `/ __ `__ \/ /  / /   / / _ \/ __ `/ __ \/ _ \/ ___/     ",
            @"___/ / /_/  __/ /_/ / / / / / / /  / /___/ /  __/ /_/ / / / /  __/ /         ",
            @"/____/\__/\___/\__,_/_/ /_/ /_/_/   \____/_/\___/\__,_/_/ /_/\___/_/         "
        };

        foreach (string line in asciiLines)
        {
            Console.WriteLine(line);
            Thread.Sleep(30);
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n                 Developer & Author: RazDays._");
        Console.WriteLine("==========================================================================\n");
        Console.ResetColor();
    }

    static void MostrarBarraProgresoFalsa(string tarea, int pasos)
    {
        Console.Write($" -> {tarea}: [");
        int totalBloques = 20;

        for (int i = 0; i <= totalBloques; i++)
        {
            Console.Write("█");
            Thread.Sleep(pasos);
        }
        Console.WriteLine("] 100%");
    }

    static string? BuscarRutaSteam()
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                if (key != null)
                {
                    object? path = key.GetValue("SteamPath");
                    if (path != null) return path.ToString()?.Replace('/', '\\');
                }
            }
        }
        catch { }
        return null;
    }

    static string? SeleccionarCarpeta()
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = GetText("Select the main Steam directory", "Selecione o diretório principal do Steam", "Seleccione el directorio principal de Steam");
            dialog.ShowNewFolderButton = false;
            return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
        }
    }

    static void CerrarSteamYProcesosRelacionados(string steamPath)
    {
        // AGREGADO: Processos directos de LuaTools agregados para asegurar liberación de archivos
        string[] directProcesses = { "steam", "steamservice", "steamwebhelper", "luatools", "luatoolsgui" };
        foreach (string procName in directProcesses)
        {
            foreach (var process in Process.GetProcessesByName(procName))
            {
                try { process.Kill(); process.WaitForExit(1000); } catch { }
            }
        }

        Process[] allProcesses = Process.GetProcesses();
        foreach (Process proc in allProcesses)
        {
            try
            {
                if (proc.MainModule != null && !string.IsNullOrEmpty(proc.MainModule.FileName))
                {
                    string procPath = proc.MainModule.FileName;
                    if (procPath.StartsWith(steamPath, StringComparison.OrdinalIgnoreCase))
                    {
                        proc.Kill();
                        proc.WaitForExit(1000);
                    }
                }
            }
            catch { }
        }
    }

    static void EliminarCarpetaAppData(string appDataEnv, string subFolder)
    {
        try
        {
            string? baseDir = Environment.GetEnvironmentVariable(appDataEnv);
            if (!string.IsNullOrEmpty(baseDir))
            {
                string targetPath = Path.Combine(baseDir, subFolder);
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                    Console.WriteLine($"  - AppData Deleted: {targetPath}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ! AppData warning: {ex.Message}");
        }
    }

    static void EliminarRegistroSteam()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Valve\Steam", false);
            Console.WriteLine("  - Registry keys purged: HKCU\\Software\\Valve\\Steam");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ! Registry purge warning: {ex.Message}");
        }
    }

    static void AutoCloseTimer(int segundos)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();

        for (int i = segundos; i > 0; i--)
        {
            Console.Write($"\r" + GetText(
                $"Press any key to close immediately or wait {i} seconds...",
                $"Pressione qualquer tecla para fechar ou aguarde {i} segundos...",
                $"Presione cualquier tecla para cerrar o espere {i} segundos..."
            ) + "   ");

            for (int ms = 0; ms < 10; ms++)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    return;
                }
                Thread.Sleep(100);
            }
        }
        Console.ResetColor();
    }

    static void PausarBreve()
    {
        Thread.Sleep(1500);
    }
}