using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Lifre.Behavior;
using Lifre.Core;
using Lifre.Persistence;

namespace Lifre.UI;

/// <summary>
/// Головний хост застосунку: ініціалізація шляхів, текстів, конфігу,
/// стану; створення вікна, трей-іконки та сервера команд;
/// автозбереження та глобальна обробка помилок (усе — у лог).
/// </summary>
public static class LifreAppHost
{
    private static ParrotWindow? _window;
    private static LifreTray? _tray;
    private static PetBrain? _brain;
    private static Logger? _log;
    private static TextStore? _texts;
    private static LifreConfig? _config;
    private static CommandPipeServer? _pipeServer;
    private static DispatcherTimer? _autosaveTimer;

    /// <summary>Ініціалізує застосунок. Викликається з App.OnStartup.</summary>
    public static async Task InitializeAsync(Application app)
    {
        await Task.CompletedTask;
        _log = new Logger();
        _texts = new TextStore();
        _texts.Reload();
        _config = new LifreConfig();

        _brain = new PetBrain(_texts, _config);
        if (_config.StartPaused)
            _brain.IsPaused = true;

        _window = new ParrotWindow(_brain, _texts, _config);
        _tray = new LifreTray(_texts, _window);

        _pipeServer = new CommandPipeServer(HandleCommandAsync, _log);
        _pipeServer.Start();

        // Глобальні обробники помилок — усе у лог, жодних MessageBox.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            _log.Error("Необроблений виняток", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _log.Error("Необроблений виняток у задачі", args.Exception);
            args.SetObserved();
        };

        _window.Show();
        _window.Wake();
    }

    /// <summary>Обробляє команди з іменованого каналу у UI-потоці.</summary>
    private static Task HandleCommandAsync(string command)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return Task.CompletedTask;

        return dispatcher.InvokeAsync(() =>
        {
            try
            {
                switch (command)
                {
                    case LifreCommands.Wake:
                        _window?.Wake();
                        break;
                    case LifreCommands.Pause:
                        if (_window is not null)
                        {
                            _window.CommandPauseToggle();
                            _tray?.RebuildMenu();
                        }
                        break;
                    case LifreCommands.Resume:
                        if (_window is { IsPaused: true })
                        {
                            _window.CommandPauseToggle();
                            _tray?.RebuildMenu();
                        }
                        break;
                    case LifreCommands.Stop:
                        _log?.Info("Отримано команду зупинки.");
                        Application.Current?.Shutdown();
                        break;
                    case "feed":
                        _window?.CommandFeed();
                        break;
                    case "play":
                        _window?.CommandPlay();
                        break;
                    case "sleep":
                        _window?.CommandSleepToggle();
                        break;
                    case "fly":
                        _window?.CommandFly();
                        break;
                    case "gift":
                        _window?.CommandGift();
                        break;
                    default:
                        _log?.Info($"Невідома команда: {command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _log?.Error("Помилка обробки команди", ex);
            }
        }).Task;
    }

    /// <summary>Коректне завершення: зберігає стан, звільняє ресурси.</summary>
    public static void Shutdown()
    {
        _autosaveTimer?.Stop();
        _pipeServer?.Dispose();
        _tray?.Dispose();
        _log?.Info($"{LifreConstants.AppName} завершено.");
    }
}
