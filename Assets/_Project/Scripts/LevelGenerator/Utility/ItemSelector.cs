using System;
using System.Collections.Generic;

namespace _Project.Scripts.LevelGenerator.Utility
{
    class ItemSelector<T> where T : notnull
    {
        private readonly LinearCongruentialGenerator random;
        private readonly Dictionary<T, int> entries;
        private int totalWeight;

        public int TotalWeight { get { return totalWeight; } }

        public int Size { get { return entries.Count; } }

        public bool Empty { get { return Size == 0; } }

        public ItemSelector()
        {
            entries = new Dictionary<T, int>();
            totalWeight = 0;
            random = new LinearCongruentialGenerator();
        }
        
        public ItemSelector(int seed)
        {
            entries = new Dictionary<T, int>();
            totalWeight = 0;
            random = new LinearCongruentialGenerator(seed);
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
        /// <exception cref="ArgumentException">
        /// If the weight is less than or equal to 0
        /// </exception>
        public void AddItem(T item, int weight)
        {
            if (entries.ContainsKey(item))
            {
                RemoveItem(item);
            }

            if (weight <= 0)
            {
                throw new ArgumentException("Weight must be greater than 0");
            }

            entries[item] = weight;
            totalWeight += weight;
        }

        /// <summary>
        /// Removes an item from the ItemSelector
        /// </summary>
        /// <param name="item">
        /// The item to remove
        /// </param>
        public void RemoveItem(T item)
        {
            if (!entries.TryGetValue(item, out _))
                return;

            totalWeight -= entries[item];
            entries.Remove(item);
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

            return entries[item];
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
        public void SetWeight(T item, int weight)
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
            return entries.ContainsKey(item);
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
            long value = random.Next(totalWeight);

            foreach (var item in entries.Keys)
            {
                long weight = entries[item];
                value -= weight;

                if (value < 0)
                {
                    return item;
                }
            }

            throw new Exception("Impossible state");
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

            return (float)entries[item] / totalWeight;
        }        
    }
}