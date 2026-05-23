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

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => {
                if (GameManager.Instance != null)
                    GameManager.Instance.ReturnToMainMenu();

                Destroy(gameObject);
            });
        }

        bool canContinue = HasLoadableNextLevel();
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.interactable = canContinue;

            if (canContinue)
            {
                continueButton.onClick.AddListener(() => {
                    if (GameManager.Instance != null)
                        GameManager.Instance.GoToNextLevel();

                    Destroy(gameObject);
                });
            }
        }
    }

    private bool HasLoadableNextLevel()
    {
        int nextLevelIndex = currentLevelIndex + 1;
        return sceneNames != null
            && nextLevelIndex >= 0
            && nextLevelIndex < sceneNames.Length
            && Application.CanStreamedLevelBeLoaded(sceneNames[nextLevelIndex]);
    }
}
