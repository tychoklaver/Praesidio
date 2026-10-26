using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
/// <summary>
/// Achievement pop-up that builds its own UI at runtime (no Canvas or prefab needed).
/// It slides in from the top, holds, and slides out again. Multiple unlocks are queued.
/// </summary>
public class AchievementPopup : MonoBehaviour
{
    private struct PopupRequest
    {
        public string title;
        public string description;
        public Sprite icon;
    }
 
    public static AchievementPopup Instance { get; private set; }
 
    [Header("Timing (seconds)")]
    [SerializeField] private float slideInSeconds = 0.5f;
    [SerializeField] private float holdSeconds = 3f;
    [SerializeField] private float slideOutSeconds = 0.4f;
 
    [Header("Audio (optional)")]
    [SerializeField] private AudioClip unlockSound;
 
    private static readonly Color Gold = new(1f, 0.82f, 0.25f, 1f);
 
    private readonly Queue<PopupRequest> _queue = new();
    private bool _showing;
 
    private RectTransform _panel;
    private CanvasGroup _group;
    private Image _icon;
    private Text _titleText;
    private Text _descriptionText;
    private AudioSource _audioSource;
 
    private float PanelHeight => _panel.sizeDelta.y;
 
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;
 
    private void Awake()
    {
        if (Instance != null && Instance != this) return;
 
        Instance = this;
    }
 
    // Start instead of OnEnable: by now AchievementManager.Instance exists.
    private void Start()
    {
        if (Instance != this) return;
 
        BuildUi();
        SetHidden();
 
        if (unlockSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D sound.
        }
 
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked += HandleUnlocked;
    }
 
    private void OnDestroy()
    {
        if (Instance == this && AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked -= HandleUnlocked;
    }
 
    private void HandleUnlocked(AchievementDefinition pDef) => Show(pDef.Title, pDef.Description, pDef.Icon);
 
    public void Show(string pTitle, string pDescription, Sprite pIcon)
    {
        _queue.Enqueue(new PopupRequest { title = pTitle, description = pDescription, icon = pIcon });
        if (!_showing) StartCoroutine(ShowQueue());
    }
 
    [ContextMenu("Test: Show popup")]
    private void TestShow() => Show("Test Achievement", "This is what an unlocked achievement looks like.", null);
 
    private IEnumerator ShowQueue()
    {
        _showing = true;
 
        while (_queue.Count > 0)
        {
            PopupRequest request = _queue.Dequeue();
 
            _titleText.text = request.title;
            _descriptionText.text = request.description;
            _icon.sprite = request.icon;
            _icon.color = request.icon != null ? Color.white : Gold; // Gold block when there is no icon yet.
 
            if (_audioSource != null) _audioSource.PlayOneShot(unlockSound);
 
            yield return SlideIn();
            yield return Hold();
            yield return SlideOut();
        }
 
        _showing = false;
    }
 
    // All timing uses unscaled time, so the pop-up also works while the game is paused.
    private IEnumerator SlideIn()
    {
        float shownY = -30f;
        float hiddenY = PanelHeight + 20f;
 
        for (float t = 0f; t < slideInSeconds; t += Time.unscaledDeltaTime)
        {
            float progress = t / slideInSeconds;
            _group.alpha = Mathf.Clamp01(progress * 3f); // Fades in over the first third.
            _panel.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(hiddenY, shownY, EaseOutBack(progress)));
            yield return null;
        }
 
        _group.alpha = 1f;
        _panel.anchoredPosition = new Vector2(0f, shownY);
    }
 
    private IEnumerator Hold()
    {
        // Icon pulses slightly so the pop-up feels alive.
        for (float t = 0f; t < holdSeconds; t += Time.unscaledDeltaTime)
        {
            float scale = 1f + 0.08f * Mathf.Sin(t * 6f);
            _icon.rectTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
 
        _icon.rectTransform.localScale = Vector3.one;
    }
 
    private IEnumerator SlideOut()
    {
        float shownY = -30f;
        float hiddenY = PanelHeight + 20f;
 
        for (float t = 0f; t < slideOutSeconds; t += Time.unscaledDeltaTime)
        {
            float progress = t / slideOutSeconds;
            _group.alpha = 1f - progress;
            _panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(shownY, hiddenY, progress * progress * progress));
            yield return null;
        }
 
        SetHidden();
    }
 
    private void SetHidden()
    {
        _group.alpha = 0f;
        _panel.anchoredPosition = new Vector2(0f, PanelHeight + 20f);
    }
 
    // Overshoots slightly past the target before settling: gives the slide-in a bouncy feel.
    private static float EaseOutBack(float pT)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = pT - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
 
    // ---- UI construction ----
 
    private void BuildUi()
    {
        var canvasGo = new GameObject("AchievementPopupCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
 
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // On top of every other UI.
 
        // No GraphicRaycaster on purpose: the pop-up never blocks clicks.
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
 
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(CanvasGroup));
        panelGo.transform.SetParent(canvasGo.transform, false);
 
        _panel = panelGo.GetComponent<RectTransform>();
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 1f); // Top center.
        _panel.pivot = new Vector2(0.5f, 1f);
        _panel.sizeDelta = new Vector2(640f, 140f);
 
        var background = panelGo.GetComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        background.raycastTarget = false;
 
        var outline = panelGo.GetComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(3f, -3f);
 
        _group = panelGo.GetComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;
 
        // Icon, left side.
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(_panel, false);
 
        _icon = iconGo.GetComponent<Image>();
        _icon.raycastTarget = false;
        _icon.preserveAspect = true;
 
        RectTransform iconRect = _icon.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(70f, 0f);
        iconRect.sizeDelta = new Vector2(100f, 100f);
 
        // Texts, right of the icon.
        CreateText("Heading", 22, FontStyle.Bold, Gold, -14f, 28f).text = "ACHIEVEMENT UNLOCKED";
        _titleText = CreateText("Title", 36, FontStyle.Bold, Color.white, -42f, 44f);
        _descriptionText = CreateText("Description", 24, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f, 1f), -88f, 44f);
    }
 
    private Text CreateText(string pName, int pFontSize, FontStyle pStyle, Color pColor, float pTopOffset, float pHeight)
    {
        var textGo = new GameObject(pName, typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(_panel, false);
 
        var text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = pFontSize;
        text.fontStyle = pStyle;
        text.color = pColor;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
 
        // Stretches horizontally with 140 px left (icon) and 20 px right margin.
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-160f, pHeight);
        rect.anchoredPosition = new Vector2(60f, pTopOffset);
 
        return text;
    }
}