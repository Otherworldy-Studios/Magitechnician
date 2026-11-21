using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Editor window that builds WaveData assets from a JSON file. Each wave set in the file becomes one WaveData per
/// difficulty, such as Crypt_Easy, Crypt_Medium, and Crypt_Hard. The Wave and WaveEntry assets are saved inside their
/// WaveData asset, so each difficulty is one file. Importing again replaces the waves inside each WaveData but keeps the
/// file, so rooms that use it keep their reference. Open it from Tools > Dungeon > Wave Importer.
/// </summary>
public class WaveImporter : EditorWindow
{
    // Used when a wave set lists no difficulties.
    private static readonly DifficultyJson[] DefaultDifficulties =
    {
        new DifficultyJson { difficulty = "Easy", countMultiplier = 0.7f, spawnIntervalMultiplier = 1.3f },
        new DifficultyJson { difficulty = "Medium", countMultiplier = 1f, spawnIntervalMultiplier = 1f },
        new DifficultyJson { difficulty = "Hard", countMultiplier = 1.5f, spawnIntervalMultiplier = 0.75f },
    };

    [SerializeField] private TextAsset _jsonFile;
    [SerializeField] private DefaultAsset _outputFolder;
    [SerializeField] private DefaultAsset _enemyFolder;

    private readonly List<string> _log = new();
    private Vector2 _logScroll;

    [MenuItem("Tools/Dungeon/Wave Importer")]
    private static void Open()
    {
        GetWindow<WaveImporter>("Wave Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Builds one WaveData per difficulty for each wave set in the JSON file. Enemies are found by prefab name in the Enemy Folder.",
            MessageType.Info);

        _jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", _jsonFile, typeof(TextAsset), false);
        _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", _outputFolder, typeof(DefaultAsset), false);
        _enemyFolder = (DefaultAsset)EditorGUILayout.ObjectField("Enemy Folder", _enemyFolder, typeof(DefaultAsset), false);

        using (new EditorGUI.DisabledScope(_jsonFile == null || _outputFolder == null))
        {
            if (GUILayout.Button("Import", GUILayout.Height(28)))
                Import();
        }

        if (_log.Count == 0)
            return;

        EditorGUILayout.Space();
        _logScroll = EditorGUILayout.BeginScrollView(_logScroll);
        foreach (string line in _log)
        {
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.EndScrollView();
    }

    private void Import()
    {
        _log.Clear();

        string outputPath = AssetDatabase.GetAssetPath(_outputFolder);
        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            _log.Add("Output Folder isn't a folder.");
            return;
        }

        WaveFileJson file;
        try
        {
            file = JsonUtility.FromJson<WaveFileJson>(_jsonFile.text);
        }
        catch (Exception exception)
        {
            _log.Add($"The JSON couldn't be read: {exception.Message}");
            return;
        }

        if (file?.waveSets == null || file.waveSets.Length == 0)
        {
            _log.Add("The JSON has no waveSets.");
            return;
        }

        string enemyPath = _enemyFolder != null ? AssetDatabase.GetAssetPath(_enemyFolder) : "Assets";
        Dictionary<string, EnemyBase> enemies = FindEnemies(enemyPath);

        int created = 0;

        foreach (WaveSetJson waveSet in file.waveSets)
        {
            if (string.IsNullOrWhiteSpace(waveSet.name))
            {
                _log.Add("Skipped a wave set with no name.");
                continue;
            }

            DifficultyJson[] difficulties = waveSet.difficulties != null && waveSet.difficulties.Length > 0
                ? waveSet.difficulties
                : DefaultDifficulties;

            foreach (DifficultyJson difficulty in difficulties)
            {
                if (BuildWaveData(waveSet, difficulty, outputPath, enemies))
                    created++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _log.Insert(0, $"Built {created} WaveData assets in {outputPath}.");
    }

    /// <summary>Creates or updates one WaveData for a wave set at one difficulty. Returns false if it had no waves.</summary>
    private bool BuildWaveData(WaveSetJson waveSet, DifficultyJson difficulty, string outputPath, Dictionary<string, EnemyBase> enemies)
    {
        string assetName = $"{waveSet.name}_{difficulty.difficulty}";
        List<WaveJson> waves = GetWaves(waveSet, difficulty);

        if (waves.Count == 0)
        {
            _log.Add($"{assetName}: no waves, so it was skipped.");
            return false;
        }

        WaveData waveData = LoadOrCreateWaveData($"{outputPath}/{assetName}.asset");
        RemoveSubAssets(waveData);

        List<Wave> builtWaves = new List<Wave>();

        for (int w = 0; w < waves.Count; w++)
        {
            Wave wave = CreateInstance<Wave>();
            wave.name = $"{assetName}_Wave{w + 1}";
            AssetDatabase.AddObjectToAsset(wave, waveData);

            List<WaveEntry> builtEntries = new List<WaveEntry>();

            foreach (EntryJson entryJson in waves[w].entries ?? Array.Empty<EntryJson>())
            {
                if (!enemies.TryGetValue(entryJson.enemy ?? "", out EnemyBase enemy))
                {
                    _log.Add($"{wave.name}: no enemy prefab named \"{entryJson.enemy}\", so its entry was skipped.");
                    continue;
                }

                WaveEntry entry = CreateInstance<WaveEntry>();
                entry.name = $"{wave.name}_{entryJson.enemy}";
                AssetDatabase.AddObjectToAsset(entry, waveData);

                SetFields(entry, serialized =>
                {
                    serialized.FindProperty("_enemy").objectReferenceValue = enemy;
                    serialized.FindProperty("count").intValue = entryJson.count;
                });

                builtEntries.Add(entry);
            }

            SetFields(wave, serialized =>
            {
                SetList(serialized.FindProperty("_entries"), builtEntries);
                serialized.FindProperty("_spawnInterval").floatValue = waves[w].spawnInterval;
            });

            builtWaves.Add(wave);
        }

        float startDelay = difficulty.startDelay > 0f ? difficulty.startDelay : waveSet.startDelay;

        SetFields(waveData, serialized =>
        {
            SetList(serialized.FindProperty("_waves"), builtWaves);
            serialized.FindProperty("_startDelay").floatValue = startDelay;
        });

        EditorUtility.SetDirty(waveData);
        _log.Add($"{assetName}: {builtWaves.Count} waves.");
        return true;
    }

    /// <summary>
    /// The waves for one difficulty: the difficulty's own waves when it lists them, otherwise the wave set's waves with
    /// each count and spawn interval scaled by the difficulty's multipliers. Then the difficulty's extra waves at the end.
    /// </summary>
    private static List<WaveJson> GetWaves(WaveSetJson waveSet, DifficultyJson difficulty)
    {
        List<WaveJson> waves = new List<WaveJson>();

        if (difficulty.waves != null && difficulty.waves.Length > 0)
        {
            waves.AddRange(difficulty.waves);
        }
        else if (waveSet.waves != null)
        {
            // 0 means the multiplier was left out of the JSON.
            float countMultiplier = difficulty.countMultiplier > 0f ? difficulty.countMultiplier : 1f;
            float intervalMultiplier = difficulty.spawnIntervalMultiplier > 0f ? difficulty.spawnIntervalMultiplier : 1f;

            foreach (WaveJson baseWave in waveSet.waves)
            {
                waves.Add(Scale(baseWave, countMultiplier, intervalMultiplier));
            }
        }

        if (difficulty.extraWaves != null)
            waves.AddRange(difficulty.extraWaves);

        return waves;
    }

    /// <summary>A copy of the wave with each count scaled and rounded, keeping at least 1, and the spawn interval scaled.</summary>
    private static WaveJson Scale(WaveJson wave, float countMultiplier, float intervalMultiplier)
    {
        EntryJson[] entries = wave.entries ?? Array.Empty<EntryJson>();
        EntryJson[] scaled = new EntryJson[entries.Length];

        for (int i = 0; i < entries.Length; i++)
        {
            scaled[i] = new EntryJson
            {
                enemy = entries[i].enemy,
                count = Mathf.Max(1, Mathf.RoundToInt(entries[i].count * countMultiplier))
            };
        }

        return new WaveJson { spawnInterval = wave.spawnInterval * intervalMultiplier, entries = scaled };
    }

    /// <summary>Every prefab with an EnemyBase in the folder and its subfolders, by prefab name.</summary>
    private Dictionary<string, EnemyBase> FindEnemies(string folder)
    {
        Dictionary<string, EnemyBase> enemies = new Dictionary<string, EnemyBase>();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            EnemyBase enemy = prefab != null ? prefab.GetComponent<EnemyBase>() : null;

            if (enemy == null)
                continue;

            if (!enemies.TryAdd(prefab.name, enemy))
                _log.Add($"Two enemy prefabs are named \"{prefab.name}\". The first one found is used.");
        }

        return enemies;
    }

    private static WaveData LoadOrCreateWaveData(string path)
    {
        WaveData waveData = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        if (waveData != null)
            return waveData;

        waveData = CreateInstance<WaveData>();
        AssetDatabase.CreateAsset(waveData, path);
        return waveData;
    }

    /// <summary>Deletes the Wave and WaveEntry assets saved inside the WaveData from the last import.</summary>
    private static void RemoveSubAssets(WaveData waveData)
    {
        string path = AssetDatabase.GetAssetPath(waveData);

        foreach (Object subAsset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (subAsset is Wave || subAsset is WaveEntry)
                Object.DestroyImmediate(subAsset, true);
        }
    }

    /// <summary>
    /// Edits an asset's serialized fields through SerializedObject, because Wave, WaveEntry, and WaveData keep their
    /// fields private. Throws with the asset's type if a field name changed, so the importer can be updated.
    /// </summary>
    private static void SetFields(Object asset, Action<SerializedObject> edit)
    {
        SerializedObject serialized = new SerializedObject(asset);

        try
        {
            edit(serialized);
        }
        catch (NullReferenceException)
        {
            throw new InvalidOperationException(
                $"A field on {asset.GetType().Name} wasn't found. Its name changed, so update WaveImporter to match.");
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetList<T>(SerializedProperty list, List<T> items) where T : Object
    {
        list.arraySize = items.Count;

        for (int i = 0; i < items.Count; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }

    // The JSON layout. JsonUtility needs public fields named exactly like the JSON keys.

    [Serializable]
    private class WaveFileJson
    {
        public WaveSetJson[] waveSets;
    }

    [Serializable]
    private class WaveSetJson
    {
        public string name;
        public float startDelay;
        public WaveJson[] waves;
        public DifficultyJson[] difficulties;
    }

    [Serializable]
    private class DifficultyJson
    {
        public string difficulty;
        public float countMultiplier;
        public float spawnIntervalMultiplier;
        public float startDelay;
        public WaveJson[] waves;
        public WaveJson[] extraWaves;
    }

    [Serializable]
    private class WaveJson
    {
        public float spawnInterval;
        public EntryJson[] entries;
    }

    [Serializable]
    private class EntryJson
    {
        public string enemy;
        public int count;
    }
}