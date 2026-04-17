using System;
using UnityEngine;

namespace Tetris.Core.Configs
{
    [CreateAssetMenu(
        fileName = "TetrominoSet",
        menuName = "Tetris/Core/Tetromino Set",
        order = 11
        )]
    public sealed class TetrominoSetSO:ScriptableObject
    {
        [SerializeField]
        private TetrominoShapeSo[] _shapes = Array.Empty<TetrominoShapeSo>();

        public TetrominoShapeSo[] Shapes => _shapes;
    }
}
