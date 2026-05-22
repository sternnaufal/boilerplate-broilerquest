using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UIManager");
                _instance = go.AddComponent<UIManager>();
            }
            return _instance;
        }
    }

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public GameObject pausePanel;
    public GameObject hudPanel; // HUD during gameplay

    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button optionsButton;
    public Button exitButton;

    [Header("Options Menu")]
    public Button backToMainButton;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Pause Menu")]
    public Button resumeButton;
    public Button pauseOptionsButton;
    public Button pauseMainMenuButton;

    [Header("HUD")]
    public Button pauseButton;

    [RuntimeInitializeOnLoadMethod]
    private static void Bootstrap()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UIManager");
            _instance = go.AddComponent<UIManager>();
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeUI();
    }

    private void InitializeUI()
    {
        CreateMainMenuUI();
        ShowMainMenu();
    }

    private void CreateMainMenuUI()
    {
        // Create Camera if not exists
        if (Camera.main == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.5f, 0.8f, 0.5f); // hijau muda tema peternakan
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0, 0, -10);
        }

        // Create EventSystem if not exists
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Create Canvas if not exists
        GameObject canvasObj = GameObject.Find("Canvas");
        Canvas canvas;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // Create UIManager object if not exists (this gameObject)
        if (this.gameObject.name != "UIManager")
        {
            this.gameObject.name = "UIManager";
        }

        // Create panels
        mainMenuPanel = CreatePanel("MainMenuPanel", canvasObj.transform);
        optionsPanel = CreatePanel("OptionsPanel", canvasObj.transform);
        pausePanel = CreatePanel("PausePanel", canvasObj.transform);
        hudPanel = CreatePanel("HUDPanel", canvasObj.transform);

        // Setup Main Menu
        SetupMainMenu(mainMenuPanel.transform);
        // Setup Options Menu
        SetupOptionsMenu(optionsPanel.transform);
        // Setup Pause Menu
        SetupPauseMenu(pausePanel.transform);
        // Setup HUD
        SetupHUD(hudPanel.transform);

        // Hide all panels initially
        HideAllPanels();
    }

    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private void SetupButtonListeners()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
        if (optionsButton != null)
            optionsButton.onClick.AddListener(ShowOptions);
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
        if (backToMainButton != null)
            backToMainButton.onClick.AddListener(ShowMainMenu);
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        if (pauseOptionsButton != null)
            pauseOptionsButton.onClick.AddListener(ShowOptionsFromPause);
        if (pauseMainMenuButton != null)
            pauseMainMenuButton.onClick.AddListener(ReturnToMainMenuFromPause);
        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    private void SetupMainMenu(Transform panelTransform)
    {
        // Title
        CreateUIText(panelTransform, "Main Title", "BoilerQuest", 48, new Vector2(0, 200), TextAnchor.MiddleCenter);

        // Start Button
        startButton = CreateUIButton(panelTransform, "Start Button", "Start Game", new Vector2(0, 0), new Vector2(200, 50), onClick: StartGame);

        // Options Button
        optionsButton = CreateUIButton(panelTransform, "Options Button", "Options", new Vector2(0, -80), new Vector2(200, 50), onClick: ShowOptions);

        // Exit Button
        exitButton = CreateUIButton(panelTransform, "Exit Button", "Exit", new Vector2(0, -160), new Vector2(200, 50), onClick: ExitGame);
    }

    private void SetupOptionsMenu(Transform panelTransform)
    {
        // Title
        CreateUIText(panelTransform, "Options Title", "Options", 36, new Vector2(0, 200), TextAnchor.MiddleCenter);

        // Music Volume
        CreateUIText(panelTransform, "MusicLabel", "Music Volume", 24, new Vector2(-200, 100), TextAnchor.MiddleLeft);
        musicVolumeSlider = CreateUISlider(panelTransform, "MusicSlider", new Vector2(0, 100), new Vector2(200, 20));
        musicVolumeSlider.minValue = 0f;
        musicVolumeSlider.maxValue = 1f;
        musicVolumeSlider.value = 0.8f;

        // SFX Volume
        CreateUIText(panelTransform, "SFXLabel", "SFX Volume", 24, new Vector2(-200, 0), TextAnchor.MiddleLeft);
        sfxVolumeSlider = CreateUISlider(panelTransform, "SFXSlider", new Vector2(0, 0), new Vector2(200, 20));
        sfxVolumeSlider.minValue = 0f;
        sfxVolumeSlider.maxValue = 1f;
        sfxVolumeSlider.value = 0.8f;

        // Back Button
        backToMainButton = CreateUIButton(panelTransform, "BackButton", "Back", new Vector2(0, -160), new Vector2(200, 50), onClick: ShowMainMenu);
    }

    private void SetupPauseMenu(Transform panelTransform)
    {
        // Title
        CreateUIText(panelTransform, "PauseTitle", "Paused", 36, new Vector2(0, 200), TextAnchor.MiddleCenter);

        // Resume Button
        resumeButton = CreateUIButton(panelTransform, "ResumeButton", "Resume", new Vector2(0, 0), new Vector2(200, 50), onClick: ResumeGame);

        // Options Button
        pauseOptionsButton = CreateUIButton(panelTransform, "PauseOptionsButton", "Options", new Vector2(0, -80), new Vector2(200, 50), onClick: ShowOptionsFromPause);

        // Main Menu Button
        pauseMainMenuButton = CreateUIButton(panelTransform, "PauseMainMenuButton", "Main Menu", new Vector2(0, -160), new Vector2(200, 50), onClick: ReturnToMainMenuFromPause);
    }

    private void SetupHUD(Transform panelTransform)
    {
        // Pause Button (top-right corner)
        pauseButton = CreateUIButton(panelTransform, "PauseButton", "||", new Vector2(180, 180), new Vector2(60, 60), onClick: PauseGame);
        // Optionally, you can add coin display, timer, etc. here
    }

    private void CreateUIText(Transform parent, string name, string text, int fontSize, Vector2 anchoredPosition, TextAnchor alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text uiText = go.AddComponent<Text>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(400, 50);

        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = Color.white;
        uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private Button CreateUIButton(Transform parent, string name, string buttonText, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Button btn = go.AddComponent<Button>();
        if (onClick != null)
            btn.onClick.AddListener(onClick);

        // Button label
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        Text labelText = labelGo.AddComponent<Text>();
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        labelText.text = buttonText;
        labelText.fontSize = 24;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;

        return btn;
    }

    private Slider CreateUISlider(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Slider slider = go.AddComponent<Slider>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        // Background
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgImg.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillGo = new GameObject("Fill Area");
        fillGo.transform.SetParent(go.transform, false);
        Image fillImg = fillGo.AddComponent<Image>();
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillImg.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        slider.fillRect = fillRt;

        // Handle
        GameObject handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(go.transform, false);
        Image handleImg = handleGo.AddComponent<Image>();
        RectTransform handleRt = handleGo.GetComponent<RectTransform>();
        handleImg.color = Color.white;
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.sizeDelta = new Vector2(20, 0);
        slider.handleRect = handleRt;

        return slider;
    }

    private void StartGame()
    {
        HideAllPanels();
        // Reset game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameActive(true);
            // Optionally re-initialize the scene for a fresh start
            GameManager.Instance.InitializeForCurrentScene();
        }
        hudPanel.SetActive(true);
    }

    private void ShowOptions()
    {
        HideAllPanels();
        optionsPanel.SetActive(true);
    }

    private void ShowOptionsFromPause()
    {
        HideAllPanels();
        optionsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        mainMenuPanel.SetActive(true);
        Time.timeScale = 1f; // Ensure time is normal
    }

    private void PauseGame()
    {
        HideAllPanels();
        pausePanel.SetActive(true);
        Time.timeScale = 0f; // Pause game
    }

    private void ResumeGame()
    {
        HideAllPanels();
        hudPanel.SetActive(true);
        Time.timeScale = 1f; // Resume game
    }

    private void ReturnToMainMenuFromPause()
    {
        HideAllPanels();
        mainMenuPanel.SetActive(true);
        Time.timeScale = 1f;
        // Reset game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    private void SetMusicVolume(float volume)
    {
        // Implement your music volume setting here
        AudioListener.volume = volume; // Simple implementation
    }

    private void SetSFXVolume(float volume)
    {
        // Implement your SFX volume setting here
        // This would typically affect your audio mixer or individual audio sources
    }

    private void Update()
    {
        // Allow pausing with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (hudPanel.activeSelf && Time.timeScale > 0)
            {
                PauseGame();
            }
            else if (pausePanel.activeSelf)
            {
                ResumeGame();
            }
        }
    }
}