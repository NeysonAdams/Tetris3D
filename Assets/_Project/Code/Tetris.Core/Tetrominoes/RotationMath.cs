using UnityEngine;

namespace Tetris.Core.Tetrominoes
{
    public static class RoatationMath
    {
        /// <summary>
        /// Applies rotation to a cell position around a pivot point.
        /// </summary>
        public static Vector3Int ApplyRotation(Vector3Int cell, Rotation3D rotation, Vector3Int pivot)
        {
            return rotation.ApplyToCellAroundPivot(cell, pivot);
        }

        /// <summary>
        /// Applies rotation to a cell position (rotates around origin).
        /// </summary>
        public static Vector3Int ApplyRotation(Vector3Int cell, Rotation3D rotation)
        {
            return rotation.ApplyToCell(cell);
        }
    }
}
