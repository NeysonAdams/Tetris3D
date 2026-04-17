using UnityEngine;

namespace Tetris.Core.Tetrominoes
{
    public static class RoatationMath
    {
        public static Vector3Int ApplyRotation(Vector3Int cell, Rotation3D rotation)
        {
            var result = cell;

            for (var i = 0; i < rotation.RxSteps; i++)
            {
                result = RotateX90(result);
            }

            for (var i = 0; i < rotation.RySteps; i++)
            {
                result = RotateY90(result);
            }

            for (var i = 0; i < rotation.RzSteps; i++)
            {
                result = RotateZ90(result);
            }

            return result;
        }

        public static Vector3Int RotateX90(Vector3Int v) => new(v.x, -v.z, v.y);
        public static Vector3Int RotateY90(Vector3Int v) => new(v.z, v.y, -v.x);
        public static Vector3Int RotateZ90(Vector3Int v) => new(-v.y, v.x,v.z);
    }
}
