using UnityEngine;


public class MGR<T> : MonoBehaviour where T : Component
{
    private static T _instance;
    [SerializeField] private bool shouldPersist;
    // In MGR<T>
    public static bool HasInstance => _instance != null;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();
                if (_instance == null)
                {
                    Debug.LogError($"[MGR] No {typeof(T).Name} found in the loaded scenes.");
                }
            }
            return _instance;
        }
    }


    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        if (shouldPersist)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}