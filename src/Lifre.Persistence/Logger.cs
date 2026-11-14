namespace Lifre.Persistence;

/// <summary>
/// Заглушка логера: застосунок нічого не пише на диск (ні логів, ні папок у Документах).
/// Методи залишені, щоб решта коду не змінювалась.
/// </summary>
public sealed class Logger
{
    /// <summary>Створює логер (нічого не робить).</summary>
    public Logger()
    {
    }

    /// <summary>Створює логер (параметр ігнорується).</summary>
    public Logger(string path)
    {
    }

    /// <summary>Інформаційне повідомлення (ігнорується).</summary>
    public void Info(string message)
    {
    }

    /// <summary>Повідомлення про помилку (ігнорується).</summary>
    public void Error(string message, Exception? ex = null)
    {
    }
}
