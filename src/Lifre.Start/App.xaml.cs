using System.Windows;
using Lifre.UI;

namespace Lifre.Start;

/// <summary>
/// Точка входу WPF-застосунку. Уся логіка — у <see cref="LifreAppHost"/>.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await LifreAppHost.InitializeAsync(this);
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        LifreAppHost.Shutdown();
        base.OnExit(e);
    }
}
