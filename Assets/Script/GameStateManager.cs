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
        var instance = EnsureInstance();
        if (instance != null)
        {
            instance.SetGameState(newState);
            return true;
        }
        return false;
    }

    private static GameStateManager EnsureInstance()
    {
        return Instance;
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
