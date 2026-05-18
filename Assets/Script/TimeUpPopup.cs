using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TimeUpPopup : MonoBehaviour
{
    public TextMeshProUGUI messageText;      // "Waktu Habis!"
    public TextMeshProUGUI scoreText;        // "Score: X"
    public Button backButton;                // Tombol Kembali
    public Button continueButton;            // Tombol Continue

    private int currentLevelIndex;
    private string[] sceneNames;

    public void Setup(int finalCoin, int levelIndex, string[] scenes)
    {
        currentLevelIndex = levelIndex;
        sceneNames = scenes;

        if (scoreText != null)
            scoreText.text = $"Score: {finalCoin}";

        // Pasang listener tombol
        backButton.onClick.AddListener(() => {
            GameManager.Instance.ReturnToMainMenu();
            Destroy(gameObject);
        });

        continueButton.onClick.AddListener(() => {
            // SEMENTARA DI-COMMENT: karena scene Beginner belum dibuat
            // GameManager.Instance.GoToNextLevel();
            Debug.Log("Tombol Continue ditekan. (Fitur lanjut ke level berikutnya masih di-comment)");
            // Destroy(gameObject); // Uncomment jika ingin popup hilang
        });
    }
}