namespace _Project.Scripts.LevelGenerator.Utility
{
    public class LinearCongruentialGenerator
    {
        private int _seed;
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

        public LinearCongruentialGenerator(int seed)
        {
            _seed = seed;
            _multiplier = 1103515245;
            _modulus = 2 << 30;
            _increment = 12345;
        }

        public int Next()
        {
            _seed = (_multiplier * _seed + _increment) % _modulus;
            return _seed;
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
