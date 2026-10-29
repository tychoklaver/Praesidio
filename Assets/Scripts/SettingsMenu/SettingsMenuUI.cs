using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsMenuUI : MonoBehaviour
{
    [System.Serializable]
    public class OptionGroup
    {
        public Button[] options;
        public int selected;
    }

    [Header("Menu")]
    [SerializeField] GameObject settingsMenu;
    [SerializeField] CanvasGroup menuGroup;

    [Header("Tabs")]
    [SerializeField] Button[] tabButtons;
    [SerializeField] GameObject[] tabPages;
    [SerializeField] TMP_Text[] tabTexts;
    [SerializeField] Sprite tabSprite;
    [SerializeField] Sprite tabActiveSprite;
    [SerializeField] Color tabTextColor;
    [SerializeField] Color tabActiveTextColor;

    [Header("Keybinds")]
    [SerializeField] Button[] keyButtons;
    [SerializeField] TMP_Text[] keyTexts;
    [SerializeField] Sprite keySprite;
    [SerializeField] Sprite emptyKeySprite;
    [SerializeField] Sprite listeningSprite;

    [Header("Toggles")]
    [SerializeField] Button[] toggles;
    [SerializeField] Color onColor;
    [SerializeField] Color offColor;

    [Header("Option groups")]
    [SerializeField] OptionGroup[] optionGroups;
    [SerializeField] Sprite optionSprite;
    [SerializeField] Sprite selectedOptionSprite;

    [Header("Sliders")]
    [SerializeField] Slider[] sliders;
    [SerializeField] TMP_Text[] sliderTexts;

    [Header("Camera")]
    [SerializeField] int cameraSpeedSlider = 5;
    [SerializeField] int zoomSpeedSlider = 6;

    [Header("Resolution")]
    [SerializeField] Button resolutionButton;
    [SerializeField] TMP_Text resolutionText;
    [SerializeField] GameObject resolutionList;
    [SerializeField] Button[] resolutionOptions;

    [Header("Display")]
    [SerializeField] Button vsyncToggle;
    [SerializeField] Slider uiScaleSlider;
    [SerializeField] int windowModeGroup = 0;
    [SerializeField] int frameRateGroup = 1;
    [SerializeField] int[] frameRates = { 30, 60, 144, -1 };
    [SerializeField] Vector2 referenceResolution = new Vector2(1920, 1080);

    [Header("Buttons")]
    [SerializeField] Button closeButton;
    [SerializeField] Button backButton;
    [SerializeField] Button applyButton;
    [SerializeField] Button resetButton;

    int listening = -1;
    string oldKeyText;
    bool closing;
    int resolutionIndex;
    int defaultResolution;
    float appliedTimer;
    TMP_Text applyText;

    Key[] chosenKeys;
    bool[] toggleStates;
    bool[] defaultToggles;
    int[] defaultOptions;
    float[] defaultSliders;
    int vsyncIndex;
    bool openedOnTop;

    void Start()
    {
        if (gameObject.scene != SceneManager.GetActiveScene())
        {
            openedOnTop = true;
        }
        else
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                if (root.GetComponent<Camera>() != null || root.GetComponent<EventSystem>() != null)
                {
                    root.SetActive(true);
                }
            }
        }

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].onClick.AddListener(() => SelectTab(index));
        }

        chosenKeys = (Key[])Keybinds.keys.Clone();
        for (int i = 0; i < keyButtons.Length; i++)
        {
            int index = i;
            keyButtons[i].onClick.AddListener(() => StartListening(index));
            keyTexts[i].text = Keybinds.GetName(chosenKeys[i]);
            UpdateKeyButton(i);
        }

        toggleStates = new bool[toggles.Length];
        defaultToggles = new bool[toggles.Length];
        for (int i = 0; i < toggles.Length; i++)
        {
            int index = i;
            toggles[i].onClick.AddListener(() => SetToggle(index, !toggleStates[index]));
            toggleStates[i] = toggles[i].image.color == onColor;
            defaultToggles[i] = toggleStates[i];
        }

        defaultOptions = new int[optionGroups.Length];
        for (int g = 0; g < optionGroups.Length; g++)
        {
            defaultOptions[g] = optionGroups[g].selected;
            for (int i = 0; i < optionGroups[g].options.Length; i++)
            {
                int group = g;
                int index = i;
                optionGroups[g].options[i].onClick.AddListener(() => SelectOption(group, index));
            }
        }

        defaultSliders = new float[sliders.Length];
        for (int i = 0; i < sliders.Length; i++)
        {
            int index = i;
            sliders[i].onValueChanged.AddListener(value => sliderTexts[index].text = value + "%");
            defaultSliders[i] = sliders[i].value;
        }

        sliders[cameraSpeedSlider].value = CameraSettings.moveSpeed * defaultSliders[cameraSpeedSlider];
        sliders[zoomSpeedSlider].value = CameraSettings.zoomSpeed * defaultSliders[zoomSpeedSlider];
        sliders[cameraSpeedSlider].onValueChanged.AddListener(UpdateCameraSettings);
        sliders[zoomSpeedSlider].onValueChanged.AddListener(UpdateCameraSettings);

        resolutionButton.onClick.AddListener(ToggleResolutionList);
        for (int i = 0; i < resolutionOptions.Length; i++)
        {
            int index = i;
            resolutionOptions[i].onClick.AddListener(() => SelectResolution(index));
            if (resolutionOptions[i].GetComponentInChildren<TMP_Text>().text == resolutionText.text)
            {
                resolutionIndex = i;
            }
        }
        defaultResolution = resolutionIndex;
        resolutionList.SetActive(false);

        applyText = applyButton.GetComponentInChildren<TMP_Text>();
        closeButton.onClick.AddListener(Close);
        backButton.onClick.AddListener(Close);
        applyButton.onClick.AddListener(Apply);
        resetButton.onClick.AddListener(ResetToDefault);

        vsyncIndex = System.Array.IndexOf(toggles, vsyncToggle);

        SelectTab(0);
        menuGroup.alpha = 0;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (closing)
        {
            menuGroup.alpha = Mathf.MoveTowards(menuGroup.alpha, 0, dt * 6);
            if (menuGroup.alpha == 0)
            {
                closing = false;
                if (openedOnTop)
                {
                    SceneManager.UnloadSceneAsync(gameObject.scene);
                }
                else
                {
                    settingsMenu.SetActive(false);
                }
            }
        }
        else
        {
            menuGroup.alpha = Mathf.MoveTowards(menuGroup.alpha, 1, dt * 6);
        }

        if (listening >= 0 && Keyboard.current != null)
        {
            ListenForKey();
        }

        if (appliedTimer > 0)
        {
            appliedTimer -= dt;
            if (appliedTimer <= 0)
            {
                applyText.text = "Apply";
            }
        }
    }

    void SelectTab(int index)
    {
        for (int i = 0; i < tabPages.Length; i++)
        {
            tabPages[i].SetActive(i == index);
            if (i == index)
            {
                tabButtons[i].image.sprite = tabActiveSprite;
                tabTexts[i].color = tabActiveTextColor;
            }
            else
            {
                tabButtons[i].image.sprite = tabSprite;
                tabTexts[i].color = tabTextColor;
            }
        }
    }

    void StartListening(int index)
    {
        if (listening >= 0)
        {
            keyTexts[listening].text = oldKeyText;
            UpdateKeyButton(listening);
        }

        listening = index;
        oldKeyText = keyTexts[index].text;
        keyTexts[index].text = "Press a key...";
        keyTexts[index].color = Color.white;
        keyButtons[index].image.sprite = listeningSprite;
        SetKeyWidth(index);
        EventSystem.current.SetSelectedGameObject(null);
    }

    void ListenForKey()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            keyTexts[listening].text = oldKeyText;
            StopListening();
            return;
        }

        foreach (KeyControl key in Keyboard.current.allKeys)
        {
            if (key != null && key.wasPressedThisFrame)
            {
                SetKey(listening, key.keyCode);
                StopListening();
                return;
            }
        }
    }

    void SetKey(int index, Key key)
    {
        for (int i = 0; i < chosenKeys.Length; i++)
        {
            if (i != index && chosenKeys[i] == key)
            {
                chosenKeys[i] = Key.None;
                keyTexts[i].text = Keybinds.GetName(Key.None);
                UpdateKeyButton(i);
            }
        }

        chosenKeys[index] = key;
        keyTexts[index].text = Keybinds.GetName(key);
        Keybinds.keys = (Key[])chosenKeys.Clone();
    }

    void StopListening()
    {
        UpdateKeyButton(listening);
        listening = -1;
    }

    void UpdateKeyButton(int index)
    {
        if (keyTexts[index].text == "+ Add")
        {
            keyButtons[index].image.sprite = emptyKeySprite;
            keyTexts[index].color = tabTextColor;
        }
        else
        {
            keyButtons[index].image.sprite = keySprite;
            keyTexts[index].color = Color.white;
        }
        SetKeyWidth(index);
    }

    void SetKeyWidth(int index)
    {
        RectTransform rect = keyButtons[index].GetComponent<RectTransform>();
        float width = Mathf.Max(56, keyTexts[index].preferredWidth + 28);
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
    }

    void SetToggle(int index, bool on)
    {
        toggleStates[index] = on;
        Transform toggle = toggles[index].transform;
        RectTransform knob = toggle.Find("Knob").GetComponent<RectTransform>();
        TMP_Text state = toggle.Find("State").GetComponent<TMP_Text>();

        if (on)
        {
            toggles[index].image.color = onColor;
            knob.anchoredPosition = new Vector2(40, 0);
            state.text = "On";
            state.color = Color.white;
        }
        else
        {
            toggles[index].image.color = offColor;
            knob.anchoredPosition = new Vector2(4, 0);
            state.text = "Off";
            state.color = tabTextColor;
        }
    }

    void SelectOption(int group, int index)
    {
        optionGroups[group].selected = index;
        Button[] options = optionGroups[group].options;
        for (int i = 0; i < options.Length; i++)
        {
            TMP_Text text = options[i].GetComponentInChildren<TMP_Text>();
            if (i == index)
            {
                options[i].image.sprite = selectedOptionSprite;
                text.color = Color.white;
            }
            else
            {
                options[i].image.sprite = optionSprite;
                text.color = tabTextColor;
            }
        }
    }

    void UpdateCameraSettings(float value)
    {
        CameraSettings.moveSpeed = GetMultiplier(cameraSpeedSlider);
        CameraSettings.zoomSpeed = GetMultiplier(zoomSpeedSlider);
    }

    float GetMultiplier(int index)
    {
        float multiplier = sliders[index].value / defaultSliders[index];
        return Mathf.Max(0.1f, multiplier);
    }

    void ToggleResolutionList()
    {
        resolutionList.SetActive(!resolutionList.activeSelf);
    }

    void SelectResolution(int index)
    {
        resolutionIndex = index;
        resolutionText.text = resolutionOptions[index].GetComponentInChildren<TMP_Text>().text;

        for (int i = 0; i < resolutionOptions.Length; i++)
        {
            TMP_Text text = resolutionOptions[i].GetComponentInChildren<TMP_Text>();
            if (i == index)
            {
                resolutionOptions[i].image.sprite = selectedOptionSprite;
                text.color = Color.white;
            }
            else
            {
                resolutionOptions[i].image.sprite = optionSprite;
                text.color = tabTextColor;
            }
        }
        resolutionList.SetActive(false);
    }

    void Apply()
    {
        ApplyDisplaySettings();
        applyText.text = "Applied!";
        appliedTimer = 1.5f;
    }

    void ApplyDisplaySettings()
    {
        string[] parts = resolutionText.text.Split('x');
        int width = int.Parse(parts[0].Trim());
        int height = int.Parse(parts[1].Trim());

        int windowMode = optionGroups[windowModeGroup].selected;
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        if (windowMode == 1)
        {
            mode = FullScreenMode.Windowed;
        }
        if (windowMode == 2)
        {
            mode = FullScreenMode.FullScreenWindow;
        }
        Screen.SetResolution(width, height, mode);

        if (toggleStates[vsyncIndex])
        {
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
        }

        int frameRate = optionGroups[frameRateGroup].selected;
        Application.targetFrameRate = frameRates[frameRate];

        float scale = uiScaleSlider.value / 100f;
        foreach (CanvasScaler scaler in FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
        {
            scaler.referenceResolution = referenceResolution / scale;
        }
    }

    void ResetToDefault()
    {
        chosenKeys = (Key[])Keybinds.defaultKeys.Clone();
        Keybinds.keys = (Key[])chosenKeys.Clone();
        for (int i = 0; i < keyTexts.Length; i++)
        {
            keyTexts[i].text = Keybinds.GetName(chosenKeys[i]);
            UpdateKeyButton(i);
        }
        listening = -1;

        for (int i = 0; i < toggles.Length; i++)
        {
            SetToggle(i, defaultToggles[i]);
        }

        for (int i = 0; i < optionGroups.Length; i++)
        {
            SelectOption(i, defaultOptions[i]);
        }

        for (int i = 0; i < sliders.Length; i++)
        {
            sliders[i].value = defaultSliders[i];
        }

        SelectResolution(defaultResolution);
    }

    void Close()
    {
        closing = true;
    }
}
