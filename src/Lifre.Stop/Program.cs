using System.IO;
using System.Diagnostics;
using Lifre.Core;
using Lifre.Interop;
using Lifre.Persistence;

namespace Lifre.Stop;

/// <summary>
/// Точка входу StopLifre.exe — без вікна, без консолі.
/// 1) Надсилає команду «stop» через іменований канал.
/// 2) Якщо процес не завершився — примусово завершує процес StartLifre.
/// Усі дії записуються у файл логів.
/// </summary>
public static class Program
{
    private const int WaitTimeoutMs = 6000;

    /// <summary>Головна точка входу.</summary>
    public static int Main()
    {
        Logger? log = null;

        var stopped = TryGracefulStop(log);
        if (!stopped)
            stopped = TryKillProcesses(log);

        log?.Info(stopped
            ? $"{LifreConstants.AppName} зупинено."
            : $"{LifreConstants.AppName} не знайдено або не вдалося зупинити.");
        return 0;
    }

    /// <summary>Надсилає «stop» через pipe і чекає завершення процесу.</summary>
    private static bool TryGracefulStop(Logger? log)
    {
        try
        {
            if (!CommandPipeClient.Send(LifreCommands.Stop, LifreConstants.PipeTimeoutMs))
                return false;

            log?.Info("Команду 'stop' надіслано.");
            return WaitForExit(TimeSpan.FromMilliseconds(WaitTimeoutMs));
        }
        catch (Exception ex)
        {
            log?.Error("Граційна зупинка не вдалася", ex);
            return false;
        }
    }

    /// <summary>Шукає процеси StartLifre і примусово їх завершує.</summary>
    private static bool TryKillProcesses(Logger? log)
    {
        var found = false;
        try
        {
            foreach (var process in Process.GetProcessesByName(LifreConstants.MainProcessName))
            {
                found = true;
                try
                {
                    log?.Info($"Завершення процесу PID {process.Id}.");
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
                catch (Exception ex)
                {
                    log?.Error($"Не вдалося завершити PID {process.Id}", ex);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            log?.Error("Помилка пошуку процесів", ex);
        }
        return found;
    }

    /// <summary>Очікує, доки не залишиться жодного процесу StartLifre.</summary>
    private static bool WaitForExit(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var processes = Process.GetProcessesByName(LifreConstants.MainProcessName);
            var any = processes.Length > 0;
            foreach (var p in processes)
                p.Dispose();
            if (!any)
                return true;
            Thread.Sleep(200);
        }
        return false;
    }
}
