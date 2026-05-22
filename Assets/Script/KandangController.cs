using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class KandangController : MonoBehaviour, IPointerClickHandler
{
    [Header("Indikator Pakan")]
    public GameObject pakanIndikatorSilang;
    public GameObject pakanIndikatorCentang;

    [Header("Indikator Kesehatan")]
    public GameObject kesehatanIndikatorSilang;
    public GameObject kesehatanIndikatorCentang;

    [Header("Emote")]
    public GameObject emotAyam;
    public GameObject emotMakan;
    public GameObject emotVitamin;
    public GameObject emotPanen;

    [Header("Timer Random (detik)")]
    public float minDelayAyam = 3f;
    public float maxDelayAyam = 8f;
    public float minDelayVitaminSetelahMakan = 2f;
    public float maxDelayVitaminSetelahMakan = 5f;
    public float minDelayPanenSetelahVitamin = 2f;
    public float maxDelayPanenSetelahVitamin = 5f;

    private enum KandangState
    {
        Kosong,
        MenungguKlikAyam,
        MenungguKlikMakan,
        MenungguKlikVitamin,
        MenungguHasilKesehatan,
        MenungguKlikPanen
    }

    private KandangState currentState;
    private Coroutine delayCoroutine;

    void Start()
    {
        ResetKeAwal();
    }

    void ResetSemuaIndikatorDanEmote()
    {
        emotAyam.SetActive(false);
        emotMakan.SetActive(false);
        emotVitamin.SetActive(false);
        emotPanen.SetActive(false);

        pakanIndikatorSilang.SetActive(true);
        pakanIndikatorCentang.SetActive(false);
        kesehatanIndikatorSilang.SetActive(true);
        kesehatanIndikatorCentang.SetActive(false);
    }

    void MulaiDelayKeAyam()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        delayCoroutine = StartCoroutine(DelayMunculEmoteAyam());
    }

    IEnumerator DelayMunculEmoteAyam()
    {
        float delay = Random.Range(minDelayAyam, maxDelayAyam);
        yield return new WaitForSeconds(delay);
        
        // Pastikan masih dalam state Kosong (tidak berubah selama delay)
        if (currentState == KandangState.Kosong)
        {
            emotAyam.SetActive(true);
            currentState = KandangState.MenungguKlikAyam;
            Debug.Log($"{name} -> Emot Ayam muncul");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
        {
            Debug.Log("Game sedang tidak aktif (timer habis atau popup muncul). Klik diabaikan.");
            return;
        }
        // Cegah klik jika sedang dalam proses delay (tidak ada state yang valid)
        if (currentState == KandangState.Kosong)
        {
            Debug.Log($"{name} diklik tapi state KOSONG, abaikan.");
            return;
        }

        // Tambahan: validasi visual sesuai state (opsional tapi sangat membantu)
        if (currentState == KandangState.MenungguKlikAyam && !emotAyam.activeSelf)
        {
            Debug.LogWarning($"{name} state MenungguAyam tapi emote ayam tidak aktif! Force reset.");
            ResetKeAwal();
            return;
        }
        if (currentState == KandangState.MenungguKlikMakan && !emotMakan.activeSelf)
        {
            Debug.LogWarning($"{name} state MenungguMakan tapi emote makan tidak aktif! Force reset.");
            ResetKeAwal();
            return;
        }
        if (currentState == KandangState.MenungguKlikVitamin && !emotVitamin.activeSelf)
        {
            Debug.LogWarning($"{name} state MenungguVitamin tapi emote vitamin tidak aktif! Force reset.");
            ResetKeAwal();
            return;
        }
        if (currentState == KandangState.MenungguHasilKesehatan)
        {
            Debug.Log($"{name} -> Sedang menunggu hasil minigame kesehatan, klik diabaikan.");
            return;
        }
        if (currentState == KandangState.MenungguKlikPanen && !emotPanen.activeSelf)
        {
            Debug.LogWarning($"{name} state MenungguPanen tapi emote panen tidak aktif! Force reset.");
            ResetKeAwal();
            return;
        }

        // Proses klik sesuai state
        KetikaDiklik();
    }

    private void KetikaDiklik()
    {
        // Validasi awal: pastikan semua referensi penting tidak null
        if (pakanIndikatorSilang == null || pakanIndikatorCentang == null ||
            kesehatanIndikatorSilang == null || kesehatanIndikatorCentang == null ||
            emotAyam == null || emotMakan == null || emotVitamin == null || emotPanen == null)
        {
            Debug.LogError($"{name}: Ada referensi GameObject yang belum diisi di Inspector!");
            return;
        }

        switch (currentState)
        {
            case KandangState.MenungguKlikAyam:
                emotAyam.SetActive(false);
                emotMakan.SetActive(true);
                currentState = KandangState.MenungguKlikMakan;
                Debug.Log($"{name} -> Memberi pakan, muncul emot makan");
                break;

            case KandangState.MenungguKlikMakan:
                emotMakan.SetActive(false);
                pakanIndikatorSilang.SetActive(false);
                pakanIndikatorCentang.SetActive(true);
                MulaiDelayKeVitamin();
                currentState = KandangState.MenungguKlikVitamin;
                Debug.Log($"{name} -> Selesai makan, centang pakan, delay ke vitamin");
                break;

            case KandangState.MenungguKlikVitamin:
                // Sembunyikan emote vitamin (opsional)
                emotVitamin.SetActive(false);
                // Tampilkan popup
                PopupKesehatan.Instance.TampilkanPopup(this);
                currentState = KandangState.MenungguHasilKesehatan;
                break;

            case KandangState.MenungguKlikPanen:
                emotPanen.SetActive(false);
                // Cek apakah CoinManager.Instance ada
                if (CoinManager.Instance != null)
                {
                    CoinManager.Instance.AddCoin(10);
                }
                else
                {
                    Debug.LogError("CoinManager.Instance tidak ditemukan! Pastikan ada GameObject dengan script CoinManager di scene.");
                }
                ResetKeAwal();
                Debug.Log($"{name} -> Dipanen, +10 coin, reset semua");
                break;

            default:
                Debug.Log($"{name} diklik tapi state tidak valid: {currentState}");
                break;
        }
    }

    void MulaiDelayKeVitamin()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        delayCoroutine = StartCoroutine(DelayMunculEmoteVitamin());
    }

    IEnumerator DelayMunculEmoteVitamin()
    {
        float delay = Random.Range(minDelayVitaminSetelahMakan, maxDelayVitaminSetelahMakan);
        yield return new WaitForSeconds(delay);
        
        if (currentState == KandangState.MenungguKlikVitamin)
        {
            emotVitamin.SetActive(true);
            Debug.Log($"{name} -> Emot vitamin muncul");
        }
        else
        {
            Debug.LogWarning($"{name} -> Delay vitamin selesai tapi state berubah, batalkan muncul emot vitamin");
        }
    }

    void MulaiDelayKePanen()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        delayCoroutine = StartCoroutine(DelayMunculEmotePanen());
    }

    IEnumerator DelayMunculEmotePanen()
    {
        float delay = Random.Range(minDelayPanenSetelahVitamin, maxDelayPanenSetelahVitamin);
        yield return new WaitForSeconds(delay);
        
        if (currentState == KandangState.MenungguKlikPanen)
        {
            emotPanen.SetActive(true);
            Debug.Log($"{name} -> Emot panen muncul");
        }
        else
        {
            Debug.LogWarning($"{name} -> Delay panen selesai tapi state berubah, batalkan muncul emot panen");
        }
    }

    void ResetKeAwal()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        ResetSemuaIndikatorDanEmote();
        currentState = KandangState.Kosong;
        MulaiDelayKeAyam();
        Debug.Log($"{name} -> Reset ke awal, mulai delay muncul ayam");
    }

    public void OnKesehatanMinigameSuccess()
    {
        if (currentState == KandangState.MenungguHasilKesehatan)
        {
            kesehatanIndikatorSilang.SetActive(false);
            kesehatanIndikatorCentang.SetActive(true);
            MulaiDelayKePanen();
            currentState = KandangState.MenungguKlikPanen;
            Debug.Log($"{name} -> Minigame kesehatan berhasil, centang kesehatan, delay ke panen");
        }
    }

    public void OnKesehatanMinigameFail()
    {
        if (currentState == KandangState.MenungguHasilKesehatan)
        {
            Debug.Log($"{name} -> Minigame kesehatan gagal, reset kandang");
            ResetKeAwal();
        }
    }
}