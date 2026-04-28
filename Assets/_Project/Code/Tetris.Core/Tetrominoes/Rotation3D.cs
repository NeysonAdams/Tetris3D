using UnityEngine;

namespace Tetris.Core.Tetrominoes
{
    /// <summary>
    /// Represents a 3D rotation using Quaternion internally.
    /// Supports discrete 90° rotations around world axes.
    /// </summary>
    public readonly struct Rotation3D
    {
        private readonly Quaternion _quaternion;

        public Quaternion Quaternion => _quaternion;

        private Rotation3D(Quaternion quaternion)
        {
            _quaternion = quaternion;
        }

        public static Rotation3D Identity => new(Quaternion.identity);

        /// <summary>
        /// Creates a rotation from individual axis step counts.
        /// Steps are applied in order: X, then Y, then Z (for initial spawn rotation).
        /// </summary>
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

        /// <summary>
        /// Applies a 90° rotation step around the specified WORLD axis.
        /// </summary>
        public Rotation3D WithStep(RotationAxis axis, int direction)
        {
            // direction: +1 = 90° clockwise, -1 = 90° counter-clockwise
            var angle = direction * 90f;

            var stepRotation = axis switch
            {
                RotationAxis.X => Quaternion.Euler(angle, 0f, 0f),
                RotationAxis.Y => Quaternion.Euler(0f, angle, 0f),
                RotationAxis.Z => Quaternion.Euler(0f, 0f, angle),
                _ => Quaternion.identity
            };

            // Multiply: new rotation * current rotation
            // This applies the new rotation in WORLD space
            var newRotation = stepRotation * _quaternion;

            return new Rotation3D(newRotation);
        }

        /// <summary>
        /// Transforms a cell position by this rotation (rotates around origin).
        /// </summary>
        public Vector3Int ApplyToCell(Vector3Int cell)
        {
            var rotated = _quaternion * new Vector3(cell.x, cell.y, cell.z);
            return new Vector3Int(
                Mathf.RoundToInt(rotated.x),
                Mathf.RoundToInt(rotated.y),
                Mathf.RoundToInt(rotated.z)
            );
        }

        /// <summary>
        /// Transforms a cell position by this rotation, rotating around a pivot point.
        /// </summary>
        public Vector3Int ApplyToCellAroundPivot(Vector3Int cell, Vector3Int pivot)
        {
            // Translate to pivot-relative coordinates
            var relative = cell - pivot;

            // Apply rotation
            var rotated = _quaternion * new Vector3(relative.x, relative.y, relative.z);

            // Translate back and round
            return new Vector3Int(
                Mathf.RoundToInt(rotated.x) + pivot.x,
                Mathf.RoundToInt(rotated.y) + pivot.y,
                Mathf.RoundToInt(rotated.z) + pivot.z
            );
        }
    }
}
