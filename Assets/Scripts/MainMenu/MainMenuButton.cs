using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] GameObject hover;
    [SerializeField] Image textImage;
    [SerializeField] Sprite normalSprite;
    [SerializeField] Sprite hoverSprite;

    [SerializeField] float hoverSpeed = 5f;
    [SerializeField] float textSlide = 14f;
    [SerializeField] float textLift = 0f;
    [SerializeField] float hoverScale = 1.04f;
    [SerializeField] float pressScale = 0.94f;
    [SerializeField] float glowPulse = 0.06f;

    CanvasGroup hoverGroup;
    RectTransform textRect;
    Vector2 textStartPosition;
    Transform underline;
    Transform bar;
    Transform diamond;
    Transform glow;
    Vector3 diamondStartPosition;
    bool isHovered;
    bool isPressed;
    float progress;
    float press;

    void Awake()
    {
        hoverGroup = hover.GetComponent<CanvasGroup>();

        if (hoverGroup == null)
            hoverGroup = hover.AddComponent<CanvasGroup>();

        hoverGroup.blocksRaycasts = false;
        hoverGroup.interactable = false;
        hover.SetActive(true);

        textRect = textImage.rectTransform;
        textStartPosition = textRect.anchoredPosition;

        underline = hover.transform.Find("Underline");
        bar = hover.transform.Find("Bar");
        diamond = hover.transform.Find("Diamond");
        glow = hover.transform.Find("Glow");

        if (diamond != null)
            diamondStartPosition = diamond.localPosition;

        Apply(0f);
    }

    void OnDisable()
    {
        isHovered = false;
        isPressed = false;
        progress = 0f;
        press = 0f;
        Apply(0f);
    }

    void Update()
    {
        progress = Mathf.MoveTowards(progress, isHovered ? 1f : 0f, Time.unscaledDeltaTime * hoverSpeed);
        press = Mathf.MoveTowards(press, isPressed ? 1f : 0f, Time.unscaledDeltaTime * 14f);

        Apply(Mathf.SmoothStep(0f, 1f, progress));
    }

    void Apply(float t)
    {
        hoverGroup.alpha = t;

        textRect.anchoredPosition = textStartPosition + new Vector2(textSlide, textLift) * t;
        textImage.sprite = t > 0.5f ? hoverSprite : normalSprite;

        float scale = Mathf.Lerp(1f, hoverScale, t) * Mathf.Lerp(1f, pressScale, press);
        transform.localScale = new Vector3(scale, scale, 1f);

        if (underline != null)
            underline.localScale = new Vector3(t, 1f, 1f);

        if (bar != null)
            bar.localScale = new Vector3(Mathf.Lerp(0.4f, 1f, t), 1f, 1f);

        if (diamond != null)
        {
            diamond.localPosition = diamondStartPosition + Vector3.left * 24f * (1f - t);
            diamond.localRotation = Quaternion.Euler(0f, 0f, (1f - t) * 180f);
        }

        if (glow != null)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f) * glowPulse;
            glow.localScale = new Vector3(pulse, pulse, 1f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }
}
