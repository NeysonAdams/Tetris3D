using System;

namespace Tetris.Core.Randomization
{
    public sealed class SystemRandomProvider :IRandomProvider
    {
        private readonly Random _random;

        public SystemRandomProvider(int seed)
        {
            _random = new Random(seed);
        }

        public int Range (int minInclusive, int maxInclusive)
        {
            return _random.Next(minInclusive, maxInclusive);
        }
    }
}
