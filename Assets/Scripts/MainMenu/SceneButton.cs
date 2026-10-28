using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneButton : MonoBehaviour
{
    [SerializeField] string sceneName;
    [SerializeField] bool quitGame;
    [SerializeField] bool openOnTop;

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

        if (openOnTop)
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            return;
        }

        SceneTransition.Load(sceneName);
    }
}
