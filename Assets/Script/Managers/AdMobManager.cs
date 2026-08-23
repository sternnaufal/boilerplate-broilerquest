using System;
using UnityEngine;
using GoogleMobileAds.Api;

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
        [SerializeField] private NativeTemplateID _nativeTemplateId = NativeTemplateID.Medium;

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
        }

        private void Start()
        {
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
#if UNITY_ANDROID
            string adUnitId = "ca-app-pub-3940256099942544/6300978111";
#elif UNITY_IOS
            string adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
            string adUnitId = "unexpected_platform";
#endif

            // Clean up existing banner
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
#if UNITY_ANDROID
            string adUnitId = "ca-app-pub-3940256099942544/2247696110";
#elif UNITY_IOS
            string adUnitId = "ca-app-pub-3940256099942544/3986624511";
#else
            string adUnitId = "unexpected_platform";
#endif

            // Clean up existing native overlay
            if (_nativeOverlayAd != null)
            {
                DestroyNativeOverlayAd();
            }

            Debug.Log($"[AdMobManager] Loading native overlay ad with ID: {adUnitId}");

            var adRequest = new AdRequest();
            var options = new NativeAdOptions
            {
                AdChoicesPosition = AdChoicesPlacement.TopRightCorner,
                MediaAspectRatio = NativeMediaAspectRatio.Any,
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

            var style = new NativeTemplateStyle
            {
                TemplateID = _nativeTemplateId,
                MainBackgroundColor = new Color(0f, 0f, 0f, 0.8f),
                CallToActionText = new NativeTemplateTextStyles
                {
                    BackgroundColor = new Color(0.2f, 0.6f, 1f, 1f),
                    FontColor = Color.white,
                    FontSize = 12,
                    Style = NativeTemplateFontStyle.Bold
                },
                PrimaryText = new NativeTemplateTextStyles
                {
                    FontColor = Color.white,
                    FontSize = 14,
                    Style = NativeTemplateFontStyle.Normal
                },
                SecondaryText = new NativeTemplateTextStyles
                {
                    FontColor = new Color(0.8f, 0.8f, 0.8f, 1f),
                    FontSize = 11,
                    Style = NativeTemplateFontStyle.Normal
                }
            };

            Debug.Log("[AdMobManager] Rendering Native Overlay ad at bottom.");
            _nativeOverlayAd.RenderTemplate(style, _nativeOverlayPosition);
            _nativeOverlayAd.Show();
        }

        public void ShowBanner(bool show = true)
        {
            _showBanner = show;
            
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
                // Native overlay position is set during RenderTemplate
                RenderAndShowNativeOverlay();
            }
        }

        public void SetNativeTemplate(NativeTemplateID templateId)
        {
            _nativeTemplateId = templateId;
            if (_nativeOverlayAd != null)
            {
                RenderAndShowNativeOverlay();
            }
        }

        private void OnDestroy()
        {
            DestroyBannerAd();
            DestroyNativeOverlayAd();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && _isInitialized)
            {
                // App resumed, refresh ads if needed
                if (_showBanner && _bannerView == null)
                    LoadBannerAd();
                
                if (_showNativeOverlay && _nativeOverlayAd == null)
                    LoadNativeOverlayAd();
            }
        }
    }
}