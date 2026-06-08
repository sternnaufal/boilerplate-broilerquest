# Implementation Plan - IoT Collection Privacy

Modifying the `KoleksiIoT` screen so that locked items (not yet purchased in the shop) are hidden behind "????" placeholders until purchased.

## UI Structure Analysis

The existing UI structure in the `KoleksiIoT` scene is organized as follows:

- **BQ_KoleksiIoTCanvas** (Canvas)
  - **PanelBackground** (Image)
  - **ProductContainer** (HorizontalLayoutGroup / Transform) - Holds all product card GameObjects
    - **Feeder** (GameObject / Image) - Product Card for Feeder
      - **ProductImage** (RawImage) - Product icon/image
      - **NameText** (TextMeshProUGUI) - Displays the product name
      - **PriceText** (TextMeshProUGUI) - Displays product price/cost
      - **BuyButton** (Button) - Purchase button
      - **OwnedBadge** (GameObject) - Owned status badge
    - **Fan** (GameObject / Image) - Product Card for Fan
      - **ProductImage** (RawImage) - Product icon/image
      - **NameText** (TextMeshProUGUI) - Displays the product name
      - **PriceText** (TextMeshProUGUI) - Displays product price/cost
      - **BuyButton** (Button) - Purchase button
      - **OwnedBadge** (GameObject) - Owned status badge
    - **Heater** (GameObject / Image) - Product Card for Heater
      - **ProductImage** (RawImage) - Product icon/image
      - **NameText** (TextMeshProUGUI) - Displays the product name
      - **PriceText** (TextMeshProUGUI) - Displays product price/cost
      - **BuyButton** (Button) - Purchase button
      - **OwnedBadge** (GameObject) - Owned status badge
  - **BackButton** (Button) - Navigation button to go back to the Main Menu

## Proposed Changes

We will modify `KoleksiIoTController.cs` to change the card text and styling dynamically based on the purchase state (`IsPurchased`).

### [Component: IoT UI Controllers]

#### [MODIFY] [KoleksiIoTController.cs](file:///d:/Bandung%20Lautan%20Api/boilerplate-broilerquest/Assets/Script/IoT/KoleksiIoTController.cs)

We will refactor `RefreshCard` to handle Name and Price placeholder toggles, image silhouette styling, and clean up direct GameObject component fetches.

##### Key Logic Changes:
1. **Name Placeholder:**
   - If **purchased**: `nameText.text = product.productName`
   - If **not purchased**: `nameText.text = "?????"`

2. **Price Placeholder:**
   - If **purchased**: `priceText.text = ""` (since it's owned)
   - If **not purchased**: `priceText.text = "?????"` (to hide the price behind the placeholder as requested in the PM's diagram)

3. **Image Silhouette:**
   - If **purchased**: `image.color = Color.white` (fully visible)
   - If **not purchased**: `image.color = new Color(0.2f, 0.2f, 0.2f, 1.0f)` (silhouette/dark shade to represent a locked state)

4. **Buy Button & Card Background:**
   - Keep current behavior for the buy button active/inactive states and background color swapping (`ownedColor` vs `lockedColor`).

Here is a preview of the proposed changes:

```csharp
    private void SetupCard(GameObject card, IoTProduct product)
    {
        RawImage image = card.GetComponentInChildren<RawImage>(true);
        if (image != null && product.productImage != null)
            image.texture = product.productImage;

        Button buyButton = FindButtonInChildren(card, "BuyButton");
        if (buyButton != null)
            ButtonHelper.AddListenerOnce(buyButton, () => BuyProduct(product));

        RefreshCard(card, product);
    }

    private void RefreshAllCards()
    {
        if (products == null || productContainer == null)
            return;

        foreach (IoTProduct product in products)
        {
            Transform cardTransform = productContainer.Find(product.productKey);
            if (cardTransform == null)
                continue;

            RefreshCard(cardTransform.gameObject, product);
        }
    }

    private void RefreshCard(GameObject card, IoTProduct product)
    {
        if (card == null || product == null) return;

        string key = product.productKey;
        int price = product.productPrice;
        bool purchased = IsPurchased(key);

        Button buyButton = FindButtonInChildren(card, "BuyButton");
        TextMeshProUGUI priceText = FindTextInChildren(card, "PriceText");
        TextMeshProUGUI nameText = FindTextInChildren(card, "NameText");
        RawImage image = card.GetComponentInChildren<RawImage>(true);
        GameObject ownedBadge = FindChildByName(card, "OwnedBadge");
        Image cardBg = card.GetComponent<Image>();

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!purchased);
            if (!purchased)
            {
                bool canAfford = CoinManager.Instance != null && CoinManager.Instance.CanAfford(price);
                buyButton.interactable = canAfford;
            }
        }

        if (nameText != null)
        {
            nameText.text = purchased ? product.productName : "?????";
        }

        if (priceText != null)
        {
            priceText.text = purchased ? "" : "?????";
        }

        if (image != null)
        {
            image.color = purchased ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        if (ownedBadge != null)
            ownedBadge.SetActive(purchased);

        if (cardBg != null)
            cardBg.color = purchased ? ownedColor : lockedColor;
    }
```

## Verification Plan

### Automated Verification
- Verify C# compilation inside Unity using `sigmap validate` or running Unity editor tests if available.
- Validate the C# script syntax using `validate_script`.

### Manual Verification
1. Open the game in Play mode.
2. Navigate to `KoleksiIoT` scene/panel.
3. Verify that all products display `?????` for their name and `?????` for price when not purchased.
4. Verify that their images are shown as silhouetted (darkened).
5. Purchase a product using coins and verify:
   - The card background transitions to `ownedColor`.
   - The name transitions from `?????` to the actual product name (e.g. "Feeder").
   - The price text disappears.
   - The `OwnedBadge` becomes active.
   - The image color becomes fully visible (`Color.white`).
