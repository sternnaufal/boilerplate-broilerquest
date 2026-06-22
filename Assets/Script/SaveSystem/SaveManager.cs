using System.Collections.Generic;
using UnityEngine;
using System;

public static class SaveManager
{
    private const string SaveKey = GameConstants.Persistence.GameSaveKey;

    private static string LevelSaveKey
    {
        get
        {
            int level = GameManager.Instance != null ? GameManager.Instance.currentLevelIndex : -1;
            return level >= 0 ? $"{SaveKey}_L{level}" : SaveKey;
        }
    }

    [Serializable]
    public class SlotSaveData
    {
        public string slotId;
        public bool occupied;
        public string prefabName;
        public int[] needQueue;
        public bool[] needSatisfied;
        public bool[] needFailed;
        public int currentNeedIndex;
        public int completedCareCount;
        public int sellReward;
        public bool hasActiveBubble;
        public int activeBubbleNeed;
    }

    [Serializable]
    public class IotSaveData
    {
        public string productKey;
        public bool active;
    }

    [Serializable]
    public class GameSaveData
    {
        public int version = 2;
        public SlotSaveData[] slots;
        public IotSaveData[] iotStates; // kept for legacy save migration
    }

    [Serializable]
    public class IotSaveContainer
    {
        public IotSaveData[] iotStates;
    }

    private static GameSaveData LoadLevelData()
    {
        string json = PlayerPrefs.GetString(LevelSaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            var data = JsonUtility.FromJson<GameSaveData>(json);
            if (data != null)
            {
                if (data.version < 2)
                    Debug.LogWarning($"SaveManager: Loaded legacy save format (v{data.version}). Needs array-style need data will use fallback regeneration.");
                return data;
            }
        }
        return new GameSaveData();
    }

    private static void SaveLevelData(GameSaveData data)
    {
        PlayerPrefs.SetString(LevelSaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private static IotSaveContainer LoadIotContainer()
    {
        string json = PlayerPrefs.GetString(SaveKey + "_IoT", "");
        if (!string.IsNullOrEmpty(json))
        {
            var data = JsonUtility.FromJson<IotSaveContainer>(json);
            if (data != null) return data;
        }
        return new IotSaveContainer();
    }

    private static void SaveIotContainer(IotSaveContainer data)
    {
        PlayerPrefs.SetString(SaveKey + "_IoT", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static void SaveSlots(StarterKandangSlot[] slots)
    {
        if (slots == null || slots.Length == 0) return;

        var sortedSlots = new StarterKandangSlot[slots.Length];
        System.Array.Copy(slots, sortedSlots, slots.Length);
        System.Array.Sort(sortedSlots, (a, b) => string.Compare(a?.name, b?.name, System.StringComparison.Ordinal));

        GameSaveData data = LoadLevelData();
        data.slots = new SlotSaveData[sortedSlots.Length];
        for (int i = 0; i < sortedSlots.Length; i++)
        {
            data.slots[i] = sortedSlots[i].GetSaveData();
        }
        SaveLevelData(data);
        GameLog.Info($"SaveManager: Slot states saved for {LevelSaveKey}.");
    }

    public static void SaveSlot(StarterKandangSlot slot, int slotIndex)
    {
        if (slot == null || slotIndex < 0) return;

        GameSaveData data = LoadLevelData();
        if (data.slots == null || slotIndex >= data.slots.Length)
        {
            Array.Resize(ref data.slots, slotIndex + 1);
        }
        data.slots[slotIndex] = slot.GetSaveData();
        SaveLevelData(data);
    }

    public static void SaveIotStates(Dictionary<string, bool> activeStates)
    {
        if (activeStates == null) return;

        var container = LoadIotContainer();
        var list = new List<IotSaveData>();
        foreach (var kvp in activeStates)
        {
            list.Add(new IotSaveData { productKey = kvp.Key, active = kvp.Value });
        }
        container.iotStates = list.ToArray();
        SaveIotContainer(container);
        GameLog.Info("SaveManager: IoT states saved.");
    }

    public static void SaveAll()
    {
        StarterKandangSlot[] slots = GameObject.FindObjectsByType<StarterKandangSlot>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (slots != null && slots.Length > 0)
        {
            bool allEmpty = true;
            foreach (var slot in slots)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    allEmpty = false;
                    break;
                }
            }
            if (allEmpty)
            {
                GameLog.Info("SaveManager: All slots are empty, skipping slot save to prevent overwrite.");
            }
            else
            {
                var sortedSlots = new StarterKandangSlot[slots.Length];
                System.Array.Copy(slots, sortedSlots, slots.Length);
                System.Array.Sort(sortedSlots, (a, b) => string.Compare(a?.name, b?.name, System.StringComparison.Ordinal));

                GameSaveData levelData = LoadLevelData();
                levelData.slots = new SlotSaveData[sortedSlots.Length];
                for (int i = 0; i < sortedSlots.Length; i++)
                {
                    levelData.slots[i] = sortedSlots[i] != null ? sortedSlots[i].GetSaveData() : null;
                }
                SaveLevelData(levelData);
            }
        }

        // Save global IoT data
        if (StarterIoTController.Instance != null)
        {
            var states = StarterIoTController.Instance.GetActiveStates();
            if (states != null)
            {
                SaveIotStates(states);
            }
        }

        GameLog.Info("SaveManager: Full game state saved.");
    }

    public static void LoadAndRestoreSlots(StarterKandangSlot[] slots, Func<string, GameObject> prefabLookup)
    {
        string json = PlayerPrefs.GetString(LevelSaveKey, "");
        
        // Fallback: try legacy single-key format if level key is empty
        if (string.IsNullOrEmpty(json))
        {
            string legacyJson = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(legacyJson))
            {
                json = legacyJson;
                GameLog.Info($"SaveManager: Migrating legacy save from {SaveKey} to {LevelSaveKey}.");
            }
        }

        if (string.IsNullOrEmpty(json)) return;

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data?.slots == null) return;

        if (slots != null)
            System.Array.Sort(slots, (a, b) => string.Compare(a?.name, b?.name, System.StringComparison.Ordinal));

        var slotsById = new Dictionary<string, StarterKandangSlot>();
        if (slots != null)
        {
            foreach (StarterKandangSlot slot in slots)
            {
                if (slot == null)
                    continue;

                string slotId = slot.SlotId;
                if (string.IsNullOrEmpty(slotId))
                {
                    Debug.LogWarning($"SaveManager: Slot {slot.name} has an empty SlotId.");
                    continue;
                }

                if (slotsById.ContainsKey(slotId))
                {
                    Debug.LogWarning($"SaveManager: Duplicate SlotId '{slotId}' found. The first slot will be used.");
                    continue;
                }

                slotsById.Add(slotId, slot);
            }
        }

        for (int i = 0; i < data.slots.Length; i++)
        {
            SlotSaveData savedSlot = data.slots[i];
            if (savedSlot == null)
                continue;

            if (string.IsNullOrEmpty(savedSlot.slotId))
            {
                if (slots != null && i < slots.Length && slots[i] != null)
                {
                    slots[i].RestoreFromSave(savedSlot, prefabLookup);
                    GameLog.Info($"SaveManager: Restored legacy slot data by index {i}. It will be upgraded on the next save.");
                }
                continue;
            }

            if (slotsById.TryGetValue(savedSlot.slotId, out StarterKandangSlot slot))
            {
                slot.RestoreFromSave(savedSlot, prefabLookup);
            }
            else
            {
                Debug.LogWarning($"SaveManager: Saved slotId '{savedSlot.slotId}' was not found in the current scene.");
            }
        }

        GameLog.Info($"SaveManager: Slot states restored from {LevelSaveKey}.");
    }

    public static void LoadIotStates(StarterIoTController controller)
    {
        var container = LoadIotContainer();
        
        // Fallback: try legacy single-key GameSaveData for IoT states
        if (container?.iotStates == null || container.iotStates.Length == 0)
        {
            string legacyJson = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(legacyJson))
            {
                var legacyData = JsonUtility.FromJson<GameSaveData>(legacyJson);
                if (legacyData?.iotStates != null && legacyData.iotStates.Length > 0)
                {
                    // Migrate IoT states from legacy format
                    container = new IotSaveContainer { iotStates = legacyData.iotStates };
                    SaveIotContainer(container);
                    GameLog.Info("SaveManager: Migrated IoT states from legacy save.");
                }
            }
        }

        if (container?.iotStates == null) return;

        var states = new Dictionary<string, bool>();
        foreach (var iot in container.iotStates)
        {
            states[iot.productKey] = iot.active;
        }
        controller.SetActiveStates(states);
        GameLog.Info("SaveManager: IoT states restored.");
    }

    public static void ClearSave()
    {
        // Clear all level saves + IoT save + legacy save
        for (int i = 0; i < 3; i++)
            PlayerPrefs.DeleteKey($"{SaveKey}_L{i}");
        PlayerPrefs.DeleteKey(SaveKey + "_IoT");
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
