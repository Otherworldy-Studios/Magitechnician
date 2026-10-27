using System;

namespace _Project.Scripts.Dungeon.Utility
{
    /// <summary>
    /// A deterministic random number generator
    /// </summary>
    public class LinearCongruentialGenerator
    {
        private long _seed;
        private int _multiplier;
        private int _modulus;
        private int _increment;

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

        public int Next()
        {
            _seed = Math.Abs((_multiplier * _seed + _increment)) % _modulus;
            return Math.Abs((int)_seed);
        }

        public int Next(int maxValue)
        {
            return Next() % maxValue;
        }

        public int Next(int minValue, int maxValue)
        {
            return Next() % (maxValue - minValue) + minValue;
        }
    }
}
