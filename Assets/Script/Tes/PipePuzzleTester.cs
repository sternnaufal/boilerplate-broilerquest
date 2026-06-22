using UnityEngine;

public class PipePuzzleTester : MonoBehaviour
{
    [Header("Puzzle Prefabs")]
    [SerializeField] private GameObject puzzleVariant1;
    [SerializeField] private GameObject puzzleVariant2;
    [SerializeField] private GameObject puzzleVariant3;

    private GameObject currentPuzzleInstance;

    // Method yang dipanggil oleh tombol
    public void LoadPuzzleVariant1()
    {
        LoadPuzzle(puzzleVariant1);
    }

    public void LoadPuzzleVariant2()
    {
        LoadPuzzle(puzzleVariant2);
    }

    public void LoadPuzzleVariant3()
    {
        LoadPuzzle(puzzleVariant3);
    }

    public void LoadRandomPuzzle()
    {
        // Pilih secara acak dari 3 varian
        GameObject[] variants = { puzzleVariant1, puzzleVariant2, puzzleVariant3 };
        GameObject selected = variants[Random.Range(0, variants.Length)];
        LoadPuzzle(selected);
    }

    private void LoadPuzzle(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab puzzle belum di-assign!");
            return;
        }

        // Hapus puzzle sebelumnya
        if (currentPuzzleInstance != null)
            Destroy(currentPuzzleInstance);

        // Cari Canvas di scene
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Tidak ada Canvas di scene! Buat Canvas terlebih dahulu.");
            return;
        }

        // Instantiate sebagai child dari Canvas
        currentPuzzleInstance = Instantiate(prefab, canvas.transform);

        // Reset posisi agar di tengah Canvas
        RectTransform rect = currentPuzzleInstance.GetComponent<RectTransform>();
        if (rect != null)
        {
            //rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        // Cari PipeGameManager
        PipeGameManager manager = currentPuzzleInstance.GetComponentInChildren<PipeGameManager>();
        if (manager != null)
        {
            manager.OnPuzzleCompleted.AddListener(OnPuzzleCompleted);
        }
        else
        {
            Debug.LogError("Prefab tidak memiliki PipeGameManager!");
        }
    }

    private void OnPuzzleCompleted()
    {
        Debug.Log("Puzzle selesai! Menutup puzzle...");
        if (currentPuzzleInstance != null)
        {
            Destroy(currentPuzzleInstance);
            currentPuzzleInstance = null;
        }
    }

    // Opsional: Jika ingin tombol "Close" manual
    public void ClosePuzzle()
    {
        if (currentPuzzleInstance != null)
        {
            Destroy(currentPuzzleInstance);
            currentPuzzleInstance = null;
        }
    }
}