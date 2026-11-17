using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Base class for every manager that should have exactly one instance, such as GameManager and InputManager.
/// Use T.Instance to reach the manager, and T.HasInstance to check whether it exists without searching.
/// A second copy destroys itself in Awake.
/// </summary>
/// <typeparam name="T">The manager class itself, for example GameManager : MGR&lt;GameManager&gt;.</typeparam>
// INTEGRATION: new script. GameManager, UIManager, InputManager, SoundController, and LevelGenerator all inherit it,
// which replaces each one's own singleton code.
public class MGR<T> : MonoBehaviour where T : Component
{
    private static T _instance; // The one instance. Set in Awake, or by the first Instance read.

    [Tooltip("Keeps this object loaded across scenes. Leave off for managers in the Managers prefab, which ManagerBootstrap keeps loaded.")]
    [SerializeField, FormerlySerializedAs("shouldPersist")] private bool _shouldPersist;

    /// <summary>
    /// True when the manager exists. Doesn't search the scene. Check it in OnDisable and OnDestroy,
    /// where the manager may already be gone while the game quits.
    /// </summary>
    public static bool HasInstance => _instance != null;

    /// <summary>
    /// The manager. If its Awake hasn't run yet, finds it in the loaded scenes, and logs an error if there's none.
    /// </summary>
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

    /// <summary>
    /// Registers this object as the instance, or destroys it if another instance exists.
    /// Subclasses call base.Awake() first, then return early when Instance != this.
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        if (_shouldPersist)
        {
            DontDestroyOnLoad(gameObject); // Only works on a root object.
        }
    }
}
