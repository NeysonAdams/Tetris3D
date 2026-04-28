using Tetris.Core.Fields;
using Tetris.Core.Tetrominoes;

namespace Tetris.Core.Spawning
{
    public interface ISpawner
    {
        Piece? TrySpawn(IReadOnlyGameField field, TetrominoType type);
    }
}
