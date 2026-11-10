using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor window that creates or updates GunPart, Charm, and Consumable assets from a JSON file.
// Open it from Tools > Items > JSON Importer. This file must be in an Editor folder.
// Running it again updates the existing assets in place, so references to them stay intact.

public class ItemJsonImporter : EditorWindow
{
    // ---------- JSON layout ----------

    [Serializable]
    private class ModifierData
    {
        public string stat;   // StatDefinition asset name, for example "Stat_Damage"
        public string op;     // "Add", "Multiply", or "Set"
        public float value;
    }

    [Serializable]
    private class GunPartData
    {
        public string id;          // asset file name
        public string name;
        public string type;        // Barrel, Magazine, Scope, Stock, or Underbarrel
        public string rarity;      // Common, Uncommon, Rare, or Epic
        public string description;
        public string prefab;      // prefab asset name, for example "PH_LongBarrel"
        public string fireMode;    // barrels only: Semi, Burst, or Auto
        public string damageType;  // magazines only: Physical, Fire, Ice, Electric, or Poison
        public List<ModifierData> modifiers = new();
    }

    [Serializable]
    private class CharmData
    {
        public string id;
        public string name;
        public string rarity;
        public string description;
        public int price;
        public List<ModifierData> modifiers = new();
    }

    [Serializable]
    private class ConsumableData
    {
        public string id;
        public string name;
        public string rarity;
        public string description;
        public float duration;
        public int instantHeal;
        public List<ModifierData> modifiers = new();
    }

    [Serializable]
    private class ItemFile
    {
        public List<GunPartData> gunParts = new();
        public List<CharmData> charms = new();
        public List<ConsumableData> consumables = new();
    }

    // ---------- Window ----------

    private TextAsset _json;
    private string _outputFolder = "Assets/_Project/Data/Items";
    private RoomCompletionLootPool _lootPool;

    private Dictionary<string, StatDefinition> _stats;
    private int _created;
    private int _updated;
    private readonly List<string> _warnings = new();

    [MenuItem("Tools/Items/JSON Importer")]
    private static void Open()
    {
        GetWindow<ItemJsonImporter>("Item Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Creates or updates item assets from a JSON file.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        _json = (TextAsset)EditorGUILayout.ObjectField("Items JSON", _json, typeof(TextAsset), false);
        _outputFolder = EditorGUILayout.TextField("Output folder", _outputFolder);
        _lootPool = (RoomCompletionLootPool)EditorGUILayout.ObjectField(
            new GUIContent("Loot pool (optional)", "Every non-starter gun part is added to this pool with its rarity as the weight."),
            _lootPool, typeof(RoomCompletionLootPool), false);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(_json == null))
        {
            if (GUILayout.Button("Import", GUILayout.Height(30)))
            {
                Import();
            }
        }
    }

    // ---------- Import ----------

    private void Import()
    {
        ItemFile file;
        try
        {
            file = JsonUtility.FromJson<ItemFile>(_json.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemImporter] Couldn't read {_json.name}: {e.Message}");
            return;
        }

        if (file == null)
        {
            Debug.LogError($"[ItemImporter] {_json.name} is empty.");
            return;
        }

        _created = 0;
        _updated = 0;
        _warnings.Clear();
        _stats = LoadStatDefinitions();

        var importedParts = new List<(GunPart part, string rarity)>();

        // Each item imports on its own, so one bad item logs an error and the rest still import.
        foreach (GunPartData data in file.gunParts)
        {
            try
            {
                GunPart part = ImportGunPart(data);
                if (part != null)
                {
                    importedParts.Add((part, data.rarity));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemImporter] {data.name} failed: {e.Message}");
            }
        }

        foreach (CharmData data in file.charms)
        {
            try
            {
                ImportCharm(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemImporter] {data.name} failed: {e.Message}");
            }
        }

        foreach (ConsumableData data in file.consumables)
        {
            try
            {
                ImportConsumable(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemImporter] {data.name} failed: {e.Message}");
            }
        }

        if (_lootPool != null)
        {
            FillLootPool(importedParts);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        foreach (string warning in _warnings)
        {
            Debug.LogWarning($"[ItemImporter] {warning}");
        }

        Debug.Log($"[ItemImporter] Done: {_created} created, {_updated} updated, {_warnings.Count} warnings.");
    }

    private GunPart ImportGunPart(GunPartData data)
    {
        Type partClass = data.type switch
        {
            "Barrel" => typeof(GunBarrel),
            "Magazine" => typeof(GunMagazine),
            "Scope" => typeof(GunScope),
            "Stock" => typeof(GunStock),
            "Underbarrel" => typeof(GunUnderbarrel),
            _ => null
        };

        if (partClass == null)
        {
            _warnings.Add($"{data.name}: unknown part type \"{data.type}\". Skipped.");
            return null;
        }

        string path = $"{_outputFolder}/GunParts/{data.type}s/{data.id}.asset";
        var part = (GunPart)LoadOrCreate(partClass, path, data.id, data.name, "_partName");

        var so = new SerializedObject(part);
        SetString(so, data.name, data.name, "_partName", "partName", "PartName");
        SetString(so, data.name, data.description, "_description", "description", "Description");
        SetObject(so, data.name, FindAsset<GameObject>(data.prefab, data.name), "_partPrefab", "partPrefab", "PartPrefab");
        SetRarity(so, data.rarity, data.name);
        WriteModifiers(Prop(so, data.name, "statModifiers", "_statModifiers", "StatModifiers"), data.modifiers, data.name, permanent: true, duration: 0f);

        if (part is GunBarrel && !string.IsNullOrEmpty(data.fireMode))
        {
            SetEnum<FireMode>(Prop(so, data.name, "FireMode", "fireMode", "_fireMode"), data.fireMode, data.name);
        }

        if (part is GunMagazine && !string.IsNullOrEmpty(data.damageType))
        {
            SetEnum<DamageType>(Prop(so, data.name, "DamageType", "damageType", "_damageType"), data.damageType, data.name);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(part);
        return part;
    }

    private void ImportCharm(CharmData data)
    {
        string path = $"{_outputFolder}/Charms/{data.id}.asset";
        var charm = (Charm)LoadOrCreate(typeof(Charm), path, data.id, data.name, "_displayName");

        var so = new SerializedObject(charm);
        SetString(so, data.name, data.name, "_displayName", "displayName", "DisplayName");
        SetString(so, data.name, data.description, "_description", "description", "Description");
        SetRarity(so, data.rarity, data.name);
        SetInt(so, data.name, data.price, "Price", "price", "_price");
        WriteModifiers(Prop(so, data.name, "statModifiers", "_statModifiers", "StatModifiers"), data.modifiers, data.name, permanent: true, duration: 0f);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(charm);
    }

    private void ImportConsumable(ConsumableData data)
    {
        string path = $"{_outputFolder}/Consumables/{data.id}.asset";
        var consumable = (Consumable)LoadOrCreate(typeof(Consumable), path, data.id, data.name, "_displayName");

        var so = new SerializedObject(consumable);
        SetString(so, data.name, data.name, "_displayName", "displayName", "DisplayName");
        SetString(so, data.name, data.description, "_description", "description", "Description");
        SetRarity(so, data.rarity, data.name);
        SetFloat(so, data.name, data.duration, "duration", "_duration", "Duration");
        SetInt(so, data.name, data.instantHeal, "instantHeal", "_instantHeal", "InstantHeal");

        // Consumable modifiers run out: each one lasts for the consumable's duration.
        WriteModifiers(Prop(so, data.name, "statModifiers", "_statModifiers", "StatModifiers"), data.modifiers, data.name, permanent: false, duration: data.duration);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(consumable);
    }

    // Replaces the asset's modifier list with the modifiers from the JSON.
    private void WriteModifiers(SerializedProperty list, List<ModifierData> modifiers, string itemName, bool permanent, float duration)
    {
        if (list == null)
        {
            return;
        }

        list.ClearArray();

        foreach (ModifierData data in modifiers)
        {
            if (!_stats.TryGetValue(data.stat, out StatDefinition stat))
            {
                _warnings.Add($"{itemName}: no StatDefinition asset named \"{data.stat}\". Modifier skipped.");
                continue;
            }

            if (!Enum.TryParse(data.op, true, out ModifierOperation operation))
            {
                _warnings.Add($"{itemName}: ModifierOperation has no \"{data.op}\" value. Modifier on {data.stat} skipped.");
                continue;
            }

            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            SerializedProperty element = list.GetArrayElementAtIndex(index);

            element.FindPropertyRelative("Stat").objectReferenceValue = stat;
            element.FindPropertyRelative("Operation").enumValueIndex = EnumIndex(operation);
            element.FindPropertyRelative("Value").floatValue = data.value;
            element.FindPropertyRelative("Duration").floatValue = duration;
            element.FindPropertyRelative("Permanent").boolValue = permanent;
        }
    }

    // Adds each non-starter part to the loot pool, or updates its weight if it's already there.
    private void FillLootPool(List<(GunPart part, string rarity)> parts)
    {
        var so = new SerializedObject(_lootPool);
        SerializedProperty rewards = so.FindProperty("_rewards");
        string[] weightNames = Enum.GetNames(typeof(RarityChance));

        foreach ((GunPart part, string rarity) in parts)
        {
            int weightIndex = Array.IndexOf(weightNames, rarity);
            if (weightIndex < 0)
            {
                continue; // Unknown rarities stay out of the pool.
            }

            SerializedProperty entry = null;
            for (int i = 0; i < rewards.arraySize; i++)
            {
                SerializedProperty existing = rewards.GetArrayElementAtIndex(i);
                if (existing.FindPropertyRelative("_item").objectReferenceValue == part)
                {
                    entry = existing;
                    break;
                }
            }

            if (entry == null)
            {
                rewards.InsertArrayElementAtIndex(rewards.arraySize);
                entry = rewards.GetArrayElementAtIndex(rewards.arraySize - 1);
                entry.FindPropertyRelative("_item").objectReferenceValue = part;
            }

            entry.FindPropertyRelative("_weight").enumValueIndex = weightIndex;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(_lootPool);
    }

    // ---------- Field access ----------

    // Returns the first serialized field that exists from the given names, or logs a warning if none do.
    // Trying several names keeps the importer working if a field is renamed, for example _price to Price.
    private SerializedProperty Prop(SerializedObject so, string itemName, params string[] names)
    {
        foreach (string name in names)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property != null)
            {
                return property;
            }
        }

        _warnings.Add($"{itemName}: {so.targetObject.GetType().Name} has no serialized field named {string.Join(" or ", names)}.");
        return null;
    }

    private void SetString(SerializedObject so, string itemName, string value, params string[] names)
    {
        SerializedProperty property = Prop(so, itemName, names);
        if (property != null) property.stringValue = value;
    }

    private void SetInt(SerializedObject so, string itemName, int value, params string[] names)
    {
        SerializedProperty property = Prop(so, itemName, names);
        if (property != null) property.intValue = value;
    }

    private void SetFloat(SerializedObject so, string itemName, float value, params string[] names)
    {
        SerializedProperty property = Prop(so, itemName, names);
        if (property != null) property.floatValue = value;
    }

    private void SetObject(SerializedObject so, string itemName, UnityEngine.Object value, params string[] names)
    {
        SerializedProperty property = Prop(so, itemName, names);
        if (property != null) property.objectReferenceValue = value;
    }

    // ---------- Helpers ----------

    // Returns the asset to update, or creates a new one. It looks in this order:
    // 1. The asset at the output path.
    // 2. Any asset of the same type in the project whose file name is the item's id,
    //    or whose display name field matches the item's name. This picks up assets made by hand.
    // 3. Otherwise, a new asset at the output path.
    private ScriptableObject LoadOrCreate(Type type, string path, string id, string displayName, string nameField)
    {
        var existing = AssetDatabase.LoadAssetAtPath(path, type) as ScriptableObject;
        if (existing == null)
        {
            existing = FindExisting(type, id, displayName, nameField);
        }

        if (existing != null)
        {
            _updated++;
            return existing;
        }

        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        var asset = CreateInstance(type);
        AssetDatabase.CreateAsset(asset, path);
        _created++;
        return asset;
    }

    // Searches the whole project for an asset of this type whose file name is the id,
    // or whose name field (for example _partName) matches the display name.
    private static ScriptableObject FindExisting(Type type, string id, string displayName, string nameField)
    {
        ScriptableObject nameMatch = null;

        foreach (string guid in AssetDatabase.FindAssets($"t:{type.Name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath(path, type) as ScriptableObject;
            if (asset == null)
            {
                continue;
            }

            // "Shadow Step", "ShadowStep", and "shadow_step" all match the id "ShadowStep".
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (Normalize(fileName) == Normalize(id) || Normalize(fileName) == Normalize(displayName))
            {
                return asset;
            }

            if (nameMatch == null)
            {
                SerializedProperty nameProperty = new SerializedObject(asset).FindProperty(nameField);
                if (nameProperty != null && nameProperty.stringValue == displayName)
                {
                    nameMatch = asset;
                }
            }
        }

        return nameMatch;
    }

    // Lowercases a name and removes spaces, underscores, and hyphens, so names compare loosely.
    private static string Normalize(string name)
    {
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : name.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    // Maps each StatDefinition asset's file name to the asset.
    private Dictionary<string, StatDefinition> LoadStatDefinitions()
    {
        var map = new Dictionary<string, StatDefinition>();
        foreach (string guid in AssetDatabase.FindAssets("t:StatDefinition"))
        {
            var stat = AssetDatabase.LoadAssetAtPath<StatDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (stat != null && !map.ContainsKey(stat.name))
            {
                map.Add(stat.name, stat);
            }
        }
        return map;
    }

    private T FindAsset<T>(string assetName, string itemName) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(assetName))
        {
            return null;
        }

        foreach (string guid in AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == assetName)
            {
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
        }

        _warnings.Add($"{itemName}: no {typeof(T).Name} named \"{assetName}\".");
        return null;
    }

    // Sets the item's ItemRarity field. Checks the common field names, since the field is private on each item class.
    private void SetRarity(SerializedObject so, string rarity, string itemName)
    {
        SerializedProperty property = Prop(so, itemName, "_rarity", "rarity", "Rarity");
        if (property == null)
        {
            return;
        }

        SetEnum<ItemRarity>(property, rarity, itemName);
    }

    private void SetEnum<T>(SerializedProperty property, string value, string itemName) where T : struct, Enum
    {
        if (property == null)
        {
            return;
        }

        if (Enum.TryParse(value, true, out T parsed))
        {
            property.enumValueIndex = EnumIndex(parsed);
        }
        else
        {
            _warnings.Add($"{itemName}: {typeof(T).Name} has no \"{value}\" value.");
        }
    }

    // SerializedProperty stores enums by their position in the declaration, not by their number.
    private static int EnumIndex<T>(T value) where T : Enum
    {
        return Array.IndexOf(Enum.GetValues(typeof(T)), value);
    }
}