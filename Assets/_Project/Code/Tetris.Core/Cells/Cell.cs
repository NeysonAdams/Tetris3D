using Tetris.Core.Tetrominoes;

namespace Tetris.Core.Cells
{
    public readonly struct Cell
    {
        public CellState State { get; }
        public TetrominoType Type { get; }

        public Cell (CellState state, TetrominoType type)
        {
            State = state; 
            Type = type;
        }

        public static Cell Locked (TetrominoType type) => new(CellState.Loked, type);
        public static Cell Empty => default;
    }
}

