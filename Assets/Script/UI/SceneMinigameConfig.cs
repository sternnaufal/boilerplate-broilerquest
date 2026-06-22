using System;
using System.Collections.Generic;
using UnityEngine;

public class SceneMinigameConfig : MonoBehaviour
{
    [Serializable]
    public struct MinigameToggle
    {
        public MinigameType type;
        public bool enabled;
    }

    [SerializeField] private MinigameToggle[] minigames;

    private static SceneMinigameConfig cached;

    public static SceneMinigameConfig Instance
    {
        get
        {
            if (cached == null)
                cached = FindFirstObjectByType<SceneMinigameConfig>();
            return cached;
        }
    }

    public bool IsEnabled(MinigameType type)
    {
        if (minigames == null) return false;
        for (int i = 0; i < minigames.Length; i++)
            if (minigames[i].type == type) return minigames[i].enabled;
        return false;
    }
}
