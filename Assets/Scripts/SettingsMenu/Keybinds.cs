using UnityEngine.InputSystem;

public static class Keybinds
{
    public const int MoveUp = 0;
    public const int MoveDown = 1;
    public const int MoveLeft = 2;
    public const int MoveRight = 3;
    public const int JumpToHeadquarters = 4;
    public const int Pause = 5;
    public const int NormalSpeed = 6;
    public const int FastSpeed = 7;
    public const int ProductionTab = 8;
    public const int TowersTab = 9;
    public const int WallsTab = 10;

    public static Key[] defaultKeys =
    {
        Key.W, Key.UpArrow,
        Key.S, Key.DownArrow,
        Key.A, Key.LeftArrow,
        Key.D, Key.RightArrow,
        Key.H, Key.None,
        Key.Space, Key.None,
        Key.F1, Key.None,
        Key.F2, Key.None,
        Key.Digit1, Key.None,
        Key.Digit2, Key.None,
        Key.Digit3, Key.None
    };

    public static Key[] keys = (Key[])defaultKeys.Clone();

    public static bool IsHeld(int action)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        for (int slot = 0; slot < 2; slot++)
        {
            Key key = keys[action * 2 + slot];
            if (key != Key.None && keyboard[key].isPressed)
            {
                return true;
            }
        }
        return false;
    }

    public static bool WasPressed(int action)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        for (int slot = 0; slot < 2; slot++)
        {
            Key key = keys[action * 2 + slot];
            if (key != Key.None && keyboard[key].wasPressedThisFrame)
            {
                return true;
            }
        }
        return false;
    }

    public static string GetName(Key key)
    {
        if (key == Key.None)
        {
            return "+ Add";
        }

        if (Keyboard.current == null)
        {
            return key.ToString();
        }

        return Keyboard.current[key].displayName;
    }
}
