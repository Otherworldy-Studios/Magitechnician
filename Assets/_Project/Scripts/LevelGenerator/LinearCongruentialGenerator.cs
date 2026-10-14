public class LinearCongruentialGenerator
{
    private double _seed;
    private double _multiplier;
    private double _modulus;
    private double _increment;

    public LinearCongruentialGenerator()
    {
        _seed = 0;
        _multiplier = 1103515245;
        _modulus = 1u << 31;
        _increment = 12345;
    }

    public LinearCongruentialGenerator(int seed)
    {
        _seed = seed;
        _multiplier = 1103515245;
        _modulus = 1u << 31;
        _increment = 12345;
    }

    public int Next()
    {
        _seed = (_multiplier * _seed + _increment) % _modulus;
        return (int)_seed;
    }

    public int Next(int maxValue)
    {
        return Next() % maxValue;
    }

    public int Next(int minValue, int maxValue)
    {
        return Next() % (maxValue - minValue) + minValue;
    }

    public double NextDouble()
    {
        _seed = (_multiplier * _seed + _increment) % _modulus;
        return _seed;
    }

    public double NextDouble(int maxValue)
    {
        return NextDouble() % maxValue;
    }

    public double NextDouble(int minValue, int maxValue)
    {
        return NextDouble() % (maxValue - minValue) + minValue;
    }
}
