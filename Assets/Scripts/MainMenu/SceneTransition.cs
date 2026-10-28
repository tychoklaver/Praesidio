using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    static SceneTransition instance;

    float closeTime = 0.45f;
    float holdTime = 0.15f;
    float openTime = 0.45f;

    RectTransform topBar;
    RectTransform bottomBar;
    bool busy;

    public static void Load(string sceneName)
    {
        if (instance == null)
        {
            GameObject transition = new GameObject("SceneTransition");
            instance = transition.AddComponent<SceneTransition>();
        }

        if (!instance.busy)
        {
            instance.StartCoroutine(instance.Transition(sceneName));
        }
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        gameObject.AddComponent<GraphicRaycaster>();

        topBar = CreateBar("TopBar");
        bottomBar = CreateBar("BottomBar");
        SetBars(0);
    }

    RectTransform CreateBar(string barName)
    {
        GameObject bar = new GameObject(barName);
        bar.transform.SetParent(transform, false);
        Image image = bar.AddComponent<Image>();
        image.color = Color.black;
        return bar.GetComponent<RectTransform>();
    }

    void SetBars(float amount)
    {
        float t = Mathf.SmoothStep(0, 1, amount);

        topBar.anchorMin = new Vector2(0, 1 - 0.5f * t);
        topBar.anchorMax = new Vector2(1, 1);
        topBar.offsetMin = Vector2.zero;
        topBar.offsetMax = Vector2.zero;

        bottomBar.anchorMin = new Vector2(0, 0);
        bottomBar.anchorMax = new Vector2(1, 0.5f * t);
        bottomBar.offsetMin = Vector2.zero;
        bottomBar.offsetMax = Vector2.zero;
    }

    IEnumerator Transition(string sceneName)
    {
        busy = true;

        float timer = 0;
        while (timer < closeTime)
        {
            timer += Time.unscaledDeltaTime;
            SetBars(timer / closeTime);
            yield return null;
        }
        SetBars(1);

        Time.timeScale = 1;
        AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
        if (loading != null)
        {
            while (!loading.isDone)
            {
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(holdTime);

        timer = 0;
        while (timer < openTime)
        {
            timer += Time.unscaledDeltaTime;
            SetBars(1 - timer / openTime);
            yield return null;
        }
        SetBars(0);

        busy = false;
    }
}
