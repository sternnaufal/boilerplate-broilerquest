public partial class StarterKandangSlot
{
    public bool IsFeedSatisfied => feedSatisfied;
    public bool IsCoolingSatisfied => coolingSatisfied;
    public bool IsHeatingSatisfied => heatingSatisfied;
    public bool IsFeedFailed => feedFailed;
    public bool IsCoolingFailed => coolingFailed;
    public bool IsHeatingFailed => heatingFailed;

    public bool IsOccupied => occupied;
    public string SlotLabel => slotLabel != null ? slotLabel.text : gameObject.name;

    public int GetFeedStatus()
    {
        if (feedSatisfied) return 1;
        if (feedFailed) return -1;
        return 0;
    }

    public int GetCoolingStatus()
    {
        if (coolingSatisfied) return 1;
        if (coolingFailed) return -1;
        return 0;
    }

    public int GetHeatingStatus()
    {
        if (heatingSatisfied) return 1;
        if (heatingFailed) return -1;
        return 0;
    }
}
