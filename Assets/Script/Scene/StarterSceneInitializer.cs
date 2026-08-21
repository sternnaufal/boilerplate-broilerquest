using System;
using UnityEngine;

public class StarterSceneInitializer : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private StarterChickenShop chickenShop;
    [SerializeField] private StarterKandangSlot[] kandangSlots;

    [Header("Tutorial")]
    [SerializeField] private StarterTutorialController tutorialController;

    [Header("Initialization")]
    [SerializeField] private bool initializeCoinManager = true;
    [SerializeField] private bool loadSavedState = true;

    private void Start()
    {
        if (initializeCoinManager && CoinManager.Instance != null)
            CoinManager.Instance.Initialize();

        if (kandangSlots == null || kandangSlots.Length == 0)
            kandangSlots = FindObjectsByType<StarterKandangSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (kandangSlots != null && kandangSlots.Length > 0)
        {
            if (chickenShop != null)
                chickenShop.SetKandangSlots(kandangSlots);

            if (loadSavedState)
            {
                SaveManager.LoadAndRestoreSlots(kandangSlots, GetChickenPrefab);
                if (chickenShop != null)
                    chickenShop.RefreshShopState();
            }
        }

        if (loadSavedState && StarterIoTController.Instance != null)
            SaveManager.LoadIotStates(StarterIoTController.Instance);

        if (BGMManager.Instance != null)
        {
            int level = GameManager.Instance != null ? GameManager.Instance.currentLevelIndex : 0;
            switch (level)
            {
                case 0: BGMManager.Instance.PlayStarterBGM(); break;
                case 1: BGMManager.Instance.PlayBeginnerBGM(); break;
                case 2: BGMManager.Instance.PlayIntermediateBGM(); break;
            }
        }

        InitTutorial();
    }

    private void InitTutorial()
    {
        if (tutorialController == null) return;

        bool needsTutorial = !tutorialController.IsTutorialDone;
        if (needsTutorial)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetDeferTimer(true);

            tutorialController.OnChickenBought += OnTutorialChickenBought;
            tutorialController.OnTutorialCompleted += OnTutorialCompleted;
            tutorialController.StartTutorial(chickenShop, kandangSlots);
        }
    }

    private void OnTutorialChickenBought()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartTimerForLevel();
    }

    private void OnTutorialCompleted()
    {
        if (tutorialController != null)
        {
            tutorialController.OnChickenBought -= OnTutorialChickenBought;
            tutorialController.OnTutorialCompleted -= OnTutorialCompleted;
        }
    }

    private GameObject GetChickenPrefab(string prefabName)
    {
        if (chickenShop != null)
            return chickenShop.GetChickenPrefabByName(prefabName);
        return null;
    }
}
