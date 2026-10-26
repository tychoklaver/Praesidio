using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates an achievement pop-up and fills in its texts and icon.
/// The look (background, fonts, layout) is NOT in code: it is built in the editor,
/// so it can be redesigned without touching this script.
/// Place the panel where it should END UP; it slides in from above to that position.
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

    [Header("Design (assign in the Inspector)")]
    [Tooltip("The whole pop-up. Its background image goes on this object. Anchor it to the top.")]
    [SerializeField] private RectTransform panel;
    [Tooltip("CanvasGroup on the panel, used for fading.")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Optional. Leave empty for a pop-up without an icon.")]
    [SerializeField] private Image iconImage;
    [Tooltip("Optional. Shown when an achievement has no icon of its own.")]
    [SerializeField] private Sprite fallbackIcon;

    [Header("Animation")]
    [Tooltip("How far above its final position the panel starts (in canvas units). Must be enough to leave the screen.")]
    [SerializeField] private float slideDistance = 190f;
    [SerializeField] private float slideInSeconds = 0.5f;
    [SerializeField] private float holdSeconds = 3f;
    [SerializeField] private float slideOutSeconds = 0.4f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip unlockSound;

    private readonly Queue<PopupRequest> _queue = new();
    private bool _showing;

    private Vector2 _shownPosition;
    private Vector2 _hiddenPosition;
    private AudioSource _audioSource;

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

        if (panel == null || group == null || titleText == null || descriptionText == null)
        {
            Debug.LogError("[AchievementPopup] Panel, Group, Title Text and Description Text must be assigned.", this);
            enabled = false;
            return;
        }

        // The position the designer placed the panel at is where it comes to rest.
        _shownPosition = panel.anchoredPosition;
        _hiddenPosition = _shownPosition + Vector2.up * slideDistance;

        // The pop-up must never block clicks on the UI underneath.
        group.blocksRaycasts = false;
        group.interactable = false;

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

            titleText.text = request.title;
            descriptionText.text = request.description;

            if (iconImage != null)
            {
                Sprite sprite = request.icon != null ? request.icon : fallbackIcon;
                iconImage.sprite = sprite;
                iconImage.enabled = sprite != null;
            }

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
        for (float t = 0f; t < slideInSeconds; t += Time.unscaledDeltaTime)
        {
            float progress = t / slideInSeconds;
            group.alpha = Mathf.Clamp01(progress * 3f); // Fades in over the first third.
            panel.anchoredPosition = Vector2.LerpUnclamped(_hiddenPosition, _shownPosition, EaseOutBack(progress));
            yield return null;
        }

        group.alpha = 1f;
        panel.anchoredPosition = _shownPosition;
    }

    private IEnumerator Hold()
    {
        // Icon pulses slightly so the pop-up feels alive.
        for (float t = 0f; t < holdSeconds; t += Time.unscaledDeltaTime)
        {
            if (iconImage != null)
            {
                float scale = 1f + 0.08f * Mathf.Sin(t * 6f);
                iconImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        if (iconImage != null) iconImage.rectTransform.localScale = Vector3.one;
    }

    private IEnumerator SlideOut()
    {
        for (float t = 0f; t < slideOutSeconds; t += Time.unscaledDeltaTime)
        {
            float progress = t / slideOutSeconds;
            group.alpha = 1f - progress;
            panel.anchoredPosition = Vector2.Lerp(_shownPosition, _hiddenPosition, progress * progress * progress);
            yield return null;
        }

        SetHidden();
    }

    private void SetHidden()
    {
        group.alpha = 0f;
        panel.anchoredPosition = _hiddenPosition;
    }

    // Overshoots slightly past the target before settling: gives the slide-in a bouncy feel.
    private static float EaseOutBack(float pT)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = pT - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
