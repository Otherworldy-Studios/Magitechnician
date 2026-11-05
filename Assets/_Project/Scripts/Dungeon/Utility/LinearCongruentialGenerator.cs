using System;
using UnityEngine;

namespace _Project.Scripts.Dungeon.Utility
{
    /// <summary>
    /// A deterministic random number generator
    /// </summary>
    public class LinearCongruentialGenerator
    {
        private long _seed;
        private long _multiplier;
        private long _modulus;
        private long _increment;

        public LinearCongruentialGenerator()
        {
            _seed = 0;
            _multiplier = 1103515245;
            _modulus = 2 << 30;
            _increment = 12345;
        }

        public LinearCongruentialGenerator(long seed)
        {
            _seed = seed;
            _multiplier = 1103515245;
            _modulus = 2 << 30;
            _increment = 12345;
        }

        public long Next()
        {
            _seed = Math.Abs((_multiplier * _seed + _increment)) % _modulus;
            return _seed;
        }

        public long Next(long maxValue)
        {
            long val = Next() % maxValue;
            //Debug.Log($"Max. Val: {maxValue} - Next: {val}");
            return val;
        }

        public long Next(long minValue, long maxValue)
        {
            long val = Next() % (maxValue - minValue) + minValue;
            //Debug.Log($"Max. Val: {maxValue} - Min. Val: {minValue} - Next: {val}");
            return val;
        }
    }
}
