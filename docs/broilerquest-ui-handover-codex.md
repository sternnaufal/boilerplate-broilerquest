# Handover: BroilerQuest Unity UI Panel Fix

Tanggal: 22 Mei 2026  
Repo: https://github.com/sternnaufal/boilerplate-broilerquest.git  
Target agent: Codex / AI coding agent  
Project type: Unity game project

---

## 1. Ringkasan masalah

Project ini adalah project Unity. Tugas saat ini adalah melanjutkan UI dasar agar minimal tersedia:

- Main screen
- Pause screen
- Option screen
- HUD/gameplay screen

Pesan dari team lead:

> "Aku pengen seminimalnya kita punya main screen, pause screen, option screen juga"  
> "Yo palingan ngelanjutin ini mi, kayaknya codinganku error dah, belum muncul panelnya"  
> "Udah ku bikin, tinggal munculin"

Kesimpulan awal:

Panel belum muncul kemungkinan besar karena project masih memiliki compile error di script C#. Jika Unity masih compile error, script seperti `UIManager` tidak akan berjalan, sehingga panel tidak akan muncul.

---

## 2. Environment yang disarankan

### Unity

Gunakan Unity Editor sesuai file project:

```txt
Unity 6000.3.15f1
```

File sumber:

```txt
ProjectSettings/ProjectVersion.txt
```

Isi saat dicek:

```txt
m_EditorVersion: 6000.3.15f1
```

Jika versi tersebut belum tersedia, install melalui Unity Hub.

### IDE / Editor

Disarankan:

1. **Unity Editor**  
   Wajib untuk membuka project, mengatur scene, Canvas, panel, button, dan Inspector reference.

2. **Visual Studio Code** atau **Rider**  
   Dipakai untuk mengedit file `.cs`.

Untuk user non-game-dev, workflow paling aman:

- Buka project dari Unity Hub
- Edit script dari VS Code / Rider
- Set reference panel dan button dari Unity Inspector

---

## 3. Cara membuka project

### Clone repo

```bash
git clone https://github.com/sternnaufal/boilerplate-broilerquest.git
cd boilerplate-broilerquest
```

### Buka di Unity Hub

1. Buka Unity Hub
2. Klik `Add`
3. Pilih folder `boilerplate-broilerquest`
4. Buka dengan Unity `6000.3.15f1`
5. Tunggu import selesai
6. Buka `Window > General > Console`
7. Bereskan semua error merah terlebih dahulu

---

## 4. Masalah penting di `Packages/manifest.json`

Ada dependency lokal:

```json
"com.gladekit.mcp-bridge": "file:C:/Users/ASUS/Downloads/glade-mcp-unity-0.6.3/glade-mcp-unity-0.6.3/unity-bridge"
```

Masalah:

- Path tersebut hanya valid di komputer pembuat awal.
- Di komputer lain kemungkinan besar package tidak ditemukan.
- Unity Package Manager bisa gagal restore project.

Solusi sementara:

Hapus dependency ini dari `Packages/manifest.json` jika project gagal dibuka karena package lokal hilang.

Catatan:

Project sudah memiliki package Unity AI Assistant:

```json
"com.unity.ai.assistant": "2.9.0-pre.1"
```

Jadi untuk MCP official Unity, sebaiknya gunakan package resmi Unity AI Assistant, bukan dependency lokal Glade MCP kecuali team memang sengaja memakainya.

---

## 5. Compile error yang ditemukan

### 5.1 `Assets/Script/UIManager.cs`

Masalah utama:

File ini berisi banyak pemanggilan generic Unity API tanpa tipe, contohnya:

```csharp
go.AddComponent();
canvasObj.GetComponent();
FindFirstObjectByType();
```

Di C#, Unity membutuhkan tipe komponen, misalnya:

```csharp
go.AddComponent<Canvas>();
go.GetComponent<RectTransform>();
FindFirstObjectByType<EventSystem>();
```

Ada juga token nyasar:

```txt
here
```

Token ini berada di tengah script dan membuat compile error.

Saran:

Jangan patch script lama secara kecil-kecilan. Lebih aman ganti `UIManager.cs` dengan versi sederhana berbasis Inspector reference.

---

### 5.2 `Assets/Script/CoinManager.cs`

Masalah:

Ada kalimat biasa di luar komentar:

```csharp
Jika pakai legacy Text, ganti dengan using UnityEngine.UI;
```

Solusi:

Ubah menjadi komentar:

```csharp
// Jika pakai legacy Text, ganti dengan using UnityEngine.UI;
```

---

### 5.3 Potensi error string multiline

Cek file berikut:

```txt
Assets/Script/PopupHasilKesehatan.cs
Assets/Script/TimeUpPopup.cs
```

Jika ada string yang terpotong langsung antar baris, ubah menjadi `\n`.

Contoh:

```csharp
messageText.text = "Ayam anda sakit karena kurang vitamin.\nYuk jaga kesehatannya!";
```

Contoh lain:

```csharp
Debug.Log("Tombol Continue ditekan.\n(Fitur lanjut ke level berikutnya masih di-comment)");
```

---

## 6. Solusi yang direkomendasikan

Gunakan UI manual dari Unity Editor.

Alasan:

- Lebih mudah untuk user yang bukan game developer.
- Lebih mudah di-debug.
- Tidak perlu generate Canvas, Panel, Button dari script.
- Reference panel dan button bisa di-drag dari Inspector.

Target object di scene:

```txt
Canvas
├── MainScreenPanel
├── PauseScreenPanel
├── OptionScreenPanel
└── HUDPanel

EventSystem
UIManager
```

---

## 7. Replacement `UIManager.cs`

Ganti seluruh isi:

```txt
Assets/Script/UIManager.cs
```

dengan kode berikut:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainScreenPanel;
    [SerializeField] private GameObject pauseScreenPanel;
    [SerializeField] private GameObject optionScreenPanel;
    [SerializeField] private GameObject hudPanel;

    [Header("Main Screen Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button exitButton;

    [Header("Pause Screen Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseOptionButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Option Screen Buttons")]
    [SerializeField] private Button backFromOptionButton;

    private bool openedFromPause = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RegisterButtons();
        ShowMainScreen();
    }

    private void RegisterButtons()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (optionButton != null)
            optionButton.onClick.AddListener(ShowOptionFromMain);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(ShowPauseScreen);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (pauseOptionButton != null)
            pauseOptionButton.onClick.AddListener(ShowOptionFromPause);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ShowMainScreen);

        if (backFromOptionButton != null)
            backFromOptionButton.onClick.AddListener(BackFromOption);
    }

    private void HideAllPanels()
    {
        if (mainScreenPanel != null) mainScreenPanel.SetActive(false);
        if (pauseScreenPanel != null) pauseScreenPanel.SetActive(false);
        if (optionScreenPanel != null) optionScreenPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    public void ShowMainScreen()
    {
        Time.timeScale = 1f;
        openedFromPause = false;

        HideAllPanels();

        if (mainScreenPanel != null)
            mainScreenPanel.SetActive(true);
    }

    private void StartGame()
    {
        Time.timeScale = 1f;

        HideAllPanels();

        if (hudPanel != null)
            hudPanel.SetActive(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameActive(true);
            GameManager.Instance.InitializeForCurrentScene();
        }
    }

    private void ShowPauseScreen()
    {
        Time.timeScale = 0f;
        openedFromPause = true;

        HideAllPanels();

        if (pauseScreenPanel != null)
            pauseScreenPanel.SetActive(true);
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        openedFromPause = false;

        HideAllPanels();

        if (hudPanel != null)
            hudPanel.SetActive(true);
    }

    private void ShowOptionFromMain()
    {
        openedFromPause = false;

        HideAllPanels();

        if (optionScreenPanel != null)
            optionScreenPanel.SetActive(true);
    }

    private void ShowOptionFromPause()
    {
        openedFromPause = true;

        HideAllPanels();

        if (optionScreenPanel != null)
            optionScreenPanel.SetActive(true);
    }

    private void BackFromOption()
    {
        HideAllPanels();

        if (openedFromPause)
        {
            if (pauseScreenPanel != null)
                pauseScreenPanel.SetActive(true);
        }
        else
        {
            if (mainScreenPanel != null)
                mainScreenPanel.SetActive(true);
        }
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseScreenPanel != null && pauseScreenPanel.activeSelf)
                ResumeGame();
            else if (hudPanel != null && hudPanel.activeSelf)
                ShowPauseScreen();
        }
    }
}
```

---

## 8. Setup manual di Unity Editor

Setelah compile error selesai:

1. Buat `Canvas` jika belum ada.
2. Buat 4 panel:
   - `MainScreenPanel`
   - `PauseScreenPanel`
   - `OptionScreenPanel`
   - `HUDPanel`
3. Buat tombol-tombol berikut:
   - Main screen:
     - `StartButton`
     - `OptionButton`
     - `ExitButton`
   - Pause screen:
     - `ResumeButton`
     - `PauseOptionButton`
     - `MainMenuButton`
   - HUD:
     - `PauseButton`
   - Option screen:
     - `BackFromOptionButton`
4. Buat empty GameObject:
   - `UIManager`
5. Attach `UIManager.cs` ke GameObject `UIManager`.
6. Drag panel dan button ke field script di Inspector.
7. Tekan Play.

Expected behavior:

```txt
Play Mode -> MainScreenPanel aktif
Start -> HUDPanel aktif
Pause -> PauseScreenPanel aktif
Resume -> HUDPanel aktif
Options dari Main -> OptionScreenPanel aktif
Options dari Pause -> OptionScreenPanel aktif
Back dari Options -> kembali ke Main/Pause sesuai asal
Escape saat HUD -> Pause
Escape saat Pause -> Resume
```

---

## 9. Testing checklist

Wajib selesai sebelum dianggap done:

```txt
[ ] Unity project terbuka tanpa package restore error
[ ] Console tidak ada compile error merah
[ ] MainScreenPanel muncul saat Play
[ ] StartButton menampilkan HUDPanel
[ ] PauseButton menampilkan PauseScreenPanel
[ ] ResumeButton kembali ke HUDPanel
[ ] OptionButton dari Main membuka OptionScreenPanel
[ ] PauseOptionButton dari Pause membuka OptionScreenPanel
[ ] BackFromOptionButton kembali ke asal yang benar
[ ] Escape bisa pause/resume
[ ] Tidak ada NullReferenceException saat tombol diklik
```

---

## 10. Unity MCP Server

Pertanyaan: apakah bisa ada MCP server agar AI agent bisa connect ke Unity?

Jawaban: bisa, dengan Unity MCP dari package resmi `com.unity.ai.assistant`.

Berdasarkan dokumentasi Unity:

- Unity MCP memungkinkan AI agent berinteraksi langsung dengan Unity Editor melalui Model Context Protocol.
- MCP bridge berjalan di Unity Editor.
- Relay binary berada di folder user `~/.unity/relay/`.
- AI client seperti Claude Code, Cursor, Windsurf, atau Claude Desktop bisa connect sebagai MCP client.
- Direct external client harus di-approve manual dari `Edit > Project Settings > AI > Unity MCP`.

Prerequisite:

```txt
Unity 6 (6000.0) atau lebih baru
com.unity.ai.assistant package installed
MCP-compatible client
```

Project ini memenuhi syarat Unity karena memakai Unity 6000.3.15f1 dan memiliki `com.unity.ai.assistant`.

### Cara enable Unity MCP

1. Buka project di Unity.
2. Pastikan compile error sudah beres.
3. Buka:
   ```txt
   Edit > Project Settings > AI > Unity MCP
   ```
4. Pastikan bridge status `Running`.
5. Jika status `Stopped`, klik `Start`.
6. Di bagian Integrations, gunakan `Configure` untuk client yang didukung jika tersedia.

### Manual MCP config untuk Windows

Untuk Windows, relay binary path:

```txt
%USERPROFILE%\.unity\relay\relay_win.exe
```

Contoh konfigurasi MCP:

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "%USERPROFILE%\\.unity\\relay\\relay_win.exe",
      "args": ["--mcp"]
    }
  }
}
```

Catatan:

Beberapa client tidak otomatis expand `%USERPROFILE%`. Jika gagal, pakai path absolut, misalnya:

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "C:\\Users\\YOUR_USERNAME\\.unity\\relay\\relay_win.exe",
      "args": ["--mcp"]
    }
  }
}
```

### Approve connection

Saat client pertama kali connect:

1. Unity akan menampilkan pending connection.
2. Buka:
   ```txt
   Edit > Project Settings > AI > Unity MCP
   ```
3. Accept connection.

### Test command ke agent

Setelah MCP connect, minta agent:

```txt
Read the Unity console messages and summarize any warnings or errors.
```

Agent seharusnya memakai tool seperti:

```txt
Unity_ReadConsole
```

---

## 11. Important note untuk Codex

Sebelum mengubah scene atau script secara besar:

1. Pastikan project compile.
2. Jangan dulu refactor gameplay.
3. Fokus pada UI flow.
4. Jangan bergantung pada generated UI runtime lama.
5. Pakai Inspector reference agar lebih stabil.
6. Jangan hapus `GameManager` integration kecuali memang compile error.
7. Jika `GameManager.Instance.SetGameActive` atau `InitializeForCurrentScene` tidak ada, sesuaikan call tersebut atau guard dengan method yang tersedia.

---

## 12. Suggested first Codex task

Prompt yang bisa diberikan ke Codex:

```txt
You are working on a Unity project at this repository:
https://github.com/sternnaufal/boilerplate-broilerquest.git

Goal:
Fix the current compile errors and implement a minimal UI flow with Main Screen, Pause Screen, Option Screen, and HUD.

Please:
1. Inspect compile-breaking C# files first.
2. Replace the broken runtime-generated UIManager with a simple Inspector-reference based UIManager.
3. Fix the comment syntax issue in CoinManager.cs.
4. Check PopupHasilKesehatan.cs and TimeUpPopup.cs for invalid multiline strings and fix them using \n if needed.
5. Do not refactor gameplay systems.
6. Keep changes minimal and safe.
7. Make sure the expected UI flow is:
   - Play -> Main screen
   - Start -> HUD
   - Pause -> Pause screen
   - Resume -> HUD
   - Options -> Option screen
   - Back -> previous screen
   - Escape toggles pause/resume
8. If Unity MCP is available, read Unity Console errors first and use them as source of truth.
```

---

## 13. Done definition

Task dianggap selesai jika:

```txt
[ ] Project bisa dibuka di Unity
[ ] Console bebas compile error
[ ] UIManager tidak membuat object UI secara runtime
[ ] Semua panel bisa dihubungkan dari Inspector
[ ] Semua tombol utama berfungsi
[ ] Pause memakai Time.timeScale = 0
[ ] Resume memakai Time.timeScale = 1
[ ] Back dari options tahu balik ke main atau pause
[ ] Ada catatan singkat perubahan yang dibuat
```
