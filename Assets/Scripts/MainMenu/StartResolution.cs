using UnityEngine;

public class StartResolution
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SetResolution()
    {
        Screen.SetResolution(1920, 1080, Screen.fullScreenMode);
    }
}
