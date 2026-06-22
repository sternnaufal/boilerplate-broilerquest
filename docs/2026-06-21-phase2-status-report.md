# Phase 2 Fixes — Status Report (June 21, 2026, Updated)

## Ringkasan

Dari 6 fix yg diimplementasikan, hanya 1 yg benar-benar berfungsi: **Humidity Toggle ON/OFF** (meski posisi UI-nya salah).

Semua fix lain (CoinOut alert, advanced minigames disable, cross-level save, timer auto-discovery, TimeUpPopup auto-find) **tidak berfungsi** setelah di-test di Unity.

---

## Fix yang Berfungsi

### Humidity Toggle ON/OFF Machine — ✅ BERFUNGSI
- Machine start OFF, tombol "HIDUPKAN MESIN", indicator berhenti
- Klik → machine ON → indicator jalan → tombol jadi "ON/OFF"
- **Issue:** Posisi UI-nya salah (kemungkinan anchor/posisi tidak sesuai layout)
- **Fix needed:** Sesuaikan posisi/anchoring popup di `HumidityToggleController.cs` atau scene

---

## Fix yang Tidak Berfungsi

### CoinOut Alert Beli Pakan — ❌ GAGAL
- UIAlertPanel mungkin gak ada di scene atau reference gak terhubung
- **Root cause:** Perlu `Tools → Restore UI Elements in All Levels`

### Advanced Minigames Disabled — ❌ GAGAL
- Flag `enableAdvancedMinigames` mungkin gak di-reset di tiap slot scene
- Perlu set manual di Inspector tiap StarterKandangSlot di Starter, Beginner, Intermediate

### Cross-Level Save — ❌ GAGAL
- Save ayam tidak bekerja sama sekali
- Dugaan: `SaveAll()` dipanggil terlalu awal (sebelum beli ayam), nge-save key level kosong
- Legacy fallback gak kepicu karena key udah ada (meski isinya kosong)

### Timer Auto-Discovery — ❌ GAGAL
- Mungkin GameObject "TimerText" gak ada di scene
- Atau timer text punya nama berbeda

### TimeUpPopup Auto-Find — ❌ GAGAL
- Mungkin GameObject "TimeUpPopup" gak ada di scene
- Atau GameManager gak terinisialisasi dengan benar

---

## Files Changed (Semua Fix Ini Tidak Berfungsi di Runtime)

| File | Change | Status |
|---|---|---|
| `StarterKandangSlot.cs` | enableAdvancedMinigames flag + pool filter | ❌ |
| `HumidityToggleController.cs` | machineOn state, HIDUPKAN MESIN flow | ✅ (posisi salah) |
| `StarterChickenShop.cs` | CoinOut alert di TryBuyFeed | ❌ |
| `SaveManager.cs` | per-level key, IoT key, legacy fallback | ❌ |
| `LevelTimer.cs` | EnsureTimerText auto-discovery | ❌ |
| `GameManager.cs` | TimeUpPopup auto-find | ❌ |

---

## Yang Perlu Dilakukan Next

1. **Save ayam** — debugging lebih dalam. Cek urutan panggilan `SaveAll()` vs `LoadAndRestoreSlots()`
2. **Restore UI** — jalanin `Tools → Restore UI Elements in All Levels` dulu sebelum test apa pun
3. **Humidity Toggle posisi** — betulin anchor/size popup biar muncul di tengah
4. **Advanced minigames** — set enableAdvancedMinigames di Inspector tiap scene
5. **Timer text** — pastikan ada GameObject "TimerText" di scene atau assign manual
6. **Testing ulang** semua bagian setelah point 1-5 selesai
