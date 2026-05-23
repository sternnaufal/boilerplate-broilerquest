using UnityEngine;

public static class GameLog
{
    public static bool Verbose;

    public static void Info(string message)
    {
        if (Verbose)
            Debug.Log(message);
    }
}
