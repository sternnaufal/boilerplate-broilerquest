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

    [Header("Exit Confirmation (manual UI)")]
    [SerializeField] private GameObject exitConfirmPanel;
    [SerializeField] private Button exitYesButton;
    [SerializeField] private Button exitNoButton;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button koleksiIoTButton;

    [Header("Options Menu")]
    [SerializeField] private Button backToMainButton;
    [SerializeField] private Button resetDataButton;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Reset Confirmation (MainMenu fallback)")]
    [SerializeField] private GameObject resetConfirmationPanel;

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
        if (SFXManager.Instance != null) SFXManager.Instance.PlayNavigate();
    }

    protected override bool PersistAcrossScenes => false;

    private void OnEnable()
    {
        RegisterButtonListeners();
    }

    private void Start()
    {
        if (BGMManager.Instance != null)
            BGMManager.Instance.PlayMainMenuBGM();
        ShowMainMenu();
    }

    private void RegisterButtonListeners()
    {
        if (buttonsRegistered)
            return;

        ButtonHelper.AddListenerOnce(startButton, () => { PlayNavSfx(); StartGame(); });
        ButtonHelper.AddListenerOnce(optionsButton, () => { PlayNavSfx(); ShowOptionsFromMain(); });
        ButtonHelper.AddListenerOnce(exitButton, () => { PlayNavSfx(); RequestExit(); });
        ButtonHelper.AddListenerOnce(koleksiIoTButton, () => { PlayNavSfx(); GoToKoleksiIoT(); });
        ButtonHelper.SetSingleListener(backToMainButton, () => { PlayNavSfx(); BackFromOptions(); });
        ButtonHelper.AddListenerOnce(resumeButton, () => { PlayNavSfx(); ResumeGame(); });
        ButtonHelper.AddListenerOnce(pauseOptionsButton, () => { PlayNavSfx(); ShowOptionsFromPause(); });
        ButtonHelper.AddListenerOnce(pauseMainMenuButton, () => { PlayNavSfx(); ReturnToMainMenuFromPause(); });
        ButtonHelper.AddListenerOnce(pauseButton, () => { PlayNavSfx(); PauseGame(); });

        ButtonHelper.AddListenerOnce(musicVolumeSlider, SetMusicVolume);
        ButtonHelper.AddListenerOnce(sfxVolumeSlider, SetSfxVolume);

        ButtonHelper.AddListenerOnce(resetDataButton, () => { PlayNavSfx(); ResetUserData(); });

        ButtonHelper.AddListenerOnce(exitYesButton, () => { PlayNavSfx(); ConfirmExitYes(); });
        ButtonHelper.AddListenerOnce(exitNoButton, () => { PlayNavSfx(); ConfirmExitNo(); });

        buttonsRegistered = true;
    }

    public void ShowMainMenu()
    {
        GameStateManager.ApplyState(GameState.Menu);
        openedFromPause = false;

        HideAllPanels();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        HideExitConfirmPanel();
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

        if (optionsPanel != null)
            optionsPanel.SetActive(true);

        if (resetConfirmationPanel != null)
            resetConfirmationPanel.SetActive(false);
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
        if (pausePanel != null) pausePanel.SetActive(false);

        System.Action doReturn = () =>
        {
            SaveManager.SaveAll();
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMainMenu();
            else
                SceneController.Instance?.GoToMainMenu();
        };

        var alert = UIAlertPanel.Instance ?? FindFirstObjectByType<UIAlertPanel>();
        if (alert == null) { doReturn(); return; }

        alert.Show(UIAlertPanel.NotificationType.MainMenuConfirm,
            onConfirm: doReturn,
            onBack: () =>
            {
                if (pausePanel != null) pausePanel.SetActive(true);
            });
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

        if (resetConfirmationPanel != null)
            resetConfirmationPanel.SetActive(false);
    }

    public void SetMusicVolume(float volume)
    {
        if (BGMManager.Instance != null)
            BGMManager.Instance.SetVolume(volume);
    }

    public void SetSfxVolume(float volume)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.SetVolume(volume);
    }

    public void GoToKoleksiIoT()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.GoToKoleksiIoT();
        }
    }

    public void RequestExit()
    {
        GameStateManager.ApplyState(GameState.Menu);

        if (exitConfirmPanel == null)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return;
        }

        exitConfirmPanel.SetActive(true);
    }

    private void HideExitConfirmPanel()
    {
        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(false);
    }

    private void ConfirmExitYes()
    {
        SaveManager.ResetDataForDemoMode();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ConfirmExitNo()
    {
        HideExitConfirmPanel();

        // Requirement: kalau tidak, kembali di main menu lagi.
        if (SceneController.Instance != null)
            SceneController.Instance.GoToMainMenu();
        else
            ShowMainMenu();
    }

    private void ResetUserData()
    {
        var alert = UIAlertPanel.Instance ?? FindFirstObjectByType<UIAlertPanel>();
        if (alert != null)
        {
            alert.Show(UIAlertPanel.NotificationType.ResetDataConfirm,
                onConfirm: () =>
                {
                    SaveManager.ResetAllData();
                    ShowMainMenu();
                },
                onBack: null);
            return;
        }

        // Fallback: ResetConfirmationPanel langsung (MainMenu scene)
        if (resetConfirmationPanel != null)
        {
            resetConfirmationPanel.SetActive(true);

            Button tidakBtn = FindButtonInChildren(resetConfirmationPanel.transform, "TidakButton");
            Button yaBtn = FindButtonInChildren(resetConfirmationPanel.transform, "YaButton");

            if (tidakBtn != null)
            {
                tidakBtn.onClick.RemoveAllListeners();
                tidakBtn.onClick.AddListener(() =>
                {
                    resetConfirmationPanel.SetActive(false);
                });
            }

            if (yaBtn != null)
            {
                yaBtn.onClick.RemoveAllListeners();
                yaBtn.onClick.AddListener(() =>
                {
                    resetConfirmationPanel.SetActive(false);
                    SaveManager.ResetAllData();
                    ShowMainMenu();
                });
            }

            return;
        }

        SaveManager.ResetAllData();
        ShowMainMenu();
    }

    private Button FindButtonInChildren(Transform parent, string name)
    {
        // ponytail: duplicate of UIAlertPanel.FindButtonInChildren
        // extract to ButtonHelper if a 3rd caller appears
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null) return btn;
            }
            Button found = FindButtonInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (exitConfirmPanel != null && exitConfirmPanel.activeSelf)
        {
            ConfirmExitNo();
            return;
        }

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
