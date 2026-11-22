using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Editor window that lists the assets in one folder that nothing in the game uses, so they can be reviewed and moved
/// to the system trash. An asset counts as used when something the build includes depends on it, directly or through
/// other assets: an enabled scene in Build Settings, anything in a Resources folder, the render pipeline assets, or the
/// preloaded assets. Open it from Tools > Project > Unused Asset Finder.
/// </summary>
public class UnusedAssetFinder : EditorWindow
{
    private class Entry
    {
        public string Path;
        public long Bytes;
        public bool Selected = true;
    }

    // Never listed: code and text files, shaders (often loaded by name), and Input System action files (the generated
    // C# class holds their data, so nothing references the asset itself).
    private static readonly HashSet<string> SkippedExtensions = new()
    {
        ".cs", ".asmdef", ".asmref", ".dll", ".so", ".bundle",
        ".shader", ".shadergraph", ".shadersubgraph", ".hlsl", ".cginc", ".compute",
        ".json", ".txt", ".md", ".pdf", ".inputactions"
    };

    private DefaultAsset _folder;
    private Vector2 _scroll;
    private readonly List<Entry> _unused = new();
    private bool _hasSearched;

    [MenuItem("Tools/Project/Unused Asset Finder")]
    private static void Open()
    {
        GetWindow<UnusedAssetFinder>("Unused Assets");
    }

    private void OnGUI()
    {
        _folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", _folder, typeof(DefaultAsset), false);

        string folderPath = _folder != null ? AssetDatabase.GetAssetPath(_folder) : null;
        bool isFolder = folderPath != null && AssetDatabase.IsValidFolder(folderPath);

        if (_folder != null && !isFolder)
            EditorGUILayout.HelpBox("Drag a folder from the Project window.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(!isFolder))
        {
            if (GUILayout.Button("Find Unused Assets", GUILayout.Height(24)))
                Find(folderPath);
        }

        if (!_hasSearched) return;

        if (_unused.Count == 0)
        {
            EditorGUILayout.HelpBox("Every asset in this folder is used.", MessageType.Info);
            return;
        }

        DrawList();
    }

    private void DrawList()
    {
        int selectedCount = _unused.Count(entry => entry.Selected);
        long selectedBytes = _unused.Where(entry => entry.Selected).Sum(entry => entry.Bytes);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"{_unused.Count} unused assets. {selectedCount} selected, " +
                                   $"{EditorUtility.FormatBytes(selectedBytes)}.", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Select All")) SetAllSelected(true);
            if (GUILayout.Button("Select None")) SetAllSelected(false);
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (Entry entry in _unused)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                entry.Selected = EditorGUILayout.ToggleLeft(entry.Path, entry.Selected);
                GUILayout.Label(EditorUtility.FormatBytes(entry.Bytes), GUILayout.Width(70));

                if (GUILayout.Button("Show", GUILayout.Width(50)))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(entry.Path));
            }
        }

        EditorGUILayout.EndScrollView();

        using (new EditorGUI.DisabledScope(selectedCount == 0))
        {
            if (GUILayout.Button("Move Selected to Trash", GUILayout.Height(28)))
                MoveSelectedToTrash(selectedCount, selectedBytes);
        }
    }

    private void Find(string folderPath)
    {
        _unused.Clear();
        _hasSearched = true;

        HashSet<string> used = new(AssetDatabase.GetDependencies(GetRoots().ToArray(), true));

        // FindAssets returns one GUID per asset file, but a file can come back more than once, so paths are deduplicated.
        HashSet<string> paths = new(AssetDatabase.FindAssets("", new[] { folderPath }).Select(AssetDatabase.GUIDToAssetPath));

        foreach (string path in paths)
        {
            if (AssetDatabase.IsValidFolder(path) || used.Contains(path) || IsSkipped(path)) continue;

            _unused.Add(new Entry { Path = path, Bytes = new FileInfo(path).Length });
        }

        _unused.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
    }

    /// <summary>The assets the build starts from. Anything none of them depend on never reaches the build.</summary>
    private static List<string> GetRoots()
    {
        List<string> roots = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToList();

        // Resources.Load can load anything in a Resources folder by name, so all of it counts as used.
        foreach (string guid in AssetDatabase.FindAssets("", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Resources/") && !AssetDatabase.IsValidFolder(path))
                roots.Add(path);
        }

        AddAssetPath(roots, GraphicsSettings.defaultRenderPipeline);
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            AddAssetPath(roots, QualitySettings.GetRenderPipelineAssetAt(i));
        }

        foreach (Object asset in PlayerSettings.GetPreloadedAssets())
        {
            AddAssetPath(roots, asset);
        }

        return roots;
    }

    private static void AddAssetPath(List<string> roots, Object asset)
    {
        if (asset == null) return;

        string path = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(path))
            roots.Add(path);
    }

    /// <summary>
    /// True for files this tool never lists: the skipped extensions, scenes (Sandbox scenes aren't in Build Settings
    /// but are still in use), and anything in an Editor folder, which only the editor loads.
    /// </summary>
    private static bool IsSkipped(string path)
    {
        if (path.EndsWith(".unity") || path.Contains("/Editor/")) return true;

        return SkippedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
    }

    private void SetAllSelected(bool selected)
    {
        foreach (Entry entry in _unused)
        {
            entry.Selected = selected;
        }
    }

    private void MoveSelectedToTrash(int selectedCount, long selectedBytes)
    {
        bool confirmed = EditorUtility.DisplayDialog("Move to Trash",
            $"Move {selectedCount} assets ({EditorUtility.FormatBytes(selectedBytes)}) to the system trash?\n\n" +
            "Commit your work first, so you can bring them back with Git if something turns out to need them.",
            "Move to Trash", "Cancel");

        if (!confirmed) return;

        string[] paths = _unused.Where(entry => entry.Selected).Select(entry => entry.Path).ToArray();
        List<string> failed = new();

        AssetDatabase.MoveAssetsToTrash(paths, failed);

        foreach (string path in failed)
        {
            Debug.LogWarning($"Couldn't move {path} to the trash.");
        }

        _unused.RemoveAll(entry => entry.Selected && !failed.Contains(entry.Path));
    }
}