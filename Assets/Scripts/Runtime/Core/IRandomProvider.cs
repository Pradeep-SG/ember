namespace Kindrith.Core
{
    public interface IRandomProvider
    {
        int NextInt(int minInclusive, int maxExclusive);
        float NextFloat();
        double NextDouble();
    }
}
