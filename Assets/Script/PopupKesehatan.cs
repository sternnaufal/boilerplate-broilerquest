using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PopupKesehatan : Singleton<PopupKesehatan>
{
    [Header("UI References")]
    public GameObject popupPanel;
    public Button offOnButton;
    public TextMeshProUGUI buttonText;
    public GameObject timingPanel;
    public RectTransform barCursor;
    public RectTransform barBackground;
    public RectTransform greenZone;
    public Button stopButton;

    [Header("Timing Settings")]
    public float moveSpeed = 300f;
    public float greenZoneWidth = 100f;

    [Header("Popup Result Prefab")]
    public GameObject popupResultPrefab;   // Assign prefab PopupHasilKesehatan

    private bool isOn = false;
    private bool isPlaying = false;
    private float cursorPosX;
    private float leftBound, rightBound;
    private int direction = 1;
    private IHealthCheckListener currentListener;
    private bool isStopped = false; // mencegah double stop

    protected override void Awake()
    {
        base.Awake();
        popupPanel.SetActive(false);
    }

    void Start()
    {
        ButtonHelper.AddListenerOnce(offOnButton, ToggleOffOn);
        ButtonHelper.AddListenerOnce(stopButton, OnStopClicked);
        stopButton.interactable = false;
        timingPanel.SetActive(false);

        if (barBackground != null && barCursor != null)
        {
            float bgLeft = barBackground.anchoredPosition.x - (barBackground.rect.width / 2f);
            float bgRight = barBackground.anchoredPosition.x + (barBackground.rect.width / 2f);
            leftBound = bgLeft;
            rightBound = bgRight;
            cursorPosX = leftBound;
            barCursor.anchoredPosition = new Vector2(cursorPosX, barCursor.anchoredPosition.y);
        }
        else
        {
            Debug.LogError("PopupKesehatan: barBackground atau barCursor tidak di-assign!");
        }
    }

    public void TampilkanPopup(KandangController kandang)
    {
        ShowHealthCheck(kandang);
    }

    public void ShowHealthCheck(IHealthCheckListener listener)
    {
        currentListener = listener;
        isOn = false;
        isPlaying = false;
        isStopped = false;
        buttonText.text = "OFF";
        timingPanel.SetActive(false);
        stopButton.interactable = false;
        popupPanel.SetActive(true);
        cursorPosX = leftBound;
        barCursor.anchoredPosition = new Vector2(cursorPosX, barCursor.anchoredPosition.y);
    }

    void ToggleOffOn()
    {
        if (isOn) return;
        isOn = true;
        buttonText.text = "ON";
        timingPanel.SetActive(true);
        stopButton.interactable = true;
        StartMoving();
    }

    void StartMoving()
    {
        isPlaying = true;
        direction = 1;
        StartCoroutine(MoveBar());
    }

    IEnumerator MoveBar()
    {
        while (isPlaying)
        {
            float step = moveSpeed * Time.deltaTime;
            cursorPosX += direction * step;
            if (cursorPosX >= rightBound)
            {
                cursorPosX = rightBound;
                direction = -1;
            }
            else if (cursorPosX <= leftBound)
            {
                cursorPosX = leftBound;
                direction = 1;
            }
            barCursor.anchoredPosition = new Vector2(cursorPosX, barCursor.anchoredPosition.y);
            yield return null;
        }
    }

    void OnStopClicked()
    {
        if (!isPlaying || isStopped) return;
        isPlaying = false;
        isStopped = true;
        StopAllCoroutines();

        // Tentukan apakah sukses
        bool success = false;
        if (greenZone != null)
        {
            float greenLeft = greenZone.anchoredPosition.x - (greenZone.rect.width / 2);
            float greenRight = greenZone.anchoredPosition.x + (greenZone.rect.width / 2);
            success = (cursorPosX >= greenLeft && cursorPosX <= greenRight);
        }
        else
        {
            float center = (leftBound + rightBound) / 2f;
            float greenLeft = center - greenZoneWidth / 2f;
            float greenRight = center + greenZoneWidth / 2f;
            success = (cursorPosX >= greenLeft && cursorPosX <= greenRight);
        }

        // Sembunyikan panel minigame dulu (biar fokus ke popup hasil)
        timingPanel.SetActive(false);
        stopButton.interactable = false;

        // Tampilkan popup hasil
        if (popupResultPrefab != null)
        {
            // Cari canvas utama (bisa dari popupPanel parent canvas)
            Canvas canvas = popupPanel.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

            GameObject resultObj = Instantiate(popupResultPrefab, canvas.transform);
            var resultScript = resultObj.GetComponent<PopupHasilKesehatan>();
            if (resultScript != null)
            {
                resultScript.Setup(success, () => {
                    // Callback setelah tombol "Kembali" ditekan
                    popupPanel.SetActive(false); // tutup popup minigame
                    NotifyResult(success);
                });
            }
        }
        else
        {
            // Fallback jika prefab tidak di-assign: langsung callback
            Debug.LogWarning("PopupResultPrefab tidak di-assign, langsung callback.");
            popupPanel.SetActive(false);
            NotifyResult(success);
        }
    }

    private void NotifyResult(bool success)
    {
        if (currentListener == null)
            return;

        if (success)
            currentListener.OnHealthCheckSuccess();
        else
            currentListener.OnHealthCheckFailure();

        currentListener = null;
    }
}
