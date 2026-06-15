using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;

    [SerializeField] private float defaultDuration = 0.3f;
    [SerializeField] private float defaultIntensity = 0.15f;

    private Vector3 originalPos;
    private Coroutine shakeRoutine;

    public static void Trigger(float duration = -1f, float intensity = -1f)
    {
        EnsureInstance();
        instance.StartShake(
            duration >= 0f ? duration : instance.defaultDuration,
            intensity >= 0f ? intensity : instance.defaultIntensity
        );
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        GameObject go = new GameObject("CameraShake");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<CameraShake>();
    }

    private void Start()
    {
        if (Camera.main != null)
            originalPos = Camera.main.transform.localPosition;
    }

    private void StartShake(float duration, float intensity)
    {
        if (Camera.main == null) return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, intensity));
    }

    private IEnumerator ShakeRoutine(float duration, float intensity)
    {
        originalPos = Camera.main.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float decay = 1f - t;

            Vector3 offset = new Vector3(
                Random.Range(-1f, 1f) * intensity * decay,
                Random.Range(-1f, 1f) * intensity * decay,
                0f
            );

            if (Camera.main != null)
                Camera.main.transform.localPosition = originalPos + offset;

            yield return null;
        }

        if (Camera.main != null)
            Camera.main.transform.localPosition = originalPos;

        shakeRoutine = null;
    }
}
