using System;
using UnityEngine;

public class StarterSceneInitializer : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private StarterChickenShop chickenShop;
    [SerializeField] private StarterKandangSlot[] kandangSlots;

    [Header("Initialization")]
    [SerializeField] private bool initializeCoinManager = true;
    [SerializeField] private bool loadSavedState = true;

    private void Start()
    {
        Debug.LogWarning($"[StarterSceneInitializer] Start — loadSavedState={loadSavedState}, chickenShop={chickenShop}, kandangSlots assigned={kandangSlots?.Length ?? 0}");

        if (initializeCoinManager && CoinManager.Instance != null)
            CoinManager.Instance.Initialize();

        if (kandangSlots == null || kandangSlots.Length == 0)
            kandangSlots = FindObjectsByType<StarterKandangSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        Debug.LogWarning($"[StarterSceneInitializer] Slots ditemukan: {kandangSlots?.Length ?? 0}");

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
    }

    private GameObject GetChickenPrefab(string prefabName)
    {
        if (chickenShop != null)
            return chickenShop.GetChickenPrefabByName(prefabName);
        return null;
    }
}
