using UnityEngine;

public partial class StarterKandangSlot
{
    [Header("VFX Effects")]
    [SerializeField] private GameObject heatVFXPrefab;
    [SerializeField] private GameObject coldVFXPrefab;
    [SerializeField] private GameObject sellVFXPrefab;
    [SerializeField] private Vector3 vfxOffset = new Vector3(0f, 0f, -2f);
    [SerializeField] private float vfxZDistance = 8f;

    private GameObject heatVFXInstance;
    private GameObject coldVFXInstance;

    private void PlayHeatEffect()
    {
        StopHeatEffect();
        if (heatVFXPrefab != null)
            heatVFXInstance = SpawnVFX(heatVFXPrefab, true);
    }

    private void StopHeatEffect()
    {
        if (heatVFXInstance != null)
        {
            Destroy(heatVFXInstance);
            heatVFXInstance = null;
        }
    }

    private void PlayColdEffect()
    {
        StopColdEffect();
        if (coldVFXPrefab != null)
            coldVFXInstance = SpawnVFX(coldVFXPrefab, true);
    }

    private void StopColdEffect()
    {
        if (coldVFXInstance != null)
        {
            Destroy(coldVFXInstance);
            coldVFXInstance = null;
        }
    }

    public void PlaySellEffect()
    {
        if (sellVFXPrefab == null) return;

        GameObject effect = SpawnVFX(sellVFXPrefab, false);
        ParticleSystem[] pss = effect.GetComponentsInChildren<ParticleSystem>(true);
        float maxDur = 5f;
        foreach (var ps in pss)
        {
            float d = ps.main.duration + ps.main.startLifetime.constantMax + ps.main.startDelay.constantMax;
            if (d > maxDur) maxDur = d;
        }
        Destroy(effect, maxDur + 1f);
    }

    private GameObject SpawnVFX(GameObject prefab, bool looping)
    {
        Transform root = Camera.main != null ? Camera.main.transform : transform.root;
        GameObject instance = Instantiate(prefab, root);

        Vector3 pos;
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(GetAnchorWorldPos());

            if (screenPos.z < 0f)
            {
                pos = Camera.main.transform.position + Camera.main.transform.forward * vfxZDistance;
            }
            else
            {
                pos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, vfxZDistance));
                pos += vfxOffset;
            }
        }
        else
        {
            pos = GetAnchorWorldPos() + vfxOffset;
        }

        instance.transform.position = pos;

        foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear();
            ps.time = 0f;
            ps.Play();
        }

        foreach (var psr in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            psr.sortingLayerName = "Default";
            psr.sortingOrder = 32767;
        }

        return instance;
    }

    private Vector3 GetAnchorWorldPos()
    {
        Transform t = chickenParent != null ? chickenParent : transform;
        return t.position;
    }

    private void LateUpdate()
    {
        if (Camera.main == null) return;

        Transform cam = Camera.main.transform;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(GetAnchorWorldPos());

        Vector3 pos;
        if (screenPos.z < 0f)
            pos = cam.position + cam.forward * vfxZDistance;
        else
            pos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, vfxZDistance));
        pos += vfxOffset;

        if (heatVFXInstance != null)
            heatVFXInstance.transform.position = pos;
        if (coldVFXInstance != null)
            coldVFXInstance.transform.position = pos;
    }

    private void StopAllEffects()
    {
        StopHeatEffect();
        StopColdEffect();
    }
}
