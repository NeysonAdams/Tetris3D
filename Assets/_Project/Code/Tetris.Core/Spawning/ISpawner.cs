using Tetris.Core.Fields;
using Tetris.Core.Tetrominoes;

namespace Tetris.Core.Spawning
{

    public interface ISpawner
    {
        /// <summary>
        /// Try to create figure with seted type in the start position
        /// </summary>
        /// <param name="field">Firls  of Spawning</param>
        /// <param name="type">Type of Tetromino</param>
        /// <returns></returns>
        Piece? TrySpawn(IReadOnlyGameField field, TetrominoType type);
    }
}
