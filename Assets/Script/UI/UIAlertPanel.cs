using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIAlertPanel : MonoBehaviour
{
    public static UIAlertPanel Instance { get; private set; }

    [Header("Notification Panels (Drag from Hierarchy)")]
    [SerializeField] private GameObject foodOutPanel;      // PakanHabis
    [SerializeField] private GameObject coinOutPanel;      // DuidHabis
    [SerializeField] private GameObject timeOutPanel;      // WaktuHabis
    [SerializeField] private GameObject mainMenuConfirmPanel; // MainMenu

    [Header("Auto-hide Duration")]
    [SerializeField] private float autoHideDelay = 3f;

    private Coroutine autoHideCoroutine;

    public enum NotificationType
    {
        FoodOut,
        CoinOut,
        TimeOut,
        MainMenuConfirm
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetAllPanelsActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void SetAllPanelsActive(bool active)
    {
        if (foodOutPanel != null) foodOutPanel.SetActive(active);
        if (coinOutPanel != null) coinOutPanel.SetActive(active);
        if (timeOutPanel != null) timeOutPanel.SetActive(active);
        if (mainMenuConfirmPanel != null) mainMenuConfirmPanel.SetActive(active);
    }

    private void EnsureParentActive(GameObject panel)
    {
        if (panel == null) return;
        if (panel.activeInHierarchy) return;

        Transform p = panel.transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf)
                p.gameObject.SetActive(true);
            p = p.parent;
        }
    }

    public void Show(NotificationType type, System.Action onConfirm = null, System.Action onBack = null)
    {
        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);
        SetAllPanelsActive(false);

        switch (type)
        {
            case NotificationType.FoodOut:
                if (foodOutPanel != null)
                {
                    EnsureParentActive(foodOutPanel);
                    foodOutPanel.SetActive(true);
                    autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(foodOutPanel));
                }
                break;
            case NotificationType.CoinOut:
                if (coinOutPanel != null)
                {
                    EnsureParentActive(coinOutPanel);
                    coinOutPanel.SetActive(true);
                    autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(coinOutPanel));
                }
                break;
            case NotificationType.TimeOut:
                if (timeOutPanel != null)
                {
                    EnsureParentActive(timeOutPanel);
                    timeOutPanel.SetActive(true);
                    autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(timeOutPanel));
                }
                break;
            case NotificationType.MainMenuConfirm:
                if (mainMenuConfirmPanel != null)
                {
                    EnsureParentActive(mainMenuConfirmPanel);
                    mainMenuConfirmPanel.SetActive(true);
                    SetupMainMenuButtons(onConfirm, onBack);
                }
                break;
        }
    }

    private IEnumerator AutoHideAfterDelay(GameObject panel)
    {
        yield return new WaitForSeconds(autoHideDelay);
        if (panel != null) panel.SetActive(false);
        autoHideCoroutine = null;
    }

    private void SetupMainMenuButtons(System.Action onConfirm, System.Action onBack = null)
    {
        if (mainMenuConfirmPanel == null) return;

        Button kembali = FindButtonInChildren(mainMenuConfirmPanel.transform, "KembaliBut");
        Button lanjutkan = FindButtonInChildren(mainMenuConfirmPanel.transform, "LanjutkanBut");

        if (kembali != null)
        {
            kembali.onClick.RemoveAllListeners();
            kembali.onClick.AddListener(() =>
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                HideMainMenuConfirm();
                onBack?.Invoke();
            });
        }
        if (lanjutkan != null)
        {
            lanjutkan.onClick.RemoveAllListeners();
            lanjutkan.onClick.AddListener(() =>
            {
                if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
                HideMainMenuConfirm();
                onConfirm?.Invoke();
            });
        }
    }

    private Button FindButtonInChildren(Transform parent, string buttonName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == buttonName)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null) return btn;
            }
            Button found = FindButtonInChildren(child, buttonName);
            if (found != null) return found;
        }
        return null;
    }

    private void HideMainMenuConfirm()
    {
        if (mainMenuConfirmPanel != null)
            mainMenuConfirmPanel.SetActive(false);
    }

    public void HideAll()
    {
        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);
        SetAllPanelsActive(false);
    }
}