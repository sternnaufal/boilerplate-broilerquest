using UnityEngine;
using UnityEngine.EventSystems;

public class PipeScript : MonoBehaviour, IPointerClickHandler
{
    float[] rotations = { 0, 90, 180, 270 };
    public float[] correctRotation;
    [SerializeField] bool isPlaced = false;
    int PossibleRots = 1;
    PipeGameManager pipeGameManager;

    private void Awake()
    {
        GameObject gm = GameObject.Find("PipeGameManager");
        if (gm != null)
            pipeGameManager = gm.GetComponent<PipeGameManager>();
        else
            Debug.LogError("GameManager not found in scene!");
    }

    private void Start()
    {
        if (correctRotation == null || correctRotation.Length == 0)
        {
            Debug.LogWarning("correctRotation not set for " + gameObject.name);
            return;
        }

        PossibleRots = correctRotation.Length;

        // Pastikan rotasi awal TIDAK pernah benar secara kebetulan
        int randomIndex;
        float startAngle;
        do
        {
            randomIndex = Random.Range(0, rotations.Length);
            startAngle = rotations[randomIndex];
        } while (IsCorrectRotation(startAngle));

        transform.eulerAngles = new Vector3(0, 0, startAngle);
        // Tidak perlu CheckAndUpdatePlacement() di sini, isPlaced sudah false by default
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //if (isPlaced) return; // Jika sudah benar, tidak bisa diputar lagi (opsional)

        transform.Rotate(new Vector3(0, 0, 90));
        CheckAndUpdatePlacement();
    }
    private bool IsCorrectRotation(float angle)
    {
        foreach (float target in correctRotation)
            if (Mathf.Approximately(angle, target)) return true;
        return false;
    }

    private void CheckAndUpdatePlacement()
    {
        float currentAngle = Mathf.Round(transform.eulerAngles.z / 90f) * 90f;
        bool isCorrect = IsCorrectRotation(currentAngle); // gunakan method yang sama

        if (isCorrect && !isPlaced)
        {
            isPlaced = true;
            pipeGameManager?.correctMove();
        }
        else if (!isCorrect && isPlaced)
        {
            isPlaced = false;
            pipeGameManager?.wrongMove();
        }
    }
}