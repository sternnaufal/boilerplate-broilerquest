using UnityEngine;

public class ChickenBounceEffect : MonoBehaviour
{
    [SerializeField] private float bounceHeight = 8f;
    [SerializeField] private float bounceSpeed = 6f;
    [SerializeField] private float squashAmount = 0.1f;

    private RectTransform imageRect; // target = Image child
    private float bounceTime;

    private void Awake()
    {
        // Image adalah sibling, jadi cari dari parent
        UnityEngine.UI.Image img = GetComponentInParent<UnityEngine.UI.Image>(true);
        
        // Kalau tidak ketemu, coba cari di parent's children (sibling)
        if (img == null && transform.parent != null)
            img = transform.parent.GetComponentInChildren<UnityEngine.UI.Image>(true);

        if (img != null)
        {
            imageRect = img.rectTransform;
            Debug.Log($"BounceEffect found: {img.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"BounceEffect: No Image found near {gameObject.name}");
        }
    }

    private void LateUpdate()
    {
        if (imageRect == null) return;

        bounceTime += Time.deltaTime * bounceSpeed;
        float bounce = Mathf.Abs(Mathf.Sin(bounceTime)) * bounceHeight;

        // Hanya gerakkan Y lokal Image, X tetap 0
        imageRect.anchoredPosition = new Vector2(0f, bounce);

        // Squash pada Image saja
        float t = bounceHeight > 0 ? bounce / bounceHeight : 0f;
        imageRect.localScale = new Vector3(1f - t * squashAmount * 0.5f, 1f + t * squashAmount, 1f);
    }
}