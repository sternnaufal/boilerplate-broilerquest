using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Pengaturan Level")]
    public int currentLevelIndex = 0;
    public string[] sceneNames = { "Starter", "Beginner", "Intermediate" };
    public float[] levelDurations =
    {
        GameConstants.LevelDuration.Starter,
        GameConstants.LevelDuration.Beginner,
        GameConstants.LevelDuration.Intermediate
    };

    [Header("Timer UI")]
    public TextMeshProUGUI timerText;

    [Header("Popup Waktu Habis (Prefab)")]
    public GameObject timeUpPopupPrefab;
    public Canvas mainCanvas;

    private float timeRemaining;
    private bool isGameActive = true;
    private bool isPopupShowing = false;
    private Coroutine timerCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

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
        CoroutineHelper.StopSafe(this, ref timerCoroutine);

        // Cari main canvas
        if (mainCanvas == null)
            mainCanvas = FindFirstObjectByType<Canvas>();

        // Cari timer text (coba dengan tag, lalu fallback ke nama)
        if (timerText == null)
        {
            GameObject timerObj = null;
            // Coba dengan tag (tidak akan exception meski tag belum ada, karena kita cek null)
            try { timerObj = GameObject.FindGameObjectWithTag("TimerText"); } catch { }
            if (timerObj == null)
                timerObj = GameObject.Find("TimerText");
            if (timerObj != null)
                timerText = timerObj.GetComponent<TextMeshProUGUI>();
        }

        // Jika tidak ada timerText di scene (misal main menu), jalankan game tanpa timer
        if (timerText == null)
        {
            isGameActive = true;  // biarkan game jalan tanpa timer
            isPopupShowing = false;
            return;
        }

        // Ada timer, reset berdasarkan level
        if (currentLevelIndex >= 0 && currentLevelIndex < levelDurations.Length)
            timeRemaining = levelDurations[currentLevelIndex];
        else
            timeRemaining = 60f;

        isGameActive = true;
        isPopupShowing = false;
        UpdateTimerDisplay();
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    IEnumerator TimerCoroutine()
    {
        while (isGameActive && timeRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
            timeRemaining--;
            UpdateTimerDisplay();

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                UpdateTimerDisplay();
                TimeUp();
            }
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    void TimeUp()
    {
        if (!isGameActive) return;
        isGameActive = false;
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
        {
            SceneController.Instance.GoToMainMenu();
        }
    }

    public bool IsGameActive()
    {
        return isGameActive && !isPopupShowing;
    }

    /// <summary>
    /// Sets the game to active state (used when starting/resuming gameplay)
    /// </summary>
    public void SetGameActive(bool active)
    {
        isGameActive = active;

        if (active)
            isPopupShowing = false;
    }

    void OnDestroy()
    {
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
