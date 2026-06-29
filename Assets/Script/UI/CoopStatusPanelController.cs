using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class CoopStatusPanelController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Row Prefab")]
    [SerializeField] private CoopStatusRowUI rowPrefab;
    [SerializeField] private RectTransform rowContainer;

    [Header("Need Icons")]
    [SerializeField] private Sprite iconFeedSukses;
    [SerializeField] private Sprite iconFeedGagal;
    [SerializeField] private Sprite iconCoolingSukses;
    [SerializeField] private Sprite iconCoolingGagal;
    [SerializeField] private Sprite iconHeatingSukses;
    [SerializeField] private Sprite iconHeatingGagal;
    [SerializeField] private Sprite iconHumidityUpSukses;
    [SerializeField] private Sprite iconHumidityUpGagal;
    [SerializeField] private Sprite iconHumidityDownSukses;
    [SerializeField] private Sprite iconHumidityDownGagal;
    [SerializeField] private Sprite iconAddDryHuskSukses;
    [SerializeField] private Sprite iconAddDryHuskGagal;
    [SerializeField] private Sprite iconReduceFeedSukses;
    [SerializeField] private Sprite iconReduceFeedGagal;

    private StarterKandangSlot[] kandangSlots;
    private CoopStatusRowUI[] rows;

    private void Start()
    {
        EnsureLayout();
        FindKandangSlots();
        CreateRows();
        SubscribeToEvents();
        RefreshAll();
    }

    private void EnsureLayout()
    {
        if (rowContainer == null) return;
        if (rowContainer.GetComponent<VerticalLayoutGroup>() != null) return;
        var vlg = rowContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.spacing = 4;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        var csf = rowContainer.GetComponent<ContentSizeFitter>();
        if (csf == null)
            csf = rowContainer.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void FindKandangSlots()
    {
        var allSlots = FindObjectsByType<StarterKandangSlot>(FindObjectsSortMode.None);
        System.Array.Sort(allSlots, (a, b) =>
            a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        kandangSlots = allSlots;
    }

    private void CreateRows()
    {
        if (rowPrefab == null || rowContainer == null)
            return;

        rows = new CoopStatusRowUI[kandangSlots.Length];

        for (int i = 0; i < kandangSlots.Length; i++)
        {
            CoopStatusRowUI row = Instantiate(rowPrefab, rowContainer);
            row.SetKandangLabel(kandangSlots[i].SlotLabel);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.anchoredPosition = Vector2.zero;
            rows[i] = row;
        }
    }

    private void SubscribeToEvents()
    {
        for (int i = 0; i < kandangSlots.Length; i++)
        {
            int index = i;
            kandangSlots[i].StateChanged += slot => OnSlotStateChanged(index);
        }
    }

    private void OnSlotStateChanged(int index)
    {
        UpdateRow(index);
    }

    public void RefreshAll()
    {
        for (int i = 0; i < rows.Length; i++)
            UpdateRow(i);
    }

    private void UpdateRow(int index)
    {
        if (index < 0 || index >= rows.Length || index >= kandangSlots.Length)
            return;

        StarterKandangSlot slot = kandangSlots[index];
        CoopStatusRowUI row = rows[index];

        if (!slot.IsOccupied)
        {
            row.SetActive(false);
            return;
        }

        row.SetActive(true);

        var icons = new List<Sprite>();
        var failedStates = new List<bool>();
        for (int i = 0; i < slot.NeedsQueue.Count; i++)
        {
            ChickenNeed need = slot.NeedsQueue[i];
            bool isFailed = !slot.NeedSatisfied[i];
            icons.Add(GetNeedIcon(need, isFailed));
            failedStates.Add(isFailed);
        }

        row.SetNeedIcons(icons, failedStates);
    }

    private Sprite GetNeedIcon(ChickenNeed need, bool isFailed)
    {
        if (isFailed)
        {
            switch (need)
            {
                case ChickenNeed.Feed: return iconFeedGagal;
                case ChickenNeed.Cooling: return iconCoolingGagal;
                case ChickenNeed.Heating: return iconHeatingGagal;
                case ChickenNeed.HumidityUp: return iconHumidityUpGagal;
                case ChickenNeed.HumidityDown: return iconHumidityDownGagal;
                case ChickenNeed.AddDryHusk: return iconAddDryHuskGagal;
                case ChickenNeed.ReduceFeed: return iconReduceFeedGagal;
            }
        }
        else
        {
            switch (need)
            {
                case ChickenNeed.Feed: return iconFeedSukses;
                case ChickenNeed.Cooling: return iconCoolingSukses;
                case ChickenNeed.Heating: return iconHeatingSukses;
                case ChickenNeed.HumidityUp: return iconHumidityUpSukses;
                case ChickenNeed.HumidityDown: return iconHumidityDownSukses;
                case ChickenNeed.AddDryHusk: return iconAddDryHuskSukses;
                case ChickenNeed.ReduceFeed: return iconReduceFeedSukses;
            }
        }

        return null;
    }
}
