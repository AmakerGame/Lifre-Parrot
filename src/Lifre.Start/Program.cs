using Lifre.Core;
using Lifre.Interop;

namespace Lifre.Start;

/// <summary>
/// Точка входу StartLifre.exe.
/// Якщо Ліфре вже працює — «будить» наявний екземпляр через іменований
/// канал і завершується, не створюючи другий процес.
/// Інакше захоплює м'ютекс одиночного екземпляра та запускає WPF-застосунок.
/// </summary>
public static class Program
{
    /// <summary>Головна точка входу.</summary>
    [STAThread]
    public static int Main()
    {
        using var singleInstance = new SingleInstance();

        if (!singleInstance.Acquire())
        {
            // Екземпляр уже працює — надсилаємо «wake» і тихо виходимо.
            CommandPipeClient.Send(LifreCommands.Wake);
            return 0;
        }

        var app = new App();
        return app.Run();
    }
}
