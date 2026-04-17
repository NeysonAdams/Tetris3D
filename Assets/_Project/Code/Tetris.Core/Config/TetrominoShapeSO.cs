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
        private Tetrominoes.TetrominoType _type;

        [SerializeField]
        private Vector3Int[] _cells = System.Array.Empty<Vector3Int>();
        [SerializeField]
        private Color _defaultColor = Color.white;
        [SerializeField]
        private string _displayName = string.Empty;

        public Tetrominoes.TetrominoType Type => _type;
        public Vector3Int[] Cells => _cells;
        public Color DefaultColor => _defaultColor;
        public string DisplayName => _displayName;
    }
}