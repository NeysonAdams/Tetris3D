using System;
using Tetris.Core.Fields;
using UnityEngine;
namespace Tetris.Core.Tetrominoes
{
    public static class GhostCalculator
    {
        public static Piece Calculate(Piece piece, IReadOnlyGameField field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            var current = piece;
            while (true)
            {
                var candidate = current.MoveBy(Vector3Int.down);
                if (!field.CanPlace(candidate))
                {
                    return current;
                }

                current = candidate;
            }
        }
    }
}
