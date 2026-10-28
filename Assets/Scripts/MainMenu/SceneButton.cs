using UnityEngine;
using UnityEngine.UI;

public class SceneButton : MonoBehaviour
{
    [SerializeField] string sceneName;
    [SerializeField] bool quitGame;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (quitGame)
        {
            Application.Quit();
            return;
        }

        SceneTransition.Load(sceneName);
    }
}
