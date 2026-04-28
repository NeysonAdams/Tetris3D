namespace Tetris.Input
{
    public interface ICameraOrientationProvider
    {
        CameraFacing CurrentFacing { get; }
    }
}
