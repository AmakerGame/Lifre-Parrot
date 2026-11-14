using System.IO;
using System.IO.Pipes;
using Lifre.Core;
using Lifre.Persistence;

namespace Lifre.UI;

/// <summary>
/// Сервер іменованого каналу: приймає команди від StopLifre.exe
/// та повторних запусків StartLifre.exe («wake», «pause», «resume», «stop»).
/// Команди маршаляться у UI-потік через наданий делегат.
/// </summary>
public sealed class CommandPipeServer : IDisposable
{
    private readonly Func<string, Task> _onCommand;
    private readonly Logger _log;
    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    /// <summary>Створює сервер із обробником команд.</summary>
    public CommandPipeServer(Func<string, Task> onCommand, Logger log)
    {
        _onCommand = onCommand;
        _log = log;
    }

    /// <summary>Запускає фоновий потік очікування підключень.</summary>
    public void Start()
    {
        _thread = new Thread(ListenLoop)
        {
            IsBackground = true,
            Name = "Lifre-PipeServer"
        };
        _thread.Start();
        _log.Info("Сервер команд запущено.");
    }

    private void ListenLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                pipe = new NamedPipeServerStream(
                    LifreConstants.PipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                pipe.WaitForConnectionAsync(_cts.Token).GetAwaiter().GetResult();

                using var reader = new StreamReader(pipe);
                var command = reader.ReadLine()?.Trim().ToLowerInvariant();
                _log.Info($"Отримано команду: {command ?? "(порожньо)"}");

                if (!string.IsNullOrEmpty(command))
                    _onCommand(command).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.Error("Помилка каналу команд", ex);
            }
            finally
            {
                pipe?.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            // Підштовхуємо сервер, щоб цикл завершився.
            using var client = new NamedPipeClientStream(".", LifreConstants.PipeName, PipeDirection.Out);
            client.Connect(500);
        }
        catch
        {
            // Ігноруємо — сервер уже зупинений або не створений.
        }
        _cts.Dispose();
    }
}
