using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The only place that touches the private members of <see cref="SettingsMenuUI"/>, through reflection
/// (the C# way to read and write private members), so that script itself never has to change.
/// The member names are hard-coded below. If SettingsMenuUI renames one, <see cref="IsCompatible"/>
/// logs which one is missing and the menu part of saving is switched off instead of failing silently.
/// </summary>
public static class SettingsMenuBridge
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly string[] RequiredFields =
        { "toggleStates", "optionGroups", "sliders", "resolutionText", "resolutionOptions", "uiScaleSlider", "referenceResolution" };

    private static readonly string[] RequiredMethods = { "SetToggle", "SelectOption", "SelectResolution" };

    /// <summary>Checks once that every member used below still exists in SettingsMenuUI.</summary>
    public static bool IsCompatible()
    {
        bool ok = true;
        Type type = typeof(SettingsMenuUI);

        foreach (string name in RequiredFields)
            if (type.GetField(name, PrivateInstance) == null) ok = Missing("field", name);

        foreach (string name in RequiredMethods)
            if (type.GetMethod(name, PrivateInstance) == null) ok = Missing("method", name);

        return ok;
    }

    /// <summary>The menu fills in its arrays in Start; before that (e.g. an inactive panel) it can't be read yet.</summary>
    public static bool IsReady(SettingsMenuUI pMenu) => Get<bool[]>(pMenu, "toggleStates") != null;

    /// <summary>Copies what the menu currently shows into pData (only the menu's own part).</summary>
    public static void Capture(SettingsMenuUI pMenu, SettingsData pData)
    {
        pData.toggles = (bool[])Get<bool[]>(pMenu, "toggleStates").Clone();
        pData.options = Array.ConvertAll(Get<SettingsMenuUI.OptionGroup[]>(pMenu, "optionGroups"), group => group.selected);
        pData.sliders = Array.ConvertAll(Get<Slider[]>(pMenu, "sliders"), slider => slider.value);
        pData.resolution = Get<TMP_Text>(pMenu, "resolutionText").text;
        pData.uiScale = Get<Slider>(pMenu, "uiScaleSlider").value / 100f;
        pData.uiReferenceResolution = Get<Vector2>(pMenu, "referenceResolution");
    }

    /// <summary>
    /// Puts saved values into the menu using the menu's own methods, so the visuals (sprites, knobs, texts)
    /// update exactly as if the player had clicked. Call after the menu's Start, so its defaults for the
    /// Reset button were already taken from the editor values.
    /// </summary>
    public static void Restore(SettingsMenuUI pMenu, SettingsData pData)
    {
        // Math.Min everywhere: if something was added or removed since the save, only the overlap is restored.
        int toggleCount = Get<bool[]>(pMenu, "toggleStates").Length;
        for (int i = 0; i < Math.Min(pData.toggles.Length, toggleCount); i++)
            Call(pMenu, "SetToggle", i, pData.toggles[i]);

        var groups = Get<SettingsMenuUI.OptionGroup[]>(pMenu, "optionGroups");
        for (int g = 0; g < Math.Min(pData.options.Length, groups.Length); g++)
            if (pData.options[g] >= 0 && pData.options[g] < groups[g].options.Length)
                Call(pMenu, "SelectOption", g, pData.options[g]);

        var sliders = Get<Slider[]>(pMenu, "sliders");
        for (int i = 0; i < Math.Min(pData.sliders.Length, sliders.Length); i++)
            sliders[i].value = pData.sliders[i]; // The menu's own listeners update the % text and camera speeds.

        // Found by text, not index, so reordering the resolution list in the editor can't pick a wrong one.
        var resolutions = Get<Button[]>(pMenu, "resolutionOptions");
        int resolution = Array.FindIndex(resolutions, option => option.GetComponentInChildren<TMP_Text>().text == pData.resolution);
        if (resolution >= 0) Call(pMenu, "SelectResolution", resolution);
    }

    private static T Get<T>(SettingsMenuUI pMenu, string pField) =>
        (T)typeof(SettingsMenuUI).GetField(pField, PrivateInstance).GetValue(pMenu);

    private static void Call(SettingsMenuUI pMenu, string pMethod, params object[] pArgs) =>
        typeof(SettingsMenuUI).GetMethod(pMethod, PrivateInstance).Invoke(pMenu, pArgs);

    private static bool Missing(string pKind, string pName)
    {
        Debug.LogError($"[SettingsMenuBridge] SettingsMenuUI no longer has the {pKind} '{pName}'. Settings in the menu will not be saved. Update the names in SettingsMenuBridge.");
        return false;
    }
}