using System.Collections.Generic;

public static class UIManager
{
    static Dictionary<string, IDisplayUI> display = new Dictionary<string, IDisplayUI>();

    internal static void RegisterUI(this IDisplayUI ui, string uiKey)
    {
        display.Add(uiKey, ui);
    }

    internal static IDisplayUI GetUI(string key)
    {
        return display[key];
    }
}
