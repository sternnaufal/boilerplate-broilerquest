using UnityEngine;

public static class GameLog
{
    public static void Info(string message)
    {
        Debug.Log(message);
    }

    public static void Warn(string message)
    {
        Debug.LogWarning(message);
    }

    public static void Error(string message)
    {
        Debug.LogError(message);
    }
}
