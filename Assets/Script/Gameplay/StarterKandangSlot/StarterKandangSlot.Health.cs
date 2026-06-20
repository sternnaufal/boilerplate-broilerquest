using UnityEngine;

public partial class StarterKandangSlot
{
    private bool TryStartHealthMinigame()
    {
        if (!useHealthMinigame)
            return false;

        ChickenNeed need = CurrentNeed;

        if (JigsawMinigameController.Instance != null && JigsawMinigameController.Instance.IsPlaying)
            return true;

        if (need == ChickenNeed.Cooling)
        {
            if (MemoryMatchController.Instance != null && MemoryMatchController.Instance.IsPlaying)
                return true;

            MemoryMatchController memoryMatch = MemoryMatchController.Instance;
            if (memoryMatch != null)
            {
                currentState = SlotState.WaitingForHealthMinigame;
                NotifyStateChanged();

                if (memoryMatch.ShowMemoryMatch(this, GetNeedTitle(need)))
                    return true;

                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
            }
        }

        if (need == ChickenNeed.Heating)
        {
            if (WiringMinigameController.Instance != null && WiringMinigameController.Instance.IsPlaying)
                return true;

            WiringMinigameController wiring = WiringMinigameController.Instance;
            if (wiring != null)
            {
                currentState = SlotState.WaitingForHealthMinigame;
                NotifyStateChanged();

                if (wiring.ShowWiring(this, wiringPairCount, wiringTimeLimit, GetWiringTitle(need)))
                    return true;

                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
            }
        }

        if (need == ChickenNeed.HumidityUp)
        {
            HumidityToggleController toggle = HumidityToggleController.Instance;
            if (toggle != null)
            {
                currentState = SlotState.WaitingForHealthMinigame;
                NotifyStateChanged();

                if (toggle.ShowToggle(this))
                    return true;

                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
            }
        }

        if (need == ChickenNeed.HumidityDown)
        {
            PipelinePuzzleController pipeline = PipelinePuzzleController.Instance;
            if (pipeline != null)
            {
                currentState = SlotState.WaitingForHealthMinigame;
                NotifyStateChanged();

                if (pipeline.ShowPuzzle(this))
                    return true;

                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
            }
        }

        if (need == ChickenNeed.AddDryHusk)
        {
            DragDropSackController sack = DragDropSackController.Instance;
            if (sack != null)
            {
                currentState = SlotState.WaitingForHealthMinigame;
                NotifyStateChanged();

                if (sack.ShowDragDrop(this))
                    return true;

                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
            }
        }

        if (need == ChickenNeed.ReduceFeed)
        {
            HoldSwipeController swipe = HoldSwipeController.Instance;
            if (swipe != null)
            {
                currentState = SlotState.WaitingForHealthMinigame;
                NotifyStateChanged();

                if (swipe.ShowHoldSwipe(this))
                    return true;

                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
            }
        }

        JigsawMinigameController jigsawController = JigsawMinigameController.Instance;
        if (jigsawController != null)
        {
            Texture puzzleTexture = GetNeedPuzzleTexture(need);
            if (puzzleTexture != null)
            {
                currentState = SlotState.WaitingForHealthMinigame;
                NotifyStateChanged();

                if (jigsawController.ShowJigsaw(this, puzzleTexture, GetNeedTitle(need)))
                    return true;

                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
            }
        }

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
