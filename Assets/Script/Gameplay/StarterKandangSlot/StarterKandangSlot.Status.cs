using System.Collections.Generic;

public partial class StarterKandangSlot
{
    public bool IsOccupied => occupied;
    public string SlotLabel => slotLabel != null ? slotLabel.text : gameObject.name;

    public IReadOnlyList<ChickenNeed> NeedsQueue => needsQueue;
    public IReadOnlyList<bool> NeedSatisfied => needSatisfied;
    public IReadOnlyList<bool> NeedFailed => needFailed;

    public int GetNeedStatus(ChickenNeed need)
    {
        if (needsQueue == null) return 0;
        for (int i = 0; i < needsQueue.Count; i++)
        {
            if (needsQueue[i] == need)
            {
                if (needSatisfied[i]) return 1;
                if (needFailed[i]) return -1;
                return 0;
            }
        }
        return 0;
    }

    public int GetFeedStatus() => GetNeedStatus(ChickenNeed.Feed);
    public int GetCoolingStatus() => GetNeedStatus(ChickenNeed.Cooling);
    public int GetHeatingStatus() => GetNeedStatus(ChickenNeed.Heating);
    public int GetHumidityUpStatus() => GetNeedStatus(ChickenNeed.HumidityUp);
    public int GetHumidityDownStatus() => GetNeedStatus(ChickenNeed.HumidityDown);
    public int GetAddDryHuskStatus() => GetNeedStatus(ChickenNeed.AddDryHusk);
    public int GetReduceFeedStatus() => GetNeedStatus(ChickenNeed.ReduceFeed);
}
