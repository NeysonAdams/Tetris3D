using UnityEngine;

namespace Tetris.Core.Configs
{
    [CreateAssetMenu(
        fileName ="GameFieldSettings",
        menuName = "Tetris/Core/Game Field Settings",
        order=0
        )]
    public sealed class GameFieldSettingsSO: ScriptableObject
    {
        private const int MinDemension = 1;

        [SerializeField]
        private int _sizeX = 5;
        [SerializeField] 
        private int _sizeY = 12;
        [SerializeField] 
        private int _sizeZ = 5;

        public int SizeX => _sizeX;
        public int SizeY => _sizeY;
        public int SizeZ => _sizeZ;

        private void OnValidate()
        {
            _sizeX = Mathf.Max(MinDemension, _sizeX);
            _sizeY = Mathf.Max(MinDemension, _sizeY);
            _sizeZ = Mathf.Max(MinDemension, _sizeZ);
        }
    }
}
