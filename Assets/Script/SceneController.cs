using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string selectLevelScene = "SelectLevel";
    [SerializeField] private string koleksiIoTScene = "KoleksiIoT";

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
