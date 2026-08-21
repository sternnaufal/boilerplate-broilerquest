using UnityEngine;

public static class DemoModeConfig
{
    public const string ResourcesPath = GameConstants.Persistence.DemoModeConfigResourcePath;

    private static DemoModeSettings _settings;

    public static bool IsDemoMode
    {
        get
        {
            if (_settings == null)
                _settings = Resources.Load<DemoModeSettings>(ResourcesPath);

            return _settings != null && _settings.demoModeEnabled;
        }
    }
}
