using UnityEngine;

public static class SingletonQuittingDetector
{
    public static bool IsQuitting { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsQuitting = false;
    }
}

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (SingletonQuittingDetector.IsQuitting)
                return null;

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                        // Awake() handles DontDestroyOnLoad based on PersistAcrossScenes
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual bool PersistAcrossScenes => true;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (PersistAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            if (PersistAcrossScenes)
            {
                // Persistent: original is authoritative, destroy this duplicate component only
                Destroy(this);
            }
            else
            {
                // Scene-local: new scene instance takes over from any stale ghost.
                // Destroy(this) would kill the component buttons reference → Missing target.
                // Destroy only the COMPONENT (not the whole GO) to avoid collateral damage
                // if the stale instance shares a GO with other persistent singletons.
                Debug.LogWarning($"[Singleton] {typeof(T).Name}: stale instance replaced (was in scene '{_instance.gameObject.scene.name}', GO='{_instance.gameObject.name}').", gameObject);
                var stale = _instance;
                _instance = this as T;
                Destroy(stale);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
        {
            _instance = null;
            // Persistent singletons are only destroyed on app quit, so block re-creation.
            // Scene-specific singletons (PersistAcrossScenes=false) are destroyed on scene
            // transitions — don't block other singletons from working in the new scene.
            if (PersistAcrossScenes)
                SingletonQuittingDetector.IsQuitting = true;
        }
    }

    protected virtual void OnApplicationQuit()
    {
        SingletonQuittingDetector.IsQuitting = true;
    }
}
