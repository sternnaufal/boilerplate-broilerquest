using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public partial class StarterKandangSlot
{
    private GameObject CreateChickenVisual(GameObject chickenPrefab)
    {
        Transform parent = chickenParent != null ? chickenParent : transform;

        GameObject prefabToInstantiate = chickenPrefab != null ? chickenPrefab : chickenVisual;
        if (prefabToInstantiate == null)
            return null;

        GameObject visual = Instantiate(prefabToInstantiate, parent);
        visual.SetActive(true);
        spawnedChickens.Add(visual);

        AssignChickenAnimator(visual);
        return visual;
    }

    private List<GameObject> GetActiveChickenVisuals()
    {
        spawnedChickens.RemoveAll(chicken => chicken == null);

        List<GameObject> visuals = new List<GameObject>();
        foreach (GameObject spawnedChicken in spawnedChickens)
        {
            if (spawnedChicken != null && spawnedChicken.activeSelf)
                visuals.Add(spawnedChicken);
        }

        return visuals;
    }

    private void PositionAllChickenVisuals()
    {
        List<GameObject> visuals = GetActiveChickenVisuals();
        int count = visuals.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 size = count > 1 ? packedChickenVisualSize : chickenVisualSize;
            Vector2 offset = count > 1 ? GetPackedChickenOffset(i, count) : chickenVisualOffset;
            PositionChickenVisual(visuals[i].transform, offset, size);
        }
    }

    private Vector2 GetPackedChickenOffset(int index, int count)
    {
        int columnCount = Mathf.Min(2, Mathf.Max(1, count));
        int rowCount = Mathf.CeilToInt(count / (float)columnCount);
        int row = index / columnCount;
        int column = index % columnCount;

        float x = (column - (columnCount - 1) * 0.5f) * packedChickenSpacing.x;
        float y = ((rowCount - 1) * 0.5f - row) * packedChickenSpacing.y;
        return chickenVisualOffset + new Vector2(x, y - 10f);
    }

    private void PositionChickenVisual(Transform visual, Vector2 anchoredOffset, Vector2 visualSize)
    {
        if (visual == null)
            return;

        visual.SetAsLastSibling();
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;

        RectTransform rect = visual as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = visualSize;
            rect.anchoredPosition = anchoredOffset;
        }
        else
        {
            visual.localPosition = new Vector3(anchoredOffset.x, anchoredOffset.y, 0f);
        }

        Image[] images = visual.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
            image.raycastTarget = false;

        TextMeshProUGUI[] labels = visual.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
            label.raycastTarget = false;
    }
}
