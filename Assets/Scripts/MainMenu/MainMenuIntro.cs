using UnityEngine;

public class MainMenuIntro : MonoBehaviour
{
    [SerializeField] RectTransform[] elements;
    [SerializeField] RectTransform[] buttons;
    [SerializeField] float startDelay = 0.3f;
    [SerializeField] float elementDelay = 0.18f;
    [SerializeField] float buttonDelay = 0.09f;
    [SerializeField] float fadeTime = 0.7f;
    [SerializeField] float buttonSlide = 50f;
    [SerializeField] float titleDrop = 30f;
    [SerializeField] float panelSlide = 300f;

    CanvasGroup[] elementGroups;
    CanvasGroup[] buttonGroups;
    Vector2[] elementStartPositions;
    Vector2[] buttonStartPositions;
    float timer;

    void Start()
    {
        elementGroups = new CanvasGroup[elements.Length];
        elementStartPositions = new Vector2[elements.Length];

        for (int i = 0; i < elements.Length; i++)
        {
            elementGroups[i] = GetGroup(elements[i]);
            elementStartPositions[i] = elements[i].anchoredPosition;
        }

        buttonGroups = new CanvasGroup[buttons.Length];
        buttonStartPositions = new Vector2[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            buttonGroups[i] = GetGroup(buttons[i]);
            buttonStartPositions[i] = buttons[i].anchoredPosition;
        }

        Animate();
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;

        Animate();

        if (timer > startDelay + elements.Length * elementDelay + buttons.Length * buttonDelay + fadeTime)
            enabled = false;
    }

    void Animate()
    {
        for (int i = 0; i < elements.Length; i++)
        {
            float t = Ease((timer - startDelay - i * elementDelay) / fadeTime);
            RectTransform element = elements[i];
            Vector2 offset = Vector2.zero;

            if (element.name == "Title")
                offset = Vector2.up * titleDrop * (1f - t);

            if (element.name == "Panel")
                offset = Vector2.left * panelSlide * (1f - t);

            if (element.name == "Ornament")
                element.localScale = new Vector3(t, 1f, 1f);

            element.anchoredPosition = elementStartPositions[i] + offset;
            elementGroups[i].alpha = t;
        }

        float buttonStart = startDelay + elements.Length * elementDelay;

        for (int i = 0; i < buttons.Length; i++)
        {
            float t = Ease((timer - buttonStart - i * buttonDelay) / fadeTime);

            buttons[i].anchoredPosition = buttonStartPositions[i] + Vector2.left * buttonSlide * (1f - t);
            buttonGroups[i].alpha = t;
        }
    }

    CanvasGroup GetGroup(RectTransform target)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();

        if (group == null)
            group = target.gameObject.AddComponent<CanvasGroup>();

        return group;
    }

    float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
