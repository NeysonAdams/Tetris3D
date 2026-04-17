using Tetris.Core.Tetrominoes;
using UnityEngine;

namespace Tetris.Core.Configs
{
    [CreateAssetMenu(
        fileName ="SpawnSettings",
        menuName ="Tetris/Core/Spawn Settings",
        order = 22
        )]
    public class SpawnSettingsSO: ScriptableObject
    {
        private const int MinRotationsSteps = 0;
        private const int MaxRotationsSteps = 3;

        [SerializeField]
        private Vector3Int _spawnOffset = Vector3Int.zero;

        [SerializeField]
        private int _initialRxSteps;
        [SerializeField]
        private int _initialRySteps;
        [SerializeField]
        private int _initialRzSteps;


        public Vector3Int SpawnOfset => _spawnOffset;
        public Rotation3D InitialRotation => new(_initialRxSteps, _initialRySteps, _initialRzSteps);

        private void OnValidate()
        {
            _initialRxSteps = Mathf.Clamp(_initialRxSteps, MinRotationsSteps, MaxRotationsSteps);
            _initialRySteps = Mathf.Clamp(_initialRySteps, MinRotationsSteps, MaxRotationsSteps);
            _initialRzSteps = Mathf.Clamp(_initialRzSteps, MinRotationsSteps, MaxRotationsSteps);
        }
    }
}
