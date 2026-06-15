using System.Collections.Generic;
using UnityEngine;
using System;

public static class SaveManager
{
    private const string SaveKey = GameConstants.Persistence.GameSaveKey;

    [Serializable]
    public class SlotSaveData
    {
        public string slotId;
        public bool occupied;
        public string prefabName;
        public bool feedSatisfied;
        public bool coolingSatisfied;
        public bool heatingSatisfied;
        public bool feedFailed;
        public bool coolingFailed;
        public bool heatingFailed;
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
        public SlotSaveData[] slots;
        public IotSaveData[] iotStates;
    }

    private static GameSaveData LoadOrCreateData()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            var data = JsonUtility.FromJson<GameSaveData>(json);
            if (data != null) return data;
        }
        return new GameSaveData();
    }

    private static void SaveData(GameSaveData data)
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static void SaveSlots(StarterKandangSlot[] slots)
    {
        if (slots == null || slots.Length == 0) return;

        GameSaveData data = LoadOrCreateData();
        data.slots = new SlotSaveData[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            data.slots[i] = slots[i].GetSaveData();
        }
        SaveData(data);
        GameLog.Info("SaveManager: Slot states saved.");
    }

    public static void SaveSlot(StarterKandangSlot slot, int slotIndex)
    {
        if (slot == null || slotIndex < 0) return;

        GameSaveData data = LoadOrCreateData();
        if (data.slots == null || slotIndex >= data.slots.Length)
        {
            Array.Resize(ref data.slots, slotIndex + 1);
        }
        data.slots[slotIndex] = slot.GetSaveData();
        SaveData(data);
    }

    public static void SaveIotStates(Dictionary<string, bool> activeStates)
    {
        if (activeStates == null) return;

        GameSaveData data = LoadOrCreateData();
        var list = new List<IotSaveData>();
        foreach (var kvp in activeStates)
        {
            list.Add(new IotSaveData { productKey = kvp.Key, active = kvp.Value });
        }
        data.iotStates = list.ToArray();
        SaveData(data);
        GameLog.Info("SaveManager: IoT states saved.");
    }

    public static void SaveAll()
    {
        GameSaveData data = LoadOrCreateData();
        bool hasChanges = false;

        StarterKandangSlot[] slots = GameObject.FindObjectsByType<StarterKandangSlot>(
            FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        if (slots != null && slots.Length > 0)
        {
            data.slots = new SlotSaveData[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                data.slots[i] = slots[i] != null ? slots[i].GetSaveData() : null;
            }

            hasChanges = true;
        }

        if (StarterIoTController.Instance != null)
        {
            var states = StarterIoTController.Instance.GetActiveStates();
            if (states != null)
            {
                var list = new List<IotSaveData>();
                foreach (var kvp in states)
                {
                    list.Add(new IotSaveData { productKey = kvp.Key, active = kvp.Value });
                }

                data.iotStates = list.ToArray();
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            SaveData(data);
            GameLog.Info("SaveManager: Full game state saved.");
        }
    }

    public static void LoadAndRestoreSlots(StarterKandangSlot[] slots, Func<string, GameObject> prefabLookup)
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data?.slots == null) return;

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

        GameLog.Info("SaveManager: Slot states restored.");
    }

    public static void LoadIotStates(StarterIoTController controller)
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data?.iotStates == null) return;

        var states = new Dictionary<string, bool>();
        foreach (var iot in data.iotStates)
        {
            states[iot.productKey] = iot.active;
        }
        controller.SetActiveStates(states);
        GameLog.Info("SaveManager: IoT states restored.");
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
