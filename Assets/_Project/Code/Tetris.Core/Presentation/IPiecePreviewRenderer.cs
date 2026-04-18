using Tetris.Core.Configs;
using UnityEngine;


namespace Tetris.Core.Presentation
{
    public interface IPiecePreviewRenderer
    {
        Texture PreviewTexture { get; }
        void SetPreview(TetrominoShapeSo shape);
    }
}
