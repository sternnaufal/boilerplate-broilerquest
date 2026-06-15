using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoopStatusRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI kandangLabel;
    [SerializeField] private Image pakanIcon;
    [SerializeField] private Image panasIcon;
    [SerializeField] private Image dinginIcon;

    public void SetKandangLabel(string text)
    {
        if (kandangLabel != null)
            kandangLabel.text = text;
    }

    public void SetPakanIcon(Sprite sprite)
    {
        SetIcon(pakanIcon, sprite);
    }

    public void SetPanasIcon(Sprite sprite)
    {
        SetIcon(panasIcon, sprite);
    }

    public void SetDinginIcon(Sprite sprite)
    {
        SetIcon(dinginIcon, sprite);
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }
}
