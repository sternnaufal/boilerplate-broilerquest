using System.Collections;
using UnityEngine;

public class BGMManager : Singleton<BGMManager>
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource bgmSource;

    [Header("Settings")]
    [SerializeField] private float volume = 1f;
    [SerializeField] private float defaultFadeDuration = 1f;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip mainMenuBGM;
    [SerializeField] private AudioClip levelSelectBGM;
    [SerializeField] private AudioClip starterBGM;
    [SerializeField] private AudioClip beginnerBGM;
    [SerializeField] private AudioClip intermediateBGM;
    [SerializeField] private AudioClip koleksiIoTBGM;

    private Coroutine fadeRoutine;

    protected override void Awake()
    {
        base.Awake();
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
        }
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = volume;

        LoadFallbackClips();
    }

    private void LoadFallbackClips()
    {
        if (mainMenuBGM != null) return;

        AudioClip fallback = Resources.Load<AudioClip>("BGM/bgm");
        if (fallback == null) return;

        mainMenuBGM = fallback;
        levelSelectBGM = fallback;
        starterBGM = fallback;
        beginnerBGM = fallback;
        intermediateBGM = fallback;
        koleksiIoTBGM = fallback;
    }

    public void SetVolume(float vol)
    {
        volume = Mathf.Clamp01(vol);
        if (bgmSource != null)
            bgmSource.volume = volume;
    }

    public float GetVolume() => volume;

    public void PlayMainMenuBGM() => PlayBGM(mainMenuBGM);
    public void PlayLevelSelectBGM() => PlayBGM(levelSelectBGM);
    public void PlayStarterBGM() => PlayBGM(starterBGM);
    public void PlayBeginnerBGM() => PlayBGM(beginnerBGM);
    public void PlayIntermediateBGM() => PlayBGM(intermediateBGM);
    public void PlayKoleksiIoTBGM() => PlayBGM(koleksiIoTBGM);

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossfadeRoutine(clip, defaultFadeDuration));
    }

    public void PlayBGMImmediate(AudioClip clip)
    {
        if (clip == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutStopRoutine(defaultFadeDuration));
    }

    public void StopBGMImmediate()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        bgmSource.Stop();
    }

    public void PauseBGM() => bgmSource.Pause();

    public void ResumeBGM()
    {
        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void FadeTo(AudioClip clip, float duration)
    {
        if (clip == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossfadeRoutine(clip, duration));
    }

    private IEnumerator CrossfadeRoutine(AudioClip newClip, float duration)
    {
        float startVol = bgmSource.volume;

        if (bgmSource.isPlaying && bgmSource.clip != null)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            bgmSource.Stop();
        }

        bgmSource.clip = newClip;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float elapsed2 = 0f;
        while (elapsed2 < duration)
        {
            elapsed2 += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, volume, elapsed2 / duration);
            yield return null;
        }

        bgmSource.volume = volume;
        fadeRoutine = null;
    }

    private IEnumerator FadeOutStopRoutine(float duration)
    {
        float startVol = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = volume;
        fadeRoutine = null;
    }
}
