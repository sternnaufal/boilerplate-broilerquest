using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class KoleksiIoTController : MonoBehaviour
{
    [System.Serializable]
    public class IoTProduct
    {
        public string productKey;
        public string productName;
        public int productPrice;
        public Texture productImage;
    }

    [Header("Products")]
    [SerializeField] private IoTProduct[] products;

    [Header("UI References")]
    [SerializeField] private Transform productContainer;
    [SerializeField] private Button backButton;

    [Header("Card Colors")]
    [SerializeField] private Color ownedColor = new Color(0.2f, 0.6f, 0.3f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Locked Placeholder")]
    [SerializeField] private string lockedPlaceholderText = "?????";
    [SerializeField] private Color lockedImageColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private readonly Dictionary<string, bool> prevPurchaseState = new Dictionary<string, bool>();
    private readonly Dictionary<string, Coroutine> pulseCoroutines = new Dictionary<string, Coroutine>();

    private static readonly AnimationCurve BounceCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 5f),
        new Keyframe(0.75f, 1.15f, 1.5f, -3f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    private void Awake()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged += OnCoinsChanged;
    }

    private void Start()
    {
        EnsureDefaultProducts();

        if (backButton != null)
        {
            ButtonHelper.AddListenerOnce(backButton, GoBack);
            if (backButton.GetComponent<SlideInFromTop>() == null)
                backButton.gameObject.AddComponent<SlideInFromTop>();
        }

        if (products != null)
            foreach (var p in products)
                prevPurchaseState[p.productKey] = IsPurchased(p.productKey);

        if (products != null && productContainer != null)
            foreach (var p in products)
            {
                var card = productContainer.Find(p.productKey);
                if (card != null) card.localScale = Vector3.zero;
            }

        SetupAllCards();
        StartCoroutine(StaggerCardEntrance());
    }

    private void OnDestroy()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int totalCoin)
    {
        RefreshAllCards();
    }

    // ── Entrance ─────────────────────────────────────────────────────────────

    private IEnumerator StaggerCardEntrance()
    {
        yield return null; // one frame so layout is ready

        float delay = 0f;
        float lastFinishTime = 0f;
        const float bounceDuration = 0.4f;

        foreach (var product in products)
        {
            var card = productContainer.Find(product.productKey);
            if (card == null) continue;
            StartCoroutine(BounceIn(card, delay));
            lastFinishTime = delay + bounceDuration;
            delay += 0.12f;
        }

        yield return new WaitForSeconds(lastFinishTime);

        foreach (var product in products)
        {
            if (IsPurchased(product.productKey)) continue;
            var card = productContainer.Find(product.productKey);
            if (card != null) StartLockedPulse(product.productKey, card);
        }
    }

    private IEnumerator BounceIn(Transform target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        target.localScale = Vector3.zero;

        float elapsed = 0f;
        const float duration = 0.4f;
        while (elapsed < duration)
        {
            target.localScale = Vector3.one * BounceCurve.Evaluate(elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    // ── Locked Pulse ─────────────────────────────────────────────────────────

    private void StartLockedPulse(string key, Transform card)
    {
        StopLockedPulse(key);
        pulseCoroutines[key] = StartCoroutine(LockedPulse(card));
    }

    private void StopLockedPulse(string key)
    {
        if (pulseCoroutines.TryGetValue(key, out var co) && co != null)
            StopCoroutine(co);
        pulseCoroutines[key] = null;
    }

    private IEnumerator LockedPulse(Transform card)
    {
        const float period = 1.8f;
        while (card != null)
        {
            float elapsed = 0f;
            while (card != null && elapsed < period)
            {
                float s = 1f + 0.025f * Mathf.Sin(elapsed / period * Mathf.PI * 2f);
                card.localScale = Vector3.one * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    // ── Unlock Reveal ─────────────────────────────────────────────────────────

    private IEnumerator PlayUnlockReveal(Image cardBg, RawImage productImage,
        TextMeshProUGUI nameText, TextMeshProUGUI priceText, IoTProduct product)
    {
        if (cardBg == null) yield break;

        // Flash white
        Color fromColor = cardBg.color;
        float t = 0f;
        const float flashDuration = 0.12f;
        while (t < flashDuration)
        {
            if (cardBg == null) yield break;
            cardBg.color = Color.Lerp(fromColor, Color.white, t / flashDuration);
            t += Time.deltaTime;
            yield return null;
        }

        // Prepare hidden reveal state
        if (productImage != null && product.productImage != null)
        {
            productImage.texture = product.productImage;
            productImage.color = new Color(1f, 1f, 1f, 0f);
        }
        if (nameText != null)
        {
            nameText.text = product.productName;
            Color c = nameText.color;
            nameText.color = new Color(c.r, c.g, c.b, 0f);
        }
        if (priceText != null) priceText.text = "";

        // Fade bg + reveal content
        t = 0f;
        const float fadeDuration = 0.45f;
        while (t < fadeDuration)
        {
            float n = t / fadeDuration;
            cardBg.color = Color.Lerp(Color.white, ownedColor, n);
            if (productImage != null) productImage.color = new Color(1f, 1f, 1f, n);
            if (nameText != null)
            {
                Color c = nameText.color;
                nameText.color = new Color(c.r, c.g, c.b, n);
            }
            t += Time.deltaTime;
            yield return null;
        }

        cardBg.color = ownedColor;
        if (productImage != null) productImage.color = Color.white;
        if (nameText != null)
        {
            Color c = nameText.color;
            nameText.color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    // ── Card Setup & Refresh ─────────────────────────────────────────────────

    private void EnsureDefaultProducts()
    {
        if (products != null && products.Length > 0) return;

        products = new IoTProduct[]
        {
            new IoTProduct { productKey = GameConstants.IoT.ProductKeyFeeder, productName = GameConstants.IoT.ProductNameFeeder, productPrice = GameConstants.Economy.AutoFeederCost },
            new IoTProduct { productKey = GameConstants.IoT.ProductKeyFan,    productName = GameConstants.IoT.ProductNameFan,    productPrice = GameConstants.Economy.AutoFanCost },
            new IoTProduct { productKey = GameConstants.IoT.ProductKeyHeater, productName = GameConstants.IoT.ProductNameHeater, productPrice = GameConstants.Economy.AutoHeaterCost },
        };
    }

    private void SetupAllCards()
    {
        if (products == null || productContainer == null) return;

        foreach (var product in products)
        {
            var cardTransform = productContainer.Find(product.productKey);
            if (cardTransform == null) continue;

            var card = cardTransform.gameObject;
            var image = card.GetComponentInChildren<RawImage>(true);
            if (image != null && product.productImage != null)
                image.texture = product.productImage;

            RefreshCard(product,
                FindButtonInChildren(card, "BuyButton"),
                FindTextInChildren(card, "NameText"),
                FindTextInChildren(card, "PriceText"),
                image,
                FindChildByName(card, "OwnedBadge"),
                card.GetComponent<Image>(),
                animated: false);
        }
    }

    private void RefreshAllCards()
    {
        if (products == null || productContainer == null) return;

        foreach (var product in products)
        {
            var cardTransform = productContainer.Find(product.productKey);
            if (cardTransform == null) continue;

            var card = cardTransform.gameObject;
            RefreshCard(product,
                FindButtonInChildren(card, "BuyButton"),
                FindTextInChildren(card, "NameText"),
                FindTextInChildren(card, "PriceText"),
                card.GetComponentInChildren<RawImage>(true),
                FindChildByName(card, "OwnedBadge"),
                card.GetComponent<Image>(),
                animated: true);
        }
    }

    private void RefreshCard(IoTProduct product, Button buyButton, TextMeshProUGUI nameText,
        TextMeshProUGUI priceText, RawImage image, GameObject ownedBadge, Image cardBg, bool animated)
    {
        if (product == null) return;

        bool purchased = IsPurchased(product.productKey);
        bool wasLocked = prevPurchaseState.TryGetValue(product.productKey, out bool prev) && !prev;
        bool justUnlocked = animated && wasLocked && purchased;

        prevPurchaseState[product.productKey] = purchased;

        if (buyButton != null) buyButton.gameObject.SetActive(false);

        if (purchased)
        {
            StopLockedPulse(product.productKey);
            var card = productContainer.Find(product.productKey);
            if (card != null) card.localScale = Vector3.one;

            if (ownedBadge != null)
            {
                bool wasInactive = !ownedBadge.activeSelf;
                ownedBadge.SetActive(true);
                if (wasInactive)
                {
                    var bounce = ownedBadge.GetComponent<ScaleBounceIn>();
                    if (bounce == null)
                        ownedBadge.AddComponent<ScaleBounceIn>(); // OnEnable triggers Play
                    else
                        bounce.Play();
                }
            }

            if (justUnlocked && cardBg != null)
            {
                StartCoroutine(PlayUnlockReveal(cardBg, image, nameText, priceText, product));
            }
            else
            {
                if (nameText != null) nameText.text = product.productName;
                if (priceText != null) priceText.text = "";
                if (image != null)
                {
                    if (product.productImage != null) image.texture = product.productImage;
                    image.color = Color.white;
                }
                if (cardBg != null) cardBg.color = ownedColor;
            }
        }
        else
        {
            if (ownedBadge != null) ownedBadge.SetActive(false);
            if (nameText != null) nameText.text = lockedPlaceholderText;
            if (priceText != null) priceText.text = lockedPlaceholderText;
            if (image != null) image.color = lockedImageColor;
            if (cardBg != null) cardBg.color = lockedColor;

            if (animated)
            {
                var card = productContainer.Find(product.productKey);
                if (card != null) StartLockedPulse(product.productKey, card);
            }
        }
    }

    private bool IsPurchased(string productKey) => StarterIoTController.CheckPurchased(productKey);

    public void GoBack()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.GoToMainMenu();
        else if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene("MainMenu");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private static TextMeshProUGUI FindTextInChildren(GameObject parent, string name)
    {
        foreach (var t in parent.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.gameObject.name == name) return t;
        return null;
    }

    private static Button FindButtonInChildren(GameObject parent, string name)
    {
        foreach (var b in parent.GetComponentsInChildren<Button>(true))
            if (b.gameObject.name == name) return b;
        return null;
    }

    private static GameObject FindChildByName(GameObject parent, string name)
    {
        var t = parent.transform.Find(name);
        return t != null ? t.gameObject : null;
    }
}
