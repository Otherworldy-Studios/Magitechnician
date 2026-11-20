using System;
using UnityEngine;

namespace _Project.Scripts.Dungeon.Utility
{
    /// <summary>
    /// A random number generator that gives the same numbers for the same seed.
    /// Each number is (multiplier × previous + increment) mod modulus.
    /// </summary>
    public class LinearCongruentialGenerator
    {
        private long _seed;       // The last number made. The next one is worked out from it.
        private long _multiplier; // Constants from the C standard library's rand.
        private long _modulus;    // 2^31. Numbers are always below this.
        private long _increment;

        /// <summary>Creates a generator with seed 0.</summary>
        public LinearCongruentialGenerator()
        {
            _seed = 0;
            _multiplier = 1103515245;
            _modulus = 2 << 30;
            _increment = 12345;
        }

        /// <summary>Creates a generator with the given seed.</summary>
        public LinearCongruentialGenerator(long seed)
        {
            _seed = seed;
            _multiplier = 1103515245;
            _modulus = 2 << 30;
            _increment = 12345;
        }

        /// <summary>Returns the next number, from 0 up to 2^31.</summary>
        public long Next()
        {
            _seed = Math.Abs((_multiplier * _seed + _increment)) % _modulus;
            return _seed;
        }

        /// <summary>Returns the next number, from 0 up to but not including maxValue.</summary>
        public long Next(long maxValue)
        {
            long val = Next() % maxValue;
            //Debug.Log($"Max. Val: {maxValue} - Next: {val}");
            return val;
        }

        /// <summary>Returns the next number, from minValue up to but not including maxValue.</summary>
        public long Next(long minValue, long maxValue)
        {
            long val = Next() % (maxValue - minValue) + minValue;
            //Debug.Log($"Max. Val: {maxValue} - Min. Val: {minValue} - Next: {val}");
            return val;
        }
    }
}
