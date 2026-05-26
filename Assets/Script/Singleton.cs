using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool applicationQuitting;

    public static T Instance
    {
        get
        {
            if (applicationQuitting)
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
                        DontDestroyOnLoad(go);
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
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
            _instance = null;
    }

    protected virtual void OnApplicationQuit()
    {
        applicationQuitting = true;
    }
}
