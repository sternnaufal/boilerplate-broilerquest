# Rencana Implementasi Fitur: Jigsaw Puzzle Minigame
**Proyek:** BroilerQuest — Branch `dev/Hylmi`
**Tanggal Analisis:** 24 Mei 2026
**Commit Terakhir Dibaca:** `35a3a20` — feat: implement KoleksiIoT scene

---

## Ringkasan Eksekutif

Fitur ini menggantikan minigame timing-bar (`PopupKesehatan`) untuk event perawatan ayam — **pakan (Feed)**, **kedinginan (Cooling)**, dan **kepanasan (Heating)** — dengan sebuah **jigsaw puzzle 3×3 berbasis klik** yang harus diselesaikan dalam 30 detik. Berhasil menyelesaikan puzzle = event perawatan sukses; waktu habis atau keluar = gagal.

Kabar baiknya: arsitektur codebase sekarang **sudah sangat siap** untuk fitur ini. `IHealthCheckListener`, `GameStateManager`, `CoroutineHelper`, `ButtonHelper`, dan `GameConstants` semuanya sudah ada dan bisa langsung dipakai. Tidak diperlukan perombakan sistem yang sudah ada.

---

## 1. Evaluasi Library dan Teknik yang Tersedia

Ini adalah bagian terpenting sebelum menulis satu baris kode pun. Pilihan teknik dan library yang tepat menentukan seberapa mudah implementasi dan seberapa besar risiko bug.

### 1.1 Masalah Terbesar: Cara Menampilkan Potongan Gambar di Grid

Ini adalah keputusan teknis paling kritis di seluruh fitur. Ada **tiga pendekatan berbeda**, dengan trade-off yang sangat berbeda:

---

#### ✅ Pilihan A: `RawImage` + `uvRect` — DIREKOMENDASIKAN

Unity memiliki komponen `RawImage` yang bisa menampilkan **region tertentu dari sebuah texture** menggunakan property `uvRect`. Ini berarti kita tidak perlu memotong texture sama sekali — cukup atur "jendela tampil" dari setiap tile.

```csharp
// Untuk tile di baris=1, kolom=2 pada grid 3x3:
RawImage tile = GetComponent<RawImage>();
tile.texture = puzzleTexture;  // satu texture, dipakai oleh semua 9 tile

float size = 1f / 3f;          // 1/gridSize
float u = col * size;          // offset horizontal (0.0, 0.33, 0.66)
float v = (2 - row) * size;   // offset vertikal, dibalik karena UV origin = kiri-bawah

tile.uvRect = new Rect(u, v, size, size);
```

**Keuntungan:**
- **Tidak butuh Read/Write Enabled** sama sekali — menghilangkan risiko terbesar di versi sebelumnya
- **Tidak ada alokasi Texture2D baru** — tidak ada memory leak
- **Tidak ada `GetPixels()`** — tidak ada kemungkinan crash di platform tertentu
- Performa lebih baik: satu texture, sembilan view berbeda
- Saat swap tile, cukup tukar nilai `uvRect` — sangat efisien

**Kekurangan:**
- Harus pakai `RawImage` bukan `Image` — tetapi tidak ada masalah karena `RawImage` juga klik-able dan bisa dipakai bersama `Button`
- UV coordinate sedikit lebih manual untuk dihitung

**Kesimpulan: Ini adalah teknik yang benar untuk kasus ini.** Tidak ada alasan untuk memakai `GetPixels()` jika `RawImage + uvRect` tersedia.

---

#### ⚠️ Pilihan B: `GetPixels()` + `new Texture2D` — TIDAK DIREKOMENDASIKAN

Pendekatan ini memotong texture secara literal menggunakan CPU, membuat 9 `Texture2D` baru, lalu membuat 9 `Sprite` baru. Ini adalah pendekatan yang ditulis di versi dokumen pertama.

**Masalah:**
- **Wajib Read/Write Enabled** — jika lupa, runtime crash
- **9 Texture2D baru per puzzle** — harus di-`Destroy()` manual, rawan memory leak
- **Lambat:** `GetPixels()` adalah operasi CPU yang memindahkan data dari GPU ke RAM
- Lebih banyak kode untuk hal yang sama

**Kapan dipakai:** Jika kamu perlu memodifikasi pixel secara programatik (blur, grayscale, efek khusus per tile). Untuk puzzle sederhana ini, tidak perlu.

---

#### ⚠️ Pilihan C: Sprite Slicing via `Sprite.Create(texture, rect, pivot)` — LEBIH BAIK DARI B, TAPI KALAH DARI A

`Sprite.Create()` bisa membuat sprite dari sub-region texture tanpa `GetPixels()`. Tidak butuh Texture2D baru. Tapi tetap butuh **Read/Write Enabled** pada beberapa versi Unity, dan tidak se-efisien `RawImage + uvRect`.

---

### 1.2 Library Animasi: DOTween vs Built-in

Animasi saat tile di-swap (tile "meluncur" ke posisi baru) membuat pengalaman bermain jauh lebih enak. Ada dua pilihan:

#### DOTween (Free — Demigiant)
DOTween adalah library tweening paling populer di ekosistem Unity. **Tidak ada di project saat ini**, tapi instalasinya sangat mudah melalui Package Manager atau Asset Store.

```csharp
// Contoh: gerakkan tile A ke posisi tile B dengan durasi 0.2 detik
tileA.transform.DOMove(positionB, 0.2f).SetEase(Ease.OutQuad);
tileB.transform.DOMove(positionA, 0.2f).SetEase(Ease.OutQuad);
```

**Keuntungan:**
- Kode sangat ringkas dan readable
- Banyak pilihan easing (OutBounce, OutElastic, dll.)
- Support `SetUpdate(true)` untuk berjalan saat `Time.timeScale = 0` — penting jika game dipause
- Sudah dipakai di jutaan Unity project, sangat stabil
- **Free** di Asset Store (ada versi Pro berbayar untuk fitur tambahan)

**Cara install:** Window → Package Manager → Add package from git URL:
```
https://github.com/Demigiant/dotween.git
```
Atau download dari Asset Store: search "DOTween".

#### Built-in: Coroutine + `Vector2.Lerp`
Alternatif tanpa dependency eksternal:

```csharp
private IEnumerator SlideToPosition(RectTransform rect, Vector2 targetPos, float duration)
{
    Vector2 startPos = rect.anchoredPosition;
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.unscaledDeltaTime; // unscaled agar tidak dipengaruhi timeScale
        rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / duration);
        yield return null;
    }
    rect.anchoredPosition = targetPos;
}
```

**Keuntungan:** Tidak butuh library eksternal, menggunakan `CoroutineHelper` yang sudah ada.
**Kekurangan:** Lebih verbose, easing terbatas (hanya linear kecuali tulis sendiri).

**Rekomendasi:** Kalau DOTween belum ada di project dan tim tidak keberatan menambahkan dependency, **pasang DOTween** — penghematan waktu penulisan kode animasi sangat signifikan. Kalau ingin zero-dependency, Coroutine + Lerp sudah cukup baik.

---

### 1.3 Unity Asset Store: Jigsaw Puzzle Asset

Ada beberapa asset di Unity Asset Store yang menyediakan sistem jigsaw siap pakai (misalnya "Jigsaw Puzzle Kit", "Puzzle Game Template"). Namun untuk kasus ini:

- Puzzle yang dibutuhkan sangat spesifik: **3×3, swap-based, dengan timer, terintegrasi ke sistem event kandang**
- Asset-asset tersebut umumnya didesain untuk puzzle mandiri, bukan minigame dalam game lain
- Adaptasinya ke `IHealthCheckListener` dan flow `StarterKandangSlot` kemungkinan memakan waktu lebih lama daripada membangun sendiri

**Kesimpulan: Tidak direkomendasikan untuk kasus ini.** Build sendiri dengan `RawImage + uvRect` jauh lebih terkontrol.

---

### 1.4 Ringkasan Keputusan Library

| Kebutuhan | Pilihan Terbaik | Alasan |
|-----------|----------------|--------|
| Menampilkan potongan gambar | `RawImage + uvRect` (built-in Unity) | Zero risk, zero memory leak, tidak butuh Read/Write |
| Animasi swap tile | DOTween (install) atau Coroutine+Lerp (built-in) | DOTween lebih cepat dikembangkan; Lerp cukup untuk MVP |
| Timer countdown | `WaitForSecondsRealtime` coroutine (built-in) | Sudah sesuai, tidak terpengaruh timeScale pause |
| Layout grid | `GridLayoutGroup` (built-in Unity UI) | Sudah ada, tidak perlu hitung posisi manual |
| Highlight seleksi | `Image` overlay dengan `enabled = false/true` | Cukup, tidak butuh shader |
| Whole asset kit | ❌ Tidak direkomendasikan | Terlalu rigid untuk diintegrasikan |

---

## 2. Analisis Codebase Saat Ini

### 2.1 Titik Integrasi yang Sudah Ada

Saat pemain mengklik kandang yang sedang dalam state `WaitingForCareClick`, flow berikut sudah berjalan:

```
StarterKandangSlot.OnPointerClick()
  └─► TryStartHealthMinigame()          ← TITIK MASUK FITUR BARU
        └─► PopupKesehatan.ShowHealthCheck(this)
              └─► [timing bar minigame]
                    └─► IHealthCheckListener.OnHealthCheckSuccess() / OnHealthCheckFailure()
                          └─► CompleteCurrentNeed() atau HideBubble() + StartNeedTimer()
```

Fitur jigsaw **cukup mengganti `PopupKesehatan`** sebagai handler dari titik `TryStartHealthMinigame()`. Interface `IHealthCheckListener` sudah dipakai oleh `StarterKandangSlot` — kita tinggal membuat `JigsawMinigameController` yang juga memanggil interface yang sama.

### 2.2 Field yang Relevan di `StarterKandangSlot`

| Field | Nilai Saat Ini | Relevansi |
|-------|---------------|-----------|
| `useHealthMinigame` | `bool` (serialized) | Toggle untuk aktifkan minigame |
| `feedBubbleSprite` | `Sprite` | Bisa dipakai sebagai fallback gambar puzzle |
| `coolingBubbleSprite` | `Sprite` | Bisa dipakai sebagai fallback gambar puzzle |
| `heatingBubbleSprite` | `Sprite` | Bisa dipakai sebagai fallback gambar puzzle |
| `currentNeed` | `ChickenNeed` enum | Menentukan gambar mana yang ditampilkan di puzzle |

### 2.3 Gambar yang Tersedia di `Assets/Gambar/`

| File | Cocok Untuk |
|------|-------------|
| `pakan puzzle.png` | Event Feed (ayam minta makan) |
| `dingin puzzle.png` | Event Cooling (ayam kedinginan) |
| `panas puzzle.png` | Event Heating (ayam kepanasan) |

Ketiga gambar puzzle ini sudah ada di project dengan resolusi `1024x1024`. Dengan teknik `RawImage + uvRect`, gambar-gambar ini **tidak perlu disentuh setting Read/Write-nya** — langsung bisa dipakai. Gambar `ayam biasa.png`, `ayam kedinginan.png`, dan `ayam kebakar.png` tetap dipakai untuk visual/animasi ayam, bukan sebagai texture utama puzzle.

---

## 3. Arsitektur yang Direkomendasikan

### 3.1 Diagram Alur Fitur

```
Pemain klik kandang (WaitingForCareClick)
  │
  ▼
TryStartHealthMinigame()
  │
  ├─── useHealthMinigame = false ──► CompleteCurrentNeed() langsung
  │
  └─── useHealthMinigame = true
         │
         ▼
   JigsawMinigameController.ShowJigsaw(
       listener: this (IHealthCheckListener),
       puzzleTexture: GetNeedTexture(currentNeed)
   )
         │
         ├── Mulai: set uvRect tiap tile, acak susunan, tampilkan panel, mulai timer 30 detik
         │
         ├── Loop: pemain pilih tile A → pilih tile B → swap uvRect → cek solved?
         │     ├── Solved ──► TutupPanel() → listener.OnHealthCheckSuccess()
         │     └── Belum ──► lanjutkan
         │
         └── Timer habis ──► TutupPanel() → listener.OnHealthCheckFailure()
```

### 3.2 File Baru yang Perlu Dibuat

```
Assets/Script/
├── JigsawMinigameController.cs   ← Controller utama (singleton, DontDestroyOnLoad)
└── JigsawPiece.cs                ← Script per-tile jigsaw

Assets/Prefab/
├── JigsawMinigame.prefab         ← Panel UI lengkap
└── JigsawTile.prefab             ← Prefab satu tile (RawImage + JigsawPiece, tanpa Button)
```

### 3.3 File yang Perlu Dimodifikasi

| File | Perubahan |
|------|-----------|
| `GameConstants.cs` | Tambah nested class `JigsawMinigame` |
| `StarterKandangSlot.cs` | Tambah field `Texture` per event, routing di `TryStartHealthMinigame()` |

---

## 4. Spesifikasi Teknis Lengkap

### 4.1 `GameConstants.cs` — Tambahan

```csharp
public static class JigsawMinigame
{
    public const int GridSize = 3;
    public const float TimeLimit = 30f;
    public const float TileSize = 150f;       // ukuran satu tile di UI (px)
    public const float TileSpacing = 4f;      // jarak antar tile
    public const float SwapDuration = 0.15f;  // durasi animasi swap (detik)
    public const float WarningThreshold = 10f; // timer berubah warna merah di bawah ini
}
```

---

### 4.2 `JigsawPiece.cs` (Full)

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JigsawPiece : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RawImage tileImage;
    [SerializeField] private Image highlightBorder;

    public int CorrectIndex { get; private set; }
    public int CurrentIndex  { get; set; }
    public Rect CurrentUvRect => tileImage.uvRect;

    public bool IsInCorrectPosition => CurrentIndex == CorrectIndex;

    public void Setup(int correctIndex, int currentIndex, Texture texture, Rect uvRect)
    {
        CorrectIndex = correctIndex;
        CurrentIndex = currentIndex;
        tileImage.texture = texture;
        tileImage.uvRect = uvRect;
        SetHighlighted(false);
    }

    public void SetUvRect(Rect rect) => tileImage.uvRect = rect;

    public void SetHighlighted(bool on)
    {
        if (highlightBorder != null)
            highlightBorder.enabled = on;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        JigsawMinigameController.Instance?.OnPieceClicked(this);
    }
}
```

---

### 4.3 `JigsawMinigameController.cs` (Full, menggunakan RawImage + uvRect)

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JigsawMinigameController : MonoBehaviour
{
    public static JigsawMinigameController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private Button closeButton;

    [Header("Tile Prefab")]
    [SerializeField] private GameObject tilePrefab;   // prefab dengan JigsawPiece + RawImage

    [Header("Settings")]
    [SerializeField] private float timeLimit = GameConstants.JigsawMinigame.TimeLimit;
    [SerializeField] private int gridSize   = GameConstants.JigsawMinigame.GridSize;

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor  = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.25f, 0.15f);

    private IHealthCheckListener currentListener;
    private JigsawPiece[]        pieces;
    private JigsawPiece          selectedPiece;
    private float                timeRemaining;
    private bool                 isPlaying;
    private Coroutine            timerCoroutine;

    public bool IsPlaying => isPlaying;

    // ─── Lifecycle ────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        popupRoot.SetActive(false);
    }

    private void Start()
    {
        ButtonHelper.SetSingleListener(closeButton, () => CompleteWithFailure());
    }

    // ─── Public API ───────────────────────────────────────────────

    public bool ShowJigsaw(IHealthCheckListener listener, Texture puzzleTexture, string eventTitle = "")
    {
        if (isPlaying || listener == null || puzzleTexture == null)
            return false;

        currentListener = listener;
        selectedPiece   = null;
        isPlaying       = false;

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(eventTitle) ? "Bantu Ayammu!" : eventTitle;

        BuildGrid(puzzleTexture);

        timeRemaining = timeLimit;
        UpdateTimerUI();

        popupRoot.SetActive(true);
        CoroutineHelper.StopAndStart(this, ref timerCoroutine, TimerRoutine());
        isPlaying = true;
        return true;
    }

    // Dipanggil oleh JigsawPiece.OnPointerClick()
    public void OnPieceClicked(JigsawPiece clicked)
    {
        if (!isPlaying) return;

        if (selectedPiece == null)
        {
            selectedPiece = clicked;
            selectedPiece.SetHighlighted(true);
            return;
        }

        if (selectedPiece == clicked)
        {
            selectedPiece.SetHighlighted(false);
            selectedPiece = null;
            return;
        }

        // Swap
        selectedPiece.SetHighlighted(false);
        SwapPieces(selectedPiece, clicked);
        selectedPiece = null;

        if (IsSolved())
            CompleteWithSuccess();
    }

    // ─── Grid Construction ────────────────────────────────────────

    private void BuildGrid(Texture texture)
    {
        // Bersihkan tile lama
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        int total = gridSize * gridSize;

        // Hitung uvRect per posisi grid — ini menggantikan seluruh GetPixels() approach
        Rect[] uvRects = ComputeUvRects(gridSize);

        // Shuffle: tentukan uvRect mana yang ditaruh di posisi mana
        int[] shuffledOrder = GenerateShuffledIndices(total);

        pieces = new JigsawPiece[total];

        for (int i = 0; i < total; i++)
        {
            GameObject tileObj = Instantiate(tilePrefab, gridContainer);
            JigsawPiece piece = tileObj.GetComponent<JigsawPiece>();

            // correctIndex = i  → tile ini "seharusnya" di posisi i
            // currentIndex = shuffledOrder[i] → tapi sekarang menampilkan uvRect dari posisi lain
            piece.Setup(
                correctIndex: i,
                currentIndex: shuffledOrder[i],
                texture:      texture,
                uvRect:       uvRects[shuffledOrder[i]]
            );
            pieces[i] = piece;
        }
    }

    // Hitung UV rect untuk setiap cell grid
    // UV origin Unity = kiri-bawah, row 0 = baris paling bawah texture
    // Kita balik agar row 0 = baris paling atas visual (sesuai ekspektasi user)
    private Rect[] ComputeUvRects(int n)
    {
        float size = 1f / n;
        Rect[] rects = new Rect[n * n];

        for (int row = 0; row < n; row++)
        {
            for (int col = 0; col < n; col++)
            {
                int index = row * n + col;
                float u = col * size;
                float v = (n - 1 - row) * size; // flip vertikal
                rects[index] = new Rect(u, v, size, size);
            }
        }

        return rects;
    }

    // ─── Swap Logic ───────────────────────────────────────────────

    private void SwapPieces(JigsawPiece a, JigsawPiece b)
    {
        // Tukar uvRect (gambar yang ditampilkan)
        Rect uvA = a.CurrentUvRect;
        int  idxA = a.CurrentIndex;

        a.SetUvRect(b.CurrentUvRect);
        a.CurrentIndex = b.CurrentIndex;

        b.SetUvRect(uvA);
        b.CurrentIndex = idxA;

        // Opsional: tambahkan animasi scale bounce singkat
        // Jika DOTween terpasang:
        //   a.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f);
        //   b.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f);
        // Jika tidak pakai DOTween, StartCoroutine(BounceScale(a.transform)) juga cukup
    }

    private bool IsSolved()
    {
        foreach (JigsawPiece piece in pieces)
            if (!piece.IsInCorrectPosition) return false;
        return true;
    }

    // ─── Shuffle ──────────────────────────────────────────────────

    private int[] GenerateShuffledIndices(int count)
    {
        int[] indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = i;

        // Fisher-Yates
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        // Pastikan tidak langsung solved
        bool alreadySolved = true;
        for (int i = 0; i < count; i++)
            if (indices[i] != i) { alreadySolved = false; break; }

        return alreadySolved ? GenerateShuffledIndices(count) : indices;
    }

    // ─── Timer ────────────────────────────────────────────────────

    private IEnumerator TimerRoutine()
    {
        while (timeRemaining > 0f)
        {
            yield return new WaitForSecondsRealtime(1f); // WaitForSecondsRealtime agar tidak dibekukan pause
            timeRemaining -= 1f;
            UpdateTimerUI();
        }
        CompleteWithFailure();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        timerText.text  = Mathf.CeilToInt(timeRemaining).ToString();
        timerText.color = timeRemaining <= GameConstants.JigsawMinigame.WarningThreshold
            ? warningTimerColor
            : normalTimerColor;
    }

    // ─── Completion ───────────────────────────────────────────────

    private void CompleteWithSuccess()
    {
        if (!isPlaying) return;
        isPlaying = false;
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        popupRoot.SetActive(false);
        GameLog.Info("JigsawMinigame: Berhasil!");
        currentListener?.OnHealthCheckSuccess();
        currentListener = null;
    }

    private void CompleteWithFailure()
    {
        if (!isPlaying) return;
        isPlaying = false;
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        popupRoot.SetActive(false);
        GameLog.Info("JigsawMinigame: Gagal / waktu habis.");
        currentListener?.OnHealthCheckFailure();
        currentListener = null;
    }
}
```

---

### 4.4 Modifikasi `StarterKandangSlot.cs`

Tambahkan field berikut di bawah `[Header("Optional Health Minigame")]`:

```csharp
[Header("Jigsaw Puzzle Textures")]
[SerializeField] private Texture jigsawFeedTexture;      // "pakan puzzle.png"
[SerializeField] private Texture jigsawCoolingTexture;   // "dingin puzzle.png"
[SerializeField] private Texture jigsawHeatingTexture;   // "panas puzzle.png"
```

> Perhatikan: tipe yang dipakai adalah `Texture`, bukan `Sprite`. `RawImage` bekerja langsung dengan `Texture` (atau `Texture2D` yang merupakan turunannya), sehingga texture **tidak perlu di-import sebagai Sprite** — cukup tipe Default.

Modifikasi `TryStartHealthMinigame()`:

```csharp
private bool TryStartHealthMinigame()
{
    if (!useHealthMinigame)
        return false;

    if (JigsawMinigameController.Instance != null && JigsawMinigameController.Instance.IsPlaying)
        return true; // blokir direct-complete jika puzzle lain sedang aktif

    // Prioritas 1: JigsawMinigameController (fitur baru)
    if (JigsawMinigameController.Instance != null)
    {
        Texture tex = GetNeedTexture(currentNeed);
        string title = GetNeedTitle(currentNeed);
        if (tex != null)
        {
            currentState = SlotState.WaitingForHealthMinigame;
            NotifyStateChanged();
            if (JigsawMinigameController.Instance.ShowJigsaw(this, tex, title))
                return true;
            currentState = SlotState.WaitingForCareClick;
            NotifyStateChanged();
        }
    }

    // Fallback: PopupKesehatan (timing bar lama)
    if (PopupKesehatan.Instance != null)
    {
        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        PopupKesehatan.Instance.ShowHealthCheck(this);
        return true;
    }

    Debug.LogWarning($"{name}: Tidak ada minigame yang tersedia, event diselesaikan langsung.");
    return false;
}

private Texture GetNeedTexture(ChickenNeed need)
{
    Texture configuredTexture = null;
    Sprite fallbackSprite = null;

    switch (need)
    {
        case ChickenNeed.Feed:
            configuredTexture = jigsawFeedTexture;
            fallbackSprite = feedBubbleSprite;
            break;
        case ChickenNeed.Cooling:
            configuredTexture = jigsawCoolingTexture;
            fallbackSprite = coolingBubbleSprite;
            break;
        case ChickenNeed.Heating:
            configuredTexture = jigsawHeatingTexture;
            fallbackSprite = heatingBubbleSprite;
            break;
    }

    if (configuredTexture != null)
        return configuredTexture;

    return fallbackSprite != null ? fallbackSprite.texture : null;
}

private string GetNeedTitle(ChickenNeed need)
{
    switch (need)
    {
        case ChickenNeed.Feed:    return "Ayam lapar! Susun makanannya!";
        case ChickenNeed.Cooling: return "Ayam kedinginan! Nyalakan kipas!";
        case ChickenNeed.Heating: return "Ayam kepanasan! Nyalakan heater!";
        default:                  return "Bantu ayammu!";
    }
}
```

---

## 5. Setup di Unity Editor

### 5.1 Persiapan Texture

Karena kita menggunakan `RawImage + uvRect`, **tidak ada syarat khusus** untuk texture. Gambar bisa diimport dengan setting Default (Texture Type: Default atau Sprite, keduanya bisa). **Tidak perlu Read/Write Enabled.**

Satu-satunya yang perlu diperhatikan: pastikan **Compression** tidak terlalu agresif agar gambar terlihat tajam saat di-split 3×3.

### 5.2 Membuat Prefab `JigsawTile`

```
JigsawTile (RectTransform)
├── RawImage (RawImage component, raycastTarget=true) ← tileImage
└── HighlightBorder (Image, kuning/oranye, enabled=false) ← outline seleksi
JigsawPiece (script) ← assign tileImage dan highlightBorder
```

### 5.3 Membuat Prefab `JigsawMinigame`

```
JigsawMinigame (GameObject, DontDestroyOnLoad candidate)
├── Backdrop (Image, hitam semi-transparan, raycastTarget=true)
└── Panel (Image, background panel)
    ├── TitleText (TextMeshProUGUI)
    ├── TimerText (TextMeshProUGUI) — font besar, posisi atas
    ├── GridContainer (menggunakan GridLayoutGroup)
    │    ├── Constraint: Fixed Column Count = 3
    │    ├── Cell Size: (150, 150)
    │    └── Spacing: (4, 4)
    └── CloseButton (Button, optional)
JigsawMinigameController (script) ← assign semua referensi
```

### 5.4 Wiring di Scene `Starter`

Di setiap `StarterKandangSlot` di Inspector:
- **Use Health Minigame**: ✓
- **Jigsaw Feed Texture**: assign `pakan puzzle.png`
- **Jigsaw Cooling Texture**: assign `dingin puzzle.png`
- **Jigsaw Heating Texture**: assign `panas puzzle.png`

---

## 6. Opsional: Menambahkan DOTween untuk Animasi

Jika tim memutuskan memasang DOTween, swap tile bisa dibuat lebih halus dengan menambahkan beberapa baris ini di `SwapPieces()`:

```csharp
// Tambahkan di atas file: using DG.Tweening;

private void SwapPieces(JigsawPiece a, JigsawPiece b)
{
    Rect uvA  = a.CurrentUvRect;
    int  idxA = a.CurrentIndex;

    a.SetUvRect(b.CurrentUvRect);
    a.CurrentIndex = b.CurrentIndex;
    b.SetUvRect(uvA);
    b.CurrentIndex = idxA;

    // Animasi punch scale — tile "mekar" sebentar saat swap
    float duration = GameConstants.JigsawMinigame.SwapDuration;
    a.transform.DOPunchScale(Vector3.one * 0.12f, duration, vibrato: 1, elasticity: 0.5f)
               .SetUpdate(true); // SetUpdate(true) = berjalan saat Time.timeScale = 0
    b.transform.DOPunchScale(Vector3.one * 0.12f, duration, vibrato: 1, elasticity: 0.5f)
               .SetUpdate(true);
}
```

**Cara install DOTween:**
1. Buka Unity Asset Store: search "DOTween (HOTween v2)"
2. Download dan Import (Free version sudah cukup)
3. Jalankan DOTween Setup panel yang muncul otomatis
4. Tambahkan `using DG.Tweening;` di file yang membutuhkan

---

## 7. Risiko dan Mitigasi (Revisi)

| Risiko | Kemungkinan | Dampak | Mitigasi |
|--------|------------|--------|----------|
| ~~Texture tidak Read/Write Enabled~~ | ~~Tinggi~~ | ~~Crash~~ | ✅ **Tidak ada lagi** — `RawImage + uvRect` tidak butuh ini |
| Gambar tidak persegi | Sedang | Tile terlihat gepeng | Set `Aspect Ratio Mode = Envelope Parent` di `RawImage` atau pastikan gambar persegi |
| Puzzle acak tidak bisa diselesaikan | Rendah | Pengalaman buruk | Fisher-Yates selalu solvable; rekursi jika kebetulan solved dari awal |
| Timer tidak berjalan saat pause | Rendah | Bug gameplay | Sudah diatasi: `WaitForSecondsRealtime` tidak dipengaruhi `Time.timeScale` |
| Tile ter-instantiate tapi controller belum siap | Rendah | NullRef | `DontDestroyOnLoad` + `Awake()` singleton menjamin instance tersedia |
| Konflik multi-slot (2+ kandang klik bersamaan) | Rendah | Puzzle muncul ganda | Guard `if (!isPlaying)` di `ShowJigsaw()` |

---

## 8. Rencana Pengerjaan (Sprint)

### Sprint 1 — Fondasi tanpa UI (0.5 hari)
- [ ] Tambahkan `GameConstants.JigsawMinigame`
- [ ] Buat `JigsawPiece.cs`
- [ ] Buat logika inti `JigsawMinigameController.cs`: `BuildGrid()`, `SwapPieces()`, `IsSolved()`, `GenerateShuffledIndices()`
- [ ] Tes logika swap dan cek solved via console log

### Sprint 2 — UI dan Integrasi (1 hari)
- [ ] Buat prefab `JigsawTile` (RawImage + JigsawPiece)
- [ ] Buat prefab `JigsawMinigame` (panel + GridLayoutGroup)
- [ ] Tes `RawImage + uvRect`: pastikan 9 tile menampilkan potongan gambar yang benar
- [ ] Modifikasi `StarterKandangSlot.TryStartHealthMinigame()` dengan routing ke jigsaw
- [ ] Tes end-to-end: klik kandang → puzzle muncul → selesaikan → event sukses/gagal

### Sprint 3 — Polish (0.5 hari)
- [ ] Timer berubah warna merah di bawah 10 detik
- [ ] Animasi swap (DOTween atau Coroutine+Lerp)
- [ ] Efek "berhasil" (flash hijau atau partikel singkat)
- [ ] Tes semua tiga jenis event (Feed, Cooling, Heating) dengan gambar berbeda
- [ ] Tes edge case: tutup paksa saat puzzle aktif, timer habis tepat saat swap

---

## 9. Pertanyaan yang Perlu Didiskusikan Tim

1. **Apakah DOTween akan dipasang?** Ini keputusan tim — bukan keharusan, tapi sangat membantu untuk animasi di fitur-fitur berikutnya juga.

2. **Apakah `PopupKesehatan` (timing bar) tetap dipertahankan atau dihapus?**
   Rekomendasi: pertahankan sebagai fallback dulu sampai jigsaw stabil, lalu hapus.

3. **Apakah menutup puzzle paksa dihitung gagal atau dibatalkan?**
   Rekomendasi: gagal — konsisten dengan perilaku `OnHealthCheckFailure()` yang sudah ada.

4. **Apakah gambar puzzle perlu berbeda per scene (Starter vs Beginner vs Intermediate)?**
   Jika ya, cukup ekspos field `jigsawFeedTexture` dst sebagai serialized per-slot. Tidak butuh perubahan arsitektur.

---

## Kesimpulan

Perubahan terbesar dari versi analisis sebelumnya: **teknik `RawImage + uvRect`** menggantikan `GetPixels()`, yang menghilangkan satu-satunya risiko teknis signifikan (keharusan Read/Write Enabled). Dengan teknik ini, implementasi menjadi lebih sederhana, lebih aman, dan lebih performan secara bersamaan.

Arsitektur codebase sudah sangat mendukung fitur ini tanpa perombakan. Estimasi total: **2 hari pengerjaan** untuk versi yang sudah polish dan siap diuji.
