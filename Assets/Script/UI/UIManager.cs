using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject hudPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button koleksiIoTButton;

    [Header("Options Menu")]
    [SerializeField] private Button backToMainButton;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("SFX")]
    [SerializeField] private AudioClip navigateSfx;

    [Header("Pause Menu")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseOptionsButton;
    [SerializeField] private Button pauseMainMenuButton;

    [Header("HUD")]
    [SerializeField] private Button pauseButton;

    private bool openedFromPause;
    private bool buttonsRegistered;

    private void PlayNavSfx()
    {
        if (SFXManager.Instance != null) SFXManager.Instance.PlaySFX(navigateSfx);
    }

    protected override bool PersistAcrossScenes => false;

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

        ButtonHelper.AddListenerOnce(startButton, () => { PlayNavSfx(); StartGame(); });
        ButtonHelper.AddListenerOnce(optionsButton, () => { PlayNavSfx(); ShowOptionsFromMain(); });
        ButtonHelper.AddListenerOnce(exitButton, () => { PlayNavSfx(); ExitGame(); });
        ButtonHelper.AddListenerOnce(koleksiIoTButton, () => { PlayNavSfx(); GoToKoleksiIoT(); });
        ButtonHelper.AddListenerOnce(backToMainButton, () => { PlayNavSfx(); BackFromOptions(); });
        ButtonHelper.AddListenerOnce(resumeButton, () => { PlayNavSfx(); ResumeGame(); });
        ButtonHelper.AddListenerOnce(pauseOptionsButton, () => { PlayNavSfx(); ShowOptionsFromPause(); });
        ButtonHelper.AddListenerOnce(pauseMainMenuButton, () => { PlayNavSfx(); ReturnToMainMenuFromPause(); });
        ButtonHelper.AddListenerOnce(pauseButton, () => { PlayNavSfx(); PauseGame(); });

        ButtonHelper.AddListenerOnce(musicVolumeSlider, SetMusicVolume);
        ButtonHelper.AddListenerOnce(sfxVolumeSlider, SetSfxVolume);

        buttonsRegistered = true;
    }

    public void ShowMainMenu()
    {
        GameStateManager.ApplyState(GameState.Menu);
        openedFromPause = false;

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
        GameStateManager.ApplyState(GameState.Paused);
        openedFromPause = true;

        HideAllPanels();

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        GameStateManager.ApplyState(GameState.Playing);
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
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        GameLog.Info($"SFX volume requested: {volume}. AudioMixer routing is not set up yet.");
#endif
    }

    public void GoToKoleksiIoT()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.GoToKoleksiIoT();
        }
    }

    public void ExitGame()
    {
        GameStateManager.ApplyState(GameState.Menu);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
