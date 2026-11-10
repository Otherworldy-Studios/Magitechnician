using UnityEngine;

/// <summary>
/// Creates the persistent managers before the first scene loads,
/// so every scene, including the Hub and Dungeon, starts with them.
/// </summary>
public static class ManagerBootstrap
{
    // Path inside a Resources folder, without the file extension.
    // The prefab must be at Assets/.../Resources/Managers.prefab.
    private const string ManagersPrefabPath = "Managers";

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
        Object.DontDestroyOnLoad(managers);
    }
}