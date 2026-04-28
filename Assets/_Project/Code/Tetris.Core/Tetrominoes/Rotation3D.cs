using UnityEngine;

namespace Tetris.Core.Tetrominoes
{
    public readonly struct Rotation3D
    {
        private readonly Quaternion _quaternion;

        public Quaternion Quaternion => _quaternion;

        private Rotation3D(Quaternion quaternion)
        {
            _quaternion = quaternion;
        }

        public static Rotation3D Identity => new(Quaternion.identity);

        public static Rotation3D FromSteps(int rxSteps, int rySteps, int rzSteps)
        {
            var rotation = Identity;

            for (var i = 0; i < (rxSteps % 4 + 4) % 4; i++)
                rotation = rotation.WithStep(RotationAxis.X, 1);

            for (var i = 0; i < (rySteps % 4 + 4) % 4; i++)
                rotation = rotation.WithStep(RotationAxis.Y, 1);

            for (var i = 0; i < (rzSteps % 4 + 4) % 4; i++)
                rotation = rotation.WithStep(RotationAxis.Z, 1);

            return rotation;
        }

        public Rotation3D WithStep(RotationAxis axis, int direction)
        {
            var angle = direction * 90f;

            var stepRotation = axis switch
            {
                RotationAxis.X => Quaternion.Euler(angle, 0f, 0f),
                RotationAxis.Y => Quaternion.Euler(0f, angle, 0f),
                RotationAxis.Z => Quaternion.Euler(0f, 0f, angle),
                _ => Quaternion.identity
            };

            var newRotation = stepRotation * _quaternion;
            return new Rotation3D(newRotation);
        }

        public Vector3Int ApplyToCell(Vector3Int cell)
        {
            var rotated = _quaternion * new Vector3(cell.x, cell.y, cell.z);
            return new Vector3Int(
                Mathf.RoundToInt(rotated.x),
                Mathf.RoundToInt(rotated.y),
                Mathf.RoundToInt(rotated.z)
            );
        }

        public Vector3Int ApplyToCellAroundPivot(Vector3Int cell, Vector3Int pivot)
        {
            var relative = cell - pivot;
            var rotated = _quaternion * new Vector3(relative.x, relative.y, relative.z);

            return new Vector3Int(
                Mathf.RoundToInt(rotated.x) + pivot.x,
                Mathf.RoundToInt(rotated.y) + pivot.y,
                Mathf.RoundToInt(rotated.z) + pivot.z
            );
        }
    }
}
