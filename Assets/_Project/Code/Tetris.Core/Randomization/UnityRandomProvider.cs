
namespace Tetris.Core.Randomization
{
    public sealed class UnityRandomProvider : IRandomProvider
    {
        public int Range (int minInclusive, int maxInclusive)
        {
            return UnityEngine.Random.Range (minInclusive, maxInclusive);
        }
    }
}
