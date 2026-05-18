using System.Collections.Generic;
using UnityEngine;

/// Statyczna klasa przechowująca rebindowalne skróty klawiszowe w PlayerPrefs.
/// Kamera i inne skrypty wywołują InputBindings.Get("akcja") zamiast hardkodować KeyCode.
public static class InputBindings
{
    static readonly Dictionary<string, KeyCode> _defaults = new Dictionary<string, KeyCode>
    {
        ["CamForward"]     = KeyCode.W,
        ["CamBackward"]    = KeyCode.S,
        ["CamLeft"]        = KeyCode.A,
        ["CamRight"]       = KeyCode.D,
        ["CamFreeFly"]     = KeyCode.Q,
        ["CamOrbit"]       = KeyCode.E,
        ["CamReset"]       = KeyCode.R,
        ["CamTiltReset"]   = KeyCode.F,
        ["CamZoom"]        = KeyCode.C,
        ["CamTurbo"]       = KeyCode.LeftShift,
        ["CamSlow"]        = KeyCode.LeftAlt,
        ["CamSwitch"]      = KeyCode.Tab,
        ["CamRotate"]      = KeyCode.Mouse1,
        ["AbilityConfirm"] = KeyCode.Mouse0,
    };

    static readonly Dictionary<string, string> _displayNames = new Dictionary<string, string>
    {
        ["CamForward"]     = "Kamera: Przód",
        ["CamBackward"]    = "Kamera: Tył",
        ["CamLeft"]        = "Kamera: Lewo",
        ["CamRight"]       = "Kamera: Prawo",
        ["CamFreeFly"]     = "Kamera: Tryb lotu (FreeFly)",
        ["CamOrbit"]       = "Kamera: Tryb orbity",
        ["CamReset"]       = "Kamera: Pełny reset",
        ["CamTiltReset"]   = "Kamera: Reset nachylenia",
        ["CamZoom"]        = "Kamera: Zoom",
        ["CamTurbo"]       = "Kamera: Turbo (szybko)",
        ["CamSlow"]        = "Kamera: Precyzja (wolno)",
        ["CamSwitch"]      = "Kamera: Zmiana planszy (MP)",
        ["CamRotate"]      = "Obrót kamery (przytrzymaj)",
        ["AbilityConfirm"] = "Potwierdzenie zdolności",
    };

    static Dictionary<string, KeyCode> _current;

    public static void Load()
    {
        _current = new Dictionary<string, KeyCode>(_defaults);
        foreach (var kv in _defaults)
        {
            int saved = PlayerPrefs.GetInt("Bind_" + kv.Key, -1);
            if (saved >= 0) _current[kv.Key] = (KeyCode)saved;
        }
    }

    static void EnsureLoaded() { if (_current == null) Load(); }

    public static KeyCode Get(string action)
    {
        EnsureLoaded();
        return _current.TryGetValue(action, out var kc) ? kc : KeyCode.None;
    }

    public static void Set(string action, KeyCode key)
    {
        EnsureLoaded();
        if (!_defaults.ContainsKey(action)) return;
        _current[action] = key;
        PlayerPrefs.SetInt("Bind_" + action, (int)key);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        _current = new Dictionary<string, KeyCode>(_defaults);
        foreach (var kv in _defaults)
            PlayerPrefs.SetInt("Bind_" + kv.Key, (int)kv.Value);
        PlayerPrefs.Save();
    }

    public static string GetDisplayName(string action) =>
        _displayNames.TryGetValue(action, out var n) ? n : action;

    public static KeyCode GetDefault(string action) =>
        _defaults.TryGetValue(action, out var kc) ? kc : KeyCode.None;

    public static IEnumerable<string> AllActions => _defaults.Keys;

    /// Obsługuje zarówno przyciski myszy (Mouse0–Mouse6) jak i klawisze klawiatury.
    public static bool GetMouseHeld(string action)
    {
        var kc = Get(action);
        if (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse6)
            return Input.GetMouseButton((int)kc - (int)KeyCode.Mouse0);
        return Input.GetKey(kc);
    }

    public static string KeyName(KeyCode kc)
    {
        switch (kc)
        {
            case KeyCode.LeftShift:    return "L.Shift";
            case KeyCode.RightShift:   return "P.Shift";
            case KeyCode.LeftAlt:      return "L.Alt";
            case KeyCode.RightAlt:     return "P.Alt";
            case KeyCode.LeftControl:  return "L.Ctrl";
            case KeyCode.RightControl: return "P.Ctrl";
            case KeyCode.Mouse0:       return "LPM";
            case KeyCode.Mouse1:       return "PPM";
            case KeyCode.Mouse2:       return "ŚPM";
            case KeyCode.UpArrow:      return "↑";
            case KeyCode.DownArrow:    return "↓";
            case KeyCode.LeftArrow:    return "←";
            case KeyCode.RightArrow:   return "→";
            case KeyCode.Return:       return "Enter";
            case KeyCode.Backspace:    return "Backspace";
            case KeyCode.Delete:       return "Delete";
            case KeyCode.Space:        return "Spacja";
            case KeyCode.Tab:          return "Tab";
            case KeyCode.Escape:       return "Esc";
            default:                   return kc.ToString();
        }
    }
}
