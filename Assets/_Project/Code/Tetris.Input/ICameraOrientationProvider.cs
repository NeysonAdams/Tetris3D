namespace Tetris.Input
{
    /// <summary>
    /// Provides camera orientation for input direction remapping.
    /// </summary>
    public interface ICameraOrientationProvider
    {
        /// <summary>
        /// Current cardinal direction the camera is facing.
        /// </summary>
        CameraFacing CurrentFacing { get; }
    }
}
