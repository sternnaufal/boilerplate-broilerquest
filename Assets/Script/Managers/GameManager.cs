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

    [Header("SFX")]
    [SerializeField] private AudioClip timeUpSfx;

    [Header("Popup Waktu Habis (Prefab)")]
    public GameObject timeUpPopupPrefab;
    public Canvas mainCanvas;

    private bool isGameActive = true;
    private bool isPopupShowing = false;

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeForCurrentScene();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeForCurrentScene();
    }

    public void InitializeForCurrentScene()
    {
        if (mainCanvas == null)
            mainCanvas = FindFirstObjectByType<Canvas>();

        if (levelTimer == null)
            levelTimer = FindFirstObjectByType<LevelTimer>();

        if (levelTimer != null && mainCanvas != null)
        {
            levelTimer.OnTimeUp -= OnTimerUp;
            levelTimer.OnTimeUp += OnTimerUp;

            if (currentLevelIndex >= 0 && currentLevelIndex < levelDurations.Length)
                levelTimer.StartTimer(levelDurations[currentLevelIndex]);
            else
                levelTimer.StartTimer(60f);
        }

        isGameActive = true;
        isPopupShowing = false;
    }

    private void OnTimerUp()
    {
        if (!isGameActive) return;
        isGameActive = false;
        if (SFXManager.Instance != null) SFXManager.Instance.PlaySFX(timeUpSfx);
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

    protected override void OnDestroy()
    {
        if (levelTimer != null)
            levelTimer.OnTimeUp -= OnTimerUp;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
