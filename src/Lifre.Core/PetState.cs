namespace Lifre.Core;

/// <summary>
/// Стани скінченного автомата поведінки папуги.
/// </summary>
public enum PetState
{
    /// <summary>Спокій — стоїть, кліпає, дивиться на курсор.</summary>
    Idle,

    /// <summary>Ходить по «землі» (перевалочка).</summary>
    Walk,

    /// <summary>Стрибає кількома підскоками.</summary>
    Hop,

    /// <summary>Летить до точки на екрані.</summary>
    Fly,

    /// <summary>Приземляється після польоту.</summary>
    Land,

    /// <summary>Спить.</summary>
    Sleep,

    /// <summary>Клює насіння, відновлює голод.</summary>
    Eat,

    /// <summary>Танцює: підстрибує, розвертається, нотки.</summary>
    Play,

    /// <summary>Чепурить пір'я.</summary>
    Preen,

    /// <summary>Підходить до курсора, цікаво дивиться.</summary>
    Peek,

    /// <summary>Радіє після погладжування.</summary>
    Petted,

    /// <summary>Ображений після надто багатьох кліків (безпечно, без втручання в систему).</summary>
    Grumpy,

    /// <summary>Користувач тримає папугу мишею.</summary>
    Held,

    /// <summary>Падає після того, як його відпустили.</summary>
    Fall,

    /// <summary>Летить за край екрана по подарунок.</summary>
    FlyOff,

    /// <summary>За екраном (невидимий), шукає подарунок.</summary>
    Away,

    /// <summary>Прилітає з подарунком у дзьобі.</summary>
    BringGift,

    /// <summary>Стоїть і простягає подарунок — клікніть, щоб прийняти.</summary>
    PresentGift
}
