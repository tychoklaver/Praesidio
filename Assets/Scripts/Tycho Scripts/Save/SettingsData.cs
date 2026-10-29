using System;
using UnityEngine;

/// <summary>
/// Saved state of the settings menu, stored inside <see cref="PlayerData"/> (same encrypted save file).
/// The settings live in two places, so there are two parts:
///   1. Values the GAME uses (keybinds, camera speed, vsync, frame rate, UI scale). They must be restored at
///      game start, before any menu exists.
///   2. Values only the MENU knows (toggles, options, sliders, resolution). They are restored into the menu
///      when it opens. Every array follows the order of the matching array on SettingsMenuUI.
/// </summary>
[Serializable]
public class SettingsData
{
    /// <summary>False until settings were saved once; until then everything keeps its default.</summary>
    public bool hasSaved;

    // Part 1: used by the game.
    public string[] keys = Array.Empty<string>(); // Keybinds.keys as Key names (names survive Unity updates better than numbers).
    public float cameraMoveSpeed = 1f;
    public float cameraZoomSpeed = 1f;
    public bool vSync;
    public int targetFrameRate = -1;
    public float uiScale = 1f;                    // 1 = 100%.
    public Vector2 uiReferenceResolution;         // The menu's reference resolution; UI scale divides it.

    // Part 2: used by the menu.
    public bool[] toggles = Array.Empty<bool>();
    public int[] options = Array.Empty<int>();    // Selected option index per option group.
    public float[] sliders = Array.Empty<float>();
    public string resolution = "";                // Text of the chosen resolution, e.g. "1920 x 1080".
}