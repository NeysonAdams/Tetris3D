using UnityEngine;
using Tetris.Core.Configs;
using Tetris.Core.Fields;
using Tetris.Core.Randomization;
using Tetris.Core.Tetrominoes;
using System;

namespace Tetris.Core.Spawning
{
    public sealed class Spawner : ISpawner
    {
        public readonly ITetrominoCatalog _catalog;
        private readonly SpawnSettingsSO _settings;

        public Spawner (ITetrominoCatalog catalog, SpawnSettingsSO settings)
        {
            if (catalog == null)
                throw new ArgumentNullException (nameof (catalog));
            if (settings == null)
                throw new ArgumentNullException (nameof (settings));

            _catalog = catalog;
            _settings = settings;
        }

        public Piece? TrySpawn(IReadOnlyGameField field, TetrominoType type)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            var shape = _catalog.GetShape(type);
            var bboxCenter = CalculateBoundingBoxCenter(shape, _settings.InitialRotation);

            var basePos = new Vector3Int(field.SizeX / 2, field.SizeY, field.SizeZ / 2);
            var spawnPos = basePos - bboxCenter + _settings.SpawnOfset;

            var piece = new Piece(shape, spawnPos, _settings.InitialRotation);
            return field.CanPlace(piece) ? piece : null;
        }

        private static Vector3Int CalculateBoundingBoxCenter(
            TetrominoShapeSo shape, Rotation3D rotation)
        {
            var cells = shape.Cells;
            var pivot = shape.PivotCell;

            if (cells.Length == 0)
            {
                throw new ArgumentException(
                    "Cannot calculate bounding box for a shape with no cells.",
                    nameof(shape));
            }

            var first = RoatationMath.ApplyRotation(cells[0], rotation, pivot) - pivot;
            var min = first;
            var max = first;

            for (var i = 1; i < cells.Length; i++)
            {
                var rotated = RoatationMath.ApplyRotation(cells[i], rotation, pivot) - pivot;
                min = Vector3Int.Min(min, rotated);
                max = Vector3Int.Max(max, rotated);
            }

            return new Vector3Int(
                (min.x + max.x) / 2,
                (min.y + max.y) / 2,
                (min.z + max.z) / 2);
        }
    }
}


