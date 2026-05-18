using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PopupResultO2 : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI messageText;
    public Button backButton;
    public Image backgroundPanel;
    public Image udangImage;

    [Header("Warna (Berhasil/Gagal)")]
    public Color successColor = new Color(194f/255f, 255f/255f, 207f/255f);
    public Color failColor = new Color(254f/255f, 165f/255f, 171f/255f); 

    [Header("Sprite Udang")]
    public Sprite udangSenangSprite;
    public Sprite udangSedihSprite;

    private System.Action onBack;

    public void Setup(bool isSuccess, System.Action onBackCallback)
    {
        onBack = onBackCallback;

        if (isSuccess)
        {
            messageText.text = "Udang bernafas dengan lega!\nOksigen cukup :>";
            if (backgroundPanel != null)
                backgroundPanel.color = successColor;
            if (udangImage != null && udangSenangSprite != null)
                udangImage.sprite = udangSenangSprite;
        }
        else
        {
            messageText.text = "Udang anda mati karena kekurangan oksigen. Huehueheuue sedih wak :C";
            if (backgroundPanel != null)
                backgroundPanel.color = failColor;
            if (udangImage != null && udangSedihSprite != null)
                udangImage.sprite = udangSedihSprite;
        }
        
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() => {
            Destroy(gameObject);
            onBack?.Invoke();
        });
    }
}