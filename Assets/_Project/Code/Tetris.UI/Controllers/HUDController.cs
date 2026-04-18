using UnityEngine;

namespace Tetris.UI.Controllers
{
    public sealed class HUDController : MonoBehaviour
    {
        private bool _isInitialized;

        public void Initialize()
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("HUDController already initialized.");
            }

            _isInitialized = true;
        }
    }
}