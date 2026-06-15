using System;
using UnityEngine;

public class StarterSceneInitializer : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private StarterChickenShop chickenShop;
    [SerializeField] private StarterKandangSlot[] kandangSlots;

    [Header("Initialization")]
    [SerializeField] private bool initializeGameManager = true;
    [SerializeField] private bool initializeCoinManager = true;
    [SerializeField] private bool loadSavedState = true;

    private void Start()
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
                SaveManager.LoadAndRestoreSlots(kandangSlots, GetChickenPrefab);
                chickenShop.RefreshShopState();
            }
        }

        if (loadSavedState && StarterIoTController.Instance != null)
            SaveManager.LoadIotStates(StarterIoTController.Instance);
    }

    private GameObject GetChickenPrefab(string prefabName)
    {
        if (chickenShop != null)
            return chickenShop.GetChickenPrefabByName(prefabName);
        return null;
    }
}
