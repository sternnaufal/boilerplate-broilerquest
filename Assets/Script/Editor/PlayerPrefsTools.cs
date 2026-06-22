#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class PlayerPrefsTools : EditorWindow
{
    private const int TestCoinAmount = 50000;

    [MenuItem("Tools/BroilerQuest/Clear All Save Data (PlayerPrefs)")]
    public static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("<b>[BroilerQuest]</b> All PlayerPrefs and Save Data cleared successfully!");
    }

    [MenuItem("Tools/BroilerQuest/Grant 50000 Test Coins")]
    public static void GrantTestCoins()
    {
        PlayerPrefs.SetInt(GameConstants.Persistence.TotalCoinKey, TestCoinAmount);
        PlayerPrefs.Save();

        // Also update live CoinManager if in PlayMode
        if (EditorApplication.isPlaying && CoinManager.Instance != null)
        {
            CoinManager.Instance.SetTotalCoin(TestCoinAmount);
            Debug.Log($"<b>[BroilerQuest]</b> Live CoinManager updated to {TestCoinAmount}.");
        }

        Debug.Log($"<b>[BroilerQuest]</b> {TestCoinAmount} coins granted! ({GameConstants.Persistence.TotalCoinKey})");
    }
}
#endif
