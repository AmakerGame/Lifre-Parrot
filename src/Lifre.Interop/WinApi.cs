using System.Runtime.InteropServices;

namespace Lifre.Interop;

/// <summary>
/// WinAPI-обгортка для папуги: стилі вікна, курсор, стан миші, показ вікна.
/// Працює на Windows 10 і 11 без додаткових бібліотек.
/// </summary>
public static class WinApi
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int SW_SHOWNOACTIVATE = 4;
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int SM_SWAPBUTTON = 23;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    /// <summary>Налаштовує стилі вікна: TOOLWINDOW + NOACTIVATE (+TOPMOST).</summary>
    public static void ApplyOverlayStyles(IntPtr hWnd, bool topmost)
    {
        var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        if (topmost)
            exStyle |= WS_EX_TOPMOST;
        SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);
    }

    /// <summary>Позиція курсора у фізичних пікселях.</summary>
    public static (int X, int Y) GetCursorPosition()
    {
        if (GetCursorPos(out var pt))
            return (pt.X, pt.Y);
        return (0, 0);
    }

    /// <summary>
    /// Чи затиснута зараз основна (логічно ліва) кнопка миші — незалежно від того,
    /// над яким вікном курсор. Потрібно, щоб не «залипати», якщо кнопку відпустили поза вікном.
    /// </summary>
    public static bool IsPrimaryMouseButtonDown()
    {
        try
        {
            var vk = GetSystemMetrics(SM_SWAPBUTTON) != 0 ? VK_RBUTTON : VK_LBUTTON;
            return (GetAsyncKeyState(vk) & 0x8000) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Робить вікно активним. Вікно папуги має стиль NOACTIVATE, а контекстне меню без активного
    /// власника закривається одразу — тому перед показом меню активуємо вікно явно.
    /// </summary>
    public static void Activate(IntPtr hWnd)
    {
        try { SetForegroundWindow(hWnd); }
        catch { /* ignore */ }
    }

    /// <summary>Показує вікно без активації (щоб не відбирати фокус у поточної програми).</summary>
    public static void WakeWindow(IntPtr hWnd)
    {
        try { ShowWindow(hWnd, SW_SHOWNOACTIVATE); }
        catch { /* ignore */ }
    }
}
