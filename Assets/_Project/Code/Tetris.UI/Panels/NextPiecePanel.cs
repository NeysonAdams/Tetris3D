using Tetris.Core.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Tetris.UI.Panels
{
    public sealed class NextPiecePanel : MonoBehaviour
    {
        [SerializeField] private RawImage _rawImage = default!;

        private bool _isInitialized;

        public void Initialize(IPiecePreviewRenderer previewRenderer)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("NextPiecePanel already initialized.");
            }

            if (previewRenderer == null) throw new System.ArgumentNullException(nameof(previewRenderer));

            if (_rawImage == null)
            {
                throw new System.InvalidOperationException(
                    "NextPiecePanel._rawImage reference not assigned. Set in Inspector.");
            }

            _rawImage.texture = previewRenderer.PreviewTexture;

            _isInitialized = true;
        }
    }
}