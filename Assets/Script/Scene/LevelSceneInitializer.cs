using System;
using UnityEngine;

public static class LevelSceneInitializer
{
    public static void Initialize(
        StarterChickenShop chickenShop,
        StarterKandangSlot[] kandangSlots,
        Func<string, GameObject> chickenPrefabLookup,
        bool initializeGameManager = true,
        bool initializeCoinManager = true,
        bool loadSavedState = true)
    {
        if (initializeGameManager && GameManager.Instance != null)
            GameManager.Instance.InitializeForCurrentScene();

        if (initializeCoinManager && CoinManager.Instance != null)
            CoinManager.Instance.Initialize();

        if (chickenShop != null && kandangSlots != null && kandangSlots.Length > 0)
        {
            chickenShop.SetKandangSlots(kandangSlots);

            if (loadSavedState)
            {
                SaveManager.LoadAndRestoreSlots(kandangSlots, chickenPrefabLookup);
                chickenShop.RefreshShopState();
            }
        }

        if (loadSavedState && StarterIoTController.Instance != null)
            SaveManager.LoadIotStates(StarterIoTController.Instance);
    }
}
