# HP Panel Fix - 15 Juni 2026

## Masalah

Tombol HP di Starter scene tidak memunculkan panel HP. Panel tetap di posisi hidden meskipun tombol sudah ditekan. Masalah terjadi secara tidak konsisten — kadang beberapa menit kemudian panel muncul sendiri.

**Root cause:** Di `StarterGameplayUI.Start()`, `hpPanelRect.rect.height` bisa bernilai 0 saat pertama kali dipanggil karena layout Canvas belum selesai dihitung oleh Unity. Akibatnya `hiddenPosition` jatuh di posisi yang sama dengan `visiblePosition`, sehingga animasi panel tidak bergerak dan panel tidak terlihat.

## Fix

### 1. `Assets/Script/Core/GameConstants.cs`

Menambahkan safety margin untuk posisi hidden HP panel:

```csharp
public const float HPPanelSafetyMargin = 50f;
```

### 2. `Assets/Script/Gameplay/StarterGameplayUI.cs`

Mengubah kalkulasi `hiddenPosition` dari hardcoded `-800f` menjadi dinamis berbasis anchor, pivot, dan `panelHeight`:

- **Primary:** `parentRect.rect.height` (kalau > 0 — layout sudah siap)
- **Fallback parent:** `Screen.height` (kalau parent belum siap)
- **Primary panelHeight:** `hpPanelRect.sizeDelta.y` (nilai authored, selalu valid)
- **Fallback panelHeight:** `hpPanelRect.rect.height` (kalau layout sudah siap)

Rumus hidden position:
```
hiddenPosition.x = visiblePosition.x
hiddenPosition.y = -parentHeight * anchorMin.y
                  - panelHeight * (1 - pivot.y)
                  - HPPanelSafetyMargin
```

Kenapa `sizeDelta.y` penting: `sizeDelta.y` (580 di scene) adalah nilai yang di-author di Inspector, nilainya tidak bergantung pada layout pass. `rect.height` bisa 0 sebelum `Canvas.ForceUpdateCanvases()` benar-benar selesai.

### 3. `Assets/Scenes/Starter.unity`

Menghapus duplikat komponen `StarterGameplayUI` dari `StarterCanvas` (fileID `1086103080`). Komponen terdaftar dua kali karena sisa dari proses pull/merge.

## Verifikasi

- `GameConstants.cs`: `HPPanelSafetyMargin` ada di line 26 ✅
- `StarterGameplayUI.cs`: Kalkulasi `hiddenPosition` menggunakan `parentHeight`, `anchor.y`, `pivot.y`, `panelHeight`, dan `HPPanelSafetyMargin` ✅
- `StarterGameplayUI.cs`: Fallback ke `sizeDelta.y` saat `rect.height == 0` ✅
- `Starter.unity`: Hanya ada 1 komponen `StarterGameplayUI` (tidak ada duplikat) ✅

## File yang Berubah

| Status | File |
|--------|------|
| Diubah | `Assets/Script/Core/GameConstants.cs` |
| Diubah | `Assets/Script/Gameplay/StarterGameplayUI.cs` |
| Diubah | `Assets/Scenes/Starter.unity` |

## Catatan Teknis

- Unity Canvas layout tidak dijamin selesai saat `Start()` dipanggil. Beberapa frame layout pass diperlukan sebelum `RectTransform.rect` memiliki nilai yang valid.
- `RectTransform.sizeDelta` menyimpan nilai authored dari Inspector dan selalu tersedia, menjadikannya fallback yang aman.
- Menurut taste Unity yang berlaku: "When calculating RectTransform positions in Start(), prefer sizeDelta.y over rect.height to avoid layout timing issues."
