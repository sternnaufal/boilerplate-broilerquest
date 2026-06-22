public enum MinigameType
{
    Jigsaw,
    MemoryMatch,
    Wiring,
    HumidityToggle,
    PipelinePuzzle,
    DragDropSack,
    HoldSwipe
}

public enum ChickenNeed
{
    Feed,
    Cooling,
    Heating,
    HumidityUp,
    HumidityDown,
    AddDryHusk,
    ReduceFeed
}

public static class GameConstants
{
    public static class Persistence
    {
        public const string TotalCoinKey = "BroilerQuest.TotalCoin";
        public const string LegacyTotalCoinKey = "TotalCoin";
        public const string KoleksiIoTPurchasedPrefix = "KoleksiIoT.Purchased.";
        public const string LevelUnlockBeginnerKey = "Level.Beginner.Unlocked";
        public const string LevelUnlockIntermediateKey = "Level.Intermediate.Unlocked";
        public const string FeedCountKey = "BroilerQuest.FeedCount";
        public const string GameSaveKey = "BroilerQuest.GameSave";
    }

    public static class LevelDuration
    {
        public const float Starter = 300f;
        public const float Beginner = 120f;
        public const float Intermediate = 180f;
    }

    public static class UI
    {
        public const float CoinTextFontSize = 34f;
        public const float ButtonLabelFontSize = 24f;
        public const float BubbleLabelFontSize = 22f;
        public const float HPPanelSafetyMargin = 50f;
    }

    public static class Difficulty
    {
        public const int StarterSteps = 3;
        public const int BeginnerSteps = 4;
        public const int IntermediateSteps = 5;
    }

    public static class StarterSlot
    {
        public const float NotificationDelay = 1f;
        public const float NeedIntervalMin = 3f;
        public const float NeedIntervalMax = 8f;
        // 0 or negative = no expiry (starter level: unlimited time)
        public const float BubbleExpiryDurationStarter    = 0f;   // disabled
        public const float BubbleExpiryDurationBeginner   = 20f;
        public const float BubbleExpiryDurationIntermediate = 15f;
    }

    public static class Economy
    {
        public const int StartingCoin = 400;
        public const int ChickenPrice = 40;
        public const int BaseSellPrice = 90;
        public const int FailPenalty = 30;
        public const int FeedCost = 5;
        public const int FeedIncrement = 1;
        public const int AutoFeederCost = 200;
        public const int AutoFanCost = 300;
        public const int AutoHeaterCost = 300;
    }

    public static class LevelUnlock
    {
        public const int BeginnerCost = 1000;
        public const int IntermediateCost = 2500;
    }

    public static class IoT
    {
        public const string ProductKeyFeeder = "AutoFeeder";
        public const string ProductKeyFan = "AutoFan";
        public const string ProductKeyHeater = "AutoHeater";
        public const string ProductNameFeeder = "Auto Feeder";
        public const string ProductNameFan = "Auto Fan";
        public const string ProductNameHeater = "Auto Heater";
    }

    public static class JigsawMinigame
    {
        public const int GridSize = 3;
        public const float TimeLimit = 25f;
        public const float TileSize = 150f;
        public const float TileSpacing = 4f;
        public const float SwapDuration = 0.15f;
        public const float WarningThreshold = 10f;
    }

    public static class MemoryMatch
    {
        public const float TimeLimit = 30f;
        public const int Columns = 3;
        public const int TotalPairs = 6;
        public const float FlipDuration = 0.25f;
        public const float MatchDelay = 0.5f;
        public const float WarningThreshold = 10f;
    }

    public static class WiringMinigame
    {
        public const float TimeLimit = 30f;
        public const int PairCount = 4;
        public const float WarningThreshold = 10f;
    }

    public static class HumidityToggle
    {
        public const float TimeLimit = 20f;
        public const int TargetSuccess = 3;
        public const int MaxFails = 1;
        public const float WarningThreshold = 5f;
        public const float IndicatorSpeed = 1.2f;
        public const float TargetZoneWidth = 0.3f;
    }

    public static class PipelinePuzzle
    {
        public const int GridSize = 3;
        public const float TimeLimit = 30f;
        public const float WarningThreshold = 10f;
    }

    public static class DragDropSack
    {
        public const int SackCount = 3;
        public const float TimeLimit = 20f;
        public const float WarningThreshold = 7f;
    }

    public static class HoldSwipe
    {
        public const float TimeLimit = 15f;
        public const int SwipeCount = 5;
        public const float WarningThreshold = 5f;
    }
}
