using System.Drawing;
using System.Windows.Forms;
using Lifre.Core;
using Lifre.Persistence;

namespace Lifre.UI;

/// <summary>
/// Іконка в системному треї (NotifyIcon). Іконка малюється програмно —
/// жодних зовнішніх ресурсів. Меню дублює контекстне меню вікна папуги.
/// </summary>
public sealed class LifreTray : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ParrotWindow _window;
    private readonly TextStore _texts;
    private readonly Icon _icon;
    private readonly Bitmap _iconBitmap;

    /// <summary>Створює трей-іконку та меню.</summary>
    public LifreTray(TextStore texts, ParrotWindow window)
    {
        _texts = texts;
        _window = window;

        _iconBitmap = CreateIconBitmap();
        _icon = Icon.FromHandle(_iconBitmap.GetHicon());

        _menu = new ContextMenuStrip();
        RebuildMenu();
        _menu.Opening += (_, _) => RebuildMenu();   // підпис «Пауза/Продовжити» завжди актуальний

        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = $"{LifreConstants.AppName} — папуга на робочому столі (як Desktop Goose)",
            ContextMenuStrip = _menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => _window.Wake();
    }

    /// <summary>Перебудовує пункти меню з урахуванням поточного стану.</summary>
    public void RebuildMenu()
    {
        _menu.Items.Clear();

        void Add(string key, Action action)
        {
            var item = new ToolStripMenuItem(_texts.MenuLabel(key));
            item.Click += (_, _) => action();
            _menu.Items.Add(item);
        }

        Add("feed", () => _window.CommandFeed());
        Add("play", () => _window.CommandPlay());
        Add("gift", () => _window.CommandGift());
        Add("fly", () => _window.CommandFly());
        var eyes = new ToolStripMenuItem(_texts.MenuLabel("liveeyes")) { Checked = _window.LiveEyes, CheckOnClick = false };
        eyes.Click += (_, _) =>
        {
            _window.CommandLiveEyesToggle();
            RebuildMenu();
        };
        _menu.Items.Add(eyes);
        Add("sleep", () => _window.CommandSleepToggle());
        _menu.Items.Add(new ToolStripSeparator());

        var pauseKey = _window.IsPaused ? "resume" : "pause";
        Add(pauseKey, () =>
        {
            _window.CommandPauseToggle();
            RebuildMenu();
        });

        _menu.Items.Add(new ToolStripSeparator());
        Add("exit", () => System.Windows.Application.Current?.Shutdown());
    }

    /// <summary>Малює просту іконку папуги програмно (32×32).</summary>
    private static Bitmap CreateIconBitmap()
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Тіло — жовто-зелене коло.
        using (var body = new SolidBrush(Color.FromArgb(0x9E, 0xC4, 0x4A)))
            g.FillEllipse(body, 4, 6, 24, 22);
        // Дзьоб — помаранчевий трикутник.
        using (var beak = new SolidBrush(Color.FromArgb(0xE8, 0x6A, 0x2E)))
        {
            g.FillPolygon(beak, new[]
            {
                new PointF(24, 14), new PointF(30, 17), new PointF(24, 20)
            });
        }
        // Око.
        using (var eye = new SolidBrush(Color.White))
            g.FillEllipse(eye, 20, 10, 6, 6);
        using (var pupil = new SolidBrush(Color.FromArgb(0x22, 0x22, 0x22)))
            g.FillEllipse(pupil, 22, 12, 3, 3);
        // Хвіст.
        using (var tail = new SolidBrush(Color.FromArgb(0x3E, 0x7C, 0x59)))
            g.FillPolygon(tail, new[]
            {
                new PointF(6, 22), new PointF(0, 30), new PointF(9, 26)
            });

        return bmp;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _icon.Dispose();
        _iconBitmap.Dispose();
    }
}
