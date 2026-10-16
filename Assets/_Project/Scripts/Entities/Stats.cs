using UnityEngine;
using System.Collections.Generic;
using System;

//Stats class holds base stats and modifiers for any GameObject and reports final values.
//Final Value = (base + every add moddifier) * every multiply modifier.
//Stats never touches current health, damage, or healing. It only reports the final values.
[DisallowMultipleComponent]
public class Stats : MonoBehaviour
{
    //If values are set, base values come from this asset, otherwise the inllin values are used.
    [SerializeField] private EntityStats _statsAsset;
    [SerializeField] private EntityStats.BaseStats _inlineBaseStats = EntityStats.BaseStats.Default;

    //Fires with the stat and its new value whenever a final value changes.
    public event Action<statType, float> OnStatChanged;

    //Permanent modifiers (Charms) stay until source is removed.
    private readonly List<EntityStatsModifier> _permanentModifiers = new List<EntityStatsModifier>();

    //Temporary modifiers (Consumables), grouped by source, stay until duration expires.
    private readonly Dictionary<object, List<EntityStatsModifier>> _temporaryModifiers = new Dictionary<object, List<EntityStatsModifier>>();

    private readonly Dictionary<statType, float> _finalValues = new Dictionary<statType, float>();
    private readonly HashSet<statType> _expiredStats = new HashSet<statType>();
    private readonly List<object> _emptySources = new List<object>();
    private static readonly statType[] _allStats = (statType[])Enum.GetValues(typeof(statType));
    private bool _initialized = false;

    public float speed => GetFinalValue(statType.speed);
    public float maxHealth => GetFinalValue(statType.maxHealth);
    public float baseDamage => GetFinalValue(statType.baseDamage);

    private EntityStats.BaseStats BaseStats => _statsAsset != null ? _statsAsset.baseStats : _inlineBaseStats;

    private void Awake() => EnsureInitialized();

    private void EnsureInitialized()
    {
        if (_initialized) 
            return;
        _initialized = true;
        foreach (statType stat in _allStats)
        {
            _finalValues[stat] = Calculate(stat);
        }
    }

    public float GetFinalValue(statType stat)
    {
        EnsureInitialized();
        return _finalValues[stat];
    }

    public float GetBaseValue(statType stat) => BaseStats.Get(stat);


    //Adding and Removing modifiers
    public void AddModifier(EntityStatsModifier modifier)
    {
        if (modifier == null) 
            throw new ArgumentNullException(nameof(modifier));
        if (modifier.Source == null) 
            throw new ArgumentException("Modifier source cannot be null.", nameof(modifier));

        EnsureInitialized();
        if (modifier.isTemporary)
        {
            if (!_temporaryModifiers.TryGetValue(modifier.Source, out List<EntityStatsModifier> list))
            {
                list = new List<EntityStatsModifier>();
                _temporaryModifiers.Add(modifier.Source, list);
            }
            list.Add(modifier);
        }
        else
        {
            _permanentModifiers.Add(modifier);
        }

        Recalculate(modifier.Stat);
    }

    public bool RemoveModifier(EntityStatsModifier modifier)
    {
        if (modifier == null) 
            return false;

        bool removed = _permanentModifiers.Remove(modifier);

        if (!removed && modifier.Source != null && _temporaryModifiers.TryGetValue(modifier.Source, out List<EntityStatsModifier> list))
        {
            removed = list.Remove(modifier);
            if (list.Count == 0)
            {
                _temporaryModifiers.Remove(modifier.Source);
            }
        }

        if (removed)
        {
            Recalculate(modifier.Stat);
        }
        return removed;
    }

    //Removes eveery modfier (permanent and temporary) from the given source.
    public void RemoveAllFromSource(object source)
    {
        if (source == null) 
            return;

        var affectedStats = new HashSet<statType>();

        for (int i = _permanentModifiers.Count - 1; i >= 0; i--)
        {
            if (!Equals(_permanentModifiers[i].Source, source)) 
                continue;
            affectedStats.Add(_permanentModifiers[i].Stat);
            _permanentModifiers.RemoveAt(i);
        }

        if (_temporaryModifiers.TryGetValue(source, out List<EntityStatsModifier> list))
        {
            foreach (EntityStatsModifier modifier in list)
            {
                affectedStats.Add(modifier.Stat);
            }
            _temporaryModifiers.Remove(source);
        }

        foreach (statType stat in affectedStats)
        {
            Recalculate(stat);
        }

    }

    //Temporary modifier expiration is handled in Update(), which checks all temporary modifiers and removes any that have expired.
    private void Update()
    {
        if (_temporaryModifiers.Count == 0)
            return;

        float deltaTime = Time.deltaTime; //scaled time; pausing game pauses buffs
        _expiredStats.Clear();
        _emptySources.Clear();

        foreach (KeyValuePair<object, List<EntityStatsModifier>> pair in _temporaryModifiers)
        {
            List<EntityStatsModifier> list = pair.Value;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                list[i].UpdateDuration(deltaTime);
                if (list[i].isExpired)
                {
                    _expiredStats.Add(list[i].Stat);
                    list.RemoveAt(i);
                }
            }

            if (list.Count == 0)
            {
                _emptySources.Add(pair.Key);
            }
        }

        foreach (object source in _emptySources)
        {
            _temporaryModifiers.Remove(source);
        }

        foreach (statType stat in _expiredStats)
        {
            Recalculate(stat);
        }
    }

    //Calculates the final value for a given stat, based on base value and all modifiers.
    private float Calculate(statType stat)
    {
        float added = 0f;
        float multiplied = 1f;

        foreach(EntityStatsModifier m in _permanentModifiers)
        {
            Accumulate(m, stat, ref added, ref multiplied);
        }

        foreach (List<EntityStatsModifier> list in _temporaryModifiers.Values)
        {
            foreach (EntityStatsModifier m in list)
            {
                Accumulate(m, stat, ref added, ref multiplied);
            }
        }

        return Mathf.Max(0f, (BaseStats.Get(stat) + added) * multiplied);
    }

    private static void Accumulate(EntityStatsModifier m, statType stat, ref float added, ref float multiplied)
    {
        if (m.Stat != stat) return;
        if (m.Type == modifierType.add) added += m.Value;
        else multiplied *= m.Value;
    }

    private void Recalculate(statType stat)
    {
        float newValue = Calculate(stat);
        if (Mathf.Approximately(newValue, _finalValues[stat]))
            return;
        _finalValues[stat] = newValue;
        OnStatChanged?.Invoke(stat, newValue);
    }
}
