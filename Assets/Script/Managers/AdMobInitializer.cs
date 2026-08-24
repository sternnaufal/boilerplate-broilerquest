using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;

namespace BroilerQuest.Managers
{
    public class AdMobInitializer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _showBanner = true;
        [SerializeField] private bool _showNativeOverlay = true;
        [SerializeField] private AdPosition _bannerPosition = AdPosition.Bottom;
        [SerializeField] private AdPosition _nativeOverlayPosition = AdPosition.Bottom;

        private static bool _isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            if (!_isInitialized)
            {
                CreateAdMobManager();
                _isInitialized = true;
            }
        }

        private static void CreateAdMobManager()
        {
            GameObject admobManagerObj = new GameObject("AdMobManager");
            DontDestroyOnLoad(admobManagerObj);
            admobManagerObj.AddComponent<AdMobManager>();
        }

        private void Awake()
        {
            if (AdMobManager.Instance != null)
            {
                ApplySettings();
            }
        }

        private void ApplySettings()
        {
            var manager = AdMobManager.Instance;
            manager.ShowBanner(_showBanner);
            manager.ShowNativeOverlay(_showNativeOverlay);
            manager.SetBannerPosition(_bannerPosition);
            manager.SetNativeOverlayPosition(_nativeOverlayPosition);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (AdMobManager.Instance != null)
            {
                ApplySettings();
            }
        }
    }
}
