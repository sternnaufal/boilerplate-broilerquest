using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CoopStatusRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI kandangLabel;
    [SerializeField] private List<Image> needIcons;

    public void SetKandangLabel(string text)
    {
        if (kandangLabel != null)
            kandangLabel.text = text;
    }

    public void SetNeedIcons(List<Sprite> icons, List<bool> failedStates)
    {
        if (needIcons == null) return;

        for (int i = 0; i < needIcons.Count; i++)
        {
            if (i < icons.Count && icons[i] != null)
            {
                SetIcon(needIcons[i], icons[i]);
                needIcons[i].color = (failedStates != null && i < failedStates.Count && failedStates[i]) ? Color.gray : Color.white;
                needIcons[i].gameObject.SetActive(true);
            }
            else
            {
                needIcons[i].gameObject.SetActive(false);
            }
        }
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
