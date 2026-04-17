using System;
using Tetris.Core.Configs;
using Tetris.Core.Tetrominoes;

namespace Tetris.Core.Randomization
{
    public sealed class BagRandomizer : IRandomizer
    {
        private readonly TetrominoType[] _bag;
        private readonly IRandomProvider _rng;
        private int _nextIndex;

        public BagRandomizer(TetrominoSetSO set,  IRandomProvider rng)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));

            if(rng == null) 
                throw new ArgumentNullException(nameof(rng));

            var shapes = set.Shapes;
            if(shapes.Length == 0)
            {
                throw new ArgumentException(
                    "TetrominoSet must contains at least one shape",
                    nameof(set));
            }

            _bag = new TetrominoType[shapes.Length];
            for (var i = 0; i < shapes.Length; i++)
            {
                _bag[i] = shapes[i].Type;
            }

            _rng = rng;
            Reset();
        }

        public TetrominoType Next()
        {
            if(_nextIndex >= _bag.Length)
            {
                Shuffle();
                _nextIndex = 0;
            }

            var result = _bag[_nextIndex];
            _nextIndex++;
            return result;
        }

        public void Reset()
        {
            Shuffle();
            _nextIndex = 0;
        }

        private void Shuffle()
        {
            for (var i = _bag.Length - 1; i >= 1; i--)
            {
                var j = _rng.Range(0, i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
        }
    }
}
