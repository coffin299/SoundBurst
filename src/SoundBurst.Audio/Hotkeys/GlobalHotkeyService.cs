using System.Runtime.InteropServices;

namespace SoundBurst.Audio.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly Dictionary<int, Action> _handlers = new();
    private readonly Dictionary<string, int> _idByGesture = new(StringComparer.OrdinalIgnoreCase);
    private int _nextId = 1;
    private IntPtr _hwnd;
    private bool _disposed;

    public void Attach(IntPtr windowHandle)
    {
        _hwnd = windowHandle;
    }

    public bool Register(string gesture, Action callback)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return false;
        }

        if (!TryParseGesture(gesture, out var modifiers, out var key))
        {
            return false;
        }

        Unregister(gesture);

        var id = _nextId++;
        if (!RegisterHotKey(_hwnd, id, modifiers, key))
        {
            return false;
        }

        _idByGesture[gesture] = id;
        _handlers[id] = callback;
        return true;
    }

    public void Unregister(string gesture)
    {
        if (!_idByGesture.TryGetValue(gesture, out var id))
        {
            return;
        }

        UnregisterHotKey(_hwnd, id);
        _idByGesture.Remove(gesture);
        _handlers.Remove(id);
    }

    public void UnregisterAll()
    {
        foreach (var id in _handlers.Keys.ToList())
        {
            UnregisterHotKey(_hwnd, id);
        }

        _handlers.Clear();
        _idByGesture.Clear();
    }

    public bool TryHandleWindowMessage(uint msg, IntPtr wParam)
    {
        if (msg != WmHotkey)
        {
            return false;
        }

        var id = wParam.ToInt32();
        if (_handlers.TryGetValue(id, out var action))
        {
            action();
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnregisterAll();
        _disposed = true;
    }

    public static bool TryParseGesture(string gesture, out uint modifiers, out uint key)
    {
        modifiers = 0;
        key = 0;
        if (string.IsNullOrWhiteSpace(gesture))
        {
            return false;
        }

        var parts = gesture.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= 0x0002;
                    break;
                case "ALT":
                    modifiers |= 0x0001;
                    break;
                case "SHIFT":
                    modifiers |= 0x0004;
                    break;
                case "WIN":
                case "WINDOWS":
                    modifiers |= 0x0008;
                    break;
                default:
                    return false;
            }
        }

        var keyName = parts[^1].ToUpperInvariant();
        if (keyName.Length == 1 && keyName[0] is >= 'A' and <= 'Z')
        {
            key = keyName[0];
            return true;
        }

        if (keyName.Length == 1 && keyName[0] is >= '0' and <= '9')
        {
            key = keyName[0];
            return true;
        }

        if (keyName.StartsWith("F", StringComparison.Ordinal) &&
            int.TryParse(keyName[1..], out var fn) &&
            fn is >= 1 and <= 24)
        {
            key = (uint)(0x70 + fn - 1);
            return true;
        }

        return false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
