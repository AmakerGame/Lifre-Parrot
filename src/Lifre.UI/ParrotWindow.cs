using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Lifre.Behavior;
using Lifre.Core;
using Lifre.Interop;
using Lifre.Persistence;
using Lifre.Rendering;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Lifre.UI;

/// <summary>
/// Прозоре borderless-вікно папуги: Topmost, без рамки, без таскбару.
/// Папуга говорить бульбашкою над головою. Клік — погладити (або прийняти подарунок),
/// затиснути й тягнути — підхопити папугу, відпустити — він впаде й приземлиться.
/// Праве відпускання кнопки — меню.
/// </summary>
public sealed class ParrotWindow : Window
{
    private const double DragThreshold = 6.0;

    /// <summary>Куди «паркуємо» вікно, коли папуга за екраном (поза будь-яким монітором).</summary>
    private const double ParkedCoordinate = -20000;

    private readonly PetBrain _brain;
    private readonly TextStore _texts;
    private readonly LifreConfig _config;
    private readonly ParrotRenderer _renderer;
    private readonly DispatcherTimer _fallbackTimer;

    private DateTime _lastFrame = DateTime.UtcNow;
    private double _dtSmooth = 0.016;
    private bool _pressed;
    private bool _dragging;
    private bool _rightPressed;
    private bool _menuOpen;
    private bool _parked;
    private bool _started;
    private bool _inFrame;
    private Point _pressCursor;

    public ParrotWindow(PetBrain brain, TextStore texts, LifreConfig config)
    {
        _brain = brain;
        _texts = texts;
        _config = config;

        Width = LifreConstants.WindowWidth;
        Height = LifreConstants.WindowHeight;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = config.AlwaysOnTop;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Opacity = 0; // показуємо після першого кадру, щоб не блимало в центрі екрана

        // Рендерер малює саме ту позу, яку оновлює «мозок».
        _renderer = new ParrotRenderer
        {
            Pose = _brain.Pose
        };
        Content = _renderer;

        // Гладкі краї й без прив'язки до пікселів — рух і лінії виглядають м'яко.
        SnapsToDevicePixels = false;
        UseLayoutRounding = false;
        RenderOptions.SetEdgeMode(_renderer, EdgeMode.Unspecified);
        RenderOptions.SetBitmapScalingMode(_renderer, BitmapScalingMode.HighQuality);

        SourceInitialized += OnSourceInitialized;
        MouseLeftButtonDown += OnLeftDown;
        MouseLeftButtonUp += OnLeftUp;
        MouseRightButtonDown += OnRightDown;
        MouseRightButtonUp += OnRightUp;
        LostMouseCapture += OnLostCapture;

        // Основний ритм — кадри композиції; страхувальний таймер працює завжди,
        // навіть якщо WPF призупинить Rendering (наприклад, коли вікно ніде не видно).
        CompositionTarget.Rendering += OnRendering;
        _fallbackTimer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _fallbackTimer.Tick += (_, _) =>
        {
            // Страхувальний таймер діє лише коли кадри композиції зупинились — інакше два джерела дають нерівний ритм.
            if ((DateTime.UtcNow - _lastFrame).TotalSeconds > 0.05)
                OnFrame();
        };
        _fallbackTimer.Start();

        Closed += (_, _) =>
        {
            CompositionTarget.Rendering -= OnRendering;
            _fallbackTimer.Stop();
        };
    }

    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public void Wake()
    {
        if (Handle != IntPtr.Zero)
            WinApi.WakeWindow(Handle);
        Topmost = _config.AlwaysOnTop;
        _brain.Greet();
    }

    public void CommandFeed() => _brain.Feed();
    public void CommandPlay() => _brain.Play();
    public void CommandGift() => _brain.GiveGift();
    public void CommandFly() => _brain.FlyNow();

    public void CommandSleepToggle()
    {
        if (_brain.State == PetState.Sleep)
            _brain.Wake();
        else
            _brain.Sleep();
    }

    public bool LiveEyes => _brain.LiveEyes;
    public void CommandLiveEyesToggle() => _brain.LiveEyes = !_brain.LiveEyes;

    public void CommandPauseToggle() => _brain.IsPaused = !_brain.IsPaused;
    public bool IsPaused => _brain.IsPaused;

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = Handle;
        if (hwnd != IntPtr.Zero)
            WinApi.ApplyOverlayStyles(hwnd, _config.AlwaysOnTop);
        EnsureInitialPosition();
    }

    private void EnsureInitialPosition()
    {
        if (_brain.Stats.PosX != 0 || _brain.Stats.PosY != 0)
            return;
        var workArea = GetWorkAreaDip();
        _brain.Pose.X = workArea.Left + (workArea.Width - Width) / 2;
        _brain.Pose.Y = workArea.Bottom - Height + LifreConstants.FeetInset;
        Left = _brain.Pose.X;
        Top = _brain.Pose.Y;
    }

    private static Rect GetWorkAreaDip()
    {
        var wa = SystemParameters.WorkArea;
        return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
    }

    /// <summary>Позиція курсора в DIP (з урахуванням масштабу екрана).</summary>
    private Point GetCursorDip()
    {
        var (x, y) = WinApi.GetCursorPosition();
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is { } target)
            return target.TransformFromDevice.Transform(new Point(x, y));
        return new Point(x, y);
    }

    // ------------------------------------------------------------------
    // Кадр
    // ------------------------------------------------------------------

    private void OnRendering(object? sender, EventArgs e) => OnFrame();

    private void OnFrame()
    {
        if (_inFrame)
            return;

        var now = DateTime.UtcNow;
        var rawDt = (now - _lastFrame).TotalSeconds;
        if (rawDt < 0.008)      // обидва джерела тікають ~16 мс — другий виклик пропускаємо
            return;
        _lastFrame = now;
        // Згладжений крок часу: нерівні інтервали між кадрами не перетворюються на ривки руху.
        _dtSmooth = rawDt > 0.05 ? rawDt : _dtSmooth * 0.8 + rawDt * 0.2;
        var dt = _dtSmooth;

        _inFrame = true;
        try
        {
            var cursor = GetCursorDip();

            // Кнопку відпустили, а подія до нас не дійшла (курсор був поза вікном) — не «залипаємо».
            if (_pressed && !WinApi.IsPrimaryMouseButtonDown())
                FinishPress();

            // Почали тягнути — папугу підхоплено.
            if (_pressed && !_dragging && (cursor - _pressCursor).Length > DragThreshold)
            {
                _dragging = true;
                _brain.BeginHold(cursor);
            }

            _brain.Update(dt, GetWorkAreaDip(), Width, Height, cursor);
            ApplyPosition();

            if (!_started)
            {
                _started = true;
                Opacity = 1;
            }

            _renderer.InvalidateVisual();
        }
        catch
        {
            // Жодна помилка в кадрі не має зупиняти папугу: скидаємо його в спокійний стан.
            try { _brain.Recover(); } catch { /* ignore */ }
        }
        finally
        {
            _inFrame = false;
        }
    }

    /// <summary>
    /// Ставить вікно в позицію папуги. Коли папуга за екраном, вікно «паркується» далеко за межами
    /// всіх моніторів — без Visibility.Hidden, щоб не втрачати кадри, стилі та кліки.
    /// </summary>
    private void ApplyPosition()
    {
        var pose = _brain.Pose;
        if (pose.Hidden)
        {
            if (!_parked)
            {
                _parked = true;
                Left = ParkedCoordinate;
                Top = ParkedCoordinate;
            }
            return;
        }

        _parked = false;
        // Вікно — на цілих фізичних пікселях (інакше WPF округлює позицію кожного кадру й картинка «дрижить»);
        // решта зсуву малюється всередині вікна з точністю до частки пікселя.
        var dpi = VisualTreeHelper.GetDpi(this);
        var scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
        var scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
        var wx = Math.Floor(pose.X * scaleX) / scaleX;
        var wy = Math.Floor(pose.Y * scaleY) / scaleY;
        Left = wx;
        Top = wy;
        _renderer.SubPixelX = pose.X - wx;
        _renderer.SubPixelY = pose.Y - wy;
    }

    // ------------------------------------------------------------------
    // Миша
    // ------------------------------------------------------------------

    private void OnLeftDown(object sender, MouseButtonEventArgs e)
    {
        _pressed = true;
        _dragging = false;
        _pressCursor = GetCursorDip();
        CaptureMouse();
        e.Handled = true;
    }

    private void OnLeftUp(object sender, MouseButtonEventArgs e)
    {
        FinishPress();
        e.Handled = true;
    }

    /// <summary>Завершує натискання: або погладжування, або відпускання підхопленого папуги.</summary>
    private void FinishPress()
    {
        if (!_pressed)
            return;

        var wasDragging = _dragging;
        _pressed = false;
        _dragging = false;
        if (IsMouseCaptured)
            ReleaseMouseCapture();

        if (wasDragging)
            _brain.EndHold();
        else
            _brain.Pet();
    }

    private void OnLostCapture(object sender, MouseEventArgs e)
    {
        if (!_pressed)
            return;
        if (_dragging)
            _brain.EndHold();
        _pressed = false;
        _dragging = false;
    }

    private void OnRightDown(object sender, MouseButtonEventArgs e)
    {
        // Меню відкриваємо на ВІДПУСКАННІ: інакше відпускання кнопки потрапляє на пункт меню під курсором.
        _rightPressed = true;
        e.Handled = true;
    }

    private void OnRightUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (!_rightPressed)
            return;
        _rightPressed = false;
        ShowContextMenu();
    }

    private void ShowContextMenu()
    {
        if (_menuOpen)
            return;

        // Вікно має NOACTIVATE — без явної активації меню одразу закривається.
        WinApi.Activate(Handle);

        var menu = BuildContextMenu();
        menu.PlacementTarget = this;
        menu.Placement = PlacementMode.MousePoint;
        menu.HorizontalOffset = 2;
        menu.VerticalOffset = 2;
        menu.StaysOpen = false;
        menu.Closed += (_, _) => _menuOpen = false;
        _menuOpen = true;
        menu.IsOpen = true;
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        void Add(string key, Action action)
        {
            var item = new MenuItem { Header = _texts.MenuLabel(key) };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
        }

        Add("feed", () => _brain.Feed());
        Add("play", () => _brain.Play());
        Add("gift", () => _brain.GiveGift());
        Add("fly", () => _brain.FlyNow());
        var eyes = new MenuItem
        {
            Header = _texts.MenuLabel("liveeyes"),
            IsCheckable = true,
            IsChecked = _brain.LiveEyes
        };
        eyes.Click += (_, _) => CommandLiveEyesToggle();
        menu.Items.Add(eyes);
        Add("sleep", () => CommandSleepToggle());
        menu.Items.Add(new Separator());
        Add(_brain.IsPaused ? "resume" : "pause", () => CommandPauseToggle());
        menu.Items.Add(new Separator());
        Add("exit", () => Application.Current?.Shutdown());
        return menu;
    }

}
