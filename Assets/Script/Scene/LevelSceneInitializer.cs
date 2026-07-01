using UnityEngine;

public class LevelSceneInitializer : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private StarterChickenShop chickenShop;

    [Header("Settings")]
    [SerializeField] private bool initializeCoinManager = true;
    [SerializeField] private bool loadSavedState = true;

    private void Start()
    {
        Debug.LogWarning($"[LevelSceneInitializer] Start — loadSavedState={loadSavedState}, chickenShop={chickenShop}");

        if (initializeCoinManager && CoinManager.Instance != null)
            CoinManager.Instance.Initialize();

        StarterKandangSlot[] kandangSlots = FindObjectsByType<StarterKandangSlot>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (kandangSlots.Length == 0)
            return;

        if (loadSavedState)
            SaveManager.LoadAndRestoreSlots(kandangSlots, GetChickenPrefab);

        if (chickenShop != null)
        {
            chickenShop.SetKandangSlots(kandangSlots);
            chickenShop.RefreshShopState();
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