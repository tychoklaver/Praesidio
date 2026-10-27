using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum WinLoseTest
{
    None,
    Victory,
    Defeat
}

public class WinLoseUI : MonoBehaviour
{
    [Header("Victory")]
    [SerializeField] GameObject victoryScreen;
    [SerializeField] CanvasGroup victoryGroup;
    [SerializeField] RectTransform victoryBanner;
    [SerializeField] Button nextLevelButton;
    [SerializeField] Button endlessButton;
    [SerializeField] Button victoryLevelSelectButton;

    [Header("Defeat")]
    [SerializeField] GameObject defeatScreen;
    [SerializeField] CanvasGroup defeatGroup;
    [SerializeField] RectTransform defeatBanner;
    [SerializeField] Button restartButton;
    [SerializeField] Button defeatLevelSelectButton;
    [SerializeField] Button mainMenuButton;

    [Header("Scenes")]
    [SerializeField] string nextLevelScene;
    [SerializeField] string levelSelectScene;
    [SerializeField] string mainMenuScene = "MainMenu";

    [Header("Settings")]
    [SerializeField] WinLoseTest showOnStart;
    [SerializeField] float speed = 12f;

    GameObject screen;
    CanvasGroup group;
    RectTransform banner;
    float fullHeight;
    bool closing;

    void Start()
    {
        nextLevelButton.onClick.AddListener(() => LoadScene(nextLevelScene));
        endlessButton.onClick.AddListener(Close);
        victoryLevelSelectButton.onClick.AddListener(() => LoadScene(levelSelectScene));
        restartButton.onClick.AddListener(() => LoadScene(SceneManager.GetActiveScene().name));
        defeatLevelSelectButton.onClick.AddListener(() => LoadScene(levelSelectScene));
        mainMenuButton.onClick.AddListener(() => LoadScene(mainMenuScene));

        fullHeight = victoryBanner.sizeDelta.y;
        victoryScreen.SetActive(false);
        defeatScreen.SetActive(false);

        if (showOnStart == WinLoseTest.Victory)
        {
            ShowVictory();
        }
        if (showOnStart == WinLoseTest.Defeat)
        {
            ShowDefeat();
        }
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                ShowVictory();
            }
            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                ShowDefeat();
            }
            if (Keyboard.current.cKey.wasPressedThisFrame && screen != null)
            {
                Close();
            }
        }

        if (screen == null)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        float target = fullHeight;
        if (closing)
        {
            target = 0;
        }

        float height = Mathf.Lerp(banner.sizeDelta.y, target, dt * speed);
        banner.sizeDelta = new Vector2(banner.sizeDelta.x, height);

        if (closing)
        {
            group.alpha = height / fullHeight;
            if (height < 2)
            {
                screen.SetActive(false);
                screen = null;
                closing = false;
            }
        }
        else
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 1, dt * 6);
        }
    }

    public void ShowVictory()
    {
        Show(victoryScreen, victoryGroup, victoryBanner);
    }

    public void ShowDefeat()
    {
        Show(defeatScreen, defeatGroup, defeatBanner);
    }

    void Show(GameObject newScreen, CanvasGroup newGroup, RectTransform newBanner)
    {
        if (screen != null)
        {
            screen.SetActive(false);
        }

        screen = newScreen;
        group = newGroup;
        banner = newBanner;
        closing = false;

        Time.timeScale = 0;
        screen.SetActive(true);
        group.alpha = 0;
        banner.sizeDelta = new Vector2(banner.sizeDelta.x, 0);
    }

    void Close()
    {
        Time.timeScale = 1;
        closing = true;
    }

    void LoadScene(string sceneName)
    {
        if (sceneName == "")
        {
            return;
        }

        Time.timeScale = 1;
        SceneManager.LoadScene(sceneName);
    }
}
