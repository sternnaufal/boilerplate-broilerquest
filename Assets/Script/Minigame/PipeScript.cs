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

        int attempts = 0;
        do
        {
            int randomIndex = Random.Range(0, rotations.Length);
            transform.eulerAngles = new Vector3(0, 0, rotations[randomIndex]);
            attempts++;
        } while (IsCorrectRotation(transform.eulerAngles.z) && attempts < 10);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //if (isPlaced) return; // Jika sudah benar, tidak bisa diputar lagi (opsional)

        transform.Rotate(new Vector3(0, 0, 90));
        CheckAndUpdatePlacement();
    }
    private bool IsCorrectRotation(float angle)
    {
        float rounded = Mathf.Round(angle / 90f) * 90f;
        foreach (float target in correctRotation)
            if (Mathf.Approximately(rounded, target)) return true;
        return false;
    }

    private void CheckAndUpdatePlacement()
    {
        float currentAngle = Mathf.Round(transform.eulerAngles.z / 90f) * 90f;
        bool isCorrect = IsCorrectRotation(currentAngle);

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