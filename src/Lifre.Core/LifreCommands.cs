namespace Lifre.Core;

/// <summary>
/// Команди, що передаються між процесами через іменований канал.
/// </summary>
public static class LifreCommands
{
    /// <summary>«Розбудити» вже запущений екземпляр (перемкнути вікно на передній план / привітатися).</summary>
    public const string Wake = "wake";

    /// <summary>Поставити на паузу.</summary>
    public const string Pause = "pause";

    /// <summary>Зняти паузу.</summary>
    public const string Resume = "resume";

    /// <summary>Коректно завершити застосунок.</summary>
    public const string Stop = "stop";
}
