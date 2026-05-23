using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string selectLevelScene = "SelectLevel";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void GoToSelectLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(selectLevelScene);
    }

    public void GoToLevel(int levelIndex)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentLevelIndex = levelIndex;
            GameManager.Instance.SetGameActive(true);
        }

        string[] sceneNames = GameManager.Instance != null
            ? GameManager.Instance.sceneNames
            : new string[] { "Starter", "Beginner", "Intermediate" };

        if (levelIndex >= 0 && levelIndex < sceneNames.Length)
        {
            SceneManager.LoadScene(sceneNames[levelIndex]);
        }
        else
        {
            Debug.LogError($"Level index {levelIndex} is out of range.");
        }
    }
}
