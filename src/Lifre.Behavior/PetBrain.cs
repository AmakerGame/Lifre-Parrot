using System.Windows;
using Lifre.Core;
using Lifre.Persistence;

namespace Lifre.Behavior;

/// <summary>
/// Мозок Ліфре. Папуга живе на «землі» (низ робочої області): ходить, стрибає,
/// танцює, чепурить пір'я, підходить до курсора, спить і їсть. Раз на кілька хвилин
/// відлітає за край екрана й повертається з подарунком — клікніть, щоб прийняти.
/// Його можна підхопити мишею (він теж дригає лапами) і відпустити — впаде й приземлиться.
///
/// Жодних втручань у систему: ніяких Блокнотів, натискань клавіш чи переміщення чужих вікон.
/// Усі «розмови» — у бульбашці над головою.
/// </summary>
public sealed class PetBrain
{
    // ------------------------------------------------------------------
    // Поля
    // ------------------------------------------------------------------

    private readonly Random _rng = new();
    private readonly TextStore _texts;
    private readonly LifreConfig _config;

    private double _stateTimer;
    private double _thinkTimer;
    private double _blinkTimer = 1.5;
    private bool _blinkClosing;
    private double _blinkHold;
    private double _giftTimer;
    private double _clickStreakDecay;
    private int _clickStreak;

    private double _speechTimer;
    private double _speechAge;

    private Point _target;
    private Point _cursor;
    private Rect _bounds = Rect.Empty;
    private double _w = LifreConstants.WindowWidth;
    private double _h = LifreConstants.WindowHeight;
    private double _groundY;

    private bool _paused;
    private bool _hasEntered;

    // Вліт з-за краю екрана: поки папуга не опинився повністю на екрані, його не притискає до меж.
    private bool _entering;
    private int _entryStage;
    private Point _entryGround;

    // Стрибки
    private int _hopsLeft;
    private int _hopDir = 1;
    private double _hopT;
    private double _hopFromX;
    private double _hopToX;
    private double _hopCrouch;
    private const double HopCrouchTime = 0.26;

    // Танець
    private double _danceT;
    private double _danceFlip;

    // Підхоплення / падіння
    private Vector _holdOffset;
    private double _velY;

    // Анімація
    private double _stateAge;
    private double _glanceTimer;
    private double _glanceX;
    private double _glanceY;
    private Point _lastCursor;
    private double _cursorStill;
    private int _preenKind;
    private double _squashVel;
    private double _squashTarget;
    private double _swayTarget;
    private double _swayVel;
    private double _stateDuration;
    private double _wingFreq = 3.0;
    private int _flyLegs;
    private Vector _vel;
    private double _sleepMin = 30;

    // Подарунки
    private double _awayTimer;
    private bool _apologyPending;
    private bool _nudgeSaid;
    private GiftKind _lastGift = GiftKind.None;

    // ------------------------------------------------------------------
    // Публічний стан
    // ------------------------------------------------------------------

    public PetState State { get; private set; } = PetState.Idle;
    public Needs Needs { get; } = new();
    public PetPose Pose { get; } = new();
    public SaveState Stats { get; } = new();

    /// <summary>
    /// «Живі очі»: погляд рухається сам (роззирається, перебігає), а не стежить за мишкою.
    /// Увімкнено за замовчуванням; вимкнено — очі дивляться на курсор.
    /// </summary>
    public bool LiveEyes { get; set; } = true;

    public bool IsPaused
    {
        get => _paused;
        set
        {
            _paused = value;
            if (!_paused)
                return;

            ClearSpeech();
            Pose.Gift = GiftKind.None;
            Pose.Lift = 0;
            Pose.Food = false;
            _velY = 0;
            // Якщо папуга був за екраном (вліт, політ по подарунок) — не «вистрибує» з краю, а падає з-над екрана.
            if (!_bounds.IsEmpty && (Pose.Hidden || _entering
                || Pose.X < _bounds.Left - 40 || Pose.X > _bounds.Right - _w + 40))
            {
                Pose.X = _bounds.Left + (_bounds.Width - _w) / 2;
                Pose.Y = _bounds.Top;
            }
            _entering = false;
            State = _groundY > 0 && Pose.Y < _groundY - 3 ? PetState.Fall : PetState.Idle;
        }
    }

    public PetBrain(TextStore texts, LifreConfig config)
    {
        _texts = texts;
        _config = config;
        _thinkTimer = 1.0 + _rng.NextDouble() * 1.5;
        _giftTimer = Math.Max(30, config.GiftIntervalSeconds) * (0.35 + _rng.NextDouble() * 0.3);
    }

    // ------------------------------------------------------------------
    // Збереження / відновлення
    // ------------------------------------------------------------------

    // ------------------------------------------------------------------
    // Команди користувача
    // ------------------------------------------------------------------

    private bool IsBusyForCommands => State is PetState.Held or PetState.Fall or PetState.Away
        or PetState.FlyOff or PetState.BringGift or PetState.PresentGift
        or PetState.Fly or PetState.Land;

    /// <summary>Команда «Літати»: піднімається й облітає екран кількома дугами, потім приземляється.</summary>
    public void FlyNow()
    {
        if (_bounds.IsEmpty || IsPaused || IsBusyForCommands)
            return;
        if (State == PetState.Sleep)
            Transition(PetState.Idle, 0.2);
        Say(Pick("Політаємо!", "Тримайся — злітаю!", "Вітер у пір'ї!"), 2.5);
        BeginFly();
    }

    /// <summary>Клік: гладить; кілька поспіль — папуга ображається; на подарунку — приймає його.</summary>
    public void Pet()
    {
        if (IsPaused || State is PetState.Held or PetState.Fall or PetState.Away or PetState.FlyOff)
            return;

        if (State == PetState.PresentGift)
        {
            AcceptGift();
            return;
        }

        if (State is PetState.Fly or PetState.Land or PetState.BringGift)
        {
            Say("Не відволікай — я лечу!", 2.5);
            return;
        }

        if (State == PetState.Sleep)
        {
            Transition(PetState.Idle, 0.4);
            Say("Ой! Я ж спав…", 2.5);
            return;
        }

        _clickStreak++;
        _clickStreakDecay = 2.2;

        if (_clickStreak >= 5)
        {
            _clickStreak = 0;
            Pose.Anger = 1.0;
            _apologyPending = _config.GiftsEnabled;
            Transition(PetState.Grumpy, 4.5);
            Say("Досить тицяти! Я ображений!", 3.5);
            return;
        }

        if (_clickStreak >= 3)
        {
            Pose.Anger = Math.Min(1.0, Pose.Anger + 0.45);
            Transition(PetState.Petted, 1.0);
            Say(Pick("Ей, обережніше!", "Ще раз — і я ображусь.", "Папуга починає сердитись.", "Мені подобається… але не так часто."), 3);
            Stats.TimesPetted++;
            return;
        }

        Pose.Anger = Math.Max(0, Pose.Anger - 0.12);
        Transition(PetState.Petted, 1.8);
        Needs.Mood = Math.Min(100, Needs.Mood + 5);
        Needs.Energy = Math.Min(100, Needs.Energy + 1);
        Stats.TimesPetted++;
        if (_rng.NextDouble() < 0.55)
            Say(Pick("Мур-р… тобто карр!", "Ще трошки!", "Мені приємно.", "Піп-піп!", "Ти хороший."), 2.5);
    }

    public void Feed()
    {
        if (IsPaused || IsBusyForCommands)
            return;
        Transition(PetState.Eat, 3.5);
        Stats.TimesFed++;
        Pose.Anger = Math.Max(0, Pose.Anger - 0.2);
        Say(Pick("Ням-ням! Дякую!", "Смачно!", "Насінинки — це любов."), 3);
    }

    public void Play()
    {
        if (IsPaused || IsBusyForCommands)
            return;
        BeginDance(4.5 + _rng.NextDouble() * 2);
        Stats.TimesPlayed++;
        Needs.Mood = Math.Min(100, Needs.Mood + 8);
        Needs.Energy = Math.Max(0, Needs.Energy - 3);
    }

    public void Sleep()
    {
        if (IsPaused || IsBusyForCommands)
            return;
        Transition(PetState.Sleep, 0);
        _sleepMin = 120;   // команда з меню — спить помітно довго, поки не розбудять
    }

    public void Wake()
    {
        if (State == PetState.Sleep)
            Transition(PetState.Idle, 0.4);
    }

    /// <summary>Привітатися (наприклад, коли програму запустили вдруге).</summary>
    public void Greet()
    {
        if (_bounds.IsEmpty || IsPaused || State is not (PetState.Idle or PetState.Walk or PetState.Peek or PetState.Preen or PetState.Sleep))
            return;
        if (State == PetState.Sleep)
            Transition(PetState.Idle, 0.3);
        Say(Pick("Привіт! Я тут!", "О, ти повернувся!", "Карр! Я вдома."), 3);
        BeginHop();
    }

    /// <summary>Команда «принести подарунок» (меню).</summary>
    public void GiveGift()
    {
        if (_bounds.IsEmpty || IsPaused || IsBusyForCommands || !_config.GiftsEnabled)
            return;
        BeginFetchGift(false);
    }

    /// <summary>Користувач схопив папугу мишею.</summary>
    public void BeginHold(Point cursorDip)
    {
        if (IsPaused || State is PetState.Away or PetState.FlyOff)
            return;
        _cursor = cursorDip;
        _holdOffset = new Vector(cursorDip.X - Pose.X, cursorDip.Y - Pose.Y);
        Pose.Gift = GiftKind.None;
        Transition(PetState.Held, 0);
        Say(Pick("Ой! Постав на місце!", "Кар-р-р! Я не мішок!", "Лечу! …ні, мене несуть."), 2.5);
    }

    /// <summary>Користувач відпустив папугу.</summary>
    public void EndHold()
    {
        if (State != PetState.Held)
            return;
        _velY = 0;
        Transition(PetState.Fall, 0);
    }

    // ------------------------------------------------------------------
    // Головний цикл
    // ------------------------------------------------------------------

    public void Update(double dt, Rect bounds, double windowWidth, double windowHeight, Point cursorDip)
    {
        dt = Math.Clamp(dt, 0.0, 0.1);
        _bounds = bounds;
        _w = windowWidth;
        _h = windowHeight;
        _cursor = cursorDip;
        _groundY = bounds.Bottom - windowHeight + LifreConstants.FeetInset;

        var prevX = Pose.X;
        var prevY = Pose.Y;
        _stateAge += dt;
        Pose.AnimPhase += dt * AnimSpeed();

        TickClickStreak(dt);
        TickAnger(dt);
        TickSpeech(dt);
        UpdateBlink(dt);
        UpdateGaze(dt);
        UpdateFacingAnim(dt);
        StepSprings(dt);
        Pose.Burst = Math.Max(0, Pose.Burst - dt * 1.1);
        Pose.Impact = Math.Max(0, Pose.Impact - dt * 2.2);

        if (_paused)
        {
            if (State == PetState.Fall)
                UpdateFall(dt);
            KeepOnScreen();
            UpdateKinematics(dt, prevX, prevY);
            UpdateRigging(dt);
            SyncPose();
            return;
        }

        if (!_hasEntered)
        {
            StartEntrance();
            _hasEntered = true;
        }

        DecayNeeds(dt);
        UpdateState(dt);
        UpdateMove(dt);
        KeepOnScreen();
        if (_entering && (State != PetState.Fly
            || (Pose.X >= _bounds.Left && Pose.X <= _bounds.Right - _w)))
            _entering = false;
        UpdateKinematics(dt, prevX, prevY);
        UpdateRigging(dt);
        SyncPose();
    }

    private double AnimSpeed() => State switch
    {
        PetState.Idle or PetState.Sleep or PetState.Away => 2.0,
        PetState.Walk or PetState.Peek => 2.8,
        PetState.Play => 6.0,
        PetState.Grumpy => 6.0,
        PetState.Fly or PetState.FlyOff or PetState.BringGift or PetState.Land
            or PetState.Fall or PetState.Held => 5.5,
        _ => 4.0
    };

    // ------------------------------------------------------------------
    // Вхід при запуску
    // ------------------------------------------------------------------

    private void StartEntrance()
    {
        var fromRight = _rng.NextDouble() > 0.5;
        // Старт — повністю за краєм екрана (вікно ще не видно), політ починається одразу на швидкості.
        Pose.X = fromRight ? _bounds.Right + 40 : _bounds.Left - _w - 40;
        Pose.Y = _bounds.Top + _bounds.Height * (0.10 + _rng.NextDouble() * 0.15);
        SetFacing(fromRight ? -1 : 1, instant: true);

        // Спершу пролітає дугою через екран, потім круто знижується до землі.
        var inward = fromRight ? -1.0 : 1.0;
        var midX = _bounds.Left + _bounds.Width * (fromRight ? 0.66 : 0.34) - _w * 0.5;
        _target = new Point(midX, _bounds.Top + _bounds.Height * (0.26 + _rng.NextDouble() * 0.10));
        _entryGround = new Point(
            _bounds.Left + _bounds.Width * (0.3 + _rng.NextDouble() * 0.4) - _w * 0.5,
            _groundY);
        _entering = true;
        _entryStage = 0;

        Transition(PetState.Fly, 16);
        _vel = new Vector(inward * 110.0 * _config.MovementSpeed * 1.8, 30);
        Say(Pick("Я прилетів! Привіт!", "Карр! Ось і я.", "Привіт! Тепер я тут живу."), 3.5);
    }

    // ------------------------------------------------------------------
    // Потреби, кліки, злість
    // ------------------------------------------------------------------

    private void DecayNeeds(double dt)
    {
        var rate = _config.DecayRate;
        Needs.Hunger = Math.Max(0, Needs.Hunger - dt * 0.10 * rate);
        Needs.Energy = Math.Max(0, Needs.Energy - dt * 0.08 * rate);
        Needs.Mood = Math.Max(0, Needs.Mood - dt * 0.04 * rate);

        if (State == PetState.Eat)
            Needs.Hunger = Math.Min(100, Needs.Hunger + dt * 28);
        else if (State == PetState.Sleep)
        {
            Needs.Energy = Math.Min(100, Needs.Energy + dt * 16);
            Needs.Mood = Math.Min(100, Needs.Mood + dt * 2.5);
        }
        else if (State == PetState.Play)
            Needs.Mood = Math.Min(100, Needs.Mood + dt * 10);
    }

    private void TickClickStreak(double dt)
    {
        if (_clickStreakDecay <= 0) return;
        _clickStreakDecay -= dt;
        if (_clickStreakDecay <= 0)
            _clickStreak = 0;
    }

    private void TickAnger(double dt)
    {
        if (State == PetState.Grumpy) return;
        if (Pose.Anger > 0)
            Pose.Anger = Math.Max(0, Pose.Anger - dt * 0.07);
    }

    private void UpdateBlink(double dt)
    {
        // Плавне кліпання: повіки швидко опускаються, коротко тримаються й повільніше піднімаються
        // (раніше фаза перемикалась 0↔1 миттєво — очі «сіпались»).
        if (!_blinkClosing)
        {
            _blinkTimer -= dt;
            if (_blinkTimer <= 0)
            {
                _blinkClosing = true;
                _blinkHold = 0.03 + _rng.NextDouble() * 0.04;
            }
            else
            {
                Pose.BlinkPhase = Math.Max(0, Pose.BlinkPhase - dt * 8.0);
                return;
            }
        }

        Pose.BlinkPhase = Math.Min(1, Pose.BlinkPhase + dt * 16.0);
        if (Pose.BlinkPhase < 1)
            return;
        _blinkHold -= dt;
        if (_blinkHold > 0)
            return;
        _blinkClosing = false;
        _blinkTimer = _rng.NextDouble() < 0.15 ? 0.2 : 1.8 + _rng.NextDouble() * 3.2;   // інколи — подвійне кліпання
    }

    private void UpdateGaze(double dt)
    {
        if (Math.Abs(_cursor.X - _lastCursor.X) + Math.Abs(_cursor.Y - _lastCursor.Y) > 2)
            _cursorStill = 0;
        else
            _cursorStill += dt;
        _lastCursor = _cursor;

        double tx = 0, ty = 0;
        if (State is not (PetState.Sleep or PetState.Away))
        {
            if (LiveEyes || (_cursorStill > 4.0 && State is PetState.Idle or PetState.Preen or PetState.Walk))
            {
                // Миша стоїть — Ліфре роззирається сам.
                _glanceTimer -= dt;
                if (_glanceTimer <= 0)
                {
                    _glanceX = _rng.NextDouble() * 2 - 0.8;   // трохи частіше дивиться вперед, за дзьоб
                    _glanceY = (_rng.NextDouble() - 0.4) * 1.2;
                    // Живі очі змінюють погляд частіше, зрідка — швидкий «переляканий» зиркіт.
                    _glanceTimer = LiveEyes
                        ? (_rng.NextDouble() < 0.2 ? 0.25 : 0.7 + _rng.NextDouble() * 2.0)
                        : 1.2 + _rng.NextDouble() * 2.8;
                }
                tx = _glanceX * 0.9;
                ty = _glanceY * 0.7;
            }
            else
            {
                var dx = _cursor.X - (Pose.X + _w * 0.5);
                var dy = _cursor.Y - (Pose.Y + _h * 0.57);
                // Невеликий ухил «вперед»: очі завжди кудись дивляться, а не просто в камеру.
                tx = Math.Clamp(Math.Clamp(dx / 260.0, -1, 1) * (Pose.Facing >= 0 ? 1 : -1) * 0.85 + 0.3, -1, 1);
                ty = Math.Clamp(dy / 260.0, -1, 1);
            }
            // Дивиться на насіння, коли їсть, і на подарунок, коли його показує.
            if (State == PetState.Eat) { tx = 0.4; ty = 1; }
            if (State == PetState.PresentGift) { tx = 0.7; ty = 0.5; }
            // У руках не дивиться в упор на курсор: розгублено роззирається й косить вниз.
            if (State == PetState.Held) { tx = Math.Sin(Pose.AnimPhase * 0.8) * 0.75; ty = 0.55 + Math.Sin(Pose.AnimPhase * 0.55) * 0.2; }
        }
        // Погляд не виходить за коло одиничного радіуса (діагональ не «переростає»), рух — м'який, без ривків.
        var len = Math.Sqrt(tx * tx + ty * ty);
        if (len > 1.0) { tx /= len; ty /= len; }
        var k = 1 - Math.Exp(-dt * 8.0);
        Pose.GazeX += (tx - Pose.GazeX) * k;
        Pose.GazeY += (ty - Pose.GazeY) * k;
    }

    private void UpdateFacingAnim(double dt)
    {
        // Розворот з м'яким кінцем: швидкість пропорційна залишку, але не менша за мінімум.
        var diff = Pose.Facing - Pose.FacingAnim;
        var step = Math.Max(Math.Abs(diff) * (1 - Math.Exp(-dt * 14.0)), dt * 2.5);
        Pose.FacingAnim = Math.Abs(diff) <= Math.Max(step, 0.004) ? Pose.Facing : Pose.FacingAnim + Math.Sign(diff) * step;
    }

    private void SetFacing(double dir, bool instant = false)
    {
        Pose.Facing = dir >= 0 ? 1 : -1;
        if (instant)
            Pose.FacingAnim = Pose.Facing;
    }

    // ------------------------------------------------------------------
    // Репліки
    // ------------------------------------------------------------------

    private void Say(string? text, double seconds = 4.0)
    {
        // Ліфре — папуга: замість людської мови в бульбашці лише папужі звуки.
        if (!_config.ShowSpeech || string.IsNullOrWhiteSpace(text))
            return;
        Pose.Speech = Pick("Чірік!", "Чірік-чірік!", "Кар-р!", "Піп-піп!", "Тьох-тьох!", "Фьюїт!", "Кр-р-р…", "Чірррр!", "Тьюїть!", "Піїп!");
        Pose.SpeechChars = 0;
        _speechTimer = Math.Min(seconds, 2.2);
        _speechAge = 0;
    }

    private void ClearSpeech()
    {
        Pose.Speech = null;
        Pose.SpeechAlpha = 0;
        _speechTimer = 0;
    }

    private void TickSpeech(double dt)
    {
        // Рот відкривається/закривається плавно: раніше Talk перемикався 0↔1 миттєво — дзьоб сіпався.
        var talkTarget = _speechTimer > 0 && _speechAge < 2.2 ? 1.0 : 0.0;
        Pose.Talk += (talkTarget - Pose.Talk) * (1 - Math.Exp(-dt * 12.0));
        if (Pose.Talk < 0.01) Pose.Talk = 0;

        if (_speechTimer <= 0)
        {
            if (Pose.Speech != null) ClearSpeech();
            return;
        }

        _speechTimer -= dt;
        _speechAge += dt;
        if (_speechTimer <= 0)
        {
            ClearSpeech();
            return;
        }

        Pose.SpeechAlpha = Math.Clamp(Math.Min(_speechAge * 6.0, _speechTimer / 0.3), 0, 1);
        Pose.SpeechChars = _speechAge * 42.0;
    }

    private T Pick<T>(params T[] items) => items[_rng.Next(items.Length)];

    private string PickChatter()
    {
        if (Needs.Mood < 35 && _rng.NextDouble() < 0.6)
            return Pick("Мені трішки сумно… погладь?", "Пограй зі мною, будь ласка.", "Нудно. Давай потанцюємо?");
        if (Needs.Hunger < 40 && _rng.NextDouble() < 0.6)
            return Pick("Щось я зголоднів…", "Пахне насінням. Десь тут точно є.", "Погодуй, будь ласка!");
        var list = _texts.Phrases;
        return list.Count > 0 ? list[_rng.Next(list.Count)] : "Карр. Я тут.";
    }

    // ------------------------------------------------------------------
    // Вибір активності
    // ------------------------------------------------------------------

    private void ChooseNextAction()
    {
        _thinkTimer = _config.IdleThinkSeconds * (0.8 + _rng.NextDouble() * 1.2);

        // Ваги: 0 ходьба, 1 стрибки, 2 політ, 3 танець, 4 чепуріння, 5 репліка, 6 до курсора, 7 дрімати, 8 їсти
        var weights = new[] { 0.28, 0.16, 0.10, 0.08, 0.10, 0.10, 0.07, 0.03, 0.0 };

        if (Needs.Mood < 50)
        {
            weights[3] += 0.10;
            weights[1] += 0.05;
        }
        if (Needs.Energy < 35)
            weights[7] += 0.25;
        if (Needs.Hunger < 50)
            weights[8] += 0.25;
        if (Pose.Anger > 0.4)
        {
            weights[3] = 0;
            weights[6] = 0;
        }

        switch (WeightedPick(weights))
        {
            case 0: BeginWalk(); break;
            case 1: BeginHop(); break;
            case 2: BeginFly(); break;
            case 3: BeginDance(3.5 + _rng.NextDouble() * 2); break;
            case 4: BeginPreen(); break;
            case 5:
                Say(PickChatter(), 4.5);
                _thinkTimer = Math.Max(_thinkTimer, 4.5);
                break;
            case 6: BeginPeek(); break;
            case 7: Transition(PetState.Sleep, 0); break;
            case 8:
                Transition(PetState.Eat, 3.5);
                Say("Пора перекусити.", 2.5);
                break;
        }
    }

    private int WeightedPick(double[] weights)
    {
        var sum = 0.0;
        foreach (var w in weights) sum += w;
        var r = _rng.NextDouble() * sum;
        var acc = 0.0;
        for (var i = 0; i < weights.Length; i++)
        {
            acc += weights[i];
            if (r <= acc) return i;
        }
        return weights.Length - 1;
    }

    private void BeginWalk()
    {
        var left = _bounds.Left;
        var right = Math.Max(left + 1, _bounds.Right - _w);
        var x = left + _rng.NextDouble() * (right - left);
        // Не ходимо на мізерні відстані — виглядає як тремтіння.
        if (Math.Abs(x - Pose.X) < 60)
            x = Pose.X + (x >= Pose.X ? 1 : -1) * (80 + _rng.NextDouble() * 120);
        _target = new Point(Math.Clamp(x, left, right), _groundY);
        Transition(PetState.Walk, 14);
    }

    private void BeginHop()
    {
        var dir = _rng.NextDouble() < 0.5 ? -1 : 1;
        if (Pose.X < _bounds.Left + 80) dir = 1;
        else if (Pose.X > _bounds.Right - _w - 80) dir = -1;
        _hopDir = dir;
        _hopsLeft = _rng.Next(2, 5);
        SetFacing(dir);
        Transition(PetState.Hop, 0);
        StartHopStep();
    }

    private void StartHopStep()
    {
        _hopT = 0;
        _hopCrouch = HopCrouchTime;
        _hopFromX = Pose.X;
        var dist = 36 + _rng.NextDouble() * 30;
        var minX = _bounds.Left - 40;
        var maxX = Math.Max(minX, _bounds.Right - _w + 40);
        _hopToX = Math.Clamp(Pose.X + _hopDir * dist, minX, maxX);
    }

    private void BeginFly()
    {
        _target = new Point(
            _bounds.Left + _rng.NextDouble() * Math.Max(1, _bounds.Width - _w),
            _bounds.Top + _bounds.Height * (0.15 + _rng.NextDouble() * 0.4));
        _flyLegs = _rng.Next(0, 3);
        Transition(PetState.Fly, 14 + _flyLegs * 8);
    }

    private void BeginDance(double seconds)
    {
        _danceT = 0;
        _danceFlip = 0.55;
        Transition(PetState.Play, seconds);
        Say(Pick("Танцюємо!", "Ла-ла-ла!", "Дивись, як умію!"), 2.5);
    }

    private void BeginPreen()
    {
        _preenKind = _rng.Next(3);
        Pose.PreenKind = _preenKind;
        Transition(PetState.Preen, 2.6 + _rng.NextDouble() * 1.6);
    }

    private void BeginPeek()
    {
        var left = _bounds.Left;
        var right = Math.Max(left + 1, _bounds.Right - _w);
        _target = new Point(Math.Clamp(_cursor.X - _w * 0.5, left, right), _groundY);
        Transition(PetState.Peek, 5.0 + _rng.NextDouble() * 2);
        Say(Pick("Що ти там робиш?", "Цікаво, цікаво…", "Можна глянути?"), 3);
    }

    // ------------------------------------------------------------------
    // Подарунки
    // ------------------------------------------------------------------

    private void BeginFetchGift(bool apology)
    {
        var goRight = _rng.NextDouble() > 0.5;
        _target = new Point(
            goRight ? _bounds.Right + 40 : _bounds.Left - _w - 40,
            _bounds.Top + _bounds.Height * (0.2 + _rng.NextDouble() * 0.3));
        Pose.Gift = GiftKind.None;
        Transition(PetState.FlyOff, 14);
        Say(apology ? "Гаразд… вибач. Зараз щось принесу." : "Я зараз! Нікуди не йди.", 3.2);
        _giftTimer = Math.Max(30, _config.GiftIntervalSeconds) * (0.7 + _rng.NextDouble() * 0.6);
    }

    private GiftKind PickGiftKind()
    {
        var all = new[]
        {
            GiftKind.Flower, GiftKind.Berry, GiftKind.Star, GiftKind.Feather, GiftKind.Gem,
            GiftKind.Letter, GiftKind.Seed, GiftKind.Cookie, GiftKind.Coin
        };
        GiftKind kind;
        do { kind = all[_rng.Next(all.Length)]; } while (kind == _lastGift);
        _lastGift = kind;
        return kind;
    }

    private static string GiftLine(GiftKind kind) => kind switch
    {
        GiftKind.Flower => "Знайшов квіточку! Це тобі.",
        GiftKind.Berry => "Ягідка! Я не їв. Майже.",
        GiftKind.Star => "Впіймав зірочку. Тримай!",
        GiftKind.Feather => "Моє найкраще пір'їнко — тобі.",
        GiftKind.Gem => "Блискітка! Рідкісна. Бери.",
        GiftKind.Letter => "Тобі лист! Там написано «Карр».",
        GiftKind.Seed => "Найсмачніша насінина. Ділюся!",
        GiftKind.Cookie => "Печиво! Я не відкусив. Чесно.",
        GiftKind.Coin => "Монетка! Купиш мені насіння.",
        _ => "Ось, це тобі!"
    };

    private void ArriveWithGift()
    {
        var gift = PickGiftKind();
        var fromRight = _rng.NextDouble() > 0.5;
        Pose.X = fromRight ? _bounds.Right + 30 : _bounds.Left - _w - 30;
        Pose.Y = _bounds.Top + _bounds.Height * (0.2 + _rng.NextDouble() * 0.25);
        SetFacing(fromRight ? -1 : 1, instant: true);

        var left = _bounds.Left + 40;
        var right = Math.Max(left + 1, _bounds.Right - _w - 40);
        _target = new Point(left + _rng.NextDouble() * (right - left), _groundY);
        Transition(PetState.BringGift, 16);
        _vel = new Vector((fromRight ? -1.0 : 1.0) * 110.0 * _config.MovementSpeed * 1.5, 20);
        Pose.Gift = gift;
    }

    private void BeginPresent()
    {
        Pose.Y = _groundY;
        OnLanded();
        _nudgeSaid = false;
        Transition(PetState.PresentGift, 11);
        Say(GiftLine(Pose.Gift), 4.5);
    }

    private void AcceptGift()
    {
        Stats.GiftsGiven++;
        Needs.Mood = Math.Min(100, Needs.Mood + 15);
        Pose.Anger = 0;
        Transition(PetState.Petted, 2.2);
        Pose.Burst = 1;
        Say(Pick("Ура! Тепер це твоє!", "Радий, що тобі сподобалось!", "Ми з тобою друзі, так?"), 3.5);
    }

    private void RejectGift()
    {
        Needs.Hunger = Math.Min(100, Needs.Hunger + 10);
        Transition(PetState.Eat, 2.0);
        Say("Не хочеш? Тоді сам з’їм!", 3);
    }

    // ------------------------------------------------------------------
    // Оновлення станів
    // ------------------------------------------------------------------

    private void UpdateState(double dt)
    {
        _stateTimer -= dt;
        _thinkTimer -= dt;

        // Висота підскоку згасає плавно, а не обнуляється стрибком, коли дію перервали посеред підскоку.
        if (State is not (PetState.Hop or PetState.Play or PetState.PresentGift))
            Pose.Lift = Math.Max(0, Pose.Lift - dt * 140.0);

        // Сторожовий таймер: жоден стан не може тривати вічно.
        if (_stateAge > 75 && State is not (PetState.Idle or PetState.Sleep or PetState.Held or PetState.Away))
        {
            Transition(PetState.Idle, 0.3);
            return;
        }

        // Потреби переривають лише спокійні дії.
        if (State is PetState.Idle or PetState.Walk or PetState.Hop or PetState.Play or PetState.Preen or PetState.Peek)
        {
            if (Needs.Energy < 12)
            {
                Transition(PetState.Sleep, 0);
                Say("Позіхає… піду посплю.", 2.5);
                return;
            }
            if (Needs.Hunger < 12)
            {
                Transition(PetState.Eat, 3.5);
                Say("Їсти хочу!", 2.5);
                return;
            }
        }

        switch (State)
        {
            case PetState.Sleep:
                // Сон у повітрі неможливий: якщо не на землі — падає/приземляється.
                if (Pose.Y < _groundY - 3)
                {
                    _velY = 0;
                    Transition(PetState.Fall, 0);
                    break;
                }
                Pose.SleepPhase = Math.Min(1, Pose.SleepPhase + dt * 0.5);
                // Прокидається не раніше мінімального часу сну (раніше — вже через ~2 с, якщо енергія повна).
                if (Pose.SleepPhase >= 1 && _stateAge >= _sleepMin && Needs.Energy >= 88)
                    Transition(PetState.Idle, 0.4);
                break;

            case PetState.Eat:
                Pose.EatIntensity = Math.Min(1, Pose.EatIntensity + dt * 3.2);
                if (_stateTimer <= 0 || Needs.Hunger >= 92)
                    Transition(PetState.Idle, 0.4);
                break;

            case PetState.Petted:
                Pose.PetIntensity = Math.Min(1, Pose.PetIntensity + dt * 5);
                if (_stateTimer <= 0)
                    Transition(PetState.Idle, 0.35);
                break;

            case PetState.Play:
                UpdateDance(dt);
                if (_stateTimer <= 0)
                    Transition(PetState.Idle, 0.35);
                break;

            case PetState.Preen:
                // Струшується «хвилями», а не безперервно.
                Pose.Wobble = _preenKind == 2 && Math.Sin(_stateAge * 5.0) > 0.3
                    ? Math.Sin(_stateAge * 28.0) * 3.0
                    : 0;
                if (_stateTimer <= 0)
                    Transition(PetState.Idle, 0.4);
                break;

            case PetState.Hop:
                UpdateHop(dt);
                break;

            case PetState.Walk:
                if (_stateTimer <= 0 || ReachedTarget(8))
                {
                    Transition(PetState.Idle, 0.4);
                    _thinkTimer = Math.Max(_thinkTimer, 1.8);   // трохи постояти, а не одразу смикатись у нову дію
                }
                break;

            case PetState.Peek:
                // Слідкує за курсором по горизонталі.
                _target = new Point(
                    Math.Clamp(_cursor.X - _w * 0.5, _bounds.Left, Math.Max(_bounds.Left + 1, _bounds.Right - _w)),
                    _groundY);
                if (_stateTimer <= 0)
                    Transition(PetState.Idle, 0.4);
                break;

            case PetState.Grumpy:
                Pose.Anger = Math.Max(Pose.Anger, 0.85);
                Pose.Wobble = Math.Sin(_stateAge * 22.0) * 1.8;
                if (_stateTimer <= 0)
                {
                    Pose.Anger = 0.25;
                    if (_apologyPending)
                    {
                        _apologyPending = false;
                        BeginFetchGift(true);
                    }
                    else
                    {
                        Transition(PetState.Idle, 0.5);
                    }
                }
                break;

            case PetState.Fly:
                if (IsCrouchingForTakeoff)
                    _squashTarget = 0.42 * Ease(_stateAge / 0.5);   // плавне присідання перед стартом, без смикання
                if (_entering && _entryStage == 0)
                {
                    // Вліт: після прольоту через екран — заходимо на посадку.
                    if (ReachedTarget(60) || _stateTimer < 10)
                    {
                        _entryStage = 1;
                        _target = _entryGround;
                    }
                    break;
                }
                if (_flyLegs > 0 && ReachedTarget(40) && _stateTimer > 3)
                {
                    // Плавно переходимо до наступної точки без зупинки — політ виглядає як одна дуга.
                    _flyLegs--;
                    _target = new Point(
                        _bounds.Left + _rng.NextDouble() * Math.Max(1, _bounds.Width - _w),
                        _bounds.Top + _bounds.Height * (0.12 + _rng.NextDouble() * 0.45));
                    break;
                }
                if (_stateTimer <= 0 || ReachedTarget(14))
                    Transition(PetState.Land, 6);
                break;

            case PetState.Land:
                if (Pose.Y >= _groundY - 2 || _stateTimer <= 0)
                {
                    Pose.Y = _groundY;
                    OnLanded();
                    Transition(PetState.Idle, 0.5);
                }
                break;

            case PetState.Fall:
                UpdateFall(dt);
                break;

            case PetState.FlyOff:
                if (IsCrouchingForTakeoff)
                    _squashTarget = 0.42 * Ease(_stateAge / 0.5);   // плавне присідання перед стартом, без смикання
                if (_stateTimer <= 0 || ReachedTarget(14))
                {
                    _awayTimer = 2.5 + _rng.NextDouble() * 3.0;
                    Transition(PetState.Away, 0);
                }
                break;

            case PetState.Away:
                _awayTimer -= dt;
                if (_awayTimer <= 0)
                    ArriveWithGift();
                break;

            case PetState.BringGift:
                if (_stateTimer <= 0 || ReachedTarget(10))
                    BeginPresent();
                break;

            case PetState.PresentGift:
                // Нетерпляче підстрибує, щоб привернути увагу.
                Pose.Lift = Math.Max(0, Math.Sin(_stateAge * 4.2)) * 5.0;
                if (!_nudgeSaid && _stateTimer < 6.0)
                {
                    _nudgeSaid = true;
                    Say("Ну ж бо — клікни на мене, це подарунок!", 4);
                }
                if (_stateTimer <= 0)
                    RejectGift();
                break;

            case PetState.Idle:
                // Після польоту чи зсуву — не висимо в повітрі.
                if (Pose.Y < _groundY - 3)
                {
                    Transition(PetState.Land, 6);
                    break;
                }
                if (_giftTimer > 0)
                    _giftTimer -= dt;
                if (_config.GiftsEnabled && _giftTimer <= 0)
                {
                    BeginFetchGift(false);
                    break;
                }
                if (_thinkTimer <= 0)
                    ChooseNextAction();
                break;
        }

        Pose.EatIntensity = Math.Max(0, Pose.EatIntensity - dt * 2.2);
        Pose.PetIntensity = Math.Max(0, Pose.PetIntensity - dt * 1.5);
    }

    private void UpdateHop(double dt)
    {
        if (_hopCrouch > 0)
        {
            _hopCrouch -= dt;
            _squashTarget = 0.5 * Ease(1.0 - _hopCrouch / HopCrouchTime);
            Pose.Lift = 0;
            return;
        }

        _hopT += dt / 0.45;
        if (_hopT >= 1)
        {
            Pose.X = _hopToX;
            Pose.Lift = 0;
            OnLanded(0.5);
            _hopsLeft--;
            if (_hopsLeft <= 0)
            {
                Transition(PetState.Idle, 0.4);
                return;
            }
            StartHopStep();
            return;
        }

        Pose.X = _hopFromX + (_hopToX - _hopFromX) * _hopT;
        Pose.Lift = Math.Sin(Math.PI * _hopT) * 22.0;
    }

    private void UpdateDance(double dt)
    {
        _danceT += dt;
        var bounce = (_danceT * 3.5 / Math.PI) % 1.0;
        Pose.Lift = 4.0 * bounce * (1.0 - bounce) * 14.0;   // парабола: плавний підскок без різкого відбиття
        _danceFlip -= dt;
        if (_danceFlip <= 0)
        {
            _danceFlip = 0.5;
            SetFacing(-Pose.Facing);
        }
    }

    private void UpdateFall(double dt)
    {
        _velY += 1700.0 * dt;
        Pose.Y += _velY * dt;
        if (Pose.Y < _groundY)
            return;

        Pose.Y = _groundY;
        _velY = 0;
        OnLanded();
        if (_paused)
        {
            State = PetState.Idle;
            return;
        }

        Transition(PetState.Idle, 0.6);
        if (_rng.NextDouble() < 0.6)
            Say(Pick("Приземлився! …майже красиво.", "Ой! Гребінь цілий?", "Фух. Дякую, що відпустив."), 3);
    }

    private bool ReachedTarget(double radius)
    {
        var dx = _target.X - Pose.X;
        var dy = _target.Y - Pose.Y;
        return Math.Sqrt(dx * dx + dy * dy) < radius;
    }

    // ------------------------------------------------------------------
    // Рух
    // ------------------------------------------------------------------

    private void UpdateMove(double dt)
    {
        if (State == PetState.Held)
        {
            var tx = _cursor.X - _holdOffset.X;
            var ty = _cursor.Y - _holdOffset.Y;
            var k = 1 - Math.Exp(-dt * 22.0);
            var nx = Pose.X + (tx - Pose.X) * k;
            var ny = Pose.Y + (ty - Pose.Y) * k;
            var vx = nx - Pose.X;
            if (Math.Abs(vx) > 1.2)
                SetFacing(vx > 0 ? 1 : -1);
            Pose.X = nx;
            Pose.Y = ny;
            _vel = new Vector(0, 0);
            return;
        }

        if (IsCrouchingForTakeoff)
        {
            _vel = new Vector(0, 0);
            return;   // спершу присів, потім стартує
        }

        var speed = 110.0 * _config.MovementSpeed;
        Point goal;

        switch (State)
        {
            case PetState.Walk:
                goal = _target;
                speed *= 0.5;
                break;
            case PetState.Peek:
                goal = _target;
                speed *= 0.9;
                break;
            case PetState.Grumpy:
                goal = new Point(
                    Math.Clamp(_cursor.X - _w * 0.5, _bounds.Left, Math.Max(_bounds.Left + 1, _bounds.Right - _w)),
                    _groundY);
                speed *= 1.3;
                break;
            case PetState.Fly:
                goal = _target;
                speed *= 1.5;
                break;
            case PetState.Land:
                goal = new Point(Pose.X, _groundY);
                speed *= 2.2;
                break;
            case PetState.FlyOff:
                goal = _target;
                speed *= 1.7;
                break;
            case PetState.BringGift:
                goal = _target;
                speed *= 1.5;
                break;
            default:
                // Після ходьби не зупиняємося миттєво: залишок швидкості плавно згасає (без «стоп-ривка»).
                if (Math.Abs(_vel.X) > 0.5 && Math.Abs(_vel.Y) < 1.0 && Pose.Y >= _groundY - 2)
                {
                    _vel = new Vector(_vel.X * Math.Exp(-dt * 12.0), 0);
                    Pose.X += _vel.X * dt;
                }
                else
                    _vel = new Vector(0, 0);
                return;
        }

        var dx = goal.X - Pose.X;
        var dy = goal.Y - Pose.Y;
        var dist = Math.Sqrt(dx * dx + dy * dy);

        // Плавний рух: бажана швидкість зменшується пропорційно до відстані (м'яке гальмування),
        // а справжня швидкість наздоганяє бажану експоненційно (м'який розгін) — жодних ривків.
        var ground = State is PetState.Walk or PetState.Peek or PetState.Grumpy;
        var arrive = ground ? 2.6 : State is PetState.Fly or PetState.Land or PetState.BringGift ? 1.8 : 0.0;
        // Біля цілі на землі — «мертва зона»: не повзе зі швидкістю 4 px/с і не перебирає лапами на місці.
        var want = arrive > 0 ? Math.Min(speed, dist * arrive + (ground ? 0.0 : 4.0)) : speed;
        if (ground) want = dist < 5.0 ? 0.0 : Math.Max(want, 14.0 * Math.Min(1.0, dist / 20.0));
        // Поки розвертається — не йде задом наперед (мунвок): стартує, коли розворот майже завершено.
        if (ground) want *= Math.Clamp(1.0 - Math.Abs(Pose.Facing - Pose.FacingAnim) * 0.8, 0.0, 1.0);
        var desired = dist > 0.5 ? new Vector(dx / dist * want, dy / dist * want) : new Vector(0, 0);
        var kv = 1 - Math.Exp(-dt * (ground ? 5.0 : 3.2));
        _vel += (desired - _vel) * kv;
        Pose.X += _vel.X * dt;
        Pose.Y += _vel.Y * dt;

        // Гістерезис: біля цілі й при русі курсора не розвертається туди-сюди.
        var turnThreshold = ground ? 14.0 : 3.0;
        if (Math.Abs(dx) > turnThreshold)
            SetFacing(dx >= 0 ? 1 : -1);
    }

    private void KeepOnScreen()
    {
        if (_bounds.IsEmpty)
            return;

        var offscreenOk = State is PetState.FlyOff or PetState.Away or PetState.BringGift
            || (_entering && State == PetState.Fly);
        if (!offscreenOk)
        {
            var minX = _bounds.Left - 40;
            var maxX = Math.Max(minX, _bounds.Right - _w + 40);
            Pose.X = Math.Clamp(Pose.X, minX, maxX);
        }

        var minY = _bounds.Top - 20;
        var maxY = Math.Max(minY, _groundY);
        Pose.Y = Math.Clamp(Pose.Y, minY, maxY);

        Pose.Hidden = Pose.X >= _bounds.Right || Pose.X + _w <= _bounds.Left;
    }

    // ------------------------------------------------------------------
    // Кінематика та «риггінг» анімації
    // ------------------------------------------------------------------

    private void OnLanded(double strength = 1.0)
    {
        Pose.Squash = 0.5 + 0.5 * strength;
        _squashVel = 0;
        Pose.Impact = Math.Max(Pose.Impact, strength);
    }

    private static double Ease(double x)
    {
        x = Math.Clamp(x, 0.0, 1.0);
        return x * x * (3.0 - 2.0 * x);
    }

    private bool IsCrouchingForTakeoff =>
        State is PetState.Fly or PetState.FlyOff && _stateAge < 0.5 && Pose.Y >= _groundY - 3;

    private void UpdateKinematics(double dt, double prevX, double prevY)
    {
        var dx = Pose.X - prevX;
        var dy = Pose.Y - prevY;
        double vx = 0, vy = 0;
        // Телепорти (вхід, повернення з подарунком) не мають давати стрибка швидкості.
        if (dt > 1e-4 && Math.Abs(dx) < 300 && Math.Abs(dy) < 300)
        {
            vx = dx / dt;
            vy = dy / dt;
        }

        // Інерція: верх тіла відстає при розгоні (різниця швидкості й її згладженого значення ≈ прискорення).
        var dir = Pose.Facing >= 0 ? 1.0 : -1.0;
        var sharp = vx - Pose.VelX;
        _swayTarget = Math.Clamp(sharp * dir * 0.02, -8, 8) + Math.Sin(Pose.AnimPhase * 0.9) * 0.8;

        var k = Math.Min(1.0, dt * 10.0);
        Pose.VelX += (vx - Pose.VelX) * k;
        Pose.VelY += (vy - Pose.VelY) * k;

        var walking = State is PetState.Walk or PetState.Peek or PetState.Grumpy;
        Pose.Gait += ((walking ? 1.0 : 0.0) - Pose.Gait) * Math.Min(1.0, dt * 9.0);
        if (walking)
        {
            // Повний цикл кроку (дві лапи) — кожні ~52 DIP шляху (спокійніша хода).
            Pose.Stride += Math.Abs(dx) * (2 * Math.PI / 32.0);
        }
    }

    /// <summary>Пружини: присідання/приземлення (з відскоком) та інерція корпусу.</summary>
    private void StepSprings(double dt)
    {
        var remaining = dt;
        while (remaining > 1e-6)
        {
            var h = Math.Min(remaining, 0.008);
            remaining -= h;
            _squashVel += ((_squashTarget - Pose.Squash) * 320.0 - _squashVel * 16.0) * h;
            Pose.Squash += _squashVel * h;
            _swayVel += ((_swayTarget - Pose.Sway) * 180.0 - _swayVel * 10.0) * h;
            Pose.Sway += _swayVel * h;
        }
        Pose.Squash = Math.Clamp(Pose.Squash, -0.6, 1.2);
        Pose.Sway = Math.Clamp(Pose.Sway, -14, 14);
        _squashTarget = 0;   // стани можуть підняти ціль у цьому кадрі
    }

    /// <summary>Аварійне відновлення, якщо щось пішло не так: папуга стає на землю й оговтується.</summary>
    public void Recover()
    {
        ClearSpeech();
        Pose.Gift = GiftKind.None;
        Pose.Lift = 0;
        Pose.Wobble = 0;
        Pose.Food = false;
        _velY = 0;
        _squashVel = 0;
        _swayVel = 0;
        Pose.Squash = 0;
        Pose.Sway = 0;
        if (double.IsNaN(Pose.X) || double.IsInfinity(Pose.X))
            Pose.X = _bounds.IsEmpty ? 0 : _bounds.Left + (_bounds.Width - _w) / 2;
        if (double.IsNaN(Pose.Y) || double.IsInfinity(Pose.Y))
            Pose.Y = _groundY;
        Transition(PetState.Idle, 0.5);
    }

    /// <summary>Плавні параметри пози: нахил, підібрані лапи, розкриття крил.</summary>
    private void UpdateRigging(double dt)
    {
        var s = State;
        var dir = Pose.Facing >= 0 ? 1.0 : -1.0;
        var fwd = Pose.VelX * dir;
        var ph = Pose.AnimPhase;

        double lean = s switch
        {
            PetState.Walk or PetState.Peek => 2.0 + Math.Sin(Pose.Stride * 2.0) * 1.6,
            PetState.Grumpy => 4.0 + Math.Sin(Pose.Stride * 2.0) * 1.6,
            PetState.Fly or PetState.FlyOff or PetState.BringGift
                => Math.Clamp(fwd * 0.045, -4, 12) + Math.Clamp(Pose.VelY * 0.025, -8, 8) + 2.0 + Math.Sin(Pose.WingPhase - 1.0) * 1.4 * Pose.WingAmp,
            PetState.Land => -10.0 + Math.Sin(Pose.WingPhase - 1.0) * 1.5 * Pose.WingAmp,
            PetState.Fall => Math.Sin(ph * 2.2) * 4.0,
            PetState.Held => Math.Sin(ph * 1.8) * 6.0 - Math.Clamp(fwd * 0.02, -12, 12),
            PetState.Hop => Pose.Lift > 1.0 ? 5.0 : 0.0,
            PetState.Play => Math.Sin(_danceT * 3.5) * 6.0,
            PetState.Eat => Math.Pow(Math.Abs(Math.Sin(ph * 3.2)), 3.0) * 12.0,
            PetState.Preen => _preenKind == 1 ? -7.0 : Math.Sin(ph * 2.5) * 5.0 - 2.0,
            PetState.PresentGift => 4.0 + Math.Sin(ph * 1.6) * 1.5,
            PetState.Petted => Math.Sin(ph * 3.0) * 3.0,
            PetState.Sleep => 5.0,
            _ => Math.Sin(ph * 1.4) * 0.8 + Pose.GazeY * 2.0
        };
        if (s == PetState.Peek)
            lean += 3.0;
        Pose.Lean += (lean - Pose.Lean) * (1 - Math.Exp(-dt * (s == PetState.Eat ? 30.0 : 12.0)));

        double tuck = s switch
        {
            PetState.Fly or PetState.FlyOff or PetState.BringGift => 1.0,
            PetState.Land => 0.35,
            PetState.Fall => 0.6,
            PetState.Held => 0.15,
            _ => Math.Clamp(Pose.Lift / 16.0, 0, 1)
        };
        Pose.Tuck += (tuck - Pose.Tuck) * (1 - Math.Exp(-dt * 14.0));

        var open = s is PetState.Fly or PetState.Land or PetState.FlyOff or PetState.BringGift
            or PetState.Fall or PetState.Held
            || (s == PetState.Preen && _preenKind == 1)
            ? 1.0 : 0.0;
        // Розкриваються швидше, складаються м'якше — без різкого «клацання» крил.
        Pose.WingOpen += (open - Pose.WingOpen) * (1 - Math.Exp(-dt * (open > Pose.WingOpen ? 12.0 : 7.0)));

        // Помах: частота й розмах плавно залежать від того, що робить птах (набір висоти, планування, падіння).
        double freq, amp;
        switch (s)
        {
            case PetState.Fly or PetState.FlyOff or PetState.BringGift:
                freq = Pose.VelY < -40 ? 3.3 : Pose.VelY > 60 ? 1.7 : 2.6;
                amp = Pose.VelY > 60 ? 0.6 : 1.0;
                break;
            case PetState.Land:
                var nearGround = Pose.Y > _groundY - 30;
                freq = nearGround ? 3.2 : 2.0;
                amp = nearGround ? 1.0 : 0.7;
                break;
            case PetState.Fall:
                freq = 3.6; amp = 0.5;
                break;
            case PetState.Held:
                freq = 4.5; amp = 0.4;
                break;
            default:
                freq = 3.0; amp = 0.0;
                break;
        }
        _wingFreq += (freq - _wingFreq) * (1 - Math.Exp(-dt * 2.5));
        Pose.WingAmp += (amp - Pose.WingAmp) * (1 - Math.Exp(-dt * 6.0));
        Pose.WingPhase += dt * _wingFreq * 2.0 * Math.PI;
        if (Pose.WingPhase > 1e5) Pose.WingPhase %= 2.0 * Math.PI;
    }

    // ------------------------------------------------------------------
    // Переходи та синхронізація
    // ------------------------------------------------------------------

    private static bool IsAirState(PetState s) => s is PetState.Fly or PetState.Land or PetState.FlyOff
        or PetState.BringGift or PetState.Fall or PetState.Held;

    private void Transition(PetState next, double duration)
    {
        var wasAir = IsAirState(State);
        if (next != PetState.Fly)
            _flyLegs = 0;
        if (IsAirState(next) && !wasAir)
            Pose.WingPhase = Math.PI * 0.5;   // перший помах починається з піднятих крил
        if (next == PetState.Sleep)
            _sleepMin = 30 + _rng.NextDouble() * 25;
        State = next;
        Stats.State = next.ToString();
        _stateTimer = duration;
        _stateDuration = duration;
        _stateAge = 0;
        Pose.Wobble = 0;

        if (next == PetState.Sleep)
            Pose.SleepPhase = 0;
        if (next != PetState.Petted)
            Pose.PetIntensity = 0;
        Pose.Food = next == PetState.Eat;
        if (next is not (PetState.BringGift or PetState.PresentGift))
            Pose.Gift = GiftKind.None;
    }

    private void SyncPose()
    {
        Pose.State = State;
        Pose.Progress = _stateDuration > 0 ? Math.Clamp(_stateAge / _stateDuration, 0, 1) : 0;
        Stats.PosX = Pose.X;
        Stats.PosY = Pose.Y;
        Stats.Facing = (int)Pose.Facing;
        Stats.State = State.ToString();
        Stats.Needs.CopyFrom(Needs);
    }
}
