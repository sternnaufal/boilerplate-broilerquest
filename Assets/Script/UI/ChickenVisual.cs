using UnityEngine;

public class ChickenBounce : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float bounceHeight = 5f;      // tinggi lompatan (jangan terlalu tinggi)
    public float bounceSpeed = 4f;       // kecepatan (naikkan agar lebih cepat)
    public bool useLocalPosition = true; // false jika menggunakan RectTransform

    private RectTransform rect;
    private Vector2 baseAnchoredPos;
    private bool isRect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            isRect = true;
            baseAnchoredPos = rect.anchoredPosition;
        }
        else
        {
            isRect = false;
            // simpan posisi awal untuk Transform biasa
        }
    }

    void Update()
    {
        float offsetY = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        if (isRect && rect != null)
        {
            rect.anchoredPosition = new Vector2(baseAnchoredPos.x, baseAnchoredPos.y + offsetY);
        }
        else
        {
            // untuk non-UI (Transform)
            transform.localPosition = new Vector3(transform.localPosition.x, 
                transform.localPosition.y + offsetY, transform.localPosition.z);
        }
    }
}