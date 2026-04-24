// ============================================================================
// SG_AtlantisTiledStep_Full.shader
// ----------------------------------------------------------------------------
// Stylized jade tiled step with radial iconography for Atlantis altar.
// Used for the BOTTOM and MIDDLE steps of the pyramid — full solid surface
// covered with 1x1 tiles with small gaps, each tile displays an icon rotated
// toward step center.
//
// Features:
//   - Jade stone appearance via tri-planar FBM noise.
//   - Procedural 1x1 tile grid with configurable gap.
//   - Per-tile icon sampled from _IconTexture, rotated radially toward center.
//   - Icon color and intensity configurable via material.
//   - Lighting: URP main light + ambient + rim.
//
// Configuration per-material-instance:
//   - _IconTexture:    Atlantis_Rings.png (bottom) or Trident_Atlantis.png (middle)
//   - _StepCenter:     (0, 0) default — offset if step is not centered at origin
//   - _IconColor:      icon tint (HDR-capable)
//   - _IconIntensity:  blend strength of icon over stone (0..1)
//   - _IconScale:      icon size within tile (0.5..1.0, 0.7 typical)
//   - _TileGap:        gap between tiles (0.02..0.15, 0.05 typical)
// ============================================================================

Shader "Custom/SG_AtlantisTiledStep_Full"
{
    Properties
    {
        [Header(Jade Stone)]
        _BaseColor ("Base Color", Color) = (0.18, 0.55, 0.34, 1) // Jade green
        _SecondaryColor ("Secondary Color", Color) = (0.12, 0.42, 0.25, 1) // Darker jade
        _Smoothness ("Smoothness", Range(0, 1)) = 0.65
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        [Header(Stone Variation FBM)]
        _StoneScale ("Stone Noise Scale", Range(0.5, 20)) = 6.0
        _StoneIntensity ("Stone Intensity", Range(0, 1)) = 0.35
        _StoneContrast ("Stone Contrast", Range(0.5, 3)) = 1.4

        [Header(Tile Grid and Icon)]
        _IconTexture ("Icon Texture (white on transparent)", 2D) = "white" {}
        _IconColor ("Icon Color (HDR)", Color) = (0.85, 0.92, 0.75, 1) // Light jade accent
        _IconIntensity ("Icon Intensity", Range(0, 1)) = 0.75
        _IconScale ("Icon Scale (within tile)", Range(0.3, 1.0)) = 0.7
        _TileSize ("Tile Size (world units)", Range(0.5, 2.0)) = 1.0
        _TileGap ("Tile Gap", Range(0.01, 0.15)) = 0.05
        _GapColor ("Gap Color", Color) = (0.05, 0.08, 0.05, 1) // Dark gap

        [Header(Icon Rotation)]
        [Toggle] _RotateToCenter ("Rotate Icons To Center", Float) = 1
        _StepCenter ("Step Center (world XZ)", Vector) = (0, 0, 0, 0)
        _IconRotation ("Manual Rotation (degrees)", Range(0, 360)) = 0
        _IconRotationOffset ("Rotation Offset (degrees)", Range(-180, 180)) = 0

        [Header(Rim Light)]
        _RimColor ("Rim Color (HDR)", Color) = (0.7, 0.95, 0.8, 1) // Jade rim
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 0.4
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

            TEXTURE2D(_IconTexture);
            SAMPLER(sampler_IconTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SecondaryColor;
                float _Smoothness;
                float _Metallic;
                float _StoneScale;
                float _StoneIntensity;
                float _StoneContrast;
                float4 _IconTexture_ST;
                float4 _IconColor;
                float _IconIntensity;
                float _IconScale;
                float _TileSize;
                float _TileGap;
                float4 _GapColor;
                float _RotateToCenter;
                float4 _StepCenter;
                float _IconRotation;
                float _IconRotationOffset;
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

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // ============================================================
                // BASE JADE STONE (tri-planar FBM)
                // ============================================================
                float3 stonePos = IN.positionWS * _StoneScale * 0.1;
                float stoneX = StoneNoise(stonePos.yzx);
                float stoneY = StoneNoise(stonePos.xzy);
                float stoneZ = StoneNoise(stonePos.xyz);

                float3 blendW = abs(normalWS);
                blendW = blendW / (blendW.x + blendW.y + blendW.z);

                float stoneNoise = stoneX * blendW.x + stoneY * blendW.y + stoneZ * blendW.z;
                stoneNoise = saturate(pow(stoneNoise, _StoneContrast));

                // Blend between primary and secondary jade colors
                float3 jadeColor = lerp(_SecondaryColor.rgb, _BaseColor.rgb, stoneNoise);
                float stoneModulation = lerp(1.0 - _StoneIntensity * 0.5, 1.0 + _StoneIntensity * 0.2, stoneNoise);
                float3 stoneAlbedo = jadeColor * stoneModulation;

                // ============================================================
                // TILE GRID (based on world XZ, top surface only)
                // ============================================================
                // Icon is rendered only on top-facing surfaces (normal pointing up)
                float topMask = saturate((normalWS.y - 0.7) * 3.0);

                // Tile coordinates relative to step center
                float2 worldXZ = IN.positionWS.xz;
                float2 tileCoord = floor(worldXZ / _TileSize);
                float2 tileLocalUV = frac(worldXZ / _TileSize);
                float2 tileCenterWorld = (tileCoord + 0.5) * _TileSize;

                // Calculate gap mask - tiles are inset by _TileGap on each side
                float halfGap = _TileGap * 0.5;
                float gapMask = step(halfGap, tileLocalUV.x) * step(tileLocalUV.x, 1.0 - halfGap)
                              * step(halfGap, tileLocalUV.y) * step(tileLocalUV.y, 1.0 - halfGap);

                // Remap UV to tile interior (excluding gap)
                float2 tileInnerUV = (tileLocalUV - halfGap) / (1.0 - _TileGap);

                // Calculate rotation angle
                float angle;
                if (_RotateToCenter > 0.5)
                {
                    // Auto-rotate toward step center
                    float2 dirToStepCenter = _StepCenter.xy - tileCenterWorld;
                    angle = atan2(dirToStepCenter.y, dirToStepCenter.x) - (3.14159265 * 0.5);
                    // Add offset (convert degrees to radians)
                    angle += _IconRotationOffset * (3.14159265 / 180.0);
                }
                else
                {
                    // Manual rotation (convert degrees to radians)
                    angle = _IconRotation * (3.14159265 / 180.0);
                }

                // Rotate UV around (0.5, 0.5)
                float2 uvCentered = tileInnerUV - 0.5;
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 rotated = float2(
                    cosA * uvCentered.x - sinA * uvCentered.y,
                    sinA * uvCentered.x + cosA * uvCentered.y
                );

                // Scale (icon occupies _IconScale fraction of tile)
                float2 iconUV = (rotated / _IconScale) + 0.5;

                // Skip sampling outside icon bounds
                float insideBounds = step(0.0, iconUV.x) * step(iconUV.x, 1.0)
                                   * step(0.0, iconUV.y) * step(iconUV.y, 1.0);

                // Sample icon texture (red channel as mask)
                float iconMask = SAMPLE_TEXTURE2D(_IconTexture, sampler_IconTexture, iconUV).r;
                iconMask *= insideBounds * topMask * _IconIntensity * gapMask;

                // ============================================================
                // TILE + GAP COMPOSITE
                // ============================================================
                // Tile interior: jade stone with optional icon
                float3 tileAlbedo = lerp(stoneAlbedo, _IconColor.rgb, iconMask);
                // Gap: dark color
                float3 albedo = lerp(_GapColor.rgb, tileAlbedo, gapMask * topMask + (1.0 - topMask));

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
                lit += _EmissionColor.rgb;

                lit = MixFog(lit, IN.fogCoord);

                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        // Shadow + Depth passes (unchanged from SG_BlockStylized pattern)
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
                float4 _BaseColor; float4 _SecondaryColor; float _Smoothness; float _Metallic;
                float _StoneScale; float _StoneIntensity; float _StoneContrast;
                float4 _IconTexture_ST; float4 _IconColor;
                float _IconIntensity; float _IconScale; float _TileSize;
                float _TileGap; float4 _GapColor;
                float _RotateToCenter; float4 _StepCenter;
                float _IconRotation; float _IconRotationOffset;
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
                float4 _BaseColor; float4 _SecondaryColor; float _Smoothness; float _Metallic;
                float _StoneScale; float _StoneIntensity; float _StoneContrast;
                float4 _IconTexture_ST; float4 _IconColor;
                float _IconIntensity; float _IconScale; float _TileSize;
                float _TileGap; float4 _GapColor;
                float _RotateToCenter; float4 _StepCenter;
                float _IconRotation; float _IconRotationOffset;
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
                float4 _BaseColor; float4 _SecondaryColor; float _Smoothness; float _Metallic;
                float _StoneScale; float _StoneIntensity; float _StoneContrast;
                float4 _IconTexture_ST; float4 _IconColor;
                float _IconIntensity; float _IconScale; float _TileSize;
                float _TileGap; float4 _GapColor;
                float _RotateToCenter; float4 _StepCenter;
                float _IconRotation; float _IconRotationOffset;
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
