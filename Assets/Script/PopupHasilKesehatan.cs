using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PopupHasilKesehatan : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI messageText;
    public Button backButton;
    public Image backgroundPanel;
    public Image ayamImage;

    [Header("Warna (Berhasil/Gagal)")]
    public Color successColor = new Color(194f/255f, 255f/255f, 207f/255f);
    public Color failColor = new Color(254f/255f, 165f/255f, 171f/255f); 

    [Header("Sprite Ayam")]
    public Sprite ayamSehatSprite;
    public Sprite ayamSakitSprite;

    private System.Action onBack;

    public void Setup(bool isSuccess, System.Action onBackCallback)
    {
        onBack = onBackCallback;

        if (isSuccess)
        {
            messageText.text = "Ayam sehat dan kuat!\nVitamin cukup :>";
            if (backgroundPanel != null)
                backgroundPanel.color = successColor;
            if (ayamImage != null && ayamSehatSprite != null)
                ayamImage.sprite = ayamSehatSprite;
        }
        else
        {
            messageText.text = "Ayam anda sakit karena kurang vitamin. Yuk jaga kesehatannya!";
            if (backgroundPanel != null)
                backgroundPanel.color = failColor;
            if (ayamImage != null && ayamSakitSprite != null)
                ayamImage.sprite = ayamSakitSprite;
        }
        
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() => {
            Destroy(gameObject);
            onBack?.Invoke();
        });
    }
}