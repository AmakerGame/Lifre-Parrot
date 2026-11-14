namespace Lifre.Core;

/// <summary>
/// Потреби папуги. Значення в діапазоні 0..100.
/// Чим нижче значення — тим сильніше стан впливає на вибір поведінки.
/// </summary>
public sealed class Needs
{
    /// <summary>Голод: зменшується з часом, відновлюється під час <see cref="PetState.Eat"/>.</summary>
    public double Hunger { get; set; } = 100.0;

    /// <summary>Енергія: зменшується з часом, відновлюється під час <see cref="PetState.Sleep"/>.</summary>
    public double Energy { get; set; } = 100.0;

    /// <summary>Настрій: зменшується повільно, відновлюється грою, сном і погладжуванням.</summary>
    public double Mood { get; set; } = 100.0;

    /// <summary>Клонує поточні значення потреб.</summary>
    public Needs Clone() => new() { Hunger = Hunger, Energy = Energy, Mood = Mood };

    /// <summary>Копіює значення з іншого екземпляра.</summary>
    public void CopyFrom(Needs other)
    {
        Hunger = other.Hunger;
        Energy = other.Energy;
        Mood = other.Mood;
    }
}
