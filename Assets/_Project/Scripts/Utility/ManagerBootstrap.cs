using UnityEngine;

/// <summary>
/// Creates the persistent managers before the first scene loads, so every scene starts with them,
/// including when Play is pressed in the Hub or Level scene.
/// </summary>
// INTEGRATION: new script. Loads the Managers prefab, which holds GameManager, UIManager (with the EventSystem and the
// screen canvases), InputManager, and SoundController. Keeping the prefab loaded keeps all of them loaded,
// so none of them is placed in a scene.
public static class ManagerBootstrap
{
    // Path inside a Resources folder, without the file extension.
    // The prefab must be at Assets/.../Resources/Managers.prefab.
    private const string ManagersPrefabPath = "Managers";

    /// <summary>Creates the Managers prefab and keeps it loaded. Unity calls this before the first scene loads.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameObject prefab = Resources.Load<GameObject>(ManagersPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[ManagerBootstrap] No prefab found at Resources/{ManagersPrefabPath}.");
            return;
        }

        GameObject managers = Object.Instantiate(prefab);
        managers.name = prefab.name;   // Removes "(Clone)" from the name in the Hierarchy.
        Object.DontDestroyOnLoad(managers); // The instance is a root object, so its children (the managers) stay too.
    }
}
