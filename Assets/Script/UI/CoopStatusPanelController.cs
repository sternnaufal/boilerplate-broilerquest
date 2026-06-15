using UnityEngine;

public class CoopStatusPanelController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Row Prefab")]
    [SerializeField] private CoopStatusRowUI rowPrefab;
    [SerializeField] private RectTransform rowContainer;

    [Header("Ikon Pakan")]
    [SerializeField] private Sprite iconPakanSukses;
    [SerializeField] private Sprite iconPakanGagal;

    [Header("Ikon Suhu Dingin")]
    [SerializeField] private Sprite iconDinginSukses;
    [SerializeField] private Sprite iconDinginGagal;

    [Header("Ikon Suhu Panas")]
    [SerializeField] private Sprite iconPanasSukses;
    [SerializeField] private Sprite iconPanasGagal;

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
        row.SetPakanIcon(PickIcon(slot.GetFeedStatus(), iconPakanSukses, iconPakanGagal));
        row.SetPanasIcon(PickIcon(slot.GetHeatingStatus(), iconPanasSukses, iconPanasGagal));
        row.SetDinginIcon(PickIcon(slot.GetCoolingStatus(), iconDinginSukses, iconDinginGagal));
    }

    private static Sprite PickIcon(int status, Sprite sukses, Sprite gagal)
    {
        if (status == 1) return sukses;
        return gagal;
    }
}
