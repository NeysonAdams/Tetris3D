using Tetris.Presentation.Configs;
using UnityEngine;

namespace Tetris.Presentation
{
    public static class GridToWorldExtensions
    {
        public static Vector3 GridToWorld (this Vector3Int grid, FieldLayoutSettingsSO layout)
        {
            if(layout == null) throw new System.ArgumentNullException (nameof (layout));

            return layout.WorldOrigin + (Vector3)grid * layout.BlockSize;

        }

        public static Vector3Int WorldToGrid(this Vector3 world, FieldLayoutSettingsSO layout)
        {
            if (layout == null) throw new System.ArgumentNullException(nameof(layout));

            var local = (world - layout.WorldOrigin) / layout.BlockSize;
            return new Vector3Int(
                Mathf.RoundToInt(local.x),
                Mathf.RoundToInt(local.y),
                Mathf.RoundToInt(local.z));
        }
    }
}
