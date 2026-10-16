using System.Windows;
using SoundBurst.App.Services;
using SoundBurst.Audio.Engine;
using SoundBurst.Audio.Hotkeys;
using SoundBurst.Core.Models;
using SoundBurst.Core.Services;

namespace SoundBurst.App;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;

    public SettingsStore SettingsStore { get; } = new();
    public AppSettings Settings { get; private set; } = new();
    public AudioEngine AudioEngine { get; } = new();
    public GlobalHotkeyService Hotkeys { get; } = new();
    public DriverStatusService DriverStatus { get; } = new();
    public UpdateService Updates { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Settings = SettingsStore.Load();
        Updates = new UpdateService(Settings.GitHubRepoUrl);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Hotkeys.Dispose();
        AudioEngine.Dispose();
        SaveSettings();
        base.OnExit(e);
    }

    public void SaveSettings() => SettingsStore.Save(Settings);
}
