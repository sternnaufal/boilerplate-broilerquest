using UnityEngine;

public class BeginnerSceneInitializer : MonoBehaviour
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
        LevelSceneInitializer.Initialize(
            chickenShop,
            kandangSlots,
            GetChickenPrefab,
            initializeGameManager,
            initializeCoinManager,
            loadSavedState
        );
    }

    private GameObject GetChickenPrefab(string prefabName)
    {
        if (chickenShop != null)
            return chickenShop.GetChickenPrefabByName(prefabName);
        return null;
    }
}
