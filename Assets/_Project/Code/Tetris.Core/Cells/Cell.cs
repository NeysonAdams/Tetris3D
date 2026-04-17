using Tetris.Core.Tetrominoes;
using TextMateSharp.Internal.Rules;

namespace Tetris.Core.Cells
{
    public readonly struct Cell
    {
        public CellState State { get; }
        public TetrominiType Type { get; }

        public Cell (CellState state, TetrominiType type)
        {
            State = state; 
            Type = type;
        }

        public static Cell Locked (TetrominiType type) => new(CellState.Loked, type);
        public static Cell Empty => default;
    }
}

