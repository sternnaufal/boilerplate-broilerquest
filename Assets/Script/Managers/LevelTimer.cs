using UnityEngine;
using TMPro;
using System;

public class LevelTimer : MonoBehaviour
{
    public event Action OnTimeUp;

    [SerializeField] private TextMeshProUGUI timerText;

    private float timeRemaining;
    private bool isRunning;
    private Coroutine timerCoroutine;

    public float TimeRemaining => timeRemaining;
    public bool IsRunning => isRunning;

    public void StartTimer(float duration)
    {
        StopTimer();
        timeRemaining = Mathf.Max(0, duration);
        isRunning = true;
        UpdateDisplay();
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    public void StopTimer()
    {
        isRunning = false;
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
    }

    public void BindTimerText(TextMeshProUGUI text)
    {
        timerText = text;
        if (timerText != null)
            UpdateDisplay();
    }

    private System.Collections.IEnumerator TimerRoutine()
    {
        while (isRunning && timeRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
            timeRemaining--;
            UpdateDisplay();

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                UpdateDisplay();
                isRunning = false;
                OnTimeUp?.Invoke();
            }
        }
    }

    private void UpdateDisplay()
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void OnDestroy()
    {
        StopTimer();
    }
}
