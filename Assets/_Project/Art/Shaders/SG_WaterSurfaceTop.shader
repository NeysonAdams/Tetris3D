// ============================================================================
// SG_WaterSurfaceTop.shader (v2 — AAA with refraction + Fresnel)
// ----------------------------------------------------------------------------
// Physically-motivated water surface for underwater scenes. Seen from below.
//
// Features over v1:
//   - Analytic normal derivation from FBM ripples (not just UV modulation)
//   - Screen-space refraction — samples scene color through distorted UV
//   - Fresnel term — view-angle-dependent transmission vs reflection
//   - Depth-based tinting — deeper under surface = more water-colored
//   - Two independent animated ripple layers
//   - Edge distance fade to skybox
//
// Dependencies:
//   - URP Asset must have "Opaque Texture" enabled (for _CameraOpaqueTexture)
//   - URP Asset must have "Depth Texture" enabled (for _CameraDepthTexture)
//a
// Rendering: Transparent queue, alpha blend, cull OFF (visible from both sides).
//
// Compatibility: Unity 6 + URP 17+ + Forward+.
// ============================================================================

Shader "Custom/SG_WaterSurfaceTop"
{
    Properties
    {
        [Header(Water Base)]
        _BaseColor ("Water Tint", Color) = (0.65, 0.88, 0.92, 1)
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.45

        [Header(Ripple Layer 1 big waves)]
        _RippleScale1 ("Ripple Scale 1", Range(0.5, 20)) = 2.5
        _RippleSpeed1 ("Ripple Speed 1", Range(0, 2)) = 0.12
        _RippleDirection1 ("Ripple Direction 1", Vector) = (1, 0.3, 0, 0)

        [Header(Ripple Layer 2 small detail)]
        _RippleScale2 ("Ripple Scale 2", Range(1, 40)) = 7.0
        _RippleSpeed2 ("Ripple Speed 2", Range(0, 2)) = 0.3
        _RippleDirection2 ("Ripple Direction 2", Vector) = (-0.5, 1, 0, 0)

        [Header(Ripple Intensity)]
        _RippleIntensity ("Ripple Intensity", Range(0, 2)) = 0.8
        _RippleContrast ("Ripple Contrast", Range(0.5, 5)) = 1.8
        _BrightSpotIntensity ("Bright Spot Intensity", Range(0, 3)) = 1.2

        [Header(Refraction)]
        _RefractionStrength ("Refraction Strength", Range(0, 0.5)) = 0.15
        _NormalStrength ("Normal Strength", Range(0, 3)) = 1.2

        [Header(Fresnel Transmission)]
        _FresnelPower ("Fresnel Power", Range(0.5, 10)) = 3.0
        _ReflectiveColor ("Reflective Color (grazing angle)", Color) = (0.78, 0.91, 0.94, 1)
        _TransmissiveColor ("Transmissive Color (straight through)", Color) = (1.0, 0.98, 0.91, 1)

        [Header(Depth Tinting)]
        _DepthFogDensity ("Depth Fog Density", Range(0, 2)) = 0.3
        _DepthFogColor ("Depth Fog Color", Color) = (0.25, 0.5, 0.55, 1)

        [Header(Distance Fade)]
        _FadeStart ("Fade Start Distance", Range(5, 200)) = 25.0
        _FadeEnd ("Fade End Distance", Range(10, 500)) = 100.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _BaseAlpha;
                float _RippleScale1;
                float _RippleSpeed1;
                float4 _RippleDirection1;
                float _RippleScale2;
                float _RippleSpeed2;
                float4 _RippleDirection2;
                float _RippleIntensity;
                float _RippleContrast;
                float _BrightSpotIntensity;
                float _RefractionStrength;
                float _NormalStrength;
                float _FresnelPower;
                float4 _ReflectiveColor;
                float4 _TransmissiveColor;
                float _DepthFogDensity;
                float4 _DepthFogColor;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float4 screenPos   : TEXCOORD3;
                float2 uv          : TEXCOORD4;
                float fogCoord     : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ----------------------------------------------------------------
            // NOISE FUNCTIONS
            // ----------------------------------------------------------------
            float Hash2D(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float ValueNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n00 = Hash2D(i + float2(0, 0));
                float n10 = Hash2D(i + float2(1, 0));
                float n01 = Hash2D(i + float2(0, 1));
                float n11 = Hash2D(i + float2(1, 1));

                float nx0 = lerp(n00, n10, f.x);
                float nx1 = lerp(n01, n11, f.x);
                return lerp(nx0, nx1, f.y);
            }

            float RippleFBM(float2 p)
            {
                float n = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < 3; i++)
                {
                    n += ValueNoise2D(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return n;
            }

            // Combined ripple height at world XZ
            float GetRippleHeight(float2 worldXZ)
            {
                float2 uv1 = worldXZ * _RippleScale1 * 0.1;
                float2 offset1 = normalize(_RippleDirection1.xy) * (_Time.y * _RippleSpeed1);
                float h1 = RippleFBM(uv1 + offset1);

                float2 uv2 = worldXZ * _RippleScale2 * 0.1;
                float2 offset2 = normalize(_RippleDirection2.xy) * (_Time.y * _RippleSpeed2);
                float h2 = RippleFBM(uv2 + offset2);

                return (h1 + h2) * 0.5;
            }

            // Analytic normal via central differences
            float3 GetRippleNormal(float2 worldXZ)
            {
                const float eps = 0.15;
                float hL = GetRippleHeight(worldXZ - float2(eps, 0));
                float hR = GetRippleHeight(worldXZ + float2(eps, 0));
                float hD = GetRippleHeight(worldXZ - float2(0, eps));
                float hU = GetRippleHeight(worldXZ + float2(0, eps));

                float3 normal = float3(hL - hR, 2.0 * eps / _NormalStrength, hD - hU);
                return normalize(normal);
            }

            // ----------------------------------------------------------------
            // VERTEX
            // ----------------------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.normalWS = normals.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                OUT.screenPos = ComputeScreenPos(positions.positionCS);
                OUT.uv = IN.uv;
                OUT.fogCoord = ComputeFogFactor(positions.positionCS.z);
                return OUT;
            }

            // ----------------------------------------------------------------
            // FRAGMENT
            // ----------------------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 viewDirWS = normalize(IN.viewDirWS);
                float2 worldXZ = IN.positionWS.xz;

                // ============================================================
                // RIPPLE HEIGHT + NORMAL
                // ============================================================
                float rippleHeight = GetRippleHeight(worldXZ);
                float rippleSaturated = saturate(pow(rippleHeight, _RippleContrast));
                float3 rippleNormalWS = GetRippleNormal(worldXZ);

                // ============================================================
                // SCREEN-SPACE REFRACTION
                // ============================================================
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                float2 refractionOffset = rippleNormalWS.xz * _RefractionStrength;
                float2 refractedUV = screenUV + refractionOffset;

                half3 refractedScene = SampleSceneColor(refractedUV);

                // ============================================================
                // DEPTH-BASED TINTING
                // ============================================================
                float sceneDepthRaw = SampleSceneDepth(refractedUV);
                float sceneDepthLinear = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                float surfaceDepthLinear = LinearEyeDepth(IN.positionCS.z / IN.positionCS.w, _ZBufferParams);
                float depthDifference = max(sceneDepthLinear - surfaceDepthLinear, 0);

                float depthFogFactor = 1.0 - exp(-depthDifference * _DepthFogDensity);
                half3 depthTinted = lerp(refractedScene, _DepthFogColor.rgb, depthFogFactor);

                // ============================================================
                // FRESNEL TRANSMISSION
                // ============================================================
                float3 fresnelNormal = normalize(lerp(IN.normalWS, rippleNormalWS, 0.5));
                float NdotV = saturate(dot(fresnelNormal, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                half3 transmitted = depthTinted * _TransmissiveColor.rgb;
                half3 reflected = _BaseColor.rgb * _ReflectiveColor.rgb;
                half3 waterColor = lerp(transmitted, reflected, fresnel);

                // ============================================================
                // BRIGHT SPOTS
                // ============================================================
                float2 uv1 = worldXZ * _RippleScale1 * 0.1;
                float2 offset1 = normalize(_RippleDirection1.xy) * (_Time.y * _RippleSpeed1);
                float r1 = RippleFBM(uv1 + offset1);

                float2 uv2 = worldXZ * _RippleScale2 * 0.1;
                float2 offset2 = normalize(_RippleDirection2.xy) * (_Time.y * _RippleSpeed2);
                float r2 = RippleFBM(uv2 + offset2);

                float interference = r1 * r2;
                interference = saturate(pow(interference, 3.0));
                float brightSpots = interference * _BrightSpotIntensity;

                waterColor = waterColor + waterColor * brightSpots;

                // ============================================================
                // ALPHA
                // ============================================================
                float rippleMod = lerp(1.0 - _RippleIntensity * 0.4, 1.0 + _RippleIntensity * 0.3, rippleSaturated);
                float alpha = _BaseAlpha * rippleMod;

                alpha = lerp(alpha, 1.0, depthFogFactor * 0.7);

                float3 camToFrag = IN.positionWS - _WorldSpaceCameraPos;
                float distToCamera = length(camToFrag);
                float fadeT = saturate((distToCamera - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));
                fadeT = smoothstep(0.0, 1.0, fadeT);
                alpha *= (1.0 - fadeT);

                alpha = saturate(alpha);

                // ============================================================
                // FOG
                // ============================================================
                waterColor = MixFog(waterColor, IN.fogCoord);

                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
