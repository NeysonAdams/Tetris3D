// ============================================================================
// SG_ArenaCheckerTiles.shader
// ----------------------------------------------------------------------------
// Stylized malachite tiles with checkerboard pattern and glowing gaps.
// Two malachite colors alternate in chess-board style with configurable gaps
// that can emit light.
//
// Features:
//   - Two malachite colors (ColorA / ColorB) in checkerboard pattern
//   - Tri-planar FBM stone variation
//   - Configurable gap between tiles with emission
//   - Tile offset for fractal alignment
//   - Rim Fresnel
//   - Full URP stack
// ============================================================================

Shader "Custom/SG_ArenaCheckerTiles"
{
    Properties
    {
        [Header(Malachite Tile A)]
        _ColorA1 ("Tile A Light", Color) = (0.05, 0.85, 0.50, 1)
        _ColorA2 ("Tile A Dark",  Color) = (0.0, 0.20, 0.12, 1)

        [Header(Malachite Tile B)]
        _ColorB1 ("Tile B Light", Color) = (0.08, 0.70, 0.45, 1)
        _ColorB2 ("Tile B Dark",  Color) = (0.0, 0.15, 0.10, 1)

        _Smoothness ("Smoothness", Range(0, 1)) = 0.82
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        [Header(Stone Variation FBM)]
        _StoneScale ("Stone Noise Scale", Range(0.5, 30)) = 10.0
        _StoneIntensity ("Stone Intensity", Range(0, 1)) = 0.35
        _StoneContrast ("Stone Contrast", Range(0.5, 3)) = 1.6

        [Header(Tile Grid)]
        _TileSize ("Tile Size (world units)", Range(0.1, 5.0)) = 1.0
        _TileOffsetX ("Tile Offset X", Range(-10, 10)) = 0.0
        _TileOffsetZ ("Tile Offset Z", Range(-10, 10)) = 0.0

        [Header(Tile Gap)]
        _TileGap ("Gap Width", Range(0.0, 0.2)) = 0.05
        _GapColor ("Gap Color", Color) = (0.0, 0.04, 0.02, 1)
        [HDR] _GapEmission ("Gap Emission (HDR)", Color) = (0.0, 1.2, 0.5, 1)
        _GapEmissionIntensity ("Gap Emission Intensity", Range(0, 5)) = 0.7

        [Header(Rim Light)]
        _RimColor ("Rim Color (HDR)", Color) = (0.2, 1.0, 0.55, 1)
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 0.45
        _RimPower ("Rim Power", Range(0.5, 10)) = 2.5

        [Header(Emission via MaterialPropertyBlock)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        LOD 200

        // ====================================================================
        // FORWARD LIT PASS
        // ====================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA1;
                float4 _ColorA2;
                float4 _ColorB1;
                float4 _ColorB2;
                float _Smoothness;
                float _Metallic;
                float _StoneScale;
                float _StoneIntensity;
                float _StoneContrast;
                float _TileSize;
                float _TileOffsetX;
                float _TileOffsetZ;
                float _TileGap;
                float4 _GapColor;
                float4 _GapEmission;
                float _GapEmissionIntensity;
                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;
                float4 _EmissionColor;
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
                float4 shadowCoord : TEXCOORD3;
                float fogCoord     : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ----------------------------------------------------------------
            // PROCEDURAL NOISE (tri-planar FBM stone)
            // ----------------------------------------------------------------
            float Hash3D(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash3D(i + float3(0, 0, 0));
                float n100 = Hash3D(i + float3(1, 0, 0));
                float n010 = Hash3D(i + float3(0, 1, 0));
                float n110 = Hash3D(i + float3(1, 1, 0));
                float n001 = Hash3D(i + float3(0, 0, 1));
                float n101 = Hash3D(i + float3(1, 0, 1));
                float n011 = Hash3D(i + float3(0, 1, 1));
                float n111 = Hash3D(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            float StoneNoise(float3 p)
            {
                float n = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < 3; i++)
                {
                    n += ValueNoise3D(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return n;
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

                OUT.positionCS  = positions.positionCS;
                OUT.positionWS  = positions.positionWS;
                OUT.normalWS    = normals.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(positions.positionWS);
                OUT.shadowCoord = GetShadowCoord(positions);
                OUT.fogCoord    = ComputeFogFactor(positions.positionCS.z);
                return OUT;
            }

            // ----------------------------------------------------------------
            // FRAGMENT
            // ----------------------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // Only apply tile pattern to top-facing surfaces
                float topMask = saturate((normalWS.y - 0.7) * 3.0);

                // ============================================================
                // TILE GRID WITH OFFSET
                // ============================================================
                float2 worldXZ = IN.positionWS.xz + float2(_TileOffsetX, _TileOffsetZ);
                float2 tileCoord = floor(worldXZ / _TileSize);
                float2 tileLocalUV = frac(worldXZ / _TileSize);

                // ============================================================
                // GAP MASK
                // ============================================================
                float halfGap = _TileGap * 0.5;
                float gapMask = step(halfGap, tileLocalUV.x) * step(tileLocalUV.x, 1.0 - halfGap)
                              * step(halfGap, tileLocalUV.y) * step(tileLocalUV.y, 1.0 - halfGap);

                // Invert for top surfaces only
                float isGap = (1.0 - gapMask) * topMask;

                // ============================================================
                // CHECKERBOARD PATTERN
                // ============================================================
                float parity = fmod(tileCoord.x + tileCoord.y, 2.0);
                parity = abs(parity);
                float checker = step(0.5, parity);

                // ============================================================
                // MALACHITE BANDS (tri-planar FBM for banding pattern)
                // ============================================================
                float3 stonePos = IN.positionWS * _StoneScale * 0.1;
                float stoneX = StoneNoise(stonePos.yzx);
                float stoneY = StoneNoise(stonePos.xzy);
                float stoneZ = StoneNoise(stonePos.xyz);

                float3 blendW = abs(normalWS);
                blendW = blendW / (blendW.x + blendW.y + blendW.z);

                float stoneNoise = stoneX * blendW.x + stoneY * blendW.y + stoneZ * blendW.z;
                stoneNoise = saturate(pow(stoneNoise, _StoneContrast));

                // Blend between light and dark within each tile
                float3 tileAColor = lerp(_ColorA2.rgb, _ColorA1.rgb, stoneNoise);
                float3 tileBColor = lerp(_ColorB2.rgb, _ColorB1.rgb, stoneNoise);

                // Pick tile based on checkerboard
                float3 malachiteAlbedo = lerp(tileAColor, tileBColor, checker);

                // Additional subtle modulation
                float stoneModulation = lerp(1.0 - _StoneIntensity * 0.3, 1.0 + _StoneIntensity * 0.1, stoneNoise);
                malachiteAlbedo *= stoneModulation;

                // ============================================================
                // COMPOSITE ALBEDO (malachite tiles + gap)
                // ============================================================
                float3 albedo = lerp(malachiteAlbedo, _GapColor.rgb, isGap);

                // ============================================================
                // GAP EMISSION
                // ============================================================
                float3 gapEmit = _GapEmission.rgb * _GapEmissionIntensity * isGap;

                // ============================================================
                // LIGHTING
                // ============================================================
                Light mainLight = GetMainLight(IN.shadowCoord, IN.positionWS, half4(1, 1, 1, 1));

                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 mainLightColor = mainLight.color * NdotL * mainLight.shadowAttenuation;
                float3 ambient = SampleSH(normalWS);

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularPower = exp2(_Smoothness * 10 + 1);
                float specular = pow(NdotH, specularPower) * _Smoothness * NdotL * mainLight.shadowAttenuation;

                // ============================================================
                // RIM LIGHT
                // ============================================================
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _RimPower);
                float3 rimLight = _RimColor.rgb * fresnel * _RimIntensity;

                // ============================================================
                // COMPOSITE
                // ============================================================
                float3 lit = albedo * (mainLightColor + ambient);
                lit += specular * mainLight.color;
                lit += rimLight;
                lit += gapEmit;
                lit += _EmissionColor.rgb;

                lit = MixFog(lit, IN.fogCoord);

                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        // ====================================================================
        // SHADOW CASTER
        // ====================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA1; float4 _ColorA2; float4 _ColorB1; float4 _ColorB2;
                float _Smoothness; float _Metallic;
                float _StoneScale; float _StoneIntensity; float _StoneContrast;
                float _TileSize; float _TileOffsetX; float _TileOffsetZ;
                float _TileGap; float4 _GapColor;
                float4 _GapEmission; float _GapEmissionIntensity;
                float4 _RimColor; float _RimIntensity; float _RimPower;
                float4 _EmissionColor;
            CBUFFER_END

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = GetShadowPositionHClip(IN);
                return OUT;
            }

            half4 ShadowPassFragment(Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return 0;
            }
            ENDHLSL
        }

        // ====================================================================
        // DEPTH ONLY
        // ====================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA1; float4 _ColorA2; float4 _ColorB1; float4 _ColorB2;
                float _Smoothness; float _Metallic;
                float _StoneScale; float _StoneIntensity; float _StoneContrast;
                float _TileSize; float _TileOffsetX; float _TileOffsetZ;
                float _TileGap; float4 _GapColor;
                float4 _GapEmission; float _GapEmissionIntensity;
                float4 _RimColor; float _RimIntensity; float _RimPower;
                float4 _EmissionColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings DepthOnlyVertex(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthOnlyFragment(Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return 0;
            }
            ENDHLSL
        }

        // ====================================================================
        // DEPTH NORMALS
        // ====================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA1; float4 _ColorA2; float4 _ColorB1; float4 _ColorB2;
                float _Smoothness; float _Metallic;
                float _StoneScale; float _StoneIntensity; float _StoneContrast;
                float _TileSize; float _TileOffsetX; float _TileOffsetZ;
                float _TileGap; float4 _GapColor;
                float4 _GapEmission; float _GapEmissionIntensity;
                float4 _RimColor; float _RimIntensity; float _RimPower;
                float4 _EmissionColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings DepthNormalsVertex(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 DepthNormalsFragment(Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return half4(NormalizeNormalPerPixel(IN.normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
