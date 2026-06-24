using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class PipeGameManager : MonoBehaviour
{
    public GameObject PipesHolder;
    public GameObject[] Pipes;

    [SerializeField] int totalPipes = 0;
    [SerializeField] int correctedPipes = 0;   // Hanya satu deklarasi
    private bool puzzleEnded = false;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 30f; 

    [Header("Events")]
    public UnityEvent OnPuzzleCompleted;   // dipanggil saat semua pipa benar
    public UnityEvent OnPuzzleFailed;

    private float timeRemaining;
    private bool isTimerRunning;
    private Coroutine timerCoroutine;
    void Start()
    {
        totalPipes = PipesHolder.transform.childCount;

        // Guard: jika tidak ada pipa, jangan jalankan puzzle
        if (totalPipes == 0)
        {
            Debug.LogError($"{name}: PipesHolder tidak punya child!");
            return;
        }

        Pipes = new GameObject[totalPipes];
        for (int i = 0; i < Pipes.Length; i++)
            Pipes[i] = PipesHolder.transform.GetChild(i).gameObject;

        StartTimer();
    }

    public void correctMove()
    {
        if (puzzleEnded) return;
        correctedPipes += 1;
        Debug.Log("Correct Move! Total: " + correctedPipes + "/" + totalPipes);

        // Guard tambahan: pastikan totalPipes valid
        if (totalPipes <= 0) return;

        if (correctedPipes >= totalPipes)
        {
            puzzleEnded = true;
            StopTimer();
            OnPuzzleCompleted?.Invoke();
        }
    }

    public void wrongMove()
    {
        if (puzzleEnded) return;
        correctedPipes = Mathf.Max(0, correctedPipes - 1);
    }

    public void StartTimer()
    {
        timeRemaining = timeLimit;
        isTimerRunning = true;
        UpdateTimerUI();
        
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private IEnumerator TimerRoutine()
    {
        while (timeRemaining > 0f)
        {
            if (puzzleEnded) yield break; // ← stop jika sudah selesai
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }

        if (!puzzleEnded)
        {
            puzzleEnded = true;
            Debug.Log("Pipe Game: Waktu habis!");
            StopTimer();
            OnPuzzleFailed?.Invoke();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));
            timerText.text = seconds.ToString();
            
            // Ubah warna menjadi merah jika waktu < 5 detik
            if (timeRemaining <= 5f)
                timerText.color = Color.red;
            else
                timerText.color = Color.white;
        }
    }

    // Method untuk mengatur timer dari luar (jika ingin diatur dari controller)
    public void SetTimeLimit(float seconds)
    {
        timeLimit = seconds;
    }
}