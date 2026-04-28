// ============================================================================
// CausticsCookieRenderer.cs
// ----------------------------------------------------------------------------
// Renders a procedural caustics Shader Graph into a RenderTexture every frame
// and assigns it as a Light Cookie on the target directional light.
//
// This is the AAA approach used in underwater games (Subnautica, Abzû):
// the light physically carries the caustics pattern, so all surfaces lit
// by this light automatically receive the caustics — without per-shader code.
//
// Usage:
//   1. Attach to any GameObject in the scene (e.g. the Sun light itself).
//   2. Assign Target Light (the directional light to apply cookie to).
//   3. Assign Caustics Material (material using SG_Caustics shader).
//   4. Configure Cookie Size (world-space tiling) and Texture Resolution.
//
// Requirements:
//   - URP Asset must have Cookie support enabled (Cookie Atlas Resolution > 0).
//   - Target light must be Directional (spot/point also work but cookie math
//     differs; this script optimizes for directional).
//
// Compatibility: Unity 6 + URP 17+.
// ============================================================================

using UnityEngine;

namespace TetrisAR.Rendering
{
    /// <summary>
    /// Renders procedural caustics into a RenderTexture and binds it as
    /// a Light Cookie on a target light.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CausticsCookieRenderer : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Directional light that will receive the caustics cookie.")]
        [SerializeField] private Light _targetLight;

        [Tooltip("Material using SG_Caustics shader. Caustics pattern will be rendered from this.")]
        [SerializeField] private Material _causticsMaterial;

        [Header("Cookie Settings")]
        [Tooltip("Resolution of the caustics RenderTexture. 512 is recommended (quality/perf balance).")]
        [SerializeField] private int _textureResolution = 512;

        [Tooltip("World-space size of the cookie tile. The caustics pattern repeats every N meters. Smaller = denser pattern on ground.")]
        [SerializeField] private float _cookieSize = 30f;

        [Header("Behavior")]
        [Tooltip("Render caustics every frame (true) or only once at start (false).")]
        [SerializeField] private bool _animateEveryFrame = true;

        private RenderTexture _cookieRT;
        private Light _cachedLight;

        // --------------------------------------------------------------------
        // LIFECYCLE
        // --------------------------------------------------------------------

        private void Awake()
        {
            if (!ValidateSetup()) return;

            CreateCookieTexture();
            AssignCookieToLight();
            RenderCausticsToTexture(); // initial render
        }

        private void OnEnable()
        {
            // Re-assign cookie in case it was cleared (e.g., after domain reload in editor)
            if (_cookieRT != null && _targetLight != null)
            {
                AssignCookieToLight();
            }
        }

        private void Update()
        {
            if (!_animateEveryFrame) return;
            if (_causticsMaterial == null || _cookieRT == null) return;

            RenderCausticsToTexture();
        }

        private void OnDisable()
        {
            // Remove cookie reference when script is disabled so the light reverts
            if (_targetLight != null && _targetLight.cookie == _cookieRT)
            {
                _targetLight.cookie = null;
            }
        }

        private void OnDestroy()
        {
            ReleaseCookieTexture();
        }

        // --------------------------------------------------------------------
        // SETUP
        // --------------------------------------------------------------------

        private bool ValidateSetup()
        {
            if (_targetLight == null)
            {
                Debug.LogWarning($"[{nameof(CausticsCookieRenderer)}] Target Light is not assigned. Caustics cookie will not be applied.", this);
                return false;
            }

            if (_causticsMaterial == null)
            {
                Debug.LogWarning($"[{nameof(CausticsCookieRenderer)}] Caustics Material is not assigned.", this);
                return false;
            }

            if (_textureResolution < 64 || _textureResolution > 4096)
            {
                Debug.LogWarning($"[{nameof(CausticsCookieRenderer)}] Texture Resolution {_textureResolution} out of reasonable range. Clamping to [64, 4096].", this);
                _textureResolution = Mathf.Clamp(_textureResolution, 64, 4096);
            }

            return true;
        }

        private void CreateCookieTexture()
        {
            // RenderTextureFormat.R8 — single channel, enough for cookie (grayscale mask).
            // If URP Asset Cookie Atlas Format is set to "Color High", we can also use
            // RGB32 / ARGB32 for colored cookies, but R8 is the lightest option.
            //
            // However, some URP cookie implementations only accept specific formats.
            // Using default format which is safe for both grayscale and color atlases.
            var descriptor = new RenderTextureDescriptor(
                _textureResolution,
                _textureResolution,
                RenderTextureFormat.Default,
                0  // no depth buffer needed for cookie
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

            // For directional lights, cookieSize defines the world-space tile size.
            // Smaller values = denser pattern. Typical 10-50 meters.
            _targetLight.cookieSize = _cookieSize;

            _cachedLight = _targetLight;
        }

        // --------------------------------------------------------------------
        // RENDER
        // --------------------------------------------------------------------

        private void RenderCausticsToTexture()
        {
            // Blit caustics shader into the RT. The material's shader references
            // _Time internally, so each frame produces the next animation step.
            //
            // Passing null as source — Blit uses only the material's output.
            Graphics.Blit(null, _cookieRT, _causticsMaterial);
        }

        // --------------------------------------------------------------------
        // CLEANUP
        // --------------------------------------------------------------------

        private void ReleaseCookieTexture()
        {
            // Clear the cookie reference from the light before destroying the RT
            if (_cachedLight != null && _cachedLight.cookie == _cookieRT)
            {
                _cachedLight.cookie = null;
            }

            if (_cookieRT != null)
            {
                _cookieRT.Release();
                Destroy(_cookieRT);
                _cookieRT = null;
            }
        }

        // --------------------------------------------------------------------
        // EDITOR HELPERS (visible in Inspector via context menu)
        // --------------------------------------------------------------------

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
