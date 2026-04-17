using System;
using System.Collections.Generic;
using Tetris.Core.Configs;
using Tetris.Core.Tetrominoes;

namespace Tetris.Core.Randomization
{
    public sealed class TetrominoCatalog : ITetrominoCatalog
    {
        private readonly Dictionary<TetrominoType, TetrominoShapeSo> _shapeByType;

        /// <summary>
        /// Build catalog by Forms
        /// </summary>
        /// <param name="set"> Set of Forms Non-null data with unique </param>
        /// <exception cref="ArgumentNullException">set is null.</exception>
        /// <exception cref="ArgumentException">
        /// if st has null element
        /// </exception>
        public TetrominoCatalog (TetrominoSetSO set)
        {
            if (set == null)
            {
                throw new ArgumentNullException (nameof (set));
            }

            var shapes = set.Shapes;
            _shapeByType = new Dictionary<TetrominoType, TetrominoShapeSo> (shapes.Length);

            for (var i = 0; i < shapes.Length; i++)
            {
                var shape = shapes [i];
                if(shape == null)
                {
                    throw new ArgumentException(
                        $"TetrominoSet contains a null shape at index {i}",
                        nameof(set)
                        );
                }

                if (_shapeByType.ContainsKey(shape.Type))
                {
                    throw new ArgumentException (
                         $"TetrominoSet contains duplicate shapes for type '{shape.Type}'.",
                        nameof(set));
                }

                _shapeByType[shape.Type] = shape;
            }

        }

        public TetrominoShapeSo GetShape (TetrominoType type)
        {
            return _shapeByType[type];
        }
    }
}
