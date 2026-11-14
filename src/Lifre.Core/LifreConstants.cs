namespace Lifre.Core;

/// <summary>
/// Глобальні константи застосунку: імена процесів, IPC, таймінги.
/// Жодних магічних чисел у коді — усе посилається сюди або на конфігурацію.
/// </summary>
public static class LifreConstants
{
    /// <summary>Назва застосунку.</summary>
    public const string AppName = "Lifre";

    /// <summary>Ім'я глобального м'ютекса одиночного екземпляра.</summary>
    public const string MutexName = @"Global\Lifre_SingleInstance_Mutex";

    /// <summary>Ім'я локального м'ютекса (fallback, якщо Global\ недоступний).</summary>
    public const string MutexNameLocal = @"Local\Lifre_SingleInstance_Mutex";

    /// <summary>Ім'я іменованого каналу для команд (wake / pause / resume / stop).</summary>
    public const string PipeName = "Lifre.Commands";

    /// <summary>Ім'я процесу головного застосунку (StartLifre.exe).</summary>
    public const string MainProcessName = "StartLifre";

    /// <summary>Таймаут очікування відповіді на команду через pipe, мс.</summary>
    public const int PipeTimeoutMs = 4000;

    /// <summary>Розмір шрифту реплік.</summary>
    public const double SpeechFontSize = 13.0;

    /// <summary>Ширина вікна папуги (DIP).</summary>
    public const double WindowWidth = 240.0;

    /// <summary>Висота вікна папуги (DIP): місце для бульбашки над головою + сам папуга.</summary>
    public const double WindowHeight = 282.0;

    /// <summary>Висота зони бульбашки над папугою всередині вікна.</summary>
    public const double SpriteHeadroom = 70.0;

    /// <summary>Зсув спрайта по X усередині вікна.</summary>
    public const double SpriteOffsetX = 10.0;

    /// <summary>
    /// Наскільки низ вікна виступає нижче робочої області. 0 — найнижча точка лапок стоїть рівно на краю
    /// панелі завдань (раніше було 9: лапи занурювались під панель Windows і обрізались).
    /// </summary>
    public const double FeetInset = 0.0;
}
