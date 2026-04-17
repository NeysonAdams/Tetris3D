using Tetris.Core.Cells;
using Tetris.Core.Tetrominoes;
using UnityEngine;

namespace Tetris.Core.Configs
{
    [CreateAssetMenu(
        fileName = "Shape_",
        menuName = "Tetris/Core/Tetromino Spahe",
        order = 10)]
    public sealed class TetrominoShapeSo : ScriptableObject
    {
        [SerializeField]
        private Tetrominoes.TetrominiType _type;

        [SerializeField]
        private Vector3Int[] _cells = System.Array.Empty<Vector3Int>();

        public Tetrominoes.TetrominiType Type => _type;
        public Vector3Int[] Cells => _cells;
    }
}