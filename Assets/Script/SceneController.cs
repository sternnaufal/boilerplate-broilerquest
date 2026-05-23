using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    private static SceneController _instance;

    public static SceneController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SceneController>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneController");
                    _instance = go.AddComponent<SceneController>();
                }
            }
            return _instance;
        }
    }

    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string selectLevelScene = "SelectLevel";
    [SerializeField] private string koleksiIoTScene = "KoleksiIoT";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GoToMainMenu()
    {
        SetGameStateOrFallback(GameState.Menu);
        SceneManager.LoadScene(mainMenuScene);
    }

    public void GoToSelectLevel()
    {
        SetGameStateOrFallback(GameState.Menu);
        SceneManager.LoadScene(selectLevelScene);
    }

    public void GoToKoleksiIoT()
    {
        SetGameStateOrFallback(GameState.Menu);
        SceneManager.LoadScene(koleksiIoTScene);
    }

    public void GoToLevel(int levelIndex)
    {
        string[] sceneNames = GameManager.Instance != null
            ? GameManager.Instance.sceneNames
            : new string[] { "Starter", "Beginner", "Intermediate" };

        if (levelIndex < 0 || levelIndex >= sceneNames.Length)
        {
            Debug.LogError($"Level index {levelIndex} is out of range.");
            return;
        }

        string sceneName = sceneNames[levelIndex];
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not in Build Settings or cannot be loaded.");
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentLevelIndex = levelIndex;
            GameManager.Instance.SetGameActive(true);
        }

        SetGameStateOrFallback(GameState.Playing);
        SceneManager.LoadScene(sceneName);
    }

    private static void SetGameStateOrFallback(GameState state)
    {
        if (GameStateManager.TrySetGameState(state))
            return;

        Time.timeScale = state == GameState.Paused ? 0f : 1f;
    }
}
