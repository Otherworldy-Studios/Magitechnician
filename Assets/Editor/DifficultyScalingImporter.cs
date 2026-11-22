using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window that builds DifficultyScaling assets from a JSON file. Each scaling in the file becomes one asset,
/// with one tier per entry in its tiers list, in order: the first tier is the first combat room. Stats are found by
/// asset name (Stat_MaxHealth) or display name (Max Health). Importing again replaces the tiers but keeps the file,
/// so the WaveManager that uses it keeps its reference. Open it from Tools > Dungeon > Difficulty Scaling Importer.
/// </summary>
public class DifficultyScalingImporter : EditorWindow
{
    private const string ExampleJson =
@"{
  ""scalings"": [
    {
      ""name"": ""DifficultyScaling_Normal"",
      ""tiers"": [
        { ""modifiers"": [] },
        { ""modifiers"": [
            { ""stat"": ""Stat_MaxHealth"", ""operation"": ""Multiply"", ""value"": 1.2 },
            { ""stat"": ""Stat_AttackDamage"", ""operation"": ""Multiply"", ""value"": 1.1 }
        ] },
        { ""modifiers"": [
            { ""stat"": ""Stat_MaxHealth"", ""operation"": ""Multiply"", ""value"": 1.4 },
            { ""stat"": ""Stat_AttackDamage"", ""operation"": ""Multiply"", ""value"": 1.2 },
            { ""stat"": ""Stat_CooldownTime"", ""operation"": ""Multiply"", ""value"": 0.9 }
        ] },
        { ""modifiers"": [
            { ""stat"": ""Stat_MaxHealth"", ""operation"": ""Multiply"", ""value"": 1.6 },
            { ""stat"": ""Stat_AttackDamage"", ""operation"": ""Multiply"", ""value"": 1.3 },
            { ""stat"": ""Stat_WindUpTime"", ""operation"": ""Multiply"", ""value"": 0.85 }
        ] }
      ]
    }
  ]
}
";

    [SerializeField] private TextAsset _jsonFile;
    [SerializeField] private DefaultAsset _outputFolder;
    [SerializeField] private DefaultAsset _statFolder;

    private readonly List<string> _log = new();
    private Vector2 _logScroll;

    [MenuItem("Tools/Dungeon/Difficulty Scaling Importer")]
    private static void Open()
    {
        GetWindow<DifficultyScalingImporter>("Difficulty Scaling Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Builds one DifficultyScaling asset for each scaling in the JSON file. Tiers are in room order. " +
            "Stats are found by asset name or display name in the Stat Folder.",
            MessageType.Info);

        _jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", _jsonFile, typeof(TextAsset), false);
        _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", _outputFolder, typeof(DefaultAsset), false);
        _statFolder = (DefaultAsset)EditorGUILayout.ObjectField("Stat Folder", _statFolder, typeof(DefaultAsset), false);

        using (new EditorGUI.DisabledScope(_jsonFile == null || _outputFolder == null))
        {
            if (GUILayout.Button("Import", GUILayout.Height(28)))
                Import();
        }

        using (new EditorGUI.DisabledScope(_outputFolder == null))
        {
            if (GUILayout.Button("Create Example JSON"))
                CreateExampleJson();
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

        ScalingFileJson file;
        try
        {
            file = JsonUtility.FromJson<ScalingFileJson>(_jsonFile.text);
        }
        catch (Exception exception)
        {
            _log.Add($"The JSON couldn't be read: {exception.Message}");
            return;
        }

        if (file?.scalings == null || file.scalings.Length == 0)
        {
            _log.Add("The JSON has no scalings.");
            return;
        }

        string statPath = _statFolder != null ? AssetDatabase.GetAssetPath(_statFolder) : "Assets";
        Dictionary<string, StatDefinition> stats = FindStats(statPath);

        int built = 0;

        foreach (ScalingJson scaling in file.scalings)
        {
            if (string.IsNullOrWhiteSpace(scaling.name))
            {
                _log.Add("Skipped a scaling with no name.");
                continue;
            }

            if (BuildScaling(scaling, outputPath, stats))
                built++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _log.Insert(0, $"Built {built} DifficultyScaling assets in {outputPath}.");
    }

    private bool BuildScaling(ScalingJson scaling, string outputPath, Dictionary<string, StatDefinition> stats)
    {
        TierJson[] tiers = scaling.tiers ?? Array.Empty<TierJson>();
        if (tiers.Length == 0)
        {
            _log.Add($"{scaling.name}: no tiers, so it scales nothing. Built anyway.");
        }

        DifficultyScaling asset = LoadOrCreate($"{outputPath}/{scaling.name}.asset");

        // DifficultyScaling, Tier and StatModifier keep their fields private, so they're set through SerializedObject.
        SerializedObject serialized = new SerializedObject(asset);
        SerializedProperty tierList = serialized.FindProperty("_tiers");
        tierList.arraySize = tiers.Length;

        for (int t = 0; t < tiers.Length; t++)
        {
            List<ModifierJson> modifiers = ResolveModifiers(scaling.name, t, tiers[t], stats, out List<StatDefinition> resolvedStats);

            SerializedProperty modifierList = tierList.GetArrayElementAtIndex(t).FindPropertyRelative("_modifiers");
            modifierList.arraySize = modifiers.Count;

            for (int m = 0; m < modifiers.Count; m++)
            {
                ModifierJson json = modifiers[m];
                SerializedProperty modifier = modifierList.GetArrayElementAtIndex(m);

                modifier.FindPropertyRelative("_stat").objectReferenceValue = resolvedStats[m];
                modifier.FindPropertyRelative("_operation").enumValueIndex = (int)ParseOperation(json.operation);
                modifier.FindPropertyRelative("_value").floatValue = json.value;

                // An enemy's modifiers last its whole life unless the JSON gives a duration.
                modifier.FindPropertyRelative("_duration").floatValue = json.duration;
                modifier.FindPropertyRelative("_permanent").boolValue = json.duration <= 0f;
            }

            _log.Add($"{scaling.name}, tier {t}: {Describe(modifiers)}");
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return true;
    }

    /// <summary>
    /// Keeps the tier's modifiers whose stat and operation are valid, and logs the rest. The stats are returned in the
    /// same order, so index m of both lists is one modifier.
    /// </summary>
    private List<ModifierJson> ResolveModifiers(string scalingName, int tierIndex, TierJson tier,
        Dictionary<string, StatDefinition> stats, out List<StatDefinition> resolvedStats)
    {
        List<ModifierJson> valid = new();
        resolvedStats = new List<StatDefinition>();

        if (tier?.modifiers == null)
            return valid;

        foreach (ModifierJson modifier in tier.modifiers)
        {
            string where = $"{scalingName}, tier {tierIndex}";

            if (modifier == null || string.IsNullOrWhiteSpace(modifier.stat))
            {
                _log.Add($"{where}: skipped a modifier with no stat.");
                continue;
            }

            if (!stats.TryGetValue(Key(modifier.stat), out StatDefinition stat))
            {
                _log.Add($"{where}: no stat named \"{modifier.stat}\". Skipped.");
                continue;
            }

            if (!Enum.TryParse(modifier.operation, true, out ModifierOperation operation))
            {
                _log.Add($"{where}: \"{modifier.operation}\" isn't Add, Multiply, or Set. Skipped {modifier.stat}.");
                continue;
            }

            if (operation == ModifierOperation.Multiply && modifier.value <= 0f)
            {
                _log.Add($"{where}: {modifier.stat} multiplies by {modifier.value}, which zeroes or flips the stat. Imported anyway.");
            }

            valid.Add(modifier);
            resolvedStats.Add(stat);
        }

        return valid;
    }

    private static ModifierOperation ParseOperation(string operation)
    {
        return Enum.TryParse(operation, true, out ModifierOperation parsed) ? parsed : ModifierOperation.Multiply;
    }

    /// <summary>Every StatDefinition under the folder, keyed by both its asset name and its display name.</summary>
    private Dictionary<string, StatDefinition> FindStats(string folderPath)
    {
        Dictionary<string, StatDefinition> stats = new();

        foreach (string guid in AssetDatabase.FindAssets("t:StatDefinition", new[] { folderPath }))
        {
            StatDefinition stat = AssetDatabase.LoadAssetAtPath<StatDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (stat == null) continue;

            AddStatKey(stats, stat.name, stat);
            if (!string.IsNullOrWhiteSpace(stat.DisplayName))
                AddStatKey(stats, stat.DisplayName, stat);
        }

        return stats;
    }

    private void AddStatKey(Dictionary<string, StatDefinition> stats, string name, StatDefinition stat)
    {
        string key = Key(name);

        if (stats.TryGetValue(key, out StatDefinition existing) && existing != stat)
        {
            _log.Add($"Two stats are named \"{name}\" ({existing.name} and {stat.name}). Using {existing.name}.");
            return;
        }

        stats[key] = stat;
    }

    // Lowercase with spaces and underscores removed, so "Max Health", "MaxHealth" and "maxhealth" all match.
    private static string Key(string name)
    {
        return name.Replace(" ", "").Replace("_", "").ToLowerInvariant();
    }

    private static string Describe(List<ModifierJson> modifiers)
    {
        if (modifiers.Count == 0) return "no modifiers";

        return string.Join(", ", modifiers.Select(m => $"{m.stat} {m.operation} {m.value}"));
    }

    private static DifficultyScaling LoadOrCreate(string path)
    {
        DifficultyScaling asset = AssetDatabase.LoadAssetAtPath<DifficultyScaling>(path);
        if (asset != null) return asset;

        asset = CreateInstance<DifficultyScaling>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private void CreateExampleJson()
    {
        string folder = AssetDatabase.GetAssetPath(_outputFolder);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/DifficultyScaling_Example.json");

        File.WriteAllText(path, ExampleJson);
        AssetDatabase.ImportAsset(path);

        _jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        _log.Clear();
        _log.Add($"Wrote {path}. Check the stat names match your StatDefinition assets, then Import.");
    }

    // JSON shapes. JsonUtility needs public fields named exactly like the JSON keys.

    [Serializable]
    private class ScalingFileJson
    {
        public ScalingJson[] scalings;
    }

    [Serializable]
    private class ScalingJson
    {
        public string name;
        public TierJson[] tiers;
    }

    [Serializable]
    private class TierJson
    {
        public ModifierJson[] modifiers;
    }

    [Serializable]
    private class ModifierJson
    {
        public string stat;
        public string operation = "Multiply";
        public float value = 1f;
        public float duration;
    }
}