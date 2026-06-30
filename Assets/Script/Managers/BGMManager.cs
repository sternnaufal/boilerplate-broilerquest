using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

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
    private Coroutine loadMenuRoutine;
    private Coroutine loadGameRoutine;
    private bool isLoadingMenu;
    private bool isLoadingGame;
    private bool playMenuOnLoad;
    private bool playGameOnLoad;

    protected override void Awake()
    {
        base.Awake();
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null)
                bgmSource = gameObject.AddComponent<AudioSource>();
        }
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = volume;

        LoadMenuBGMFallback();
        LoadGameBGMFallback();
    }

    private void LoadMenuBGMFallback()
    {
        if (mainMenuBGM != null) return;
        if (isLoadingMenu) return;

        string path = Path.Combine(Application.streamingAssetsPath, "BGM", "bgm.wav");
        if (!File.Exists(path)) return;

        loadMenuRoutine = StartCoroutine(LoadMenuRoutine(path));
    }

    private void LoadGameBGMFallback()
    {
        if (starterBGM != null) return;
        if (isLoadingGame) return;

        string gamePath = Path.Combine(Application.streamingAssetsPath, "BGM", "bgm_game.wav");
        if (!File.Exists(gamePath)) return;

        loadGameRoutine = StartCoroutine(LoadGameRoutine(gamePath));
    }

    private IEnumerator LoadMenuRoutine(string path)
    {
        isLoadingMenu = true;
        string url = new System.Uri(path).AbsoluteUri;

        using UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            isLoadingMenu = false;
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
        if (clip == null) { isLoadingMenu = false; yield break; }

        clip.name = "bgm_menu";
        if (mainMenuBGM == null) mainMenuBGM = clip;
        if (levelSelectBGM == null) levelSelectBGM = clip;
        if (koleksiIoTBGM == null) koleksiIoTBGM = clip;
        isLoadingMenu = false;

        if (playMenuOnLoad)
        {
            playMenuOnLoad = false;
            PlayBGM(clip);
        }
    }

    private IEnumerator LoadGameRoutine(string path)
    {
        isLoadingGame = true;
        string url = new System.Uri(path).AbsoluteUri;

        using UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            isLoadingGame = false;
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
        if (clip == null) { isLoadingGame = false; yield break; }

        clip.name = "bgm_game";
        if (starterBGM == null) starterBGM = clip;
        if (beginnerBGM == null) beginnerBGM = clip;
        if (intermediateBGM == null) intermediateBGM = clip;
        isLoadingGame = false;

        if (playGameOnLoad)
        {
            playGameOnLoad = false;
            PlayBGM(clip);
        }
    }

    public void SetVolume(float vol)
    {
        volume = Mathf.Clamp01(vol);
        if (bgmSource != null)
            bgmSource.volume = volume;
    }

    public float GetVolume() => volume;

    public void PlayMainMenuBGM()
    {
        if (mainMenuBGM != null) { PlayBGM(mainMenuBGM); return; }
        if (isLoadingMenu) playMenuOnLoad = true;
    }

    public void PlayStarterBGM() => PlayInGameBGM(starterBGM);
    public void PlayBeginnerBGM() => PlayInGameBGM(beginnerBGM);
    public void PlayIntermediateBGM() => PlayInGameBGM(intermediateBGM);

    private void PlayInGameBGM(AudioClip clip)
    {
        if (clip != null) { PlayBGM(clip); return; }
        if (isLoadingGame) { playGameOnLoad = true; return; }
        StopBGM();
    }

    public void PlayMenuBGM()
    {
        AudioClip menuClip = mainMenuBGM != null ? mainMenuBGM : levelSelectBGM;
        if (menuClip != null)
        {
            if (bgmSource.clip == menuClip && bgmSource.isPlaying) return;
            PlayBGM(menuClip);
        }
        else if (isLoadingMenu)
        {
            playMenuOnLoad = true;
        }
    }

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
                elapsed += Time.unscaledDeltaTime;
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
            elapsed2 += Time.unscaledDeltaTime;
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
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = volume;
        fadeRoutine = null;
    }
}
