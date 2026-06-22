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
        GameStateManager.ApplyState(GameState.Menu);
        if (BGMManager.Instance != null) BGMManager.Instance.PlayMenuBGM();
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(mainMenuScene);
        else
            SceneManager.LoadScene(mainMenuScene);
    }

    public void GoToSelectLevel()
    {
        GameStateManager.ApplyState(GameState.Menu);
        if (BGMManager.Instance != null) BGMManager.Instance.PlayMenuBGM();
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(selectLevelScene);
        else
            SceneManager.LoadScene(selectLevelScene);
    }

    public void GoToKoleksiIoT()
    {
        GameStateManager.ApplyState(GameState.Menu);
        if (BGMManager.Instance != null) BGMManager.Instance.PlayMenuBGM();
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(koleksiIoTScene);
        else
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

        if (BGMManager.Instance != null)
        {
            switch (levelIndex)
            {
                case 0: BGMManager.Instance.PlayStarterBGM(); break;
                case 1: BGMManager.Instance.PlayBeginnerBGM(); break;
                case 2: BGMManager.Instance.PlayIntermediateBGM(); break;
            }
        }

        GameStateManager.ApplyState(GameState.Playing);
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

}
