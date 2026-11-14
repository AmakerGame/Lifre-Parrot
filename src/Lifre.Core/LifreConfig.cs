namespace Lifre.Core;

/// <summary>
/// Налаштування застосунку, що зберігаються у config.json.
/// Усі поля необов'язкові — відсутні значення замінюються типовими.
/// </summary>
public sealed class LifreConfig
{
    /// <summary>Чи показувати репліки папуги (фрази з phrases.txt).</summary>
    public bool ShowSpeech { get; set; } = true;

    /// <summary>Множник швидкості пересування та польоту (0.2 .. 3.0).</summary>
    public double MovementSpeed { get; set; } = 1.0;

    /// <summary>Множник швидкості зменшення потреб (0.1 .. 5.0).</summary>
    public double DecayRate { get; set; } = 1.0;

    /// <summary>Чи папуга завжди поверх усіх вікон.</summary>
    public bool AlwaysOnTop { get; set; } = true;

    /// <summary>Інтервал (секунди) між автоматичними діями у стані Idle.</summary>
    public double IdleThinkSeconds { get; set; } = 4.0;

    /// <summary>Чи стартувати на паузі (нічого не робити, поки не натиснути «Продовжити»).</summary>
    public bool StartPaused { get; set; } = false;

    /// <summary>Чи приносити подарунки.</summary>
    public bool GiftsEnabled { get; set; } = true;

    /// <summary>Середній інтервал між подарунками, секунди (мінімум 30).</summary>
    public double GiftIntervalSeconds { get; set; } = 120.0;
}
