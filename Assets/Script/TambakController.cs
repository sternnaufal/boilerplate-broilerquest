using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class TambakController : MonoBehaviour, IPointerClickHandler
{
    [Header("Indikator Makanan")]
    public GameObject makananIndikatorSilang;
    public GameObject makananIndikatorCentang;

    [Header("Indikator Oksigen")]
    public GameObject o2IndikatorSilang;
    public GameObject o2IndikatorCentang;

    [Header("Emote")]
    public GameObject emotUdang;
    public GameObject emotMakan;
    public GameObject emotOksigen;
    public GameObject emotJual;

    [Header("Timer Random (detik)")]
    public float minDelayUdang = 3f;
    public float maxDelayUdang = 8f;
    public float minDelayOksigenSetelahMakan = 2f;
    public float maxDelayOksigenSetelahMakan = 5f;
    public float minDelayJualSetelahOksigen = 2f;
    public float maxDelayJualSetelahOksigen = 5f;

    private enum TambakState
    {
        Kosong,
        MenungguKlikUdang,
        MenungguKlikMakan,
        MenungguKlikOksigen,
        MenungguHasilO2,
        MenungguKlikJual
    }

    private TambakState currentState;
    private Coroutine delayCoroutine;

    void Start()
    {
        ResetKeAwal();
    }

    void ResetSemuaIndikatorDanEmote()
    {
        emotUdang.SetActive(false);
        emotMakan.SetActive(false);
        emotOksigen.SetActive(false);
        emotJual.SetActive(false);

        makananIndikatorSilang.SetActive(true);
        makananIndikatorCentang.SetActive(false);
        o2IndikatorSilang.SetActive(true);
        o2IndikatorCentang.SetActive(false);
    }

    void MulaiDelayKeUdang()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        delayCoroutine = StartCoroutine(DelayMunculEmoteUdang());
    }

    IEnumerator DelayMunculEmoteUdang()
    {
        float delay = Random.Range(minDelayUdang, maxDelayUdang);
        yield return new WaitForSeconds(delay);
        
        // Pastikan masih dalam state Kosong (tidak berubah selama delay)
        if (currentState == TambakState.Kosong)
        {
            emotUdang.SetActive(true);
            currentState = TambakState.MenungguKlikUdang;
            Debug.Log($"{name} -> Emot Udang muncul");
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
        if (currentState == TambakState.Kosong)
        {
            Debug.Log($"{name} diklik tapi state KOSONG, abaikan.");
            return;
        }

        // Tambahan: validasi visual sesuai state (opsional tapi sangat membantu)
        if (currentState == TambakState.MenungguKlikUdang && !emotUdang.activeSelf)
        {
            Debug.LogWarning($"{name} state MenungguUdang tapi emote udang tidak aktif! Force reset.");
            ResetKeAwal();
            return;
        }
        if (currentState == TambakState.MenungguKlikMakan && !emotMakan.activeSelf)
        {
            Debug.LogWarning($"{name} state MenungguMakan tapi emote makan tidak aktif! Force reset.");
            ResetKeAwal();
            return;
        }
        if (currentState == TambakState.MenungguKlikOksigen && !emotOksigen.activeSelf)
        {
            Debug.LogWarning($"{name} state MenungguOksigen tapi emote oksigen tidak aktif! Force reset.");
            ResetKeAwal();
            return;
        }
        if (currentState == TambakState.MenungguHasilO2)
        {
            Debug.Log($"{name} -> Sedang menunggu hasil minigame O2, klik diabaikan.");
            return;
        }
        if (currentState == TambakState.MenungguKlikJual && !emotJual.activeSelf)
        {
            Debug.LogWarning($"{name} state MenungguJual tapi emote jual tidak aktif! Force reset.");
            ResetKeAwal();
            return;
        }

        // Proses klik sesuai state
        KetikaDiklik();
    }

    private void KetikaDiklik()
    {
        // Validasi awal: pastikan semua referensi penting tidak null
        if (makananIndikatorSilang == null || makananIndikatorCentang == null ||
            o2IndikatorSilang == null || o2IndikatorCentang == null ||
            emotUdang == null || emotMakan == null || emotOksigen == null || emotJual == null)
        {
            Debug.LogError($"{name}: Ada referensi GameObject yang belum diisi di Inspector!");
            return;
        }

        switch (currentState)
        {
            case TambakState.MenungguKlikUdang:
                emotUdang.SetActive(false);
                emotMakan.SetActive(true);
                currentState = TambakState.MenungguKlikMakan;
                Debug.Log($"{name} -> Memberi makan, muncul emot makan");
                break;

            case TambakState.MenungguKlikMakan:
                emotMakan.SetActive(false);
                makananIndikatorSilang.SetActive(false);
                makananIndikatorCentang.SetActive(true);
                MulaiDelayKeOksigen();
                currentState = TambakState.MenungguKlikOksigen;
                Debug.Log($"{name} -> Selesai makan, centang makanan, delay ke oksigen");
                break;

            case TambakState.MenungguKlikOksigen:
                // Sembunyikan emote O2 (opsional)
                emotOksigen.SetActive(false);
                // Tampilkan popup
                PopupO2.Instance.TampilkanPopup(this);
                currentState = TambakState.MenungguHasilO2;
                break;

            case TambakState.MenungguKlikJual:
                emotJual.SetActive(false);
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
                Debug.Log($"{name} -> Dijual, +10 coin, reset semua");
                break;

            default:
                Debug.Log($"{name} diklik tapi state tidak valid: {currentState}");
                break;
        }
    }

    void MulaiDelayKeOksigen()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        delayCoroutine = StartCoroutine(DelayMunculEmoteOksigen());
    }

    IEnumerator DelayMunculEmoteOksigen()
    {
        float delay = Random.Range(minDelayOksigenSetelahMakan, maxDelayOksigenSetelahMakan);
        yield return new WaitForSeconds(delay);
        
        if (currentState == TambakState.MenungguKlikOksigen)
        {
            emotOksigen.SetActive(true);
            Debug.Log($"{name} -> Emot oksigen muncul");
        }
        else
        {
            Debug.LogWarning($"{name} -> Delay oksigen selesai tapi state berubah, batalkan muncul emot oksigen");
        }
    }

    void MulaiDelayKeJual()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        delayCoroutine = StartCoroutine(DelayMunculEmoteJual());
    }

    IEnumerator DelayMunculEmoteJual()
    {
        float delay = Random.Range(minDelayJualSetelahOksigen, maxDelayJualSetelahOksigen);
        yield return new WaitForSeconds(delay);
        
        if (currentState == TambakState.MenungguKlikJual)
        {
            emotJual.SetActive(true);
            Debug.Log($"{name} -> Emot jual muncul");
        }
        else
        {
            Debug.LogWarning($"{name} -> Delay jual selesai tapi state berubah, batalkan muncul emot jual");
        }
    }

    void ResetKeAwal()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        ResetSemuaIndikatorDanEmote();
        currentState = TambakState.Kosong;
        MulaiDelayKeUdang();
        Debug.Log($"{name} -> Reset ke awal, mulai delay muncul udang");
    }

    public void OnO2MinigameSuccess()
    {
        if (currentState == TambakState.MenungguHasilO2)
        {
            o2IndikatorSilang.SetActive(false);
            o2IndikatorCentang.SetActive(true);
            MulaiDelayKeJual();
            currentState = TambakState.MenungguKlikJual;
            Debug.Log($"{name} -> Minigame O2 berhasil, centang O2, delay ke jual");
        }
    }

    public void OnO2MinigameFail()
    {
        if (currentState == TambakState.MenungguHasilO2)
        {
            Debug.Log($"{name} -> Minigame O2 gagal, reset kolam");
            ResetKeAwal();
        }
    }
}