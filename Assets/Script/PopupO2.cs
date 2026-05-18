using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
public class PopupO2 : MonoBehaviour
{
    public static PopupO2 Instance;

    [Header("UI References")]
    public GameObject popupPanel;
    public Button offOnButton;
    public TextMeshProUGUI buttonText;               // Jika pakai Text biasa, ganti dengan TMPro jika perlu
    public GameObject timingPanel;
    public RectTransform barCursor;       // Cursor (garis)
    public RectTransform barBackground;   // GameObject Merah (background bar)
    public RectTransform greenZone;       // GameObject Ijo (zona hijau)
    public Button stopButton;

    [Header("Timing Settings")]
    public float moveSpeed = 300f;        // Kecepatan gerak (pixel/detik)
    public float greenZoneWidth = 100f;   // Lebar zona hijau (jika tidak otomatis dari Ijo)

    private bool isOn = false;
    private bool isPlaying = false;
    private float cursorPosX;
    private float leftBound, rightBound;
    private int direction = 1;
    private TambakController currentTambak;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        popupPanel.SetActive(false);
    }

    void Start()
    {
        offOnButton.onClick.AddListener(ToggleOffOn);
        stopButton.onClick.AddListener(OnStopClicked);
        stopButton.interactable = false;
        timingPanel.SetActive(false);

        if (barBackground != null && barCursor != null)
        {
            // Pastikan cursor dan background memiliki parent yang sama
            // Hitung batas kiri dan kanan background (dalam koordinat parent)
            float bgLeft = barBackground.anchoredPosition.x - (barBackground.rect.width / 2f);
            float bgRight = barBackground.anchoredPosition.x + (barBackground.rect.width / 2f);
            leftBound = bgLeft;
            rightBound = bgRight;
            
            // Posisikan cursor di batas kiri
            cursorPosX = leftBound;
            barCursor.anchoredPosition = new Vector2(cursorPosX, barCursor.anchoredPosition.y);
        }
        else
        {
            Debug.LogError("PopupO2: barBackground atau barCursor tidak di-assign!");
        }
    }

    public void TampilkanPopup(TambakController tambak)
    {
        currentTambak = tambak;
        isOn = false;
        isPlaying = false;
        buttonText.text = "OFF";
        timingPanel.SetActive(false);
        stopButton.interactable = false;
        popupPanel.SetActive(true);
        // Reset posisi cursor ke kiri
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
        if (!isPlaying) return;
        isPlaying = false;
        StopAllCoroutines();

        bool success = false;
        // Menentukan apakah posisi cursor berada di dalam zona hijau
        if (greenZone != null)
        {
            float greenLeft = greenZone.anchoredPosition.x - (greenZone.rect.width / 2);
            float greenRight = greenZone.anchoredPosition.x + (greenZone.rect.width / 2);
            success = (cursorPosX >= greenLeft && cursorPosX <= greenRight);
        }
        else
        {
            // Fallback: zona hijau di tengah dengan lebar greenZoneWidth
            float center = (leftBound + rightBound) / 2f;
            float greenLeft = center - greenZoneWidth / 2f;
            float greenRight = center + greenZoneWidth / 2f;
            success = (cursorPosX >= greenLeft && cursorPosX <= greenRight);
        }

        popupPanel.SetActive(false);

        if (currentTambak != null)
        {
            if (success)
                currentTambak.OnO2MinigameSuccess();
            else
                currentTambak.OnO2MinigameFail();
        }
    }
}