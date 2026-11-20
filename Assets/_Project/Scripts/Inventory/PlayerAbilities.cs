using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Manages abilities granted by inventory items, including permanent and timed abilities.
/// </summary>
public class PlayerAbilities : MonoBehaviour
{
    private struct AbilityEntry
    {
        public IInventoryItem _item;
        public GameObject _instance;
        public bool _hasDuration;
    }

    private List<AbilityEntry> _abilities = new List<AbilityEntry>();

    [Title("Ability Testing")]
    [SerializeField] private GunPart _testItem;
    [SerializeField] private float _testDuration = 5f;

    /// <summary>
    /// Adds a permanent ability from the given item.
    /// </summary>
    public void Add(IInventoryItem item)
    {
        if (!item.AbilityPrefab)
            return;

        _abilities.Add(new AbilityEntry()
        {
            _item = item,
            _instance = Instantiate(item.AbilityPrefab, transform),
            _hasDuration = false
        });
    }

    /// <summary>
    /// Adds a timed ability from the given item.
    /// The ability is automatically removed after the duration expires.
    /// </summary>
    public void Add(IInventoryItem item, float duration)
    {
        if (!item.AbilityPrefab || duration <= 0)
            return;

        GameObject instance = Instantiate(item.AbilityPrefab, transform);
        _abilities.Add(new AbilityEntry()
        {
            _item = item,
            _instance = instance,
            _hasDuration = true
        });

        StartCoroutine(HandleTimedAbility(duration, instance));
    }

    /// <summary>
    /// Removes one ability associated with the given item.
    /// Prioritizes permanent abilities, otherwise removes the oldest timed ability.
    /// </summary>
    public void Remove(IInventoryItem item)
    {
        int oldestTimedItemIndex = -1;

        for (int i = 0; i < _abilities.Count; i++)
        {
            if (_abilities[i]._item != item)
                continue;

            if (!_abilities[i]._hasDuration)
            {
                Destroy(_abilities[i]._instance);
                _abilities.RemoveAt(i);
                return;
            }

            if (oldestTimedItemIndex == -1)
            {
                oldestTimedItemIndex = i;
            }
        }

        if (oldestTimedItemIndex != -1)
        {
            Destroy(_abilities[oldestTimedItemIndex]._instance);
            _abilities.RemoveAt(oldestTimedItemIndex);
        }
    }

    /// <summary>
    /// Waits for the specified duration before removing the exact ability instance.
    /// </summary>
    private IEnumerator HandleTimedAbility(float duration, GameObject instance)
    {
        yield return new WaitForSeconds(duration);

        for (int i = 0; i < _abilities.Count; i++)
        {
            if (_abilities[i]._instance == instance)
            {
                Destroy(_abilities[i]._instance);
                _abilities.RemoveAt(i);
                yield break;
            }
        }
    }

    /// <summary>
    /// Adds the selected test item's ability for the specified duration.
    /// </summary>
    [Button("Add Test Item")]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    private void AddTestItem()
    {
        if (!_testItem)
            return;

        Add(_testItem, _testDuration);
    }

    /// <summary>
    /// Removes an ability associated with the selected test item.
    /// </summary>
    [Button("Remove Test Item")]
    [EnableIf("@UnityEngine.Application.isPlaying")]
    private void RemoveTestItem()
    {
        if (!_testItem)
            return;

        Remove(_testItem);
    }
}