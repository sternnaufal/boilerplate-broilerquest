# Testing Checklist — 2026-06-21

Covers all features after UI Restore, Defensive Fixes, and CoinManager fix. Test in order.

---

## A. Editor Tools (Run First)

- [ ] **Tools → Restore UI Elements in All Levels** — jalanin, console gak ada error
- [ ] **Tools → BroilerQuest → Grant 50000 Test Coins** — jalanin sebelum PlayMode
- [ ] **Tools → BroilerQuest → Clear All Save Data (PlayerPrefs)** — buat reset fresh state

---

## B. Main Menu & Level Select

- [ ] Main Menu muncul tanpa error
- [ ] Klik **Start** → pindah ke Select Level
- [ ] **Starter** button: langsung bisa diklik (free)
- [ ] **Beginner** button: tampilkan harga 1000, klik → beli → masuk scene
- [ ] **Intermediate** button: tampilkan harga 2500, klik → beli → masuk scene
- [ ] Kalo coin gak cukup: muncul pesan "Coin tidak cukup!"
- [ ] Level yang sudah dibeli: tampilkan nama aja (gak ada harga)
- [ ] Bounce-in animation jalan di Select Level

---

## C. Shop — Beli Ayam & Pakan

- [ ] **Starter**: buka HP panel → klik **Shop** → panel ShopAPK muncul
- [ ] Opsi ayam tampil dengan harga (40 coin)
- [ ] Klik beli ayam: coin berkurang, visual ayam muncul di kandang
- [ ] Kalo coin gak cukup: gak bisa klik, atau muncul alert
- [ ] Kalo semua kandang penuh: tombol bayar disable
- [ ] **Beli pakan**: 5 coin → +1 feed, feed count terupdate
- [x] Kalo coin gak cukup buat pakan: muncul **UIAlertPanel FoodOut**
- [ ] **Exit** dari ShopAPK balik ke halaman utama HP

---

## D. HP Panel Navigation

- [ ] **HP toggle button**: klik buka panel, klik lagi tutup
- [ ] Panel slide in/out dengan animasi
- [ ] **Shop** button di HP → buka ShopAPK
- [ ] **IoT** button di HP → buka IoTAPK
- [ ] **Exit** dari ShopAPK → balik ke halaman utama HP
- [ ] **Exit** dari IoTAPK → balik ke halaman utama HP

---

## E. Care Bubble System

### Starter (no expiry)

- [ ] Beli ayam → tunggu bubble muncul (3-8 detik)
- [ ] Bubble Feed: sprite MAKAN + label "MAKAN"
- [ ] Bubble Cooling: sprite KIPAS + label "KIPAS"
- [ ] Bubble Heating: sprite HEATER + label "HEATER"
- [ ] Biarin bubble 30+ detik → **gak boleh expired**
      a

### Beginner (20s expiry)

- [ ] Beli ayam → bubble muncul
- [ ] Diemin 21+ detik → **bubble expired**, need marked failed
- [ ] Bubble bisa muncul: HumidityUp / HumidityDown (random)

### Intermediate (15s expiry)

- [ ] Beli ayam → bubble muncul
- [ ] Diemin 16+ detik → **bubble expired**
- [ ] Bubble bisa muncul: HumidityUp, HumidityDown, AddDryHusk, ReduceFeed

### IoT Auto-Care

- [ ] Beli **Auto Feeder** → tap feed bubble → selesai tanpa minigame (tetap kurangi 1 feed)
- [ ] Beli **Auto Fan** → tap cooling bubble → selesai tanpa minigame
- [ ] Beli **Auto Heater** → tap heating bubble → selesai tanpa minigame
- [ ] Tanpa IoT: tap bubble → masuk minigame

---

## F. Minigames (7 Total)

### Memory Match (Cooling)

- [ ] 12 kartu (6 pasang), flip animation
- [ ] Cocokkan semua → **OnHealthCheckSuccess**
- [ ] Timer habis → **OnHealthCheckFailure**
- [ ] SFX sukses/gagal jalan

### Jigsaw Puzzle (Feed / Heating / fallback)

- [ ] 3x3 grid, susun puzzle
- [ ] Selesai → **OnHealthCheckSuccess**
- [ ] Timer habis → **OnHealthCheckFailure**
- [ ] Texture sesuai kebutuhan (Feed/Cooling/Heating)
- [ ] SFX sukses/gagal jalan

### Wiring Minigame (Heating)

- [ ] 4 pasang kabel, drag-connect left→right
- [ ] Semua terhubung → sukses
- [ ] Timer habis → gagal

### Humidity Toggle (HumidityUp)

- [ ] Indicator ping-pong, tap di zone target
- [ ] 3 sukses → sukses
- [ ] 1 gagal → gagal
- [ ] Timer habis → gagal

### Pipeline Puzzle (HumidityDown)

- [ ] 3x3 grid, tempatkan pipe tiles
- [ ] Jalur tersambung → sukses
- [ ] Timer habis → gagal

### Drag Drop Sack (AddDryHusk)

- [ ] 3 sacks, drag ke drop zone
- [ ] Semua terdrop → sukses
- [ ] Timer habis → gagal

### Hold Swipe (ReduceFeed)

- [ ] Hold-progress bar, feed pile mengecil
- [ ] 5 swipe → sukses
- [ ] Timer habis → gagal

---

## G. Pause & Resume

- [ ] Klik **PAUSE** → game pause, HUD hilang, pause panel muncul
- [ ] Klik **RESUME** → game lanjut
- [ ] Selama pause: bubble timer **berhenti** (gak expired)
- [ ] Selama minigame: bubble timer **berhenti**
- [ ] **EXIT button** muncul di pause panel
- [ ] Klik **EXIT** → popup konfirmasi "Kembali / Lanjutkan"
- [ ] Klik **Kembali** → popup tutup, game resume
- [ ] Klik **Lanjutkan** → save + balik ke Main Menu

---

## H. Sell Chicken

- [ ] 3 needs selesai (sukses/gagal) → bubble **JUAL** muncul
- [ ] Klik JUAL → coin bertambah, ayam ilang dari kandang
- [ ] Gagal semua: reward = 90 - 3×30 = **0 coin**
- [ ] Sukses semua: reward = **90 coin**
- [ ] Sebagian gagal: reward sesuai jumlah fail

---

## I. Save & Load

- [ ] Beli ayam → main menu → masuk lagi → ayam **restore**
- [ ] Progress needs (satisfied/failed) **tersimpan**
- [ ] IoT states (ON/OFF) **tersimpan**
- [ ] Jumlah **coin** sesuai terakhir (gak reset ke 400)
- [ ] Jumlah **feed** sesuai terakhir
- [ ] Level unlock status **tersimpan**
- [ ] Clear save → fresh state: coin 400, feed 0, no chickens

---

## J. IoT Collection

- [ ] **Koleksi IoT** dari Main Menu → tampilkan semua produk
- [ ] Produk yang sudah dibeli: badge "OWNED"
- [ ] Produk yang belum: tampilkan harga
- [ ] Coin refresh di koleksi IoT

---

## K. Timer & Time-Up

- [ ] **Starter** (300s): timer jalan, time-up → popup muncul
- [ ] **Beginner** (120s): timer jalan
- [ ] **Intermediate** (180s): timer jalan
- [ ] Time-up popup: tampilkan total coin
- [ ] **Continue** button: pindah ke level berikutnya (kalo ada)
- [ ] **Back** button: balik ke Main Menu

---

## L. Edge Cases

- [ ] **Kandang penuh**: beli ayam pas semua slot terisi → disable / kasih tahu
- [ ] **Feed habis**: tap feed bubble → muncul UIAlertPanel FoodOut
- [ ] **Pause lalu exit game**: progress tetep tersimpan
- [ ] **Close app mid-minigame**: restore sebagai care bubble (fresh timer)
- [ ] **0 coin**: gak bisa beli apa-apa, alert muncul
- [ ] **Quick tap spam**: gak bikin state corrupt (double complete, dsb)
- [ ] **Ganti scene cepat**: gak boleh ada null ref dari singleton

---

## M. Visual & Polish

- [ ] HPPanel slide animation mulus
- [ ] Coop status panel (CoopStatusManager) — icon sukses/gagal per need
- [ ] Floating feedback "+X Coin" animasi naik
- [ ] Camera shake pas minigame selesai
- [ ] UIAlertPanel notifikasi: FoodOut, CoinOut, TimeOut — auto-hide 3 detik
- [ ] HealthCheckResultOverlay: "Berhasil!" (hijau) / "Gagal!" (merah)
- [ ] TimeUpPopup: tampil pas timer level habis
- [ ] Gak ada tombol yang overlap / blocked raycast
