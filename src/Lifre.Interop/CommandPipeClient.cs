using System.IO;
using System.IO.Pipes;
using Lifre.Core;

namespace Lifre.Interop;

/// <summary>
/// Клієнт іменованого каналу: надсилає команди («wake», «stop» тощо)
/// до вже запущеного екземпляра Ліфре. Повністю неблокуючий щодо UI.
/// </summary>
public static class CommandPipeClient
{
    /// <summary>
    /// Надсилає команду в канав і повертає <c>true</c>, якщо хтось її прийняв.
    /// </summary>
    public static bool Send(string command, int timeoutMs = LifreConstants.PipeTimeoutMs)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", LifreConstants.PipeName, PipeDirection.Out);
            pipe.Connect(timeoutMs);
            using var writer = new StreamWriter(pipe) { AutoFlush = true };
            writer.WriteLine(command);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
