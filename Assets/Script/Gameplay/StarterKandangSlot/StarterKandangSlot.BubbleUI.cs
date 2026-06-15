using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class StarterKandangSlot
{
    private void RefreshSlotLabel()
    {
        if (!autoNumberSlotLabel)
            return;

        if (slotLabel == null)
            slotLabel = FindSlotLabel();

        if (slotLabel == null)
            return;

        int slotNumber = GetSlotIndexInParent() + 1;
        slotLabel.text = string.Format(slotLabelFormat, slotNumber);
    }

    private TextMeshProUGUI FindSlotLabel()
    {
        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label != null && label.gameObject.name == "Kandang")
                return label;
        }

        return labels.Length > 0 ? labels[0] : null;
    }

    private int GetSlotIndexInParent()
    {
        if (transform.parent == null)
            return transform.GetSiblingIndex();

        int slotIndex = 0;
        for (int i = 0; i < transform.parent.childCount; i++)
        {
            Transform sibling = transform.parent.GetChild(i);
            StarterKandangSlot siblingSlot = sibling.GetComponent<StarterKandangSlot>();
            if (siblingSlot == null)
                continue;

            if (siblingSlot == this)
                return slotIndex;

            slotIndex++;
        }

        return transform.GetSiblingIndex();
    }

    private void ShowBubble(Sprite sprite, string label)
    {
        EnsureBubbleVisual();

        if (bubbleVisual != null)
            bubbleVisual.SetActive(true);

        if (bubbleImage != null)
        {
            bubbleImage.sprite = sprite;
            bubbleImage.color = Color.white;
            bubbleImage.enabled = true;
        }

        if (bubbleLabel != null)
        {
            if (label == feedBubbleText || label == coolingBubbleText || label == heatingBubbleText)
            {
                bubbleLabel.text = "";
                bubbleLabel.enabled = false;
            }
            else
            {
                bubbleLabel.text = label;
                bubbleLabel.enabled = !string.IsNullOrEmpty(label);
            }
        }
    }

    private void HideBubble()
    {
        if (bubbleVisual != null)
            bubbleVisual.SetActive(false);

        if (bubbleImage != null)
            bubbleImage.enabled = false;

        if (bubbleLabel != null)
            bubbleLabel.enabled = false;
    }

    private void EnsureBubbleVisual()
    {
        if (bubbleVisual == null)
            CreateFallbackBubble();

        if (bubbleVisual == null)
            return;

        if (bubbleImage == null)
            bubbleImage = bubbleVisual.GetComponent<Image>();

        if (bubbleLabel == null)
            bubbleLabel = bubbleVisual.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void CreateFallbackBubble()
    {
        GameObject bubbleObject = new GameObject("AutoBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bubbleObject.transform.SetParent(transform, false);

        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
        bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRect.pivot = new Vector2(0.5f, 0.5f);
        bubbleRect.sizeDelta = bubbleSize;
        bubbleRect.anchoredPosition = bubbleOffset;

        bubbleVisual = bubbleObject;
        bubbleImage = bubbleObject.GetComponent<Image>();
        bubbleImage.raycastTarget = false;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(bubbleObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        bubbleLabel = labelObject.GetComponent<TextMeshProUGUI>();
        bubbleLabel.alignment = TextAlignmentOptions.Center;
        bubbleLabel.fontSize = GameConstants.UI.BubbleLabelFontSize;
        bubbleLabel.fontStyle = FontStyles.Bold;
        bubbleLabel.color = new Color(0.12f, 0.16f, 0.10f, 1f);
        bubbleLabel.raycastTarget = false;
    }

    private void PrepareSlotHitbox()
    {
        Image slotImage = GetComponent<Image>();
        if (slotImage == null)
            return;

        slotImage.color = new Color(1f, 1f, 1f, 0f);
        slotImage.raycastTarget = true;
    }
}
