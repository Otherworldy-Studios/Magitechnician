using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Manages abilities granted by inventory items, including permanent and timed abilities.
/// </summary>
public class PlayerAbilities : MonoBehaviour
{
    private struct AbilityEntry
    {
        public IInventoryItem Item;
        public GameObject Instance;
        public bool HasDuration;
        public float EndTime; // Time.time when a timed ability wears off.
    }

    [Title("Ability Testing")]
    [SerializeField] private GunPart _testGunPart;
    [SerializeField] private Consumable _testConsumable;
    [SerializeField] private Charm _testCharm;
    [SerializeField] private float _testDuration = 5f;

    private readonly List<AbilityEntry> _abilities = new();

    /// <summary>Fires each time one of the player's abilities does its effect, with the item that granted it.</summary>
    public event Action<IInventoryItem> OnAbilityActivated;

    // Checked every frame, so a timed ability still ends on time after this component is turned off and on.
    // Time.time stops while the game is paused, so paused time doesn't count.
    private void Update()
    {
        for (int i = _abilities.Count - 1; i >= 0; i--)
        {
            AbilityEntry entry = _abilities[i];
            if (!entry.HasDuration || Time.time < entry.EndTime) continue;

            DestroyAbility(entry.Instance);
            _abilities.RemoveAt(i);
        }
    }

    /// <summary>
    /// Adds a permanent ability from the given item.
    /// </summary>
    public void Add(IInventoryItem item)
    {
        if (!item.AbilityPrefab) return;

        _abilities.Add(new AbilityEntry
        {
            Item = item,
            Instance = CreateAbility(item),
            HasDuration = false
        });
    }

    /// <summary>
    /// Adds a timed ability from the given item.
    /// The ability is automatically removed after the duration expires.
    /// </summary>
    public void Add(IInventoryItem item, float duration)
    {
        if (!item.AbilityPrefab || duration <= 0) return;

        _abilities.Add(new AbilityEntry
        {
            Item = item,
            Instance = CreateAbility(item),
            HasDuration = true,
            EndTime = Time.time + duration
        });
    }

    /// <summary>
    /// Removes one ability associated with the given item.
    /// Prioritizes permanent abilities, otherwise removes the oldest timed ability.
    /// </summary>
    public void Remove(IInventoryItem item)
    {
        int oldestTimedIndex = -1;

        for (int i = 0; i < _abilities.Count; i++)
        {
            if (_abilities[i].Item != item) continue;

            if (!_abilities[i].HasDuration)
            {
                RemoveAt(i);
                return;
            }

            if (oldestTimedIndex == -1)
            {
                oldestTimedIndex = i;
            }
        }

        if (oldestTimedIndex != -1)
        {
            RemoveAt(oldestTimedIndex);
        }
    }

    /// <summary>Creates the item's ability prefab, and passes on activations from every ItemAbility on it.</summary>
    private GameObject CreateAbility(IInventoryItem item)
    {
        GameObject instance = Instantiate(item.AbilityPrefab, transform);

        foreach (ItemAbility ability in instance.GetComponentsInChildren<ItemAbility>(true))
        {
            ability.SetSourceItem(item);
            ability.OnActivated += HandleAbilityActivated;
        }

        return instance;
    }

    private void RemoveAt(int index)
    {
        DestroyAbility(_abilities[index].Instance);
        _abilities.RemoveAt(index);
    }

    private void DestroyAbility(GameObject instance)
    {
        foreach (ItemAbility ability in instance.GetComponentsInChildren<ItemAbility>(true))
        {
            ability.OnActivated -= HandleAbilityActivated;
        }

        Destroy(instance);
    }

    private void HandleAbilityActivated(ItemAbility ability)
    {
        OnAbilityActivated?.Invoke(ability.SourceItem);
    }

    #region Debug

    /// <summary>
    /// Adds the selected test item's ability for the specified duration.
    /// </summary>
    [Button("Add Test Gun Part")]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    private void AddTestGunPart()
    {
        if (!_testGunPart) return;

        Add(_testGunPart, _testDuration);
    }
    
    /// <summary>
    /// Adds the selected test item's ability for the specified duration.
    /// </summary>
    [Button("Add Test Charm")]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    private void AddTestCharm()
    {
        if (!_testCharm) return;

        Add(_testCharm, _testDuration);
    }
    
    /// <summary>
    /// Adds the selected test item's ability for the specified duration.
    /// </summary>
    [Button("Add Test Consumable")]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    private void AddTestConsumable()
    {
        if (!_testConsumable) return;

        Add(_testConsumable, _testDuration);
    }

    /// <summary>
    /// Removes an ability associated with the selected test item.
    /// </summary>
    [Button("Remove Test Items")]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    private void RemoveTestItems()
    {
        if (_testGunPart)
            Remove(_testGunPart);
        else if (_testCharm)
            Remove(_testCharm);
        else if (_testConsumable)
            Remove(_testConsumable);
    }

    #endregion
}