namespace Lifre.Core;

/// <summary>
/// Позиція папуги в координатах робочого столу та параметри анімації.
/// Єдиний екземпляр спільний для «мозку» і рендерера.
/// </summary>
public sealed class PetPose
{
    /// <summary>Поточний стан.</summary>
    public PetState State { get; set; } = PetState.Idle;

    /// <summary>Координата X лівого верхнього кута вікна.</summary>
    public double X { get; set; }

    /// <summary>Координата Y лівого верхнього кута вікна.</summary>
    public double Y { get; set; }

    /// <summary>Напрямок: -1 — вліво, 1 — вправо.</summary>
    public double Facing { get; set; } = 1;

    /// <summary>Плавний напрямок (-1..1) для анімації розвороту.</summary>
    public double FacingAnim { get; set; } = 1;

    /// <summary>Фаза анімації (росте з часом залежно від стану).</summary>
    public double AnimPhase { get; set; }

    /// <summary>Висота стрибка над землею (DIP).</summary>
    public double Lift { get; set; }

    /// <summary>Присідання/приземлення 0..1 (стискає тіло).</summary>
    public double Squash { get; set; }

    /// <summary>Фаза кліпання очей 0..1.</summary>
    public double BlinkPhase { get; set; }

    /// <summary>Інтенсивність «погладжування» 0..1.</summary>
    public double PetIntensity { get; set; }

    /// <summary>Інтенсивність їжі 0..1 — дзьоб відкривається.</summary>
    public double EatIntensity { get; set; }

    /// <summary>Фаза сну 0..1.</summary>
    public double SleepPhase { get; set; }

    /// <summary>Рівень образи 0..1 (брови, щоки, схрещені крила).</summary>
    public double Anger { get; set; }

    /// <summary>Куди дивляться очі (-1..1), вже з урахуванням напрямку.</summary>
    public double GazeX { get; set; }

    /// <summary>Куди дивляться очі по вертикалі (-1..1).</summary>
    public double GazeY { get; set; }

    /// <summary>Подарунок у дзьобі (None — немає).</summary>
    public GiftKind Gift { get; set; } = GiftKind.None;

    /// <summary>Спалах іскор після прийняття подарунка 1..0.</summary>
    public double Burst { get; set; }

    /// <summary>Чи показувати насіння перед папугою (під час їжі).</summary>
    public bool Food { get; set; }

    /// <summary>Поточний текст репліки (null — мовчить).</summary>
    public string? Speech { get; set; }

    /// <summary>Прозорість бульбашки 0..1.</summary>
    public double SpeechAlpha { get; set; }

    /// <summary>1, поки папуга «говорить» (дзьоб ворушиться).</summary>
    public double Talk { get; set; }

    /// <summary>Папуга за межами екрана — вікно ховається.</summary>
    public bool Hidden { get; set; }

    /// <summary>Згладжена швидкість по X (DIP/с).</summary>
    public double VelX { get; set; }

    /// <summary>Згладжена швидкість по Y (DIP/с).</summary>
    public double VelY { get; set; }

    /// <summary>Фаза кроку (радіани), росте разом із пройденою відстанню — лапи не «ковзають».</summary>
    public double Stride { get; set; }

    /// <summary>Нахил корпусу (градуси), уже згладжений.</summary>
    public double Lean { get; set; }

    /// <summary>Швидке тремтіння (градуси): струшування пір'я, злість.</summary>
    public double Wobble { get; set; }

    /// <summary>Наскільки підібрані лапи 0..1 (плавно).</summary>
    public double Tuck { get; set; }

    /// <summary>Наскільки розкриті крила 0..1 (плавно).</summary>
    public double WingOpen { get; set; }

    /// <summary>Сила удару при приземленні 1..0 (для пилу).</summary>
    public double Impact { get; set; }

    /// <summary>Різновид чепуріння: 0 — дзьобом по крилу, 1 — потягується, 2 — струшується.</summary>
    public int PreenKind { get; set; }

    /// <summary>Інерція корпусу (градуси нахилу «зсувом»): верх і гребінь відстають при розгоні.</summary>
    public double Sway { get; set; }

    /// <summary>Інтенсивність ходьби 0..1 (плавно): визначає розмах і підйом лап.</summary>
    public double Gait { get; set; }

    /// <summary>Прогрес поточного стану 0..1 (за таймером), для їжі тощо.</summary>
    public double Progress { get; set; }

    /// <summary>Фаза помаху крил (радіани). Росте з плавною частотою, яку задає мозок.</summary>
    public double WingPhase { get; set; }

    /// <summary>Розмах помаху крил 0..1 (плавно): у польоті 1, при падінні/підхопленні — легке тріпотіння.</summary>
    public double WingAmp { get; set; }

    /// <summary>Скільки символів репліки вже «надруковано» (ефект друкарської машинки).</summary>
    public double SpeechChars { get; set; }
}
