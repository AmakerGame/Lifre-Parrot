using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Lifre.Core;

namespace Lifre.Rendering;

/// <summary>
/// Рендер Ліфре за референсом (жовта корела-стікер): одне суцільне тіло-«листок»
/// з високим гребенем, великі очі з коричневою райдужкою, рожевий дзьоб, червоні щічки,
/// тонкі ніжки, кремова обводка-стікер. Контури лежать у ParrotShapes у координатах
/// референсу; тут — анімація: ходьба з перевалочкою, стрибки з присіданням, політ із
/// підібраними лапами, розворот, погляд за курсором, тінь, бульбашка реплік і подарунки.
/// </summary>
public sealed class ParrotRenderer : FrameworkElement
{
    private const double Cx = 110;
    private const double GroundY = 200;
    private const double OffX = LifreConstants.SpriteOffsetX;
    private const double OffY = LifreConstants.SpriteHeadroom;

    // Референс (349×433) → простір спрайта: масштаб і зсув.
    private const double RefScale = 0.5;
    private const double RefOffX = 33.5;
    private const double RefOffY = 2.0;

    // Кінчик дзьоба в просторі спрайта (де тримаємо подарунок).
    private const double BeakX = 150;
    private const double BeakY = 106;

    private static readonly Brush BodyBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xF7, 0xD5, 0x52)));
    private static readonly Brush HaloBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xC8)));
    private static readonly Brush OutlineBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x2B, 0x23, 0x14)));
    private static readonly Brush EyeBrush = Freeze(new SolidColorBrush(Colors.White));
    private static readonly Brush IrisBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x9A, 0x69, 0x46)));
    private static readonly Brush BeakBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xF5, 0xC2, 0xD0)));
    private static readonly Brush MouthBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x4B, 0x2C, 0x3C)));
    private static readonly Brush CheekBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xE4, 0x56, 0x42)));
    private static readonly Brush CheekAngryBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xD8, 0x3A, 0x2C)));
    private static readonly Brush ShadowBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x20, 0x18, 0x08)));
    private static readonly Brush HeartBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xE8, 0x4A, 0x5F)));
    private static readonly Brush NoteBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x5B, 0x6E, 0xC9)));
    private static readonly Brush ZzzBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x6E, 0x93, 0xD8)));
    private static readonly Brush SeedBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x8A, 0x6A, 0x3A)));
    private static readonly Brush BubbleBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0xFD, 0xF2)));
    private static readonly Brush TextBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x2B, 0x23, 0x14)));
    private static readonly Brush SparkleBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x3D)));

    private static readonly Pen HaloPen = FreezePen(MakePen(HaloBrush, 10.5));
    private static readonly Pen OutlinePen = FreezePen(MakePen(OutlineBrush, 4.2));
    private static readonly Pen DetailPen = FreezePen(MakePen(OutlineBrush, 3.6));
    private static readonly Pen EyePen = FreezePen(MakePen(OutlineBrush, 3.4));
    private static readonly Pen LidPen = FreezePen(MakePen(OutlineBrush, 4.0));
    private static readonly Pen BrowPen = FreezePen(MakePen(OutlineBrush, 3.8));
    private static readonly Pen CheekPen = FreezePen(MakePen(new SolidColorBrush(Color.FromRgb(0x6A, 0x2A, 0x20)), 2.6));
    private static readonly Pen BeakPen = FreezePen(MakePen(MouthBrush, 3.6));
    private static readonly Pen MouthPen = FreezePen(MakePen(MouthBrush, 3.0));
    // Ніжки як на референсі: тонка темна лінія з кремовою обводкою (без заливки всередині).
    private static readonly Pen LegHaloPen = FreezePen(MakePen(HaloBrush, 10.0));
    private static readonly Pen LegPen = FreezePen(MakePen(OutlineBrush, 4.4));
    private static readonly Pen GiftPen = FreezePen(MakePen(OutlineBrush, 1.5));
    private static readonly Pen BubblePen = FreezePen(MakePen(OutlineBrush, 1.6));
    private static readonly Pen NotePen = FreezePen(MakePen(NoteBrush, 1.8));

    private static readonly Typeface UiFont =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface ZzzFont =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

    private static readonly Geometry UnitSparkle = MakeSparkle();

    private readonly record struct EyeSpec(double X, double Y, double Rx, double Ry, double Rot,
        double IrisRx, double IrisRy, double Inner);

    // Ліве (велике) і праве (частково за дзьобом) око — позиції з референсу.
    private static readonly EyeSpec LeftEye = new(161, 179, 23, 27, 12, 16, 19, +1);
    private static readonly EyeSpec RightEye = new(246, 177, 14, 19, 8, 11, 12, -1);

    private static Pen MakePen(Brush b, double thickness)
    {
        return new Pen(b, thickness)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
    }

    private static Brush Freeze(Brush b) { b.Freeze(); return b; }
    private static Pen FreezePen(Pen p) { p.Brush.Freeze(); p.Freeze(); return p; }

    private static Geometry MakeSparkle()
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            const double k = 0.28;
            ctx.BeginFigure(new Point(0, -1), true, true);
            ctx.LineTo(new Point(k, -k), true, false);
            ctx.LineTo(new Point(1, 0), true, false);
            ctx.LineTo(new Point(k, k), true, false);
            ctx.LineTo(new Point(0, 1), true, false);
            ctx.LineTo(new Point(-k, k), true, false);
            ctx.LineTo(new Point(-1, 0), true, false);
            ctx.LineTo(new Point(-k, -k), true, false);
        }
        g.Freeze();
        return g;
    }

    private enum WingMode { Rest, Crossed, Flex }

    /// <summary>Поза, яку малюємо. Вікно підставляє сюди <c>PetBrain.Pose</c>.</summary>
    public PetPose Pose { get; set; } = new();

    /// <summary>Дробова частина позиції (DIP): вікно ставиться на цілі пікселі, а спрайт зсувається плавно.</summary>
    public double SubPixelX { get; set; }
    public double SubPixelY { get; set; }

    private string? _speechCacheText;
    private FormattedText? _speechCache;

    public ParrotRenderer()
    {
        Width = LifreConstants.WindowWidth;
        Height = LifreConstants.WindowHeight;
    }

    private static readonly Brush DustBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xC9, 0xB6, 0x8A)));
    private static readonly Geometry WingLeaf = ParrotShapes.FlightWing;

    // Майже невидима (alpha 3/255) область навколо папуги: без неї кліки по прозорих пікселях
    // шарового вікна проходять «крізь» птаха на робочий стіл.
    private static readonly Brush HitBrush = Freeze(new SolidColorBrush(Color.FromArgb(3, 0, 0, 0)));
    private static readonly Pen AngerPen = FreezePen(MakePen(new SolidColorBrush(Color.FromRgb(0xE0, 0x30, 0x40)), 2.4));

    private FormattedText? _speechPart;
    private int _speechPartLen = -1;

    private static readonly Pen SpeedPen = FreezePen(MakePen(new SolidColorBrush(Color.FromRgb(0xB8, 0xC4, 0xD8)), 2.2));

    protected override void OnRender(DrawingContext dc)
    {
        var pose = Pose;
        var t = pose.AnimPhase;
        var state = pose.State;

        var held = state == PetState.Held;
        var flying = state is PetState.Fly or PetState.Land or PetState.FlyOff
            or PetState.BringGift or PetState.Fall or PetState.Held;
        var sleeping = state == PetState.Sleep;

        // ---- Розворот: плавно проходить через 0 ----
        var sx = pose.FacingAnim;
        if (Math.Abs(sx) < 0.06) sx = sx >= 0 ? 0.06 : -0.06;
        var dir = sx >= 0 ? 1.0 : -1.0;

        // ---- Вертикальні рухи ----
        // Під час ходьби тіло найвище, коли лапи перетинаються, і найнижче — у фазі максимального кроку.
        var bob = 5.0 * Math.Pow(Math.Cos(pose.Stride), 2.0) * Math.Clamp(pose.Gait, 0, 1);   // гладка хвиля без зламів
        var flapPh = pose.WingPhase;
        // У польоті тіло злегка підкидає на помаху крил + повільна хвиля.
        var flyBob = flying && !held ? -Math.Cos(flapPh) * 4.2 * pose.WingAmp + Math.Sin(t * 2.4) * 2.4 : 0.0;
        var petBounce = pose.PetIntensity * (0.5 - 0.5 * Math.Cos(t * 2.4)) * 3.5;
        var up = pose.Lift + bob * 0.45 + petBounce + flyBob;

        // ---- Нахил: згладжений мозком + швидке тремтіння ----
        var lean = pose.Lean + pose.Wobble;

        // ---- Дихання, присідання/приземлення, витягування при падінні ----
        var breath = sleeping ? Math.Sin(t * 1.5) * 0.012 : Math.Sin(t * 2.0) * 0.0035;
        var stretch = state == PetState.Fall ? Math.Clamp(pose.VelY / 9000.0, 0, 0.12) : 0.0;
        var squashY = (sleeping ? 0.88 : 1.0) + breath - 0.18 * pose.Squash + stretch;
        var squashX = (sleeping ? 1.05 : 1.0) + 0.14 * pose.Squash - stretch * 0.5;
        if (flying && !held)
        {
            // Удар крил «пульсує» тілом: на змаху вниз стискається, на змаху вгору розправляється.
            var pulse = Math.Sin(flapPh) * 0.028 * pose.WingAmp;
            squashY += pulse;
            squashX -= pulse * 0.7;
        }
        squashY = Math.Clamp(squashY, 0.7, 1.18);
        squashX = Math.Clamp(squashX, 0.85, 1.2);

        dc.PushTransform(new TranslateTransform(SubPixelX, SubPixelY));
        dc.PushTransform(new TranslateTransform(OffX, OffY));

        if (!flying)
        {
            var feetX = Cx + 26 * sx;
            DrawShadow(dc, feetX, pose.Lift);
            DrawDust(dc, pose.Impact, feetX);
        }

        dc.PushTransform(new TranslateTransform(0, -up));
        dc.DrawRoundedRectangle(HitBrush, null, new Rect(15, 4, 190, 206), 40, 40);   // клікабельна зона
        dc.PushTransform(new ScaleTransform(sx, 1, Cx, 0));
        dc.PushTransform(new RotateTransform(lean, Cx, GroundY - 50));
        dc.PushTransform(new SkewTransform(pose.Sway, 0, Cx, GroundY));   // інерція: верх відстає
        dc.PushTransform(new ScaleTransform(squashX, squashY, Cx, GroundY));

        DrawSpeedLines(dc, pose, dir, t);
        DrawFeathers(dc, pose, t);

        // Легке розмиття руху: бліді «сліди» птаха позаду на великій швидкості.
        var spd = Math.Sqrt(pose.VelX * pose.VelX + pose.VelY * pose.VelY);
        if (flying && !held && spd > 150)
        {
            var ga = Math.Clamp((spd - 150) / 220.0, 0, 1) * 0.24;
            for (var gi = 2; gi >= 1; gi--)
            {
                dc.PushOpacity(ga / gi);
                dc.PushTransform(new TranslateTransform(-pose.VelX * dir * 0.045 * gi, -pose.VelY * 0.045 * gi));
                dc.PushTransform(new MatrixTransform(RefScale, 0, 0, RefScale, RefOffX, RefOffY));
                DrawBird(dc, pose, t, flapPh - 0.55 * gi, dir);
                dc.Pop();
                dc.Pop();
                dc.Pop();
            }
        }

        dc.PushTransform(new MatrixTransform(RefScale, 0, 0, RefScale, RefOffX, RefOffY));
        DrawBird(dc, pose, t, flapPh, dir);
        dc.Pop();

        DrawFood(dc, pose, t);
        DrawGiftInBeak(dc, pose, t);
        DrawPetHearts(dc, pose, t);
        DrawNotes(dc, pose, t);
        DrawBurst(dc, pose);
        DrawAngerMark(dc, pose, t);

        dc.Pop();   // squash
        dc.Pop();   // skew
        dc.Pop();   // lean
        dc.Pop();   // facing
        dc.Pop();   // up

        // Текст і літери не відзеркалюємо — малюємо поза розворотом.
        DrawSleepZzz(dc, pose, t, dir);
        DrawSpeech(dc, pose);

        dc.Pop();   // sprite offset
        dc.Pop();   // sub-pixel
    }

    private static void DrawShadow(DrawingContext dc, double feetX, double lift)
    {
        var k = Math.Clamp(lift / 40.0, 0, 1);
        dc.PushOpacity(0.20 * (1 - 0.6 * k));
        dc.DrawEllipse(ShadowBrush, null, new Point(feetX, GroundY + 3), 46 * (1 - 0.3 * k), 6 * (1 - 0.3 * k));
        dc.Pop();
    }

    /// <summary>Хмарки пилу з обох боків від лап одразу після приземлення.</summary>
    private static void DrawDust(DrawingContext dc, double impact, double feetX)
    {
        if (impact <= 0.02)
            return;

        var p = 1 - impact;                 // 0 → 1: хмарка розростається й тане
        dc.PushOpacity(Math.Clamp(impact * 0.95, 0, 1));
        for (var side = -1; side <= 1; side += 2)
        {
            for (var k = 0; k < 3; k++)
            {
                var x = feetX + side * (18 + p * (30 + k * 16));
                var y = GroundY + 1 - p * (6 + k * 8);
                var r = (5.0 + k * 3.0) * (0.6 + p * 1.1);
                dc.DrawEllipse(DustBrush, null, new Point(x, y), r, r * 0.8);
            }
        }
        dc.Pop();
    }

    /// <summary>Лінії вітру позаду папуги: біжать назад і мерехтять, коли він летить швидко вперед.</summary>
    private static void DrawSpeedLines(DrawingContext dc, PetPose pose, double dir, double t)
    {
        var fwd = pose.VelX * dir;
        if (fwd < 90 || pose.State is PetState.Held or PetState.Walk or PetState.Peek or PetState.Grumpy)
            return;

        var a = Math.Clamp((fwd - 90) / 170.0, 0, 1);
        for (var i = 0; i < 5; i++)
        {
            var ph = (t * 0.55 + i * 0.37) % 1.0;                 // 0 → 1: лінія пролітає назад і зникає
            var y = 66 + i * 26 + Math.Sin(i * 2.3) * 6;
            var x0 = 40 - ph * 34 - i * 3;
            var len = (18 + i * 3) * (0.6 + a * 0.9);
            dc.PushOpacity(a * 0.6 * Math.Sin(ph * Math.PI));
            dc.DrawLine(SpeedPen, new Point(x0, y), new Point(x0 - len, y + 1.5));
            dc.Pop();
        }
    }

    /// <summary>Малює птаха в координатах референсу.</summary>
    private void DrawBird(DrawingContext dc, PetPose pose, double t, double flapPh, double dir)
    {
        var anger = Math.Clamp(pose.Anger, 0, 1);
        var state = pose.State;
        var sleeping = state == PetState.Sleep;
        var dancing = state == PetState.Play;

        // Плавне розкриття крила: те саме крило, що й у спокої, підіймається від спини й росте.
        var open = Math.Clamp(pose.WingOpen, 0, 1);
        var baseMode = open > 0.3 ? WingMode.Rest
            : dancing ? WingMode.Flex
            : anger >= 0.35 ? WingMode.Crossed
            : WingMode.Rest;

        var stretchPose = state == PetState.Preen && pose.PreenKind == 1;
        var flapS = Math.Sin(flapPh - 0.2 * Math.Sin(flapPh));          // майже синусоїда: плавно, з легкою асиметрією
        var held = state == PetState.Held;
        var swing = stretchPose ? 138.0 + Math.Sin(t * 3.0) * 8.0
            : held ? 150.0 + flapS * 26.0                // піднято вгору й тріпоче, а не стирчить убік «руками»
            : 95.0 + flapS * 55.0 * pose.WingAmp;     // розмах залежить від інтенсивності помаху
        var theta = swing * open;
        // Під час помаху вгору крило трохи складається, вниз — розкривається (як у справжньої птахи).
        var wScale = (1.0 + 0.3 * open) * (1.0 - 0.10 * Math.Max(0.0, Math.Cos(flapPh)) * pose.WingAmp);
        var wingOut = open > 0.02;

        // Хвіст запізнюється за рухом: тягнеться назад при швидкості та підлітає при падінні.
        var fwd = pose.VelX * dir;
        var tailLag = Math.Clamp(fwd * 0.04, -8, 10) + Math.Clamp(pose.VelY * 0.006, -4, 8);
        var sway = open > 0.5 ? Math.Sin(flapPh - 1.2) * 5.0 * pose.WingAmp : Math.Sin(t * (state == PetState.Idle ? 1.4 : 3.0)) * 2.5;
        // Іноді в спокої махне хвостиком (швидкий помах раз на кілька секунд).
        var flick = state == PetState.Idle ? Math.Pow(Math.Max(0, Math.Sin(t * 0.37)), 8.0) * Math.Sin(t * 6.0) * 3.5 : 0.0;
        var droop = sleeping ? 6.0 : 0.0;
        // Хвіст хилиться зсувом (skew) від лінії кріплення x=120, а не поворотом навколо середини:
        // кути кріплення (118,277) та (121,358) лишаються на місці, тож між хвостом і тілом не виникає щілини.
        var tailRot = new SkewTransform(0, sway + droop + tailLag + flick, 120, 0);

        // 1. Ніжки (під тілом) — у кожному стані
        DrawLegs(dc, pose, t);

        // 2. Кремова обводка-стікер навколо силуету
        dc.DrawGeometry(null, HaloPen, ParrotShapes.BodyFill);
        dc.PushTransform(tailRot);
        dc.DrawGeometry(null, HaloPen, ParrotShapes.TailFill);
        dc.Pop();
        if (open < 0.62)
        {
            dc.PushOpacity(1.0 - open * 1.6);
            DrawBigWingHalo(dc, baseMode);
            dc.Pop();
        }
        // Далеке крило в польоті — повноцінне (друге таке саме), з відставанням за фазою.
        // Далеке крило махає в протилежний бік (вперед-вгору), ближнє — назад-вгору: разом утворюють «V».
        var farS = Math.Sin(flapPh - 0.2 * Math.Sin(flapPh) - 0.25);
        var farTheta = stretchPose ? -105.0
            : held ? -open * (150.0 + farS * 26.0)
            : -open * (25.0 + (farS * pose.WingAmp + 1.0) * 0.5 * 120.0);
        if (wingOut)
        {
            dc.PushOpacity(Math.Clamp(open * 1.4, 0, 1));
            DrawFarFlightWing(dc, farTheta, wScale * 0.94, true);
            dc.Pop();
        }
        if (wingOut)
        {
            dc.PushOpacity(Math.Clamp(open * 4.0, 0, 1));
            dc.DrawGeometry(null, HaloPen, Xf(WingLeaf, theta, wScale, 145, 262));
            dc.Pop();
        }

        // 3. Хвіст, тіло
        dc.PushTransform(tailRot);
        dc.DrawGeometry(BodyBrush, null, ParrotShapes.TailFill);
        dc.DrawGeometry(null, OutlinePen, ParrotShapes.TailLine);
        dc.Pop();
        dc.DrawGeometry(BodyBrush, null, ParrotShapes.BodyFill);
        dc.DrawGeometry(null, OutlinePen, ParrotShapes.BodyLine);

        // 3б. Далеке крило поверх обводки тіла, але під тілом (видно лише те, що виглядає зі-за спини)
        // 5–6. Обличчя трохи «повертається» за поглядом: щічки, очі й дзьоб зсуваються разом.
        dc.PushTransform(new TranslateTransform(pose.GazeX * 3.5, pose.GazeY * 2.0));
        DrawCheeks(dc, anger, pose.PetIntensity);
        DrawFace(dc, pose, anger, sleeping, t);
        dc.Pop();

        // Край тіла лежить поверх правого ока та щічки (як на референсі): око наполовину «за» контуром.
        dc.PushClip(new RectangleGeometry(new Rect(215, 120, 85, 135)));
        dc.DrawGeometry(null, OutlinePen, ParrotShapes.BodyLine);
        dc.Pop();

        // 7. Крила — поверх усього (тулуб, щічки, обличчя), корінь вростає в тіло.
        DrawWings(dc, pose, baseMode, t, open, flapS);
        if (wingOut)
        {
            // Далеке крило — повноцінне, поверх грудей (видно, як і ближнє).
            dc.PushOpacity(Math.Clamp(open * 1.6, 0, 1));
            DrawFarFlightWing(dc, farTheta, wScale * 0.94, false);
            dc.Pop();
        }
        if (wingOut)
        {
            DrawWingShape(dc, theta, wScale, 145, 262, 0, 0, Math.Clamp(open * 4.0, 0, 1));
        }

    }

    // ------------------------------------------------------------------
    // Ніжки
    // ------------------------------------------------------------------

    private static double Lerp(double a, double b, double k) => a + (b - a) * k;

    private static void Foot(double phase, out double pos, out double lift)
    {
        const double amp = 8.0;
        var u = phase / (2.0 * Math.PI);
        u -= Math.Floor(u);
        if (u < 0.5)
        {
            pos = amp * (1.0 - 4.0 * u);   // опора: +A → −A з постійною швидкістю
            lift = 0.0;
        }
        else
        {
            var s = (u - 0.5) * 2.0;        // винос уперед: −A → +A, плавний старт і зупинка
            var e = s * s * (3.0 - 2.0 * s);
            pos = -amp + 2.0 * amp * e;
            lift = Math.Sin(Math.PI * s) * 8.0;
        }
    }

    private static void DrawLegs(DrawingContext dc, PetPose pose, double t)
    {
        var state = pose.State;
        var energetic = state == PetState.Play;

        // 0 — стоїть на землі, 1 — підтягнуті під тіло (плавно, з мозку)
        var tuck = Math.Clamp(pose.Tuck, 0, 1);
        var gait = Math.Clamp(pose.Gait, 0, 1);

        double posL = 0, posR = 0, liftL = 0, liftR = 0;
        if (gait > 0.01)
        {
            // Хода: лапи в протифазі. Підняті — під час переносу вперед, на землі — рухаються назад
            // зі швидкістю тіла (розмах 19 = довжина кроку), тому не «ковзають».
            // Опорна лапа рухається назад РІВНО зі швидкістю тіла (лінійно) — не ковзає й не «сіпається»;
            // винесена вперед лапа летить м'якою дугою. Півкроку тіла = 16 DIP, тож розмах ±8.
            Foot(pose.Stride, out posL, out liftL);
            Foot(pose.Stride + Math.PI, out posR, out liftR);
            posL *= gait; posR *= gait; liftL *= gait; liftR *= gait;
        }
        else if (energetic)
        {
            var s = Math.Sin(t * 1.17);
            liftL = Math.Max(0, s) * 5.0;
            liftR = Math.Max(0, -s) * 5.0;
        }

        // Під час перенесення/падіння болтає лапами
        var swing = state is PetState.Held or PetState.Fall;
        var dangle = swing ? Math.Sin(t * 3.0) * 6.0 : 0.0;
        var dangleR = swing ? Math.Sin(t * 3.0 + 1.6) * 6.0 : 0.0;

        var lFx = Lerp(180 + posL, 176 + dangle, tuck);
        var lFy = Lerp(397 - liftL, 380 + Math.Abs(dangle) * 0.5, tuck);
        var rFx = Lerp(229 + posR, 221 + dangleR, tuck);
        var rFy = Lerp(394 - liftR, 377 + Math.Abs(dangleR) * 0.5, tuck);

        var legs = new[]
        {
            BuildLeg(179, 364, lFx, lFy, tuck, 1.0, liftL * 0.6 + tuck * 4.0, gait),
            BuildLeg(224, 361, rFx, rFy, tuck, 0.85, liftR * 0.6 + tuck * 4.0, gait)
        };
        foreach (var g in legs) dc.DrawGeometry(null, LegHaloPen, g);
        foreach (var g in legs) dc.DrawGeometry(null, LegPen, g);
    }

    /// <summary>Лапка: стегно → «коліно» (згинається вперед, коли лапа піднята) → ступня → пальці.</summary>
    private static Geometry BuildLeg(double rx, double ry, double fx, double fy, double tuck, double size, double knee, double gait = 0)
    {
        // Під час ходьби пальці коротші, щоб не торкатися сусідньої лапи.
        var f1x = Lerp(31, 11, tuck) * size * (1.0 - 0.45 * Math.Clamp(gait, 0, 1));
        var f1y = Lerp(8, 4, tuck) * size;
        var f2x = Lerp(14, 5, tuck) * size;
        var f2y = Lerp(17, 8, tuck) * size;
        var bx = Lerp(-12, -5, tuck) * size;
        var by = Lerp(9, 4, tuck) * size;

        var kx = (rx + fx) / 2 + knee;
        var ky = (ry + fy) / 2;

        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(rx, ry), false, false);
            ctx.LineTo(new Point(kx, ky), true, true);
            ctx.LineTo(new Point(fx, fy), true, true);
            ctx.LineTo(new Point(fx + f1x, fy + f1y), true, true);
            ctx.BeginFigure(new Point(fx, fy), false, false);
            ctx.LineTo(new Point(fx + f2x, fy + f2y), true, true);
            ctx.BeginFigure(new Point(fx, fy), false, false);
            ctx.LineTo(new Point(fx + bx, fy + by), true, true);
        }
        g.Freeze();
        return g;
    }

    /// <summary>Далеке крило в польоті: те саме крило, що й ближнє, але від грудного плеча.</summary>
    private static void DrawFarFlightWing(DrawingContext dc, double theta, double scale, bool halo)
    {
        if (halo)
            dc.DrawGeometry(null, HaloPen, Xf(WingLeaf, theta, scale, 272, 250, 127, -12));
        else
            DrawWingShape(dc, theta, scale, 272, 250, 127, -12, 1.0);
    }

    /// <summary>
    /// Крило цілком: заливка + відкрита обводка по зовнішньому краю (корінь «вростає» в тіло).
    /// Геометрію трансформуємо, а не малюємо під PushTransform, тож товщина ліній не «дихає» разом з масштабом.
    /// </summary>
    private static void DrawWingShape(DrawingContext dc, double theta, double scale, double px, double py, double tx, double ty, double alpha)
    {
        var opaque = alpha >= 0.999;
        if (!opaque) dc.PushOpacity(alpha);
        dc.DrawGeometry(BodyBrush, null, Xf(ParrotShapes.FlightWing, theta, scale, px, py, tx, ty));
        dc.DrawGeometry(null, OutlinePen, Xf(ParrotShapes.FlightWingEdge, theta, scale, px, py, tx, ty));
        if (!opaque) dc.Pop();
    }

    /// <summary>Копія геометрії з перетворенням «зсув → масштаб → поворот» навколо плеча (перо лінії не масштабується).</summary>
    private static Geometry Xf(Geometry src, double theta, double scale, double px, double py, double tx = 0, double ty = 0)
    {
        var m = Matrix.Identity;
        m.Translate(tx, ty);
        m.ScaleAt(scale, scale, px, py);
        m.RotateAt(theta, px, py);
        var g = src.Clone();
        g.Transform = new MatrixTransform(m);
        return g;
    }

    private static void DrawBigWingHalo(DrawingContext dc, WingMode mode)
    {
        switch (mode)
        {
            case WingMode.Rest:
                dc.DrawGeometry(null, HaloPen, ParrotShapes.FarWing);
                break;
            case WingMode.Crossed:
                dc.DrawGeometry(null, HaloPen, Xf(WingLeaf, -82, 0.66, 273, 262, 128, 0));
                break;
            case WingMode.Flex:
                dc.DrawGeometry(null, HaloPen, Xf(WingLeaf, FlexAngle(0), 0.74, 273, 262, 128, 0));
                break;
        }
    }

    private static double FlexAngle(double t) => -152.0 + Math.Sin(t * 1.17) * 14.0;

    private static void DrawWings(DrawingContext dc, PetPose pose, WingMode mode, double t, double open, double flapS)
    {
        switch (mode)
        {
            case WingMode.Rest:
            {
                var wiggle = pose.State switch
                {
                    PetState.Walk or PetState.Peek => Math.Sin(pose.Stride) * 2.0,
                    PetState.Petted => 6.0 + Math.Sin(t * 3.0) * 3.0,
                    PetState.Preen => 7.0 + Math.Sin(t * 2.5) * 4.0,
                    PetState.Hop => 3.0 + pose.Lift * 0.25,
                    PetState.PresentGift => Math.Sin(t * 2.0) * 3.0,
                    _ => 0.0
                };
                // Далеке крило (з боку грудей) теж трепече в польоті.
                if (open < 0.62)
                {
                    dc.PushOpacity(1.0 - open * 1.6);
                    dc.PushTransform(new RotateTransform(open * 10.0, 272, 250));
                    dc.DrawGeometry(BodyBrush, OutlinePen, ParrotShapes.FarWing);
                    dc.Pop();
                    dc.Pop();
                }
                if (open < 0.25)
                {
                    // лінії крила в спокої плавно розчиняються, поки з'являється повне крило
                    dc.PushOpacity(Math.Clamp(1.0 - open * 4.0, 0, 1));
                    dc.PushTransform(new RotateTransform(wiggle, 140, 255));
                    dc.DrawGeometry(null, DetailPen, ParrotShapes.NearWingOut);
                    dc.DrawGeometry(null, DetailPen, ParrotShapes.NearWingIn);
                    dc.Pop();
                    dc.Pop();
                }
                break;
            }
            case WingMode.Crossed:
                // Крило притиснуте до грудей, кінчик дивиться вперед (образа).
                DrawWingShape(dc, -82, 0.66, 273, 262, 128, 0, 1.0);
                break;
            case WingMode.Flex:
                // Танець: крило піднято вгору й пружинить в такт.
                DrawWingShape(dc, FlexAngle(t), 0.74, 273, 262, 128, 0, 1.0);
                break;
        }
    }

    private static void DrawCheeks(DrawingContext dc, double anger, double blush)
    {
        var grow = 1.0 + anger * 0.15 + blush * 0.18;
        var brush = anger > 0.35 ? CheekAngryBrush : CheekBrush;

        // Права щічка наполовину ховається за краєм тіла.
        dc.PushClip(ParrotShapes.BodyFill);
        dc.PushTransform(new RotateTransform(0, 258, 208));
        dc.DrawEllipse(brush, null, new Point(258, 208), 8 * grow, 11 * grow);
        dc.Pop();
        dc.Pop();

        dc.PushTransform(new RotateTransform(-8, 148, 217));
        dc.DrawEllipse(brush, CheekPen, new Point(148, 217), 18 * grow, 23 * grow);
        dc.Pop();
    }

    private void DrawFace(DrawingContext dc, PetPose pose, double anger, bool sleeping, double t)
    {
        var yawn = Yawn(pose);
        var sleepBlink = sleeping ? Math.Clamp(pose.SleepPhase * 4.0 - 0.3, 0, 1) : 0.0;
        var blink = Math.Max(Math.Max(pose.BlinkPhase, sleepBlink), yawn > 0.5 ? 1.0 : 0.0);
        var happy = pose.State == PetState.Petted || pose.PetIntensity > 0.55;
        var lid = Math.Clamp((anger - 0.45) * 1.8, 0, 1) * 0.55;
        var gaze = anger * 1.0;
        var irisK = pose.State is PetState.Held or PetState.Fall ? 0.82 : 1.0;
        var gx = Math.Clamp(pose.GazeX, -1, 1);
        var gy = Math.Clamp(pose.GazeY, -1, 1);

        // Обидва ока дивляться в один бік: спільний зсув зіниць, пропорційний розміру ока.
        // (раніше в очей були різні зсуви — виходив «розбіжний», подвійний погляд)
        var ex = 3.0 + gaze * 4.0 + gx * 4.5;      // базово зіниці зсунуті в бік дзьоба — погляд спрямований
        var ey = 1.0 + gaze * 3.0 + gy * 3.5;

        // праве око — під дзьобом і наполовину за краєм тіла, тому малюємо першим і обрізаємо по тілу
        var clipGeo = ParrotShapes.BodyFill.Clone();
        clipGeo.Transform = new TranslateTransform(-pose.GazeX * 3.5, -pose.GazeY * 2.0);
        dc.PushClip(clipGeo);
        DrawEye(dc, RightEye, ex * 0.6, ey * 0.72, lid, blink, happy, sleeping, irisK);
        dc.Pop();
        DrawEye(dc, LeftEye, ex, ey, lid, blink, happy, sleeping, irisK);

        DrawBeak(dc, pose, t);

        if (anger > 0.25 && blink < 0.85 && !happy)
        {
            var k = Math.Clamp((anger - 0.25) / 0.5, 0, 1);
            var up = (1 - k) * 6.0;
            dc.DrawLine(BrowPen, new Point(LeftEye.X - 2, LeftEye.Y - 40 - up), new Point(LeftEye.X + 20, LeftEye.Y - 27 - up * 0.5));
            dc.DrawLine(BrowPen, new Point(RightEye.X - 12, RightEye.Y - 21 - up * 0.5), new Point(RightEye.X + 2, RightEye.Y - 27 - up));
        }
    }

    private static void DrawEye(DrawingContext dc, EyeSpec e, double gx, double gy,
        double lid, double blink, bool happy, bool sleeping, double irisK = 1.0)
    {
        dc.PushTransform(new RotateTransform(e.Rot, e.X, e.Y));

        if (happy || blink > 0.85)
        {
            // «^» — радість, «︶» — заплющені очі
            var half = e.Rx * 0.8;
            var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new Point(e.X - half, e.Y + (happy ? 4 : -2)), false, false);
                ctx.QuadraticBezierTo(new Point(e.X, e.Y + (happy ? -12 : 10)), new Point(e.X + half, e.Y + (happy ? 4 : -2)), true, true);
            }
            g.Freeze();
            dc.DrawGeometry(null, EyePen, g);
            dc.Pop();
            return;
        }

        var ry = e.Ry * (1.0 - blink * 0.8);
        var centre = new Point(e.X, e.Y);
        var ellipse = new EllipseGeometry(centre, e.Rx, ry);

        // Повіка: похила лінія, що зрізає верх ока (сердитий погляд).
        var c = -ry + lid * ry * 1.1;
        var m = e.Inner * lid * 0.45;
        var clipped = lid > 0.02;
        if (clipped)
        {
            var xl = -e.Rx - 6;
            var xr = e.Rx + 6;
            var poly = new StreamGeometry();
            using (var ctx = poly.Open())
            {
                ctx.BeginFigure(new Point(e.X + xl, e.Y + c + m * xl), true, true);
                ctx.LineTo(new Point(e.X + xr, e.Y + c + m * xr), true, false);
                ctx.LineTo(new Point(e.X + xr, e.Y + ry + 6), true, false);
                ctx.LineTo(new Point(e.X + xl, e.Y + ry + 6), true, false);
            }
            poly.Freeze();
            dc.PushClip(poly);
        }

        dc.DrawEllipse(EyeBrush, EyePen, centre, e.Rx, ry);

        dc.PushClip(ellipse);
        dc.DrawEllipse(IrisBrush, null, new Point(e.X + gx, e.Y + gy + (clipped ? -ry * 0.1 : 0)), e.IrisRx * irisK, e.IrisRy * irisK * (1.0 - blink * 0.6));
        dc.Pop();

        if (clipped)
        {
            dc.Pop();
            // кінці повіки — перетин лінії з еліпсом
            var a = 1 / (e.Rx * e.Rx) + m * m / (ry * ry);
            var b = 2 * c * m / (ry * ry);
            var cc = c * c / (ry * ry) - 1;
            var disc = b * b - 4 * a * cc;
            if (disc > 0)
            {
                var sq = Math.Sqrt(disc);
                var x1 = (-b - sq) / (2 * a);
                var x2 = (-b + sq) / (2 * a);
                dc.DrawLine(LidPen, new Point(e.X + x1, e.Y + c + m * x1), new Point(e.X + x2, e.Y + c + m * x2));
            }
        }

        dc.Pop();
    }

    private static void DrawBeak(DrawingContext dc, PetPose pose, double t)
    {
        var open = pose.EatIntensity * 9.0 + (pose.State == PetState.Petted ? 3.0 : 0.0);
        open += pose.Talk * (0.5 - 0.5 * Math.Cos(t * 5.0)) * 6.0;
        open += Yawn(pose) * 11.0;
        if (pose.State == PetState.Preen)
            open += (0.5 - 0.5 * Math.Cos(t * 4.0)) * 2.5;

        // Все безперервне: жодних порогів, на яких дзьоб «клацає» між закритим і відкритим виглядом.
        var k = Math.Clamp(open / 1.5, 0.0, 1.0);
        if (k > 0.001)
        {
            // нижня половинка дзьоба опускається, всередині темний рот
            dc.PushOpacity(k);
            dc.DrawEllipse(MouthBrush, null, new Point(214, 219 + open * 0.4), 11, 2 + open * 0.55);
            dc.DrawEllipse(BeakBrush, BeakPen, new Point(212, 224 + open), 12, 6.5);
            dc.Pop();
        }

        dc.DrawGeometry(BeakBrush, null, ParrotShapes.Beak);
        dc.DrawGeometry(null, BeakPen, ParrotShapes.BeakRidge);
        if (k < 0.999)
        {
            // лінії закритого рота плавно зникають, коли дзьоб відкривається
            dc.PushOpacity(1.0 - k);
            dc.DrawGeometry(null, MouthPen, ParrotShapes.Mouth);
            dc.DrawGeometry(null, MouthPen, ParrotShapes.MouthBarb);
            dc.DrawGeometry(null, MouthPen, ParrotShapes.BeakChin);
            dc.Pop();
        }
    }

    // ------------------------------------------------------------------
    // Подарунки, їжа, ефекти
    // ------------------------------------------------------------------

    private static readonly Brush PinkBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0x8F, 0xB1)));
    private static readonly Brush GoldBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0xC8, 0x2E)));
    private static readonly Brush GreenBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x5C, 0xB8, 0x4A)));
    private static readonly Brush RedBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xE0, 0x30, 0x40)));
    private static readonly Brush BlueBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x5F, 0xC4, 0xF2)));
    private static readonly Brush TealBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x3F, 0xB8, 0xB0)));
    private static readonly Brush CreamBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0xF6, 0xDD)));
    private static readonly Brush TanBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xD9, 0xA3, 0x5B)));
    private static readonly Brush ChocBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x5A, 0x34, 0x1C)));
    private static readonly Brush SeedFillBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xE4, 0xDE, 0xC8)));
    private static readonly Brush WhiteBrush = Freeze(new SolidColorBrush(Colors.White));

    private static readonly Pen GiftThinPen = FreezePen(MakePen(OutlineBrush, 1.0));
    private static readonly Pen StemPen = FreezePen(MakePen(new SolidColorBrush(Color.FromRgb(0x3F, 0x8A, 0x35)), 1.8));
    private static readonly Pen GoldDarkPen = FreezePen(MakePen(new SolidColorBrush(Color.FromRgb(0xB5, 0x7B, 0x10)), 1.2));

    private static readonly Geometry StarGeo = MakeStar();
    private static readonly Geometry FeatherGeo = MakeFeather();
    private static readonly Geometry GemGeo = Poly(-8, -3, -4, -8, 4, -8, 8, -3, 0, 9);

    private static Geometry Poly(params double[] xy)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(xy[0], xy[1]), true, true);
            for (var i = 2; i + 1 < xy.Length; i += 2)
                ctx.LineTo(new Point(xy[i], xy[i + 1]), true, false);
        }
        g.Freeze();
        return g;
    }

    private static Geometry MakeStar()
    {
        var pts = new double[20];
        for (var i = 0; i < 10; i++)
        {
            var r = i % 2 == 0 ? 10.0 : 4.3;
            var a = -Math.PI / 2 + i * Math.PI / 5;
            pts[i * 2] = Math.Cos(a) * r;
            pts[i * 2 + 1] = Math.Sin(a) * r;
        }
        return Poly(pts);
    }

    private static Geometry MakeFeather()
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(-8, 9), true, true);
            ctx.QuadraticBezierTo(new Point(-11, -6), new Point(6, -10), true, false);
            ctx.QuadraticBezierTo(new Point(9, 1), new Point(-8, 9), true, false);
        }
        g.Freeze();
        return g;
    }

    private static void DrawSparkle(DrawingContext dc, double x, double y, double r)
    {
        dc.PushTransform(new MatrixTransform(r, 0, 0, r, x, y));
        dc.DrawGeometry(SparkleBrush, null, UnitSparkle);
        dc.Pop();
    }

    /// <summary>Насіння на землі перед папугою: поки він їсть, зерен поступово меншає.</summary>
    private static void DrawFood(DrawingContext dc, PetPose pose, double t)
    {
        if (pose.State != PetState.Eat && !pose.Food)
            return;

        var count = pose.State == PetState.Eat ? (int)Math.Ceiling(6 * (1 - pose.Progress)) : 6;
        for (var i = 0; i < count; i++)
        {
            var x = 126 + i * 9 + (i % 2) * 2;
            var y = 197 + (i % 2) * 1.5;
            dc.PushTransform(new RotateTransform(i * 35 - 40, x, y));
            dc.DrawEllipse(SeedBrush, null, new Point(x, y), 3.2, 1.8);
            dc.Pop();
        }
    }

    /// <summary>Подарунок у дзьобі: несе в польоті, високо тримає, коли показує.</summary>
    private static void DrawGiftInBeak(DrawingContext dc, PetPose pose, double t)
    {
        if (pose.Gift == GiftKind.None)
            return;

        var presenting = pose.State == PetState.PresentGift;
        var ax = BeakX + 9;
        var ay = BeakY + 12 + (presenting ? Math.Sin(t * 1.6) * 2.0 : Math.Sin(t * 5.0) * 1.5);
        var s = presenting ? 1.45 : 1.05;

        DrawGift(dc, pose.Gift, ax, ay, s);

        if (presenting)
        {
            for (var i = 0; i < 3; i++)
            {
                var ang = t * 0.35 + i * 2.1;
                var px = ax + Math.Cos(ang) * 19;
                var py = ay + Math.Sin(ang) * 15;
                var size = 2.2 + 2.2 * Math.Abs(Math.Sin(t * 0.9 + i * 1.7));
                DrawSparkle(dc, px, py, size);
            }
        }
    }

    private static void DrawGift(DrawingContext dc, GiftKind kind, double x, double y, double s)
    {
        dc.PushTransform(new MatrixTransform(s, 0, 0, s, x, y));
        switch (kind)
        {
            case GiftKind.Flower:
                dc.DrawLine(StemPen, new Point(0, 2), new Point(-2, 15));
                for (var i = 0; i < 5; i++)
                {
                    dc.PushTransform(new RotateTransform(i * 72, 0, 0));
                    dc.DrawEllipse(PinkBrush, GiftThinPen, new Point(0, -5.6), 3.8, 5.0);
                    dc.Pop();
                }
                dc.DrawEllipse(GoldBrush, GiftThinPen, new Point(0, 0), 3.2, 3.2);
                break;

            case GiftKind.Berry:
                dc.DrawLine(StemPen, new Point(-5, -1), new Point(0, -10));
                dc.DrawLine(StemPen, new Point(5, 0), new Point(0, -10));
                dc.DrawEllipse(RedBrush, GiftThinPen, new Point(-5, 4), 5.2, 5.2);
                dc.DrawEllipse(RedBrush, GiftThinPen, new Point(5, 5), 5.2, 5.2);
                dc.DrawEllipse(WhiteBrush, null, new Point(-6.6, 2.4), 1.3, 1.3);
                dc.DrawEllipse(WhiteBrush, null, new Point(3.4, 3.4), 1.3, 1.3);
                dc.PushTransform(new RotateTransform(-30, 4, -9));
                dc.DrawEllipse(GreenBrush, GiftThinPen, new Point(4, -9), 4.2, 2.0);
                dc.Pop();
                break;

            case GiftKind.Star:
                dc.DrawGeometry(GoldBrush, GiftPen, StarGeo);
                break;

            case GiftKind.Feather:
                dc.DrawGeometry(TealBrush, GiftThinPen, FeatherGeo);
                dc.DrawLine(GiftPen, new Point(-9, 10), new Point(5, -8));
                break;

            case GiftKind.Gem:
                dc.DrawGeometry(BlueBrush, GiftPen, GemGeo);
                dc.DrawLine(GiftThinPen, new Point(-8, -3), new Point(8, -3));
                dc.DrawLine(GiftThinPen, new Point(-4, -8), new Point(-2.5, -3));
                dc.DrawLine(GiftThinPen, new Point(4, -8), new Point(2.5, -3));
                dc.DrawLine(GiftThinPen, new Point(-2.5, -3), new Point(0, 9));
                dc.DrawLine(GiftThinPen, new Point(2.5, -3), new Point(0, 9));
                break;

            case GiftKind.Letter:
                dc.DrawRoundedRectangle(CreamBrush, GiftPen, new Rect(-9.5, -6.5, 19, 13), 1.5, 1.5);
                dc.DrawLine(GiftThinPen, new Point(-9.5, -6.5), new Point(0, 1.5));
                dc.DrawLine(GiftThinPen, new Point(0, 1.5), new Point(9.5, -6.5));
                dc.DrawEllipse(HeartBrush, null, new Point(0, 2), 2.2, 2.2);
                break;

            case GiftKind.Seed:
                dc.PushTransform(new RotateTransform(25, 0, 0));
                dc.DrawEllipse(SeedFillBrush, GiftPen, new Point(0, 0), 4.8, 8.5);
                dc.DrawLine(GiftThinPen, new Point(-1.6, -5.5), new Point(-1.6, 5.5));
                dc.DrawLine(GiftThinPen, new Point(1.6, -5.5), new Point(1.6, 5.5));
                dc.Pop();
                break;

            case GiftKind.Cookie:
                dc.DrawEllipse(TanBrush, GiftPen, new Point(0, 0), 8.8, 8.8);
                dc.DrawEllipse(ChocBrush, null, new Point(-3, -3), 1.5, 1.5);
                dc.DrawEllipse(ChocBrush, null, new Point(3, -1.5), 1.5, 1.5);
                dc.DrawEllipse(ChocBrush, null, new Point(-1, 3.5), 1.5, 1.5);
                dc.DrawEllipse(ChocBrush, null, new Point(4, 4.5), 1.3, 1.3);
                break;

            case GiftKind.Coin:
                dc.DrawEllipse(GoldBrush, GiftPen, new Point(0, 0), 8.5, 8.5);
                dc.DrawEllipse(null, GoldDarkPen, new Point(0, 0), 5.5, 5.5);
                dc.DrawEllipse(WhiteBrush, null, new Point(-3.2, -3.4), 1.3, 1.3);
                break;
        }
        dc.Pop();
    }

    private static void DrawBurst(DrawingContext dc, PetPose pose)
    {
        if (pose.Burst <= 0.01)
            return;

        var prog = 1.0 - pose.Burst;
        dc.PushOpacity(Math.Clamp(pose.Burst * 1.4, 0, 1));
        for (var i = 0; i < 8; i++)
        {
            var ang = i * Math.PI / 4.0 + 0.3;
            var r = 10 + prog * 48;
            DrawSparkle(dc, BeakX + 9 + Math.Cos(ang) * r, BeakY + 12 + Math.Sin(ang) * r, 3 + 4 * pose.Burst);
        }
        dc.Pop();
    }

    private static void DrawPetHearts(DrawingContext dc, PetPose pose, double t)
    {
        if (pose.PetIntensity < 0.08 && pose.State != PetState.Petted)
            return;

        var intensity = Math.Max(pose.PetIntensity, pose.State == PetState.Petted ? 0.65 : 0);

        for (var i = 0; i < 3; i++)
        {
            var phase = (t * 0.18 + i * 0.33) % 1.0;
            var x = Cx + 40 + i * 12 + Math.Sin(t * 1.2 + i) * 4;
            var y = 62 - phase * 44;
            dc.PushOpacity(Math.Clamp(intensity * 0.86 * (1 - phase * phase), 0, 1));
            DrawHeart(dc, x, y, 5 + intensity * 3);
            dc.Pop();
        }
    }

    private static void DrawHeart(DrawingContext dc, double x, double y, double s)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(x, y + s * 0.85), true, true);
            ctx.BezierTo(new Point(x - s * 1.3, y - s * 0.15), new Point(x - s * 0.55, y - s), new Point(x, y - s * 0.3), true, false);
            ctx.BezierTo(new Point(x + s * 0.55, y - s), new Point(x + s * 1.3, y - s * 0.15), new Point(x, y + s * 0.85), true, false);
        }
        g.Freeze();
        dc.DrawGeometry(HeartBrush, null, g);
    }

    /// <summary>Нотки над папугою, поки він танцює.</summary>
    private static void DrawNotes(DrawingContext dc, PetPose pose, double t)
    {
        if (pose.State != PetState.Play)
            return;

        for (var i = 0; i < 3; i++)
        {
            var phase = (t * 0.2 + i * 0.33) % 1.0;
            var x = Cx + 34 + i * 16 + Math.Sin(t * 0.8 + i) * 5;
            var y = 62 - phase * 50;
            dc.PushOpacity(Math.Clamp(1 - phase * phase, 0, 1));
            dc.PushTransform(new RotateTransform(-20, x, y));
            dc.DrawEllipse(NoteBrush, null, new Point(x, y), 3.4, 2.6);
            dc.Pop();
            dc.DrawLine(NotePen, new Point(x + 3, y - 0.5), new Point(x + 3, y - 11));
            dc.DrawLine(NotePen, new Point(x + 3, y - 11), new Point(x + 8, y - 7));
            dc.Pop();
        }
    }

    /// <summary>Позіхання перед сном: 0 → 1 → 0 за першу половину засинання.</summary>
    private static double Yawn(PetPose pose) =>
        pose.State == PetState.Sleep && pose.SleepPhase < 0.5 ? Math.Sin(pose.SleepPhase * 2.0 * Math.PI) : 0.0;

    /// <summary>Пір'їнки, що злітають і кружляють позаду папуги в польоті.</summary>
    private static void DrawFeathers(DrawingContext dc, PetPose pose, double t)
    {
        if (pose.WingOpen < 0.6 || pose.State == PetState.Held)
            return;

        for (var i = 0; i < 6; i++)
        {
            var ph = (t * 0.2 + i * 0.1667) % 1.0;
            var x = 80 - ph * 44 - i * 6;
            var y = 112 + ph * 78 + Math.Sin(ph * 9 + i * 2) * 7;
            dc.PushOpacity(Math.Clamp(1 - ph, 0, 1) * 0.9);
            dc.PushTransform(new RotateTransform(ph * 220 + i * 60, x, y));
            dc.DrawEllipse(BodyBrush, GiftThinPen, new Point(x, y), 2.2, 5.0);
            dc.Pop();
            dc.Pop();
        }
    }

    /// <summary>Мультяшна «жилка гніву» над головою, коли Ліфре ображений.</summary>
    private static void DrawAngerMark(DrawingContext dc, PetPose pose, double t)
    {
        if (pose.State != PetState.Grumpy && pose.Anger < 0.7)
            return;

        var k = Math.Clamp(pose.Anger, 0, 1);
        var sc = 1.0 + 0.18 * Math.Sin(t * 8.0);
        dc.PushOpacity(Math.Clamp(k * 1.2, 0, 1));
        dc.PushTransform(new MatrixTransform(sc, 0, 0, sc, 172, 50));
        for (var sxs = -1; sxs <= 1; sxs += 2)
        {
            for (var sys = -1; sys <= 1; sys += 2)
            {
                var g = new StreamGeometry();
                using (var ctx = g.Open())
                {
                    ctx.BeginFigure(new Point(sxs * 3, sys * 9), false, false);
                    ctx.QuadraticBezierTo(new Point(sxs * 3, sys * 3), new Point(sxs * 9, sys * 3), true, false);
                }
                g.Freeze();
                dc.DrawGeometry(null, AngerPen, g);
            }
        }
        dc.Pop();
        dc.Pop();
    }

    private void DrawSleepZzz(DrawingContext dc, PetPose pose, double t, double dir)
    {
        if (pose.State != PetState.Sleep || pose.SleepPhase <= 0.05)
            return;

        var alpha = Math.Min(1.0, pose.SleepPhase);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (var i = 0; i < 3; i++)
        {
            var phase = (t * 0.25 + i * 0.32) % 1.0;
            var text = new FormattedText(
                i == 2 ? "Z" : "z",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                ZzzFont,
                11 + i * 4 + phase * 3,
                ZzzBrush,
                dpi);
            var px = Cx + dir * (46 + i * 10 + phase * 10) - (dir < 0 ? text.Width : 0);
            dc.PushOpacity(alpha * (1 - phase * 0.5) * 0.85);
            dc.DrawText(text, new Point(px, 66 - i * 12 - phase * 22));
            dc.Pop();
        }
    }

    // ------------------------------------------------------------------
    // Бульбашка реплік
    // ------------------------------------------------------------------

    private FormattedText MakeSpeechText(string text) => new(
        text,
        CultureInfo.InvariantCulture,
        FlowDirection.LeftToRight,
        UiFont,
        LifreConstants.SpeechFontSize - 0.5,
        TextBrush,
        VisualTreeHelper.GetDpi(this).PixelsPerDip)
    {
        MaxTextWidth = 186,
        MaxTextHeight = 46,
        Trimming = TextTrimming.CharacterEllipsis
    };

    private void DrawSpeech(DrawingContext dc, PetPose pose)
    {
        var text = pose.Speech;
        if (string.IsNullOrEmpty(text) || pose.SpeechAlpha <= 0.01)
            return;

        if (_speechCache == null || !string.Equals(text, _speechCacheText, StringComparison.Ordinal))
        {
            _speechCacheText = text;
            _speechCache = MakeSpeechText(text);
            _speechPart = null;
            _speechPartLen = -1;
        }

        FormattedText ft = _speechCache!;
        var shown = ft;

        // «Друкарська машинка»: слова з'являються по одному (розмір бульбашки рахуємо за повним текстом).
        var chars = (int)pose.SpeechChars;
        if (chars < text.Length)
        {
            var end = text.IndexOf(' ', Math.Max(0, chars));
            var len = end < 0 ? text.Length : end;
            if (len < text.Length)
            {
                if (_speechPart == null || _speechPartLen != len)
                {
                    _speechPartLen = len;
                    _speechPart = MakeSpeechText(text.Substring(0, len));
                }
                shown = _speechPart;
            }
        }

        var w = ft.Width + 18;
        var h = ft.Height + 12;
        var bottom = -12.0;
        var top = bottom - h;
        var left = Math.Clamp(Cx - w / 2,
            -OffX + 2,
            Math.Max(-OffX + 2, LifreConstants.WindowWidth - OffX - w - 2));
        var tailX = Math.Clamp(Cx + 4, left + 14, left + w - 14);

        var pop = 0.82 + 0.18 * Math.Clamp(pose.SpeechAlpha, 0, 1);
        dc.PushOpacity(Math.Clamp(pose.SpeechAlpha, 0, 1));
        dc.PushTransform(new ScaleTransform(pop, pop, tailX, bottom + 9));
        dc.DrawRoundedRectangle(BubbleBrush, BubblePen, new Rect(left, top, w, h), 9, 9);
        dc.DrawGeometry(BubbleBrush, null, Poly(tailX - 6, bottom - 1, tailX + 6, bottom - 1, tailX + 1, bottom + 9));
        dc.DrawLine(BubblePen, new Point(tailX - 6, bottom), new Point(tailX + 1, bottom + 9));
        dc.DrawLine(BubblePen, new Point(tailX + 6, bottom), new Point(tailX + 1, bottom + 9));
        dc.DrawText(shown, new Point(left + 9, top + 6));
        dc.Pop();   // pop
        dc.Pop();   // opacity
    }
}
