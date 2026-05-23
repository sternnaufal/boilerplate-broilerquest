using TMPro;
using UnityEngine;

public class StarterSceneInitializer : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private StarterChickenShop chickenShop;
    [SerializeField] private StarterKandangSlot[] kandangSlots;

    [Header("Initialization")]
    [SerializeField] private bool initializeGameManager = true;
    [SerializeField] private bool initializeCoinManager = true;

    private void Start()
    {
        if (initializeGameManager && GameManager.Instance != null)
            GameManager.Instance.InitializeForCurrentScene();

        if (initializeCoinManager && CoinManager.Instance != null)
            CoinManager.Instance.Initialize(coinText);

        if (chickenShop != null && kandangSlots != null && kandangSlots.Length > 0)
            chickenShop.SetKandangSlots(kandangSlots);
    }
}
