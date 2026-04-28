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
        [SerializeField] private Tetrominoes.TetrominoType _type;
        [SerializeField] private Vector3Int[] _cells = System.Array.Empty<Vector3Int>();
        [SerializeField] private int _pivotCellIndex = 0;
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private string _displayName = string.Empty;

        public Tetrominoes.TetrominoType Type => _type;
        public Vector3Int[] Cells => _cells;
        public int PivotCellIndex => _pivotCellIndex;

        public Vector3Int PivotCell => _cells.Length > 0 && _pivotCellIndex < _cells.Length
            ? _cells[_pivotCellIndex]
            : Vector3Int.zero;

        public Color DefaultColor => _defaultColor;
        public string DisplayName => _displayName;

        private void OnValidate()
        {
            if (_cells.Length > 0)
                _pivotCellIndex = Mathf.Clamp(_pivotCellIndex, 0, _cells.Length - 1);
        }
    }
}
