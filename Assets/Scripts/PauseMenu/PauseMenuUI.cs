using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject pauseMenu;
    [SerializeField] CanvasGroup pauseGroup;
    [SerializeField] RectTransform banner;
    [SerializeField] GameObject confirmPopup;
    [SerializeField] CanvasGroup confirmGroup;
    [SerializeField] RectTransform confirmPanel;

    [Header("Buttons")]
    [SerializeField] Button resumeButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button exitButton;
    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;

    [Header("Settings")]
    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField] string settingsScene = "SettingsMenu";
    [SerializeField] bool openOnStart;
    [SerializeField] float speed = 12f;

    bool isPaused;
    bool closing;
    float timeScaleBefore = 1;
    float fullHeight;

    void Start()
    {
        resumeButton.onClick.AddListener(Resume);
        settingsButton.onClick.AddListener(OpenSettings);
        restartButton.onClick.AddListener(Restart);
        exitButton.onClick.AddListener(AskExit);
        yesButton.onClick.AddListener(ExitToMainMenu);
        noButton.onClick.AddListener(CloseConfirm);

        fullHeight = banner.sizeDelta.y;
        if (banner.GetComponent<RectMask2D>() == null)
        {
            banner.gameObject.AddComponent<RectMask2D>();
        }

        pauseMenu.SetActive(false);
        confirmPopup.SetActive(false);

        if (openOnStart)
        {
            Pause();
        }
    }

    void Update()
    {
        if (SceneManager.GetSceneByName(settingsScene).isLoaded)
        {
            Animate();
            return;
        }

        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (escapePressed || Keybinds.WasPressed(Keybinds.Pause))
        {
            if (confirmPopup.activeSelf)
            {
                CloseConfirm();
            }
            else if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        Animate();
    }

    void Animate()
    {
        float dt = Time.unscaledDeltaTime;

        float target = 1;
        if (closing)
        {
            target = 0;
        }

        float height = Mathf.Lerp(banner.sizeDelta.y, target * fullHeight, dt * speed);
        banner.sizeDelta = new Vector2(banner.sizeDelta.x, height);

        if (closing)
        {
            pauseGroup.alpha = height / fullHeight;
            if (height < 2)
            {
                pauseMenu.SetActive(false);
                closing = false;
            }
        }
        else
        {
            pauseGroup.alpha = Mathf.MoveTowards(pauseGroup.alpha, 1, dt * 6);
        }

        if (confirmPopup.activeSelf)
        {
            confirmGroup.alpha = Mathf.MoveTowards(confirmGroup.alpha, 1, dt * 8);
            float popupSize = Mathf.Lerp(confirmPanel.localScale.x, 1, dt * speed);
            confirmPanel.localScale = Vector3.one * popupSize;
        }
    }

    public void Pause()
    {
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0;
        isPaused = true;
        closing = false;

        pauseMenu.SetActive(true);
        pauseGroup.alpha = 0;
        banner.sizeDelta = new Vector2(banner.sizeDelta.x, 0);
    }

    public void Resume()
    {
        Time.timeScale = timeScaleBefore;
        isPaused = false;
        closing = true;
        confirmPopup.SetActive(false);
    }

    void OpenSettings()
    {
        if (!SceneManager.GetSceneByName(settingsScene).isLoaded)
        {
            SceneManager.LoadSceneAsync(settingsScene, LoadSceneMode.Additive);
        }
    }

    void Restart()
    {
        SceneTransition.Load(SceneManager.GetActiveScene().name);
    }

    void AskExit()
    {
        confirmPopup.SetActive(true);
        confirmGroup.alpha = 0;
        confirmPanel.localScale = Vector3.one * 0.9f;
    }

    void CloseConfirm()
    {
        confirmPopup.SetActive(false);
    }

    void ExitToMainMenu()
    {
        SceneTransition.Load(mainMenuScene);
    }
}
