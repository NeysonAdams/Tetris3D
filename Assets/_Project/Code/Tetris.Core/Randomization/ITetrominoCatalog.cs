using Tetris.Core.Configs;
using Tetris.Core.Tetrominoes;

namespace Tetris.Core.Randomization
{
    public interface ITetrominoCatalog
    {
        TetrominoShapeSo GetShape(TetrominoType type);
    }
}
