using System.Collections.Generic;
using UnityEngine;

public static class UIManager
{
    static Dictionary<string, IDisplayUI> display = new Dictionary<string, IDisplayUI>();

    internal static void RegisterUI(this IDisplayUI ui, string uiKey)
    {
        if (display.ContainsKey(uiKey))
            display[uiKey] = ui;
        else display.Add(uiKey, ui);
    }

    internal static IDisplayUI GetUI(string key)
    {
        return display[key];
    }
}
