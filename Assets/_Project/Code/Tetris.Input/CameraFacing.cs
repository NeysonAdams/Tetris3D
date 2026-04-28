namespace Tetris.Input
{
    /// <summary>
    /// Cardinal direction the camera is facing relative to the game field.
    /// Used to remap input controls based on camera orientation.
    /// </summary>
    public enum CameraFacing
    {
        /// <summary>Camera facing +Z (default forward)</summary>
        Front = 0,

        /// <summary>Camera facing +X (right side)</summary>
        Right = 1,

        /// <summary>Camera facing -Z (back side)</summary>
        Back = 2,

        /// <summary>Camera facing -X (left side)</summary>
        Left = 3
    }
}
