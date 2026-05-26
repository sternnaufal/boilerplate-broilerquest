public static class GameConstants
{
    public static class Persistence
    {
        public const string TotalCoinKey = "BroilerQuest.TotalCoin";
        public const string LegacyTotalCoinKey = "TotalCoin";
        public const string KoleksiIoTPurchasedPrefix = "KoleksiIoT.Purchased.";
        public const string LevelUnlockBeginnerKey = "Level.Beginner.Unlocked";
        public const string LevelUnlockIntermediateKey = "Level.Intermediate.Unlocked";
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
    }

    public static class StarterSlot
    {
        public const int BaseSellReward = 20;
        public const int CareBonus = 10;
        public const float NeedInterval = 5f;
        public const float NotificationDelay = 1f;
        public const float NeedIntervalMin = 3f;
        public const float NeedIntervalMax = 8f;
    }

    public static class Economy
    {
        public const int StartingCoin = 400;
        public const int ChickenPrice = 50;
        public const int BaseSellPrice = 90;
        public const int FailPenalty = 30;
        public const int FeedCost = 50;
        public const int FeedIncrement = 10;
        public const int AutoFeederCost = 200;
        public const int AutoFanCost = 300;
        public const int AutoHeaterCost = 300;
    }

    public static class LevelUnlock
    {
        public const int BeginnerCost = 750;
        public const int IntermediateCost = 1500;
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
        public const float TimeLimit = 15f;
        public const float TileSize = 150f;
        public const float TileSpacing = 4f;
        public const float SwapDuration = 0.15f;
        public const float WarningThreshold = 10f;
    }
}
