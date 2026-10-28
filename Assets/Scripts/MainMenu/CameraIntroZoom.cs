using System.Collections;
using UnityEngine;

public class CameraIntroZoom : MonoBehaviour
{
    [SerializeField] float startSize = 60f;
    [SerializeField] float duration = 3f;
    [SerializeField] float delay = 0f;
    [SerializeField] float controlAt = 0.45f;

    IEnumerator Start()
    {
        Camera cam = GetComponent<Camera>();
        CameraController controller = GetComponent<CameraController>();

        float endSize = cam.orthographicSize;
        cam.orthographicSize = startSize;

        if (controller != null)
        {
            controller.enabled = false;
        }

        yield return null;
        yield return new WaitForSecondsRealtime(delay);

        float timer = 0;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            float t = 1 - Mathf.Pow(1 - progress, 4);
            float logSize = Mathf.Lerp(Mathf.Log(startSize), Mathf.Log(endSize), t);
            cam.orthographicSize = Mathf.Exp(logSize);

            if (progress >= controlAt && controller != null)
            {
                controller.enabled = true;
                yield break;
            }

            yield return null;
        }
        cam.orthographicSize = endSize;

        if (controller != null)
        {
            controller.enabled = true;
        }
    }
}
