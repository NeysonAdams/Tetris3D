using Tetris.Core.Cells;
using Tetris.Core.Tetrominoes;
using UnityEngine;

namespace Tetris.Core.Fields
{
    public interface IReadOnlyGameField
    {
        int SizeX { get; }
        int SizeY { get; }
        int SizeZ {  get; }

        Cell GetCell(int x, int y, int z);
        bool IsInside(Vector3Int positon);
        bool CanPlace(Piece piece);
    }
}
