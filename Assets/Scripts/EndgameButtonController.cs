using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndgameButtonController : MonoBehaviour
{
    [SerializeField]
    private Button tryAgainButton;

    [SerializeField]
    private Button mainMenuButton;

    private void Start()
    {
        tryAgainButton.onClick.AddListener(() => LoadScene("GameWorld"));
        mainMenuButton.onClick.AddListener(() => LoadScene("MainMenu"));
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log("Clicked button");
    }
}
