using UnityEngine;
using Tetris.Core.Configs;

namespace Tetris.Core.Tetrominoes
{
    public readonly struct Piece
    {
        public TetrominoShapeSo Shape { get; }
        public Vector3Int Position { get; }
        public Rotation3D Rotation { get; }

        public Piece (TetrominoShapeSo shape, Vector3Int position, Rotation3D roation)
        {
            Shape = shape; 
            Position = position; 
            Rotation = roation;
        }

        public Piece WithPosition (Vector3Int position) => new (Shape, position, Rotation);
        public Piece WithRotation (Rotation3D rotation) => new (Shape, Position, rotation);
        public Piece MoveBy(Vector3Int delta) => new(Shape, Position + delta, Rotation);

        public Piece Rotate (RotationAxis axis, int direction) =>
            new (Shape, Position, Rotation.WithStep (axis, direction));
    }
}
