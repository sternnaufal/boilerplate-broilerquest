# BUG-BUG-FIX

## 2026-05-31 - KoleksiIoT locked cards revealed shop data

### Bug
`KoleksiIoTController` still behaved like a shop view for unpurchased IoT items. Locked collection cards showed the real coin price and kept the buy button active, so the collection screen revealed item data before purchase and allowed purchases outside the intended shop flow.

### Fix
Updated `Assets/Script/IoT/KoleksiIoTController.cs` so collection cards now read purchase state from the existing `StarterIoTController.CheckPurchased` API:

- Purchased cards show the real product name, real image, owned badge, and owned card color.
- Unpurchased cards show `?????` in `NameText` and `PriceText`.
- Unpurchased card images are darkened with `lockedImageColor`.
- `BuyButton` is always hidden in KoleksiIoT so purchases remain tied to the shop flow.

### Verification
Verified the `KoleksiIoT` scene contains `BQ_KoleksiIoTCanvas`, `ProductContainer`, and the three product cards `AutoFeeder`, `AutoHeater`, and `AutoFan`. Each checked card has the expected manual UI children: `NameText`, `PriceText`, `BuyButton`, `OwnedBadge`, and `ProductImage`.

## 2026-05-31 - IoT shop buttons could not buy locked devices

### Bug
`StarterIoTController` displayed `BELI` for locked IoT devices, but the same button was only wired to `ToggleDevice`. `RefreshAll` also set `toggleButton.interactable = purchased`, so locked devices could never be clicked and therefore could not be bought from the shop.

### Fix
Updated `Assets/Script/IoT/StarterIoTController.cs` so the existing manual button now has two states:

- If the device is not purchased, clicking the button spends coins and marks the device purchased.
- If the device is already purchased, clicking the button toggles ON/OFF.
- Locked shop rows now show the configured/default price in the status text.
- The buy button is interactable when the player can afford the item, or when the item is already purchased for toggling.

### Verification
Verified in `Starter.unity` through Unity MCP:

- `StarterIoTController` exists in the shop panel.
- The Feeder UI key was corrected from `AutoFeed` to `AutoFeeder`.
- Device prices are serialized in the scene as Feeder `200`, Fan `300`, and Heater `300`.
- With `400` coins, the Feeder shop button shows `200 Koin` and is interactable.
- Invoking the Feeder button buys the device, marks `AutoFeeder` purchased, changes status to `OFF`, and reduces coins to `200`.
- A direct Fan purchase attempt with only `200` coins fails and leaves Fan unpurchased.

## 2026-05-31 - Auto Heater and Auto Fan shop buttons felt unclickable

### Bug
Locked IoT shop buttons were only interactable when the player could afford the item. After buying another item, Auto Heater and Auto Fan could drop below their `300` coin requirement and become non-interactable, which made the shop feel broken because the buttons could not be pressed at all.

### Fix
Updated `Assets/Script/IoT/StarterIoTController.cs` so every IoT shop button remains interactable. The controller now handles the purchase attempt after the click:

- If the player has enough coins, the item is purchased.
- If the player does not have enough coins, the purchase fails without disabling the button.
- Purchased items still use the same button for ON/OFF toggling.

### Verification
Verified through Unity MCP:

- In edit mode with no purchases, Auto Heater, Auto Feeder, and Auto Fan all show their prices and are interactable.
- In Play Mode with `600` coins, `PurchaseDevice` successfully buys Auto Heater and Auto Fan, both become purchased, and coins drop to `0`.

## 2026-05-31 - Active IoT completed care before bubble tap

### Bug
`StarterKandangSlot` completed a chicken need immediately when the related IoT device was active. This skipped the bubble entirely, while the PM requirement says the bubble should still appear and only disappear after the player taps it.

### Fix
Updated `Assets/Script/Gameplay/StarterKandangSlot/StarterKandangSlot.cs` so need bubbles always appear. When the player taps a care bubble:

- If the related HP IoT toggle is ON, the need completes immediately without feed consumption or minigame.
- If the related HP IoT toggle is OFF, the existing flow continues: feed checks for pakan, then starts the health/minigame flow when enabled.
- The mapping remains Feed -> Auto Feeder, Cooling/KIPAS -> Auto Fan, Heating/HEATER -> Auto Heater.

### Verification
Verified through Unity MCP:

- `StarterKandangSlot.cs` compiles with 0 errors and 0 warnings.
- `Starter.unity` has no missing scripts or broken prefabs.
- In Play Mode, with Auto Fan purchased but OFF, the Cooling/KIPAS helper returns `false` and does not mark the need satisfied.
- In Play Mode, with Auto Fan purchased and ON, the Cooling/KIPAS helper returns `true`, marks the need satisfied, and returns the slot to `WaitingForCareEvent`.

## 2026-05-31 - Testing coin cheat reset to default on Play

### Bug
Manually setting `PlayerPrefs` coin to `1000` did not survive entering Play Mode because the `CoinManager` in `Starter.unity` had `resetCoinOnStart` enabled. Its startup flow overwrote saved coin with `GameConstants.Economy.StartingCoin` (`400`).

### Fix
Updated the `CoinManager` component in `Assets/Scenes/Starter.unity` so `resetCoinOnStart` is disabled. For testing, local `PlayerPrefs` coin was set to `1000`, allowing the saved value to persist when Play Mode starts.

### Verification
Verified in Play Mode through Unity MCP:

- `CoinManager` reports `1000`.
- `PlayerPrefs` reports `1000`.
- The value no longer resets to `400` when Play Mode starts.
