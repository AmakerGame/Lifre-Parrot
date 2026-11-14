namespace Lifre.Core;

/// <summary>
/// Знімок стану, що зберігається у save.json: позиція, потреби, статистика.
/// </summary>
public sealed class SaveState
{
    /// <summary>Позиція лівого верхнього кута вікна папуги (DIP-координати).</summary>
    public double PosX { get; set; }

    /// <summary>Позиція лівого верхнього кута вікна папуги (DIP-координати).</summary>
    public double PosY { get; set; }

    /// <summary>Потреби папуги.</summary>
    public Needs Needs { get; set; } = new();

    /// <summary>Поточний стан скінченного автомату.</summary>
    public string State { get; set; } = nameof(PetState.Idle);

    /// <summary>Напрямок погляду: -1 ліворуч, 1 праворуч.</summary>
    public int Facing { get; set; } = 1;

    /// <summary>Кількість погладжувань за всю історію.</summary>
    public long TimesPetted { get; set; }

    /// <summary>Кількість прийомів їжі.</summary>
    public long TimesFed { get; set; }

    /// <summary>Кількість ігор.</summary>
    public long TimesPlayed { get; set; }

    /// <summary>Скільки подарунків прийнято.</summary>
    public long GiftsGiven { get; set; }
}
