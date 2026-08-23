using UnityEngine;
using UnityEngine.SceneManagement;

namespace BroilerQuest.Managers
{
    public class AdMobInitializer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _showBanner = true;
        [SerializeField] private bool _showNativeOverlay = true;
        [SerializeField] private AdPosition _bannerPosition = AdPosition.Bottom;
        [SerializeField] private AdPosition _nativeOverlayPosition = AdPosition.Bottom;
        [SerializeField] private NativeTemplateID _nativeTemplateId = NativeTemplateID.Medium;

        private static bool _isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            // This ensures the AdMobManager persists across all scenes
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
            
            var admobManager = admobManagerObj.AddComponent<AdMobManager>();
            
            // We can't set serialized fields from static method easily,
            // but we can use the public methods after initialization
            // The default values in AdMobManager are already set to show both ads at bottom
        }

        private void Awake()
        {
            // If this component exists in a scene (added manually), 
            // apply its settings to the singleton
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
            manager.SetNativeTemplate(_nativeTemplateId);
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
            // Re-apply settings on scene load in case they were changed
            if (AdMobManager.Instance != null)
            {
                ApplySettings();
            }
        }
    }
}