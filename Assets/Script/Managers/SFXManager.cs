using UnityEngine;

public class SFXManager : Singleton<SFXManager>
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Settings")]
    [SerializeField] private float volume = 1f;

    [Header("UI")]
    [SerializeField] private AudioClip navigateSfx;
    [SerializeField] private AudioClip buttonClickSfx;

    [Header("Shop")]
    [SerializeField] private AudioClip buySuccessSfx;
    [SerializeField] private AudioClip buyFailSfx;
    [SerializeField] private AudioClip feedBuySfx;

    [Header("IoT")]
    [SerializeField] private AudioClip iotToggleOnSfx;
    [SerializeField] private AudioClip iotToggleOffSfx;

    [Header("Chicken Care")]
    [SerializeField] private AudioClip careCompleteSfx;
    [SerializeField] private AudioClip sellCompleteSfx;

    [Header("Health Minigame (Timing)")]
    [SerializeField] private AudioClip timingToggleOnSfx;
    [SerializeField] private AudioClip timingSuccessSfx;
    [SerializeField] private AudioClip timingFailSfx;

    [Header("Health Check Result")]
    [SerializeField] private AudioClip healthSuccessSfx;
    [SerializeField] private AudioClip healthFailSfx;

    [Header("Jigsaw Puzzle")]
    [SerializeField] private AudioClip jigsawPieceClickSfx;
    [SerializeField] private AudioClip jigsawPieceSwapSfx;
    [SerializeField] private AudioClip jigsawCompleteSfx;
    [SerializeField] private AudioClip jigsawFailSfx;

    [Header("Pipeline Puzzle")]
    [SerializeField] private AudioClip pipeRotateSfx;

    [Header("Need Bubble")]
    [SerializeField] private AudioClip bubbleNeedSfx;

    [Header("Minigame Result")]
    [SerializeField] private AudioClip minigameSuccessSfx;
    [SerializeField] private AudioClip minigameFailSfx;

    [Header("Level")]
    [SerializeField] private AudioClip timeUpSfx;
    [SerializeField] private AudioClip unlockSuccessSfx;
    [SerializeField] private AudioClip unlockFailSfx;

    protected override void Awake()
    {
        base.Awake();
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }
    }

    public void SetVolume(float vol)
    {
        volume = Mathf.Clamp01(vol);
        if (sfxSource != null)
            sfxSource.volume = volume;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXAtPoint(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public void PlayNavigate() { PlaySFX(navigateSfx); }
    public void PlayButtonClick() { PlaySFX(buttonClickSfx); }

    public void PlayBuySuccess() { PlaySFX(buySuccessSfx); }
    public void PlayBuyFail() { PlaySFX(buyFailSfx); }
    public void PlayFeedBuy() { PlaySFX(feedBuySfx); }

    public void PlayIotToggleOn() { PlaySFX(iotToggleOnSfx); }
    public void PlayIotToggleOff() { PlaySFX(iotToggleOffSfx); }
    public void PlayIotToggle(bool isOn) { if (isOn) PlayIotToggleOn(); else PlayIotToggleOff(); }

    public void PlayCareComplete() { PlaySFX(careCompleteSfx); }
    public void PlaySellComplete() { PlaySFX(sellCompleteSfx); }

    public void PlayTimingToggleOn() { PlaySFX(timingToggleOnSfx); }
    public void PlayTimingSuccess() { PlaySFX(timingSuccessSfx); }
    public void PlayTimingFail() { PlaySFX(timingFailSfx); }

    public void PlayHealthSuccess() { PlaySFX(healthSuccessSfx); }
    public void PlayHealthFail() { PlaySFX(healthFailSfx); }

    public void PlayJigsawPieceClick() { PlaySFX(jigsawPieceClickSfx); }
    public void PlayJigsawPieceSwap() { PlaySFX(jigsawPieceSwapSfx); }
    public void PlayJigsawComplete() { PlaySFX(jigsawCompleteSfx); }
    public void PlayJigsawFail() { PlaySFX(jigsawFailSfx); }

    public void PlayPipeRotate() { PlaySFX(pipeRotateSfx); }

    public void PlayBubbleNeed() { PlaySFX(bubbleNeedSfx); }

    public void PlayMinigameSuccess() { PlaySFX(minigameSuccessSfx); }
    public void PlayMinigameFail() { PlaySFX(minigameFailSfx); }

    public void PlayTimeUp() { PlaySFX(timeUpSfx); }
    public void PlayUnlockSuccess() { PlaySFX(unlockSuccessSfx); }
    public void PlayUnlockFail() { PlaySFX(unlockFailSfx); }
}
