using UnityEngine;

namespace Tetris.Core.Tetrominoes
{
    public static class RoatationMath
    {
        public static Vector3Int ApplyRotation(Vector3Int cell, Rotation3D rotation, Vector3Int pivot)
        {
            return rotation.ApplyToCellAroundPivot(cell, pivot);
        }

        public static Vector3Int ApplyRotation(Vector3Int cell, Rotation3D rotation)
        {
            return rotation.ApplyToCell(cell);
        }
    }
}
