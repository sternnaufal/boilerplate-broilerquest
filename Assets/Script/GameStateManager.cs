using System;
using UnityEngine;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}

public class GameStateManager : Singleton<GameStateManager>
{
    public event Action<GameState> StateChanged;

    [SerializeField] private GameState initialState = GameState.Menu;

    public GameState CurrentState { get; private set; }
    private bool hasState;

    private void Start()
    {
        if (!hasState)
            SetGameState(initialState);
    }

    public void SetGameState(GameState newState)
    {
        if (hasState && CurrentState == newState)
            return;

        CurrentState = newState;
        hasState = true;
        ApplyTimeScale(newState);
        ApplyGameManagerState(newState);
        StateChanged?.Invoke(newState);
    }

    public void SetMenu()
    {
        SetGameState(GameState.Menu);
    }

    public void SetPlaying()
    {
        SetGameState(GameState.Playing);
    }

    public void SetPaused()
    {
        SetGameState(GameState.Paused);
    }

    public void SetGameOver()
    {
        SetGameState(GameState.GameOver);
    }

    public static bool TrySetGameState(GameState newState)
    {
        EnsureInstance().SetGameState(newState);
        return true;
    }

    private static GameStateManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<GameStateManager>();
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("GameStateManager");
        return go.AddComponent<GameStateManager>();
    }

    private static void ApplyTimeScale(GameState state)
    {
        Time.timeScale = state == GameState.Paused ? 0f : 1f;
    }

    private static void ApplyGameManagerState(GameState state)
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.SetGameActive(state == GameState.Playing);
    }
}
