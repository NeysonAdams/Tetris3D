using UnityEngine;

namespace TetrisAR.Rendering
{
    [DisallowMultipleComponent]
    public sealed class CausticsCookieRenderer : MonoBehaviour
    {
        [SerializeField] private Light _targetLight;
        [SerializeField] private Material _causticsMaterial;
        [SerializeField] private int _textureResolution = 512;
        [SerializeField] private float _cookieSize = 30f;
        [SerializeField] private bool _animateEveryFrame = true;

        private RenderTexture _cookieRT;
        private Light _cachedLight;

        private void Awake()
        {
            if (!ValidateSetup()) return;

            CreateCookieTexture();
            AssignCookieToLight();
            RenderCausticsToTexture();
        }

        private void OnEnable()
        {
            if (_cookieRT != null && _targetLight != null)
                AssignCookieToLight();
        }

        private void Update()
        {
            if (!_animateEveryFrame) return;
            if (_causticsMaterial == null || _cookieRT == null) return;
            RenderCausticsToTexture();
        }

        private void OnDisable()
        {
            if (_targetLight != null && _targetLight.cookie == _cookieRT)
                _targetLight.cookie = null;
        }

        private void OnDestroy() => ReleaseCookieTexture();

        private bool ValidateSetup()
        {
            if (_targetLight == null)
            {
                Debug.LogWarning($"[{nameof(CausticsCookieRenderer)}] Target Light not assigned.", this);
                return false;
            }

            if (_causticsMaterial == null)
            {
                Debug.LogWarning($"[{nameof(CausticsCookieRenderer)}] Caustics Material not assigned.", this);
                return false;
            }

            _textureResolution = Mathf.Clamp(_textureResolution, 64, 4096);
            return true;
        }

        private void CreateCookieTexture()
        {
            var descriptor = new RenderTextureDescriptor(
                _textureResolution,
                _textureResolution,
                RenderTextureFormat.Default,
                0
            )
            {
                useMipMap = false,
                autoGenerateMips = false,
                sRGB = false
            };

            _cookieRT = new RenderTexture(descriptor)
            {
                name = "CausticsCookie_RT",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            _cookieRT.Create();
        }

        private void AssignCookieToLight()
        {
            if (_targetLight == null || _cookieRT == null) return;

            _targetLight.cookie = _cookieRT;
            _targetLight.cookieSize = _cookieSize;
            _cachedLight = _targetLight;
        }

        private void RenderCausticsToTexture()
        {
            Graphics.Blit(null, _cookieRT, _causticsMaterial);
        }

        private void ReleaseCookieTexture()
        {
            if (_cachedLight != null && _cachedLight.cookie == _cookieRT)
                _cachedLight.cookie = null;

            if (_cookieRT != null)
            {
                _cookieRT.Release();
                Destroy(_cookieRT);
                _cookieRT = null;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Rebuild Cookie Now")]
        private void RebuildCookieNow()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("Rebuild only works in Play Mode.", this);
                return;
            }

            ReleaseCookieTexture();
            if (ValidateSetup())
            {
                CreateCookieTexture();
                AssignCookieToLight();
                RenderCausticsToTexture();
            }
        }
#endif
    }
}
