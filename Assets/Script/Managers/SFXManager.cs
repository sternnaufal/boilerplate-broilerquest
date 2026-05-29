using UnityEngine;

public class SFXManager : Singleton<SFXManager>
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Settings")]
    [SerializeField] private float volume = 1f;

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
}
