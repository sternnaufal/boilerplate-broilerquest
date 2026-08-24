using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

namespace BroilerQuest.Managers
{
    public class AdMobManager : MonoBehaviour
    {
        private static AdMobManager _instance;
        public static AdMobManager Instance => _instance;

        [Header("Banner Settings")]
        [SerializeField] private bool _showBanner = true;
        [SerializeField] private AdPosition _bannerPosition = AdPosition.Bottom;

        [Header("Native Overlay Settings")]
        [SerializeField] private bool _showNativeOverlay = true;
        [SerializeField] private AdPosition _nativeOverlayPosition = AdPosition.Bottom;

        private BannerView _bannerView;
        private NativeOverlayAd _nativeOverlayAd;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DestroyBannerAd();
            DestroyNativeOverlayAd();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (DemoModeConfig.IsDemoMode) return;

            if (!_isInitialized)
            {
                InitializeMobileAds();
                return;
            }

            if (_showBanner)
            {
                DestroyBannerAd();
                LoadBannerAd();
            }

            if (_showNativeOverlay)
            {
                DestroyNativeOverlayAd();
                LoadNativeOverlayAd();
            }
        }

        private void Start()
        {
            if (DemoModeConfig.IsDemoMode)
            {
                Debug.Log("[AdMobManager] Demo mode aktif — iklan tidak ditampilkan.");
                return;
            }

            InitializeMobileAds();
        }

        private void InitializeMobileAds()
        {
            MobileAds.Initialize((InitializationStatus initStatus) =>
            {
                if (initStatus == null)
                {
                    Debug.LogError("[AdMobManager] Google Mobile Ads initialization failed.");
                    return;
                }

                Debug.Log("[AdMobManager] Google Mobile Ads initialization complete.");
                _isInitialized = true;

                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (_showBanner)
                        LoadBannerAd();

                    if (_showNativeOverlay)
                        LoadNativeOverlayAd();
                });
            });
        }

        private void LoadBannerAd()
        {
            if (DemoModeConfig.IsDemoMode) return;

#if UNITY_ANDROID
            string adUnitId = "ca-app-pub-3940256099942544/6300978111";
#elif UNITY_IOS
            string adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
            string adUnitId = "unexpected_platform";
#endif

            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
            }

            Debug.Log($"[AdMobManager] Loading banner ad with ID: {adUnitId}");

            _bannerView = new BannerView(adUnitId, AdSize.Banner, _bannerPosition);

            _bannerView.OnBannerAdLoaded += () =>
            {
                Debug.Log("[AdMobManager] Banner ad loaded successfully.");
            };

            _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                Debug.LogError($"[AdMobManager] Banner ad failed to load: {error.GetMessage()}");
            };

            _bannerView.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log($"[AdMobManager] Banner ad paid: {adValue.Value} {adValue.CurrencyCode}");
            };

            _bannerView.LoadAd(new AdRequest());
        }

        private void LoadNativeOverlayAd()
        {
            if (DemoModeConfig.IsDemoMode) return;

#if UNITY_ANDROID
            string adUnitId = "ca-app-pub-3940256099942544/2247696110";
#elif UNITY_IOS
            string adUnitId = "ca-app-pub-3940256099942544/3986624511";
#else
            string adUnitId = "unexpected_platform";
#endif

            if (_nativeOverlayAd != null)
            {
                DestroyNativeOverlayAd();
            }

            Debug.Log($"[AdMobManager] Loading native overlay ad with ID: {adUnitId}");

            var adRequest = new AdRequest();
            var options = new NativeAdOptions
            {
                AdChoicesPlacement = AdChoicesPlacement.TopRightCorner,
                MediaAspectRatio = MediaAspectRatio.Any,
            };

            NativeOverlayAd.Load(adUnitId, adRequest, options,
                (NativeOverlayAd ad, LoadAdError error) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (error != null)
                        {
                            Debug.LogError($"[AdMobManager] Native Overlay ad failed to load: {error.GetMessage()}");
                            return;
                        }

                        if (ad == null)
                        {
                            Debug.LogError("[AdMobManager] Native Overlay ad load event fired with null ad and null error.");
                            return;
                        }

                        Debug.Log("[AdMobManager] Native Overlay ad loaded successfully.");
                        _nativeOverlayAd = ad;

                        RegisterNativeOverlayEventHandlers(ad);
                        RenderAndShowNativeOverlay();
                    });
                });
        }

        private void RegisterNativeOverlayEventHandlers(NativeOverlayAd ad)
        {
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log($"[AdMobManager] Native Overlay ad paid: {adValue.Value} {adValue.CurrencyCode}");
            };

            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[AdMobManager] Native Overlay ad impression recorded.");
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("[AdMobManager] Native Overlay ad clicked.");
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[AdMobManager] Native Overlay ad full screen content opened.");
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdMobManager] Native Overlay ad full screen content closed.");
            };
        }

        private void RenderAndShowNativeOverlay()
        {
            if (_nativeOverlayAd == null) return;

            Debug.Log("[AdMobManager] Rendering Native Overlay ad at bottom.");
            _nativeOverlayAd.RenderTemplate(new NativeTemplateStyle(), _nativeOverlayPosition);
            _nativeOverlayAd.Show();
        }

        public void ShowBanner(bool show = true)
        {
            _showBanner = show;

            if (DemoModeConfig.IsDemoMode) return;

            if (_bannerView != null)
            {
                if (show)
                    _bannerView.Show();
                else
                    _bannerView.Hide();
            }
            else if (show && _isInitialized)
            {
                LoadBannerAd();
            }
        }

        public void ShowNativeOverlay(bool show = true)
        {
            _showNativeOverlay = show;

            if (DemoModeConfig.IsDemoMode) return;

            if (_nativeOverlayAd != null)
            {
                if (show)
                    _nativeOverlayAd.Show();
                else
                    _nativeOverlayAd.Hide();
            }
            else if (show && _isInitialized)
            {
                LoadNativeOverlayAd();
            }
        }

        public void DestroyBannerAd()
        {
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
                Debug.Log("[AdMobManager] Banner ad destroyed.");
            }
        }

        public void DestroyNativeOverlayAd()
        {
            if (_nativeOverlayAd != null)
            {
                _nativeOverlayAd.Destroy();
                _nativeOverlayAd = null;
                Debug.Log("[AdMobManager] Native Overlay ad destroyed.");
            }
        }

        public void RefreshBannerAd()
        {
            if (DemoModeConfig.IsDemoMode) return;

            if (_bannerView != null)
            {
                _bannerView.LoadAd(new AdRequest());
            }
            else if (_isInitialized)
            {
                LoadBannerAd();
            }
        }

        public void RefreshNativeOverlayAd()
        {
            if (DemoModeConfig.IsDemoMode) return;

            if (_nativeOverlayAd != null)
            {
                DestroyNativeOverlayAd();
            }

            if (_isInitialized)
            {
                LoadNativeOverlayAd();
            }
        }

        public void SetBannerPosition(AdPosition position)
        {
            _bannerPosition = position;
            if (_bannerView != null)
            {
                _bannerView.SetPosition(position);
            }
        }

        public void SetNativeOverlayPosition(AdPosition position)
        {
            _nativeOverlayPosition = position;
            if (_nativeOverlayAd != null)
            {
                RenderAndShowNativeOverlay();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && _isInitialized && !DemoModeConfig.IsDemoMode)
            {
                if (_showBanner && _bannerView == null)
                    LoadBannerAd();

                if (_showNativeOverlay && _nativeOverlayAd == null)
                    LoadNativeOverlayAd();
            }
        }
    }
}
