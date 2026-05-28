using UnityEngine;

public partial class StarterKandangSlot
{
    private bool TryStartHealthMinigame()
    {
        if (!useHealthMinigame)
            return false;

        if (JigsawMinigameController.Instance != null && JigsawMinigameController.Instance.IsPlaying)
            return true;

        JigsawMinigameController jigsawController = JigsawMinigameController.Instance;
        if (jigsawController != null)
        {
            Texture puzzleTexture = GetNeedPuzzleTexture(currentNeed);
            if (puzzleTexture != null)
            {
                currentState = SlotState.WaitingForHealthMinigame;
                NotifyStateChanged();

                if (jigsawController.ShowJigsaw(this, puzzleTexture, GetNeedTitle(currentNeed)))
                    return true;

                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
            }
        }

        if (PopupKesehatan.Instance == null)
        {
            Debug.LogWarning($"{name}: PopupKesehatan belum tersedia, kebutuhan diselesaikan langsung.");
            return false;
        }

        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        PopupKesehatan.Instance.ShowHealthCheck(this);
        return true;
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
            default:
                return "Susun Puzzle";
        }
    }
}
