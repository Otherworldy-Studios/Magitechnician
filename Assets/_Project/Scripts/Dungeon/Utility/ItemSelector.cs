using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Dungeon.Utility
{
    /// <summary>
    /// A weighted random selector
    /// </summary>
    /// <typeparam name="T">
    /// The type of item that will be selected
    /// </typeparam>
    class ItemSelector<T> where T : notnull
    {
        private readonly LinearCongruentialGenerator _random;
        private readonly Dictionary<T, long> _entries;
        private long _totalWeight;

        public long TotalWeight { get { return _totalWeight; } }

        public int Size { get { return _entries.Count; } }

        public bool Empty { get { return Size == 0; } }

        public ItemSelector()
        {
            _entries = new Dictionary<T, long>();
            _totalWeight = 0;
            _random = new LinearCongruentialGenerator(Random.Range(1, int.MaxValue));
        }
        
        public ItemSelector(long seed)
        {
            _entries = new Dictionary<T, long>();
            _totalWeight = 0;
            _random = new LinearCongruentialGenerator(seed);
        }

        /// <summary>
        /// Adds an item to the ItemSelector
        /// </summary>
        /// <param name="item">
        /// The item to add
        /// </param>
        /// <param name="weight">
        /// The weight of the item
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// If the weight is less than or equal to 0
        /// </exception>
        public void AddItem(T item, long weight)
        {
            if (_entries.ContainsKey(item))
            {
                RemoveItem(item);
            }

            if (weight <= 0)
            {
                throw new ArgumentException("Weight must be greater than 0");
            }

            _entries[item] = weight;
            _totalWeight += weight;
        }

        /// <summary>
        /// Removes an item from the ItemSelector
        /// </summary>
        /// <param name="item">
        /// The item to remove
        /// </param>
        public void RemoveItem(T item)
        {
            if (!_entries.TryGetValue(item, out _))
                return;

            _totalWeight -= _entries[item];
            _entries.Remove(item);
        }

        /// <summary>
        /// Finds the weight of an item.
        /// </summary>
        /// <param name="item">
        /// The item
        /// </param>
        /// <returns>
        /// The weight of the item or -1 if it is not
        /// found
        /// </returns>
        public long Weight(T item)
        {
            if (!Contains(item))
                return -1;

            return _entries[item];
        }

        /// <summary>
        /// Set the weight of an item
        /// </summary>
        /// <param name="item">
        /// The item
        /// </param>
        /// <param name="weight">
        /// The weight of the item
        /// </param>
        public void SetWeight(T item, long weight)
        {
            if (!Contains(item))
                return;
            
            RemoveItem(item);
            AddItem(item, weight);
        }

        /// <summary>
        /// Determines if an item is in the
        /// ItemSelector.
        /// </summary>
        /// <param name="item">
        /// The item
        /// </param>
        /// <returns>
        /// If the item is found
        /// </returns>
        public bool Contains(T item)
        {
            return _entries.ContainsKey(item);
        }

        /// <summary>
        /// Rolls the ItemSelector for an item
        /// </summary>
        /// <returns>
        /// The rolled item
        /// </returns>
        /// <exception cref="Exception">
        /// Occurs if the ItemSelector is empty
        /// </exception>
        public T Roll()
        {
            if(_totalWeight <= 0)
                return default(T);

            long value = _random.Next(_totalWeight);
            //Debug.Log($"Rolling value: {value}");

            foreach (var item in _entries.Keys)
            {
                long weight = _entries[item];
                value -= weight;

                if (value <= 0)
                {
                    return item;
                }
            }

            throw new Exception("Impossible state");
        }

        /// <summary>
        /// Rolls the ItemSelector and removes the rolled item
        /// </summary>
        /// <returns>
        /// The rolled item
        /// </returns>
        public T RollWithRemoval()
        {
            T item = Roll();
            RemoveItem(item);
            //Debug.Log($"Total Weight after removal: {_totalWeight}");
            return item;
        }

        /// <summary>
        /// The probability of receiving an item from a roll
        /// </summary>
        /// <param name="item">
        /// The item
        /// </param>
        /// <returns>
        /// The probability of receiving the item
        /// </returns>
        public float Chance(T item)
        {
            if (!Contains(item))
                return 0;

            return (float)_entries[item] / _totalWeight;
        }        
    }
}