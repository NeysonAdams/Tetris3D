using System;
using Tetris.Core.Tetrominoes;
using UnityEngine;

namespace Tetris.Core.Configs
{
    [CreateAssetMenu(
        fileName = "BlockVisualConfig",
        menuName = "Tetris/Core/Block Visual Config",
        order = 30
        )]
    public sealed class BlockVisualConfigSO: ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField]
            private TetrominoType _type;
            [SerializeField]
            private Material? _material;

            public TetrominoType Type => _type;
            public Material? Material => _material;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();
        
        public Entry[] Entrys => _entries;

        public bool TryGetMaterial(TetrominoType type, out Material material)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Type == type && _entries[i].Material != null)
                {
                    material = _entries[i].Material;    
                    return true;
                }
            }

            material = null;
            return false; 
        }
    }
}
