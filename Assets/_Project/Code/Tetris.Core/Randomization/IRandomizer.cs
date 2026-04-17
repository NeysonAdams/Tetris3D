using Tetris.Core.Tetrominoes;

namespace Tetris.Core.Randomization
{
    public interface IRandomizer
    {
        TetrominoType Next();
        void Reset();
    }
}