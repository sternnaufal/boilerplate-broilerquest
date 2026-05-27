# AI Agent Prompt – CoopQuest Game Balancing Variable Analysis

## Konteks

Kamu adalah AI agent yang bertugas menganalisis codebase sebuah game simulasi peternakan ayam (CoopQuest).
Game ini memiliki sistem balancing ekonomi berbasis variabel. Tugasmu adalah menemukan dan mendokumentasikan
semua fungsi/method yang bertanggung jawab atas perubahan nilai variabel-variabel utama game.

---

## Variabel Utama yang Harus Ditelusuri

### 1. `Gold`

- Tipe: integer/float
- Fungsi: mata uang utama player
- Alias yang mungkin di code: `gold`, `Gold`, `currency`, `money`, `playerGold`, `uang`

### 2. `HargaJualAyam`

- Tipe: integer/float
- Fungsi: harga jual satu ekor ayam, dipengaruhi oleh kondisi perawatan
- Nilai awal: `90`
- Nilai minimum: `0`
- Degradasi: turun `30` per kondisi yang gagal (max 3 kondisi)
- Alias yang mungkin: `hargaJual`, `chickenSellPrice`, `sellPrice`, `hargaJualAyam`, `chickenPrice`

### 3. `JumlahPakan`

- Tipe: integer
- Fungsi: stok pakan ayam yang dimiliki player
- Alias yang mungkin: `jumlahPakan`, `feedCount`, `pakanCount`, `feedStock`, `pakan`

---

## Mapping Kondisi → Variabel yang Berubah

Untuk setiap kondisi di bawah, temukan fungsi/method yang dipanggil:

| Kondisi (Event)                       | Variabel Berubah     | Nilai Perubahan            | Scene                  |
| ------------------------------------- | -------------------- | -------------------------- | ---------------------- |
| Modal Awal / Game Start               | Gold++               | +400                       | Choose Level, Gameplay |
| Beli Ayam                             | Gold--               | -40                        | Gameplay               |
| Beli Ayam                             | HargaJualAyam (init) | = 90                       | Gameplay               |
| Beli Pakan Ayam                       | Gold--               | -5                         | Gameplay               |
| Beli Pakan Ayam                       | JumlahPakan++        | +1                         | Gameplay               |
| Kasih Makan Ayam (berhasil)           | JumlahPakan--        | -1                         | Gameplay               |
| Gagal Kasih Makan Ayam (timeout 25s)  | HargaJualAyam--      | -30                        | Gameplay               |
| Gagal Mendinginkan Ayam (timeout 25s) | HargaJualAyam--      | -30                        | Gameplay               |
| Gagal Memanaskan Ayam (timeout 25s)   | HargaJualAyam--      | -30                        | Gameplay               |
| Beli Auto Feeder                      | Gold--               | -200                       | Gameplay               |
| Beli Auto Fan                         | Gold--               | -300                       | Gameplay               |
| Beli Auto Heater                      | Gold--               | -300                       | Gameplay               |
| Jual Ayam                             | Gold++               | += HargaJualAyam (current) | Gameplay               |
| Buka Lahan Beginner                   | Gold--               | -1000                      | Choose Level           |
| Buka Lahan Intermediate               | Gold--               | -2500                      | Choose Level           |

---

## Logika Harga Jual Ayam

HargaJualAyam mengikuti tier berdasarkan jumlah kondisi perawatan yang berhasil dipenuhi:

```
Jika semua kondisi terpenuhi (sempurna) → HargaJualAyam = 90  → Untung +45
Jika 2 kondisi terpenuhi              → HargaJualAyam = 60  → Untung +15
Jika 1 kondisi terpenuhi              → HargaJualAyam = 30  → Rugi   -15
Jika tidak ada kondisi terpenuhi      → HargaJualAyam = 0   → Rugi   -45

Modal per ayam = 40
Biaya pakan per ayam = 5
Total modal per ayam (tanpa auto) = 45
```

**Cari fungsi yang:**

- Menghitung `HargaJualAyam` final sebelum transaksi jual
- Mengecek berapa kondisi yang terpenuhi dari 3 kondisi (makan, suhu dingin, suhu panas)
- Mereset `HargaJualAyam` ke 90 setiap kali ayam baru dibeli

---

## Sistem Puzzle (Timer-Based)

Tiga jenis puzzle dengan batas waktu 25 detik:

- `Puzzle Kasih Makan` → timer 25 detik → jika gagal: `HargaJualAyam -= 30`
- `Puzzle Mendinginkan Ayam` → timer 25 detik → jika gagal: `HargaJualAyam -= 30`
- `Puzzle Memanaskan Ayam` → timer 25 detik → jika gagal: `HargaJualAyam -= 30`

**Cari fungsi yang:**

- Memulai timer 25 detik untuk setiap puzzle
- Menangani callback saat timer habis (timeout handler)
- Menentukan apakah puzzle berhasil atau gagal
- Memanggil penalti `HargaJualAyam -= 30` saat gagal

---

## Instruksi untuk Agent

### Langkah 1 – Temukan file yang relevan

Cari file yang kemungkinan berisi logika ekonomi dan variabel game:

```
search_files("Gold", "currency", "hargaJual", "jumlahPakan", "sellPrice", "feedCount")
search_files("puzzle", "timer", "timeout", "25")
search_files("buyChicken", "sellChicken", "belayam", "jualAyam")
search_files("ChooseLevel", "Gameplay", "GameplayScene")
```

### Langkah 2 – Identifikasi kelas/manager utama

Cari class atau singleton yang kemungkinan mengelola state ekonomi:

- `GameManager`, `EconomyManager`, `PlayerData`, `GameState`
- `ChickenManager`, `FarmManager`, `InventoryManager`
- Class yang punya field `gold`, `Gold`, atau `currency`

### Langkah 3 – Telusuri setiap event

Untuk setiap kondisi di tabel Mapping di atas, temukan:

1. Nama fungsi/method yang dipanggil
2. File dan baris kode tempat fungsi itu berada
3. Parameter apa yang diterima fungsi tersebut
4. Apakah ada validasi sebelum perubahan nilai (misal: cek apakah Gold cukup)

### Langkah 4 – Dokumentasikan hasilnya

Buat laporan dengan format:

```
VARIABEL: Gold
  EVENT: Beli Ayam
    FUNGSI: [nama fungsi]
    FILE: [path file]
    BARIS: [nomor baris]
    LOGIKA: [penjelasan singkat apa yang dilakukan fungsi]
    VALIDASI: [apakah ada pengecekan sebelum Gold dikurangi?]
```

---

## Pertanyaan Spesifik untuk Agent

1. Di mana nilai awal `Gold = 400` di-set? Apakah di constructor, Awake(), Start(), atau fungsi init terpisah?
2. Apakah `HargaJualAyam` di-reset setiap kali ayam dibeli, atau di-carry over antar ayam?
3. Apakah ada proteksi agar `Gold` tidak bisa negatif?
4. Bagaimana Auto Feeder, Auto Fan, dan Auto Heater mempengaruhi puzzle — apakah mereka bypass timer, atau auto-solve puzzle?
5. Apakah ada event atau Observer pattern yang dipanggil setiap kali `Gold` berubah (untuk update UI)?
6. Di scene `Choose Level`, apakah biaya buka lahan dicheck dulu terhadap saldo Gold sebelum dipotong?
7. Fungsi mana yang menentukan apakah player "sempurna" atau "gagal" — dan di mana keputusan akhir harga jual ditetapkan?

---

## Output yang Diharapkan

Setelah analisis selesai, agent harus menghasilkan:

- Daftar lengkap fungsi per variabel
- Dependency map: fungsi mana yang memanggil fungsi mana
- Potensi bug atau inkonsistensi balancing yang ditemukan di code
- Rekomendasi titik code yang perlu dimodifikasi jika nilai balancing ingin diubah (misal: Gold awal bukan 400 tapi 500)
