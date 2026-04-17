namespace Tetris.Core.Randomization
{
    public interface IRandomProvider
    {
        int Range(int minInclusive, int maxExclusive);
    }
}