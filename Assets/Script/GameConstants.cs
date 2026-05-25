public static class GameConstants
{
    public static class Persistence
    {
        public const string TotalCoinKey = "BroilerQuest.TotalCoin";
        public const string LegacyTotalCoinKey = "TotalCoin";
        public const string KoleksiIoTPurchasedPrefix = "KoleksiIoT.Purchased.";
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
    }

    public static class JigsawMinigame
    {
        public const int GridSize = 3;
        public const float TimeLimit = 30f;
        public const float TileSize = 150f;
        public const float TileSpacing = 4f;
        public const float SwapDuration = 0.15f;
        public const float WarningThreshold = 10f;
    }
}
