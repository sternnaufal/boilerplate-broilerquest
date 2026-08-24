using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [Header("Pengaturan Level")]
    public int currentLevelIndex = 0;
    public string[] sceneNames = { "Starter", "Beginner", "Intermediate" };
    public float[] levelDurations =
    {
        GameConstants.LevelDuration.Starter,
        GameConstants.LevelDuration.Beginner,
        GameConstants.LevelDuration.Intermediate
    };

    [Header("Timer")]
    [SerializeField] private LevelTimer levelTimer;

    [Header("Popup Waktu Habis (Prefab)")]
    public GameObject timeUpPopupPrefab;
    public Canvas mainCanvas;

    private bool isGameActive = true;
    private bool isPopupShowing = false;
    private bool isRecoveringFromMissingTimeUpUi = false;
    private bool deferTimer;

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        InitializeForCurrentScene();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeForCurrentScene();
    }

    void OnSceneUnloaded(Scene scene)
    {
        UnbindTimer();
        mainCanvas = null;
    }

    private void UnbindTimer()
    {
        if (levelTimer != null)
        {
            levelTimer.OnTimeUp -= OnTimerUp;
            levelTimer = null;
        }
    }

    public void InitializeForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // Hanya jalankan timer untuk scene gameplay — bukan MainMenu, SelectLevel, dll.
        if (System.Array.IndexOf(sceneNames, sceneName) < 0)
        {
            UnbindTimer();
            return;
        }

        // Auto-detect level index dari scene name
        for (int i = 0; i < sceneNames.Length; i++)
        {
            if (sceneNames[i] == sceneName)
            {
                currentLevelIndex = i;
                break;
            }
        }

        // Re-find Canvas: prefer "StarterCanvas", fallback any Canvas
        mainCanvas = null;
        GameObject canvasObj = GameObject.Find("StarterCanvas");
        if (canvasObj != null)
            mainCanvas = canvasObj.GetComponent<Canvas>();
        if (mainCanvas == null)
            mainCanvas = FindFirstObjectByType<Canvas>();

        // TimeUpPopup fallback: GameObject.Find -> Resources.Load
        if (timeUpPopupPrefab == null)
        {
            GameObject found = GameObject.Find("TimeUpPopup");
            if (found != null)
                timeUpPopupPrefab = found;
        }
        if (timeUpPopupPrefab == null)
            timeUpPopupPrefab = Resources.Load<GameObject>("TimeUpPopup");

        // Auto-add LevelTimer jika belum ada di scene
        if (levelTimer == null)
            levelTimer = FindFirstObjectByType<LevelTimer>();
        if (levelTimer == null && gameObject != null)
            levelTimer = gameObject.AddComponent<LevelTimer>();

        // Assign TimerText dari Canvas langsung ke LevelTimer
        if (levelTimer != null && mainCanvas != null)
        {
            var timerTMP = mainCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (timerTMP != null && timerTMP.gameObject.name == "TimerText")
                levelTimer.BindTimerText(timerTMP);
        }

        if (levelTimer != null)
        {
            levelTimer.OnTimeUp -= OnTimerUp;
            levelTimer.OnTimeUp += OnTimerUp;

            if (!deferTimer)
            {
                if (currentLevelIndex >= 0 && currentLevelIndex < levelDurations.Length)
                    levelTimer.StartTimer(levelDurations[currentLevelIndex]);
                else
                    levelTimer.StartTimer(60f);
            }
        }

        isGameActive = true;
        isPopupShowing = false;
        isRecoveringFromMissingTimeUpUi = false;
    }

    private void OnTimerUp()
    {
        if (!isGameActive) return;
        SaveManager.SaveAll();
        isGameActive = false;

        if (DemoModeConfig.IsDemoMode)
            SaveManager.ResetDataForDemoMode();

        if (SFXManager.Instance != null) SFXManager.Instance.PlayTimeUp();
        isPopupShowing = true;
        GameStateManager.TrySetGameState(GameState.GameOver);

        if (timeUpPopupPrefab != null && mainCanvas != null)
        {
            GameObject popup = Instantiate(timeUpPopupPrefab, mainCanvas.transform);
            var popupScript = popup.GetComponent<TimeUpPopup>();
            if (popupScript != null)
            {
                int finalCoin = (CoinManager.Instance != null) ? CoinManager.Instance.GetTotalCoin() : 0;
                popupScript.Setup(finalCoin, currentLevelIndex, sceneNames);
            }
        }
        else
        {
            Debug.LogError("Popup prefab atau mainCanvas tidak di-assign di GameManager!");
            if (!isRecoveringFromMissingTimeUpUi)
            {
                isRecoveringFromMissingTimeUpUi = true;
                ReturnToMainMenu();
            }
        }
    }

    public void GoToNextLevel()
    {
        isPopupShowing = false;
        int nextLevel = currentLevelIndex + 1;

        while (nextLevel < sceneNames.Length)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneNames[nextLevel]))
            {
                Debug.LogWarning($"Scene '{sceneNames[nextLevel]}' is not available. Skipping next level.");
                nextLevel++;
                continue;
            }

            currentLevelIndex = nextLevel;
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(sceneNames[nextLevel]);
            else
                SceneManager.LoadScene(sceneNames[nextLevel]);
            return;
        }

        GameLog.Info("Sudah level terakhir! Kembali ke menu utama.");
        ReturnToMainMenu();
    }

    public void ReturnToMainMenu()
    {
        isPopupShowing = false;
        currentLevelIndex = 0;
        GameStateManager.TrySetGameState(GameState.Menu);

        if (SceneController.Instance != null)
            SceneController.Instance.GoToMainMenu();
    }

    public bool IsGameActive()
    {
        return isGameActive && !isPopupShowing;
    }

    public void SetGameActive(bool active)
    {
        isGameActive = active;
        if (active)
            isPopupShowing = false;
    }

    public void SetDeferTimer(bool defer)
    {
        deferTimer = defer;
    }

    public void StartTimerForLevel()
    {
        deferTimer = false;
        if (levelTimer == null)
            levelTimer = FindFirstObjectByType<LevelTimer>();
        if (levelTimer != null)
        {
            levelTimer.OnTimeUp -= OnTimerUp;
            levelTimer.OnTimeUp += OnTimerUp;
            if (currentLevelIndex >= 0 && currentLevelIndex < levelDurations.Length)
                levelTimer.StartTimer(levelDurations[currentLevelIndex]);
            else
                levelTimer.StartTimer(60f);
        }
    }

    protected override void OnDestroy()
    {
        UnbindTimer();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        base.OnDestroy();
    }
}
