using UnityEngine;

public partial class StarterKandangSlot
{
    private static bool IsMinigameEnabled(MinigameType type)
    {
        var config = SceneMinigameConfig.Instance;
        return config == null || config.IsEnabled(type);
    }

    private bool TryStartHealthMinigame()
    {
        if (!useHealthMinigame)
            return false;

        ChickenNeed need = CurrentNeed;

        if (IsPuzzleActive())
            return true;

        if (need == ChickenNeed.Cooling && IsMinigameEnabled(MinigameType.MemoryMatch))
        {
            if (TryMemoryMatch(need)) return true;
        }

        if (need == ChickenNeed.Heating && IsMinigameEnabled(MinigameType.Wiring))
        {
            if (TryWiring(need)) return true;
        }

        if (need == ChickenNeed.HumidityUp && IsMinigameEnabled(MinigameType.HumidityToggle))
        {
            if (TryHumidityToggle()) return true;
        }

        if (need == ChickenNeed.HumidityDown && IsMinigameEnabled(MinigameType.PipelinePuzzle))
        {
            if (TryPipelinePuzzle()) return true;
        }

        if (need == ChickenNeed.AddDryHusk && IsMinigameEnabled(MinigameType.DragDropSack))
        {
            if (TryDragDropSack()) return true;
        }

        if (need == ChickenNeed.ReduceFeed && IsMinigameEnabled(MinigameType.HoldSwipe))
        {
            if (TryHoldSwipe()) return true;
        }

        return TryJigsawFallback(need);
    }

    private bool TryMemoryMatch(ChickenNeed need)
    {
        if (MemoryMatchController.Instance != null && MemoryMatchController.Instance.IsPlaying)
            return true;

        MemoryMatchController mm = MemoryMatchController.Instance;
        if (mm == null) return false;

        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        if (mm.ShowMemoryMatch(this, GetNeedTitle(need))) return true;
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        return false;
    }

    private bool TryWiring(ChickenNeed need)
    {
        if (WiringMinigameController.Instance != null && WiringMinigameController.Instance.IsPlaying)
            return true;

        WiringMinigameController wiring = WiringMinigameController.Instance;
        if (wiring == null) return false;

        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        if (wiring.ShowWiring(this, wiringPairCount, wiringTimeLimit, GetWiringTitle(need))) return true;
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        return false;
    }

    private bool TryHumidityToggle()
    {
        HumidityToggleController toggle = HumidityToggleController.Instance;
        if (toggle == null) return false;

        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        if (toggle.ShowToggle(this)) return true;
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        return false;
    }

    private bool TryPipelinePuzzle()
    {
        PipelinePuzzleController pipeline = PipelinePuzzleController.Instance;
        if (pipeline == null) return false;

        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        if (pipeline.ShowPuzzle(this)) return true;
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        return false;
    }

    private bool TryDragDropSack()
    {
        DragDropSackController sack = DragDropSackController.Instance;
        if (sack == null) return false;

        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        if (sack.ShowDragDrop(this)) return true;
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        return false;
    }

    private bool TryHoldSwipe()
    {
        HoldSwipeController swipe = HoldSwipeController.Instance;
        if (swipe == null) return false;

        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        if (swipe.ShowHoldSwipe(this)) return true;
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        return false;
    }

    private bool TryJigsawFallback(ChickenNeed need)
    {
        JigsawMinigameController jigsaw = JigsawMinigameController.Instance;
        if (jigsaw == null) return false;

        Texture puzzleTexture = GetNeedPuzzleTexture(need);
        if (puzzleTexture == null) return false;

        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        if (jigsaw.ShowJigsaw(this, puzzleTexture, GetNeedTitle(need))) return true;
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        return false;
    }

    private Texture GetNeedPuzzleTexture(ChickenNeed need)
    {
        Texture configuredTexture = null;
        Sprite fallbackSprite = null;

        switch (need)
        {
            case ChickenNeed.Feed:
                configuredTexture = jigsawFeedTexture;
                fallbackSprite = feedBubbleSprite;
                break;
            case ChickenNeed.Cooling:
                configuredTexture = jigsawCoolingTexture;
                fallbackSprite = coolingBubbleSprite;
                break;
            case ChickenNeed.Heating:
                configuredTexture = jigsawHeatingTexture;
                fallbackSprite = heatingBubbleSprite;
                break;
        }

        if (configuredTexture != null)
            return configuredTexture;

        return fallbackSprite != null ? fallbackSprite.texture : null;
    }

    private string GetNeedTitle(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Feed:
                return "Susun Puzzle Pakan";
            case ChickenNeed.Cooling:
                return "Susun Puzzle Dingin";
            case ChickenNeed.Heating:
                return "Susun Puzzle Panas";
            case ChickenNeed.HumidityUp:
                return "Atur Kelembaban";
            case ChickenNeed.HumidityDown:
                return "Pipeline Pipa";
            case ChickenNeed.AddDryHusk:
                return "Tambah Sekam";
            case ChickenNeed.ReduceFeed:
                return "Kurangi Pakan";
            default:
                return "Susun Puzzle";
        }
    }

    private string GetWiringTitle(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Heating:
                return "Hubungkan Kabel Heater";
            default:
                return "Hubungkan Kabel";
        }
    }
}
