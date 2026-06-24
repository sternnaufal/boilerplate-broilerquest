using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class PipePrefabMinigameController : MonoBehaviour
{
    public static PipePrefabMinigameController Instance { get; private set; }

    [Header("Prefab Varian")]
    [SerializeField] private GameObject[] pipePrefabs;

    private GameObject currentInstance;
    private IHealthCheckListener currentListener;
    private PipeGameManager currentManager;
//tes
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool ShowRandomPipePuzzle(IHealthCheckListener listener)
    {
        if (pipePrefabs == null || pipePrefabs.Length == 0)
        {
            Debug.LogError("PipePrefabMinigameController: Tidak ada prefab!");
            return false;
        }

        if (currentInstance != null)
            Destroy(currentInstance);

        GameObject selected = pipePrefabs[Random.Range(0, pipePrefabs.Length)];
        currentInstance = Instantiate(selected);
        currentListener = listener;

        // 🔧 Pastikan Canvas prefab memiliki GraphicRaycaster
        Canvas canvas = currentInstance.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = currentInstance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        canvas.sortingOrder = 500; // tinggi agar di atas UI lain
        canvas.enabled = true;

        if (currentInstance.GetComponent<GraphicRaycaster>() == null)
            currentInstance.AddComponent<GraphicRaycaster>();

        // Cari PipeGameManager
        currentManager = currentInstance.GetComponentInChildren<PipeGameManager>();
        if (currentManager == null)
        {
            Debug.LogError("Prefab tidak memiliki PipeGameManager!");
            Destroy(currentInstance);
            currentInstance = null;
            return false;
        }

        // Hapus semua listener sebelumnya
        currentManager.OnPuzzleCompleted.RemoveAllListeners();
        currentManager.OnPuzzleFailed.RemoveAllListeners();

        // Daftarkan listener
        currentManager.OnPuzzleCompleted.AddListener(OnPuzzleCompleted);
        currentManager.OnPuzzleFailed.AddListener(OnPuzzleFailed); // <-- tambahkan ini

        return true;
    }

    private void OnPuzzleCompleted()
    {
        currentListener?.OnHealthCheckSuccess();
        ClosePuzzle();
    }

    private void OnPuzzleFailed()
    {
        currentListener?.OnHealthCheckFailure();
        ClosePuzzle();
    }

    public void ForceClosePuzzle()
    {
        currentListener?.OnHealthCheckFailure();
        ClosePuzzle();
    }

    private void ClosePuzzle()
    {
        if (currentInstance != null)
            Destroy(currentInstance);
        currentInstance = null;
        currentManager = null;
        currentListener = null;
    }
}