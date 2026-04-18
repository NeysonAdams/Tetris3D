using System.Collections.Generic;
using System;
using Tetris.Core.Cells;
using Tetris.Core.Configs;
using Tetris.Core.Tetrominoes;
using UnityEngine;

namespace Tetris.Core.Fields
{
    public sealed class GameField : IReadOnlyGameField
    {
        private readonly Cell[,,] _cells;

        public int SizeX { get; }
        public int SizeY { get; }
        public int SizeZ { get; }

        public GameField (GameFieldSettingsSO settings)
        {
            if (settings == null)
                throw new ArgumentNullException (nameof (settings));
            if (settings.SizeX < 1 || settings.SizeY < 1 || settings.SizeZ < 1)
            {
                throw new ArgumentException(
                    $"GameField dimensions must be >= 1, got ({settings.SizeX}, {settings.SizeY}, {settings.SizeZ}).",
                    nameof(settings));
            }

            SizeX = settings.SizeX;
            SizeY = settings.SizeY;
            SizeZ = settings.SizeZ;

            _cells = new Cell[SizeZ, SizeX, SizeY];
        }

        public Cell GetCell(int x, int y, int z) 
        {
            if (x < 0 || x >= SizeX || y < 0 || y >= SizeY || z < 0 || z >= SizeZ)
            {
                throw new IndexOutOfRangeException(
                    $"Cell position ({x}, {y}, {z}) is outside field bounds ({SizeX}, {SizeY}, {SizeZ}).");
            }
            return _cells[x, y, z];
        }

        public bool IsInside(Vector3Int position) 
        {
            return position.x >= 0 && position.x <= SizeX 
                && position.y >= 0 && position.y <= SizeY
                && position.z >= 0 && position.z <= SizeZ;
        }
        public bool CanPlace(Piece piece)
        {
            if(piece.Shape == null)
                throw new ArgumentNullException(nameof(piece));

            var cells = piece.Shape.Cells;
            for (var i = 0; i<cells.Length;i++)
            {
                var rotated = RoatationMath.ApplyRotation(cells[i], piece.Rotation);
                var worldPos = piece.Position + rotated;

                if (worldPos.y >= SizeY) continue;

                if (!IsInside(worldPos)) return false;

                if (_cells[worldPos.x, worldPos.y, worldPos.z].State != CellState.Empty)
                    return false;
            }

            return true;
        }

        public void WriteCells(Piece piece, TetrominoType type)
        {
            if (piece.Shape == null)
                throw new ArgumentNullException(nameof(piece));

            var cells = piece.Shape.Cells;
            for (var i = 0; i < cells.Length; i++)
            {
                var rotated = RoatationMath.ApplyRotation(cells[i], piece.Rotation);
                var worldPos = piece.Position + rotated;

                if (worldPos.y >= SizeY) continue;

                if (!IsInside(worldPos))
                {
                    throw new InvalidOperationException(
                        $"Cannot write cell at ({worldPos.x}, {worldPos.y}, {worldPos.z}) — position outside field. " +
                        "Caller must verify CanPlace before WriteCells.");
                }

                _cells[worldPos.x, worldPos.y, worldPos.z] = Cell.Locked(type);
            }
        }
        public int[] GetFullLayers()
        {
            var result = new List<int>();
            for(var y = 0; y< SizeY; y++)
            {
                if (IsLayerFull(y))
                    result.Add(y);
            }

            return result.ToArray();
        }

        private bool IsLayerFull(int y)
        {
            for (var x = 0; x < SizeX; x++)
            {
                for (int z = 0; z < SizeZ; z++)
                {
                    if (_cells[x, y, z].State != CellState.Loked)
                        return false;
                }
            }

            return true;
        }
        public void RemoveLayers(int[] ys)
        {
            if (ys == null)
                throw new ArgumentNullException(nameof(ys));

            if (ys.Length == 0) return;

            var removed = new HashSet<int>(ys);

            var writeY = 0;
            var readY = 0;

            while (writeY < SizeY)
            {
                while (readY < SizeY && removed.Contains(readY))
                {
                    readY++;
                }

                if(readY < SizeY)
                {
                    if(readY != writeY)
                    {
                        CopyLayer(readY, writeY);
                    }
                    readY++;
                }
                else
                {
                    ClearLayer(writeY);
                }
            }
        }

        private void CopyLayer(int srcY, int dstY)
        {
            for (var x = 0; x < SizeX; x++)
                for (var z = 0; z < SizeZ; z++)
                    _cells[x, dstY, z] = _cells[x, srcY, z];
        }

        private void ClearLayer(int y)
        {
            for (var x = 0; x < SizeX; x++)
                for (var z = 0; z < SizeZ; z++)
                    _cells[x, y, z] = default;
        }

        public void Clear() => Array.Clear(_cells, 0, _cells.Length);
    }
}