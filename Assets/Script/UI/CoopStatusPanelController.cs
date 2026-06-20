using UnityEngine;
using System.Collections.Generic;

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
        FindKandangSlots();
        CreateRows();
        SubscribeToEvents();
        RefreshAll();
    }

    private void FindKandangSlots()
    {
        kandangSlots = FindObjectsByType<StarterKandangSlot>(FindObjectsSortMode.None);
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
            bool isFailed = slot.NeedFailed[i];
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
