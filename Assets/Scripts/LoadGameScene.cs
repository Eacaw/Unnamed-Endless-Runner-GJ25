using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadGameScene : MonoBehaviour
{
    [SerializeField]
    private Button playButton;

    [SerializeField]
    private string sceneToLoad;

    private void Start()
    {
        if (playButton == null)
        {
            Debug.LogError("Button reference not assigned in the Inspector.");
            return;
        }

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene name to load is not set.");
            return;
        }

        playButton.onClick.AddListener(LoadScene);
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
