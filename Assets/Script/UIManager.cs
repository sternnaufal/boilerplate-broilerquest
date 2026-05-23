using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject hudPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button koleksiIoTButton;

    [Header("Options Menu")]
    [SerializeField] private Button backToMainButton;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Pause Menu")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseOptionsButton;
    [SerializeField] private Button pauseMainMenuButton;

    [Header("HUD")]
    [SerializeField] private Button pauseButton;

    private bool openedFromPause;
    private bool buttonsRegistered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        RegisterButtonListeners();
    }

    private void Start()
    {
        ShowMainMenu();
    }

    private void RegisterButtonListeners()
    {
        if (buttonsRegistered)
            return;

        RegisterButton(startButton, StartGame);
        RegisterButton(optionsButton, ShowOptionsFromMain);
        RegisterButton(koleksiIoTButton, GoToKoleksiIoT);
        RegisterButton(backToMainButton, BackFromOptions);
        RegisterButton(resumeButton, ResumeGame);
        RegisterButton(pauseOptionsButton, ShowOptionsFromPause);
        RegisterButton(pauseMainMenuButton, ReturnToMainMenuFromPause);
        RegisterButton(pauseButton, PauseGame);

        RegisterSlider(musicVolumeSlider, SetMusicVolume);
        RegisterSlider(sfxVolumeSlider, SetSfxVolume);

        buttonsRegistered = true;
    }

    private static void RegisterButton(Button button, UnityAction action)
    {
        if (button == null || button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.AddListener(action);
    }

    private static void RegisterSlider(Slider slider, UnityAction<float> action)
    {
        if (slider == null || slider.onValueChanged.GetPersistentEventCount() > 0)
            return;

        slider.onValueChanged.AddListener(action);
    }

    public void ShowMainMenu()
    {
        Time.timeScale = 1f;
        openedFromPause = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameActive(false);

        HideAllPanels();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void ShowMainScreen()
    {
        ShowMainMenu();
    }

    public void StartGame()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.GoToSelectLevel();
        }
    }
    
    public void PauseGame()
    {
        Time.timeScale = 0f;
        openedFromPause = true;

        HideAllPanels();

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        openedFromPause = false;

        HideAllPanels();

        if (hudPanel != null)
            hudPanel.SetActive(true);
    }

    public void ShowOptionsFromMain()
    {
        openedFromPause = false;

        HideAllPanels();

        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    public void ShowOptionsFromPause()
    {
        openedFromPause = true;

        HideAllPanels();

        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    public void BackFromOptions()
    {
        HideAllPanels();

        if (openedFromPause)
        {
            if (pausePanel != null)
                pausePanel.SetActive(true);
        }
        else if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    public void ReturnToMainMenuFromPause()
    {
        ShowMainMenu();

        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMainMenu();
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (hudPanel != null)
            hudPanel.SetActive(false);
    }

    public void SetMusicVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void SetSfxVolume(float volume)
    {
        // Hook this to an AudioMixer group when SFX routing exists.
    }

    public void GoToKoleksiIoT()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.GoToKoleksiIoT();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (pausePanel != null && pausePanel.activeSelf)
        {
            ResumeGame();
        }
        else if (hudPanel != null && hudPanel.activeSelf)
        {
            PauseGame();
        }
    }
}
