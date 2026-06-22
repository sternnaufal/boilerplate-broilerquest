using UnityEngine;
using UnityEngine.Events;

public class PipeGameManager : MonoBehaviour
{
    public GameObject PipesHolder;
    public GameObject[] Pipes;

    [SerializeField] int totalPipes = 0;
    [SerializeField] int correctedPipes = 0;   // Hanya satu deklarasi

    public UnityEvent OnPuzzleCompleted;

    void Start()
    {
        totalPipes = PipesHolder.transform.childCount;
        Pipes = new GameObject[totalPipes];
        for (int i = 0; i < Pipes.Length; i++)
        {
            Pipes[i] = PipesHolder.transform.GetChild(i).gameObject;
        }
    }

    public void correctMove()
    {
        correctedPipes += 1;
        Debug.Log("Correct Move! Total Corrected Pipes: " + correctedPipes);
        if (correctedPipes == totalPipes)
        {
            Debug.Log("Pipe Game Completed!");
            OnPuzzleCompleted?.Invoke();   // ✅ Sekarang hanya dipanggil jika semua benar
        }
    }

    public void wrongMove()
    {
        correctedPipes = Mathf.Max(0, correctedPipes - 1);
    }
}