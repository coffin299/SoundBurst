using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;
using SoundBurst.Audio.Devices;
using SoundBurst.Core.Models;

namespace SoundBurst.App;

public partial class MainWindow : Window
{
    private readonly AudioDeviceEnumeratorService _devices = new();
    private bool _suppressDeviceEvents;
    private bool _hotkeysAttached;
    private HwndSource? _hwndSource;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        App.Current.Hotkeys.Attach(helper.Handle);
        _hwndSource = HwndSource.FromHwnd(helper.Handle);
        _hwndSource?.AddHook(WndProc);
        _hotkeysAttached = true;
        RegisterClipHotkeys();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (App.Current.Hotkeys.TryHandleWindowMessage((uint)msg, wParam))
        {
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadUiFromSettings();
        RefreshDriverStatus();
        RefreshClipList();
        App.Current.AudioEngine.LevelsChanged += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                MicLevelBar.Value = App.Current.AudioEngine.MicPeak;
                MixLevelBar.Value = App.Current.AudioEngine.MixPeak;
            });
        };
    }

    private void LoadUiFromSettings()
    {
        _suppressDeviceEvents = true;
        var settings = App.Current.Settings;

        var captures = _devices.ListCaptureDevices();
        MicCombo.ItemsSource = captures;
        MicCombo.SelectedItem = FindById(captures, settings.MicDeviceId) ?? captures.FirstOrDefault();

        var renders = _devices.ListRenderDevices();
        VirtualOutCombo.ItemsSource = renders;
        VirtualOutCombo.SelectedItem = FindById(renders, settings.VirtualOutputDeviceId)
                                       ?? renders.FirstOrDefault(d => d.Name.Contains("SoundBurst", StringComparison.OrdinalIgnoreCase))
                                       ?? renders.FirstOrDefault();

        MonitorCombo.ItemsSource = renders;
        MonitorCombo.SelectedItem = FindById(renders, settings.MonitorDeviceId) ?? renders.FirstOrDefault();

        MicVolumeSlider.Value = settings.MicVolume;
        MonitorVolumeSlider.Value = settings.MonitorVolume;
        ClipMasterSlider.Value = settings.MasterClipVolume;
        MonitorToggle.IsChecked = settings.MonitorEnabled;
        _suppressDeviceEvents = false;
    }

    private static AudioDeviceInfo? FindById(IEnumerable<AudioDeviceInfo> items, string? id)
        => string.IsNullOrEmpty(id) ? null : items.FirstOrDefault(i => i.Id == id);

    private void RefreshDriverStatus()
    {
        var status = App.Current.DriverStatus.GetStatus();
        if (!status.DriverPresent)
        {
            DriverStatusText.Text =
                $"仮想デバイス未検出。Test Mode を有効化し driver をインストールしてください。" +
                $" 期待: {status.SpeakersName} / {status.MicrophoneName}。" +
                $" Test Signing: {(status.TestSigningEnabled ? "ON" : "OFF")}";
        }
        else
        {
            DriverStatusText.Text =
                $"仮想ドライバ検出: {status.SpeakersName} → {status.MicrophoneName}。" +
                $" Test Signing: {(status.TestSigningEnabled ? "ON" : "OFF")}。" +
                " Discord の入力に Microphone 側を選んでください。";
        }
    }

    private void RefreshClipList()
    {
        ClipList.ItemsSource = null;
        ClipList.ItemsSource = App.Current.Settings.Clips.ToList();
    }

    private void RegisterClipHotkeys()
    {
        if (!_hotkeysAttached)
        {
            return;
        }

        App.Current.Hotkeys.UnregisterAll();
        foreach (var clip in App.Current.Settings.Clips)
        {
            if (string.IsNullOrWhiteSpace(clip.Hotkey))
            {
                continue;
            }

            var captured = clip;
            App.Current.Hotkeys.Register(captured.Hotkey, () =>
            {
                Dispatcher.Invoke(() => App.Current.AudioEngine.PlayClip(captured));
            });
        }
    }

    private void PersistDevices()
    {
        if (_suppressDeviceEvents)
        {
            return;
        }

        var s = App.Current.Settings;
        s.MicDeviceId = (MicCombo.SelectedItem as AudioDeviceInfo)?.Id;
        s.VirtualOutputDeviceId = (VirtualOutCombo.SelectedItem as AudioDeviceInfo)?.Id;
        s.MonitorDeviceId = (MonitorCombo.SelectedItem as AudioDeviceInfo)?.Id;
        App.Current.SaveSettings();
    }

    private void DeviceChanged(object sender, SelectionChangedEventArgs e) => PersistDevices();

    private void MicVolume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        App.Current.Settings.MicVolume = (float)MicVolumeSlider.Value;
        App.Current.AudioEngine.SetMicVolume((float)MicVolumeSlider.Value);
        App.Current.SaveSettings();
    }

    private void MonitorVolume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        App.Current.Settings.MonitorVolume = (float)MonitorVolumeSlider.Value;
        App.Current.AudioEngine.SetMonitorVolume((float)MonitorVolumeSlider.Value);
        App.Current.SaveSettings();
    }

    private void ClipMaster_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        App.Current.Settings.MasterClipVolume = (float)ClipMasterSlider.Value;
        App.Current.AudioEngine.SetMasterClipVolume((float)ClipMasterSlider.Value);
        App.Current.SaveSettings();
    }

    private void MonitorToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        var on = MonitorToggle.IsChecked == true;
        App.Current.Settings.MonitorEnabled = on;
        App.Current.AudioEngine.SetMonitorEnabled(on);
        App.Current.SaveSettings();
    }

    private void StartStop_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.Current.AudioEngine.IsRunning)
            {
                App.Current.AudioEngine.Stop();
                StartStopButton.Content = "ミックス開始";
                EngineStatusText.Text = "停止中";
                return;
            }

            PersistDevices();
            App.Current.AudioEngine.Start(App.Current.Settings);
            StartStopButton.Content = "ミックス停止";
            EngineStatusText.Text = "稼働中";
        }
        catch (Exception ex)
        {
            EngineStatusText.Text = ex.Message;
            MessageBox.Show(ex.Message, "SoundBurst", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AddClip_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Audio|*.wav;*.mp3;*.flac;*.m4a|All|*.*"
        };
        if (dlg.ShowDialog() != true)
        {
            return;
        }

        var hotkey = Prompt("ホットキー（任意）", "例: Ctrl+Shift+1 （空ならなし）");
        var clip = new SoundClip
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName),
            FilePath = dlg.FileName,
            Volume = 1f,
            Hotkey = string.IsNullOrWhiteSpace(hotkey) ? null : hotkey.Trim()
        };

        App.Current.Settings.Clips.Add(clip);
        App.Current.SaveSettings();
        RefreshClipList();
        RegisterClipHotkeys();
    }

    private static string? Prompt(string title, string message)
    {
        var win = new Window
        {
            Title = title,
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.White
        };
        var box = new TextBox { Margin = new Thickness(12, 12, 12, 0) };
        var ok = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "スキップ", Width = 80, IsCancel = true };
        string? result = null;
        ok.Click += (_, _) => { result = box.Text; win.DialogResult = true; };
        cancel.Click += (_, _) => { result = null; win.DialogResult = false; };
        var panel = new DockPanel { Margin = new Thickness(12) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        DockPanel.SetDock(buttons, Dock.Bottom);
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        panel.Children.Add(new StackPanel
        {
            Children =
            {
                new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 8) },
                box
            }
        });
        win.Content = panel;
        win.ShowDialog();
        return result;
    }

    private void PlayClip_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is SoundClip clip)
        {
            App.Current.AudioEngine.PlayClip(clip);
        }
    }

    private void StopClip_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is SoundClip clip)
        {
            App.Current.AudioEngine.StopClip(clip.Id);
        }
    }

    private void DeleteClip_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not SoundClip clip)
        {
            return;
        }

        App.Current.AudioEngine.StopClip(clip.Id);
        App.Current.Settings.Clips.RemoveAll(c => c.Id == clip.Id);
        App.Current.SaveSettings();
        RefreshClipList();
        RegisterClipHotkeys();
    }

    private void RefreshDriver_Click(object sender, RoutedEventArgs e) => RefreshDriverStatus();

    private void ShowSetup_Click(object sender, RoutedEventArgs e)
        => SetupText.Visibility = SetupText.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatusText.Text = "確認中…";
        var (available, version, error) = await App.Current.Updates.CheckAsync();
        if (error is not null)
        {
            UpdateStatusText.Text = error;
            ApplyUpdateButton.IsEnabled = false;
            return;
        }

        if (!available)
        {
            UpdateStatusText.Text = "最新です";
            ApplyUpdateButton.IsEnabled = false;
            return;
        }

        UpdateStatusText.Text = $"更新あり: {version}";
        ApplyUpdateButton.IsEnabled = true;
    }

    private async void ApplyUpdate_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatusText.Text = "ダウンロード中…";
        var (applied, error) = await App.Current.Updates.DownloadAndApplyAsync(p =>
        {
            Dispatcher.Invoke(() => UpdateStatusText.Text = $"ダウンロード {p}%");
        });
        if (!applied)
        {
            UpdateStatusText.Text = error ?? "更新に失敗しました";
        }
    }
}
