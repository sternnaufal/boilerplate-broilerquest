using System.Collections.Generic;
using UnityEngine;
using System;

public static class SaveManager
{
    private const string SaveKey = GameConstants.Persistence.GameSaveKey;

    [Serializable]
    public class SlotSaveData
    {
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
        StarterKandangSlot[] slots = GameObject.FindObjectsByType<StarterKandangSlot>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        if (slots.Length > 0)
            SaveSlots(slots);

        if (StarterIoTController.Instance != null)
        {
            var states = StarterIoTController.Instance.GetActiveStates();
            if (states != null)
                SaveIotStates(states);
        }
    }

    public static void LoadAndRestoreSlots(StarterKandangSlot[] slots, Func<string, GameObject> prefabLookup)
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data?.slots == null) return;

        for (int i = 0; i < data.slots.Length && i < slots.Length; i++)
        {
            slots[i].RestoreFromSave(data.slots[i], prefabLookup);
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
