using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillHex : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] float hoverScale = 1.04f;
    [SerializeField] float speed = 12f;
    [SerializeField] float changeTime = 0.25f;
    [SerializeField] float glowAlpha = 0.45f;
    [SerializeField] Color selectedColor = new Color32(226, 178, 92, 255);
    [SerializeField] float holdTime = 0.8f;
    [SerializeField] float holdAlpha = 0.7f;

    public static bool isHolding;

    public float appearDelay;
    public bool learned;
    public bool selected;
    public bool locked;
    public Color borderColor;
    public Color fillColor;
    public Color iconColor;
    public Color labelColor;
    public Color glowColor;
    public bool canLearn;
    public SkillTreeUI tree;
    public int index;

    Image border;
    Image fill;
    Image icon;
    TMP_Text label;
    Image glow;
    Image ring;
    Image lockIcon;
    Image holdFill;
    bool holding;
    float holdAmount;
    float holdFade;
    float squeeze;
    float timer;
    float scale;
    float hover;
    float ringAlpha;
    float pop;
    float popAmount;
    float popTime = 1;
    float glowAmount;
    float change = 1;
    float startGlow;
    Color startBorder;
    Color startFill;
    Color startIcon;
    Color startLabel;
    Color lastBorder;
    Color lastFill;
    bool hovered;
    bool firstFrame = true;

    void Awake()
    {
        border = transform.Find("Border").GetComponent<Image>();
        fill = transform.Find("Fill").GetComponent<Image>();
        icon = transform.Find("Icon").GetComponent<Image>();
        label = transform.Find("Label").GetComponent<TMP_Text>();
        glow = transform.Find("Glow").GetComponent<Image>();
        ring = transform.Find("Selected").GetComponent<Image>();
        lockIcon = transform.Find("Lock").GetComponent<Image>();
        lockIcon.gameObject.SetActive(true);

        GameObject fillCopy = Instantiate(fill.gameObject, transform);
        fillCopy.name = "HoldFill";
        fillCopy.transform.SetSiblingIndex(fill.transform.GetSiblingIndex() + 1);
        holdFill = fillCopy.GetComponent<Image>();
        holdFill.type = Image.Type.Filled;
        holdFill.fillMethod = Image.FillMethod.Vertical;
        holdFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        holdFill.fillAmount = 0;
        holdFill.raycastTarget = false;

        glow.gameObject.SetActive(true);
        ring.gameObject.SetActive(true);
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        timer += dt;
        if (timer < appearDelay)
        {
            return;
        }

        if (firstFrame)
        {
            border.color = borderColor;
            fill.color = fillColor;
            icon.color = iconColor;
            label.color = labelColor;
            lastBorder = borderColor;
            lastFill = fillColor;
            if (learned)
            {
                glowAmount = 1;
            }
            startGlow = glowAmount;
            if (!locked)
            {
                lockIcon.color = new Color(lockIcon.color.r, lockIcon.color.g, lockIcon.color.b, 0);
            }
            firstFrame = false;
        }

        if (borderColor != lastBorder || fillColor != lastFill)
        {
            startBorder = lastBorder;
            startFill = fill.color;
            startIcon = icon.color;
            startLabel = label.color;
            startGlow = glowAmount;
            lastBorder = borderColor;
            lastFill = fillColor;
            change = 0;
        }

        UpdateHold(dt);

        float hoverTarget = 0;
        if (hovered)
        {
            hoverTarget = 1;
        }
        hover = Mathf.Lerp(hover, hoverTarget, dt * speed);
        popTime = Mathf.Min(popTime + dt / 0.2f, 1);
        pop = Mathf.Sin(popTime * Mathf.PI) * popAmount;
        scale = Mathf.Lerp(scale, 1, dt * speed);
        float hoverSize = Mathf.Lerp(1, hoverScale, hover);
        float squeezeTarget = 0;
        if (holding)
        {
            squeezeTarget = holdAmount * 0.06f;
        }
        squeeze = Mathf.Lerp(squeeze, squeezeTarget, dt * speed);
        transform.localScale = Vector3.one * scale * (hoverSize + pop - squeeze);

        change = Mathf.Min(change + dt / changeTime, 1);
        float t = Mathf.SmoothStep(0, 1, change);

        Color baseBorder = Color.Lerp(startBorder, borderColor, t);
        border.color = Color.Lerp(baseBorder, Color.white, hover * 0.3f);
        fill.color = Color.Lerp(startFill, fillColor, t);
        icon.color = Color.Lerp(startIcon, iconColor, t);
        label.color = Color.Lerp(startLabel, labelColor, t);

        float targetGlow = 0;
        if (learned)
        {
            targetGlow = 1;
        }
        glowAmount = Mathf.Lerp(startGlow, targetGlow, t);
        glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowAmount * glowAlpha);
        glow.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1, glowAmount);

        Color lockColor = lockIcon.color;
        float lockTarget = 0;
        if (locked)
        {
            lockTarget = 1;
        }
        lockColor.a = Mathf.MoveTowards(lockColor.a, lockTarget, dt / changeTime);
        lockIcon.color = lockColor;

        float ringTarget = 0;
        if (selected)
        {
            ringTarget = 1;
        }
        ringAlpha = Mathf.Lerp(ringAlpha, ringTarget, dt * speed);
        ring.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, ringAlpha);
    }

    void UpdateHold(float dt)
    {
        if (holding && Mouse.current != null && !Mouse.current.leftButton.isPressed)
        {
            StopHold();
        }

        if (holding)
        {
            holdAmount += dt / holdTime;
            if (holdAmount >= 1)
            {
                holdAmount = 1;
                StopHold();
                holdFade = 1;
                tree.LearnSkill(index);
            }
        }
        else if (holdFade > 0)
        {
            holdFade = Mathf.MoveTowards(holdFade, 0, dt * 4);
            if (holdFade == 0)
            {
                holdAmount = 0;
            }
        }
        else
        {
            holdAmount = Mathf.MoveTowards(holdAmount, 0, dt * 4);
        }

        float alpha = holdAlpha;
        if (holdFade > 0)
        {
            alpha = holdAlpha * holdFade;
        }
        holdFill.fillAmount = holdAmount;
        holdFill.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (canLearn)
        {
            holding = true;
            isHolding = true;
            holdFade = 0;
        }
    }

    void OnDisable()
    {
        StopHold();
    }

    void StopHold()
    {
        if (holding)
        {
            holding = false;
            isHolding = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    public void Press()
    {
        popAmount = -0.04f;
        popTime = 0;
    }

    public void LearnEffect()
    {
        popAmount = 0.08f;
        popTime = 0;
    }
}
