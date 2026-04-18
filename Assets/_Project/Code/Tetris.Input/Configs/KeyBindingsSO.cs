using UnityEngine;

namespace Tetris.Input.Configs
{

    [CreateAssetMenu(
        fileName = "KeyBindings",
        menuName = "Tetris/Input/key Binbigs",
        order = 40)]
    public class KeyBindingsSO : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private KeyCode _moveNegativeX = KeyCode.LeftArrow;
        [SerializeField] private KeyCode _movePositiveX = KeyCode.RightArrow;
        [SerializeField] private KeyCode _moveNegativeZ = KeyCode.DownArrow;
        [SerializeField] private KeyCode _movePositiveZ = KeyCode.UpArrow;

        [Header("Drop")]
        [SerializeField] private KeyCode _softDrop = KeyCode.LeftShift;
        [SerializeField] private KeyCode _hardDrop = KeyCode.Space;

        [Header("Rotation")]
        [SerializeField] private KeyCode _rotateNegativeX = KeyCode.A;
        [SerializeField] private KeyCode _rotatePositiveX = KeyCode.D;
        [SerializeField] private KeyCode _rotateNegativeY = KeyCode.Q;
        [SerializeField] private KeyCode _rotatePositiveY = KeyCode.E;
        [SerializeField] private KeyCode _rotateNegativeZ = KeyCode.S;
        [SerializeField] private KeyCode _rotatePositiveZ = KeyCode.W;

        [Header("Meta")]
        [SerializeField] private KeyCode _pause = KeyCode.Escape;

        public KeyCode MoveNegativeX => _moveNegativeX;
        public KeyCode MovePositiveX => _movePositiveX;
        public KeyCode MoveNegativeZ => _moveNegativeZ;
        public KeyCode MovePositiveZ => _movePositiveZ;

        public KeyCode SoftDrop => _softDrop;
        public KeyCode HardDrop => _hardDrop;

        public KeyCode RotateNegativeX => _rotateNegativeX;
        public KeyCode RotatePositiveX => _rotatePositiveX;
        public KeyCode RotateNegativeY => _rotateNegativeY;
        public KeyCode RotatePositiveY => _rotatePositiveY;
        public KeyCode RotateNegativeZ => _rotateNegativeZ;
        public KeyCode RotatePositiveZ => _rotatePositiveZ;

        public KeyCode Pause => _pause;
    }
}
