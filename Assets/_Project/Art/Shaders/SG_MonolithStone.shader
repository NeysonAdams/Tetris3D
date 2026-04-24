// ============================================================================
// SG_MonolithStone.shader
// ----------------------------------------------------------------------------
// Stylized weathered monolith stone shader for 3D Tetris AR project.
// Designed for the altar monolith base — the largest, oldest, most weathered
// stone surface in the scene.
//
// Built on SG_BlockStylized (same base lighting + FBM stone + edge + rim),
// adds two procedural layers for "ancient cracked stone" look:
//   - Cracks (Voronoi-based dark lines tracing cell boundaries)
//   - Fine roughness (additional FBM at finer scale, lower amplitude)
//
// All procedural — no textures.
// GPU Instancing enabled. Compatible with URP 17+ Forward+.
// ============================================================================

Shader "Custom/SG_MonolithStone"
{
    Properties
    {
        [Header(Base Surface)]
        _BaseColor ("Base Color", Color) = (0.784, 0.722, 0.604, 1) // #C8B89A weathered limestone
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        [Header(Stone Variation FBM)]
        _StoneScale ("Stone Noise Scale", Range(0.5, 20)) = 4.0
        _StoneIntensity ("Stone Intensity", Range(0, 1)) = 0.4
        _StoneContrast ("Stone Contrast", Range(0.5, 3)) = 1.5

        [Header(Cracks)]
        _CracksScale ("Cracks Scale", Range(1, 30)) = 6.0
        _CracksIntensity ("Cracks Intensity", Range(0, 1)) = 0.7
        _CracksWidth ("Cracks Width", Range(0.01, 0.3)) = 0.12
        _CracksContrast ("Cracks Contrast", Range(0.5, 8)) = 2.0
        _CracksColor ("Cracks Color", Color) = (0.12, 0.10, 0.08, 1) // dark stone shadow

        [Header(Fine Roughness)]
        _RoughnessScale ("Roughness Scale", Range(5, 100)) = 40.0
        _RoughnessIntensity ("Roughness Color Intensity", Range(0, 0.5)) = 0.2
        _RoughnessNormalStrength ("Roughness Bump Strength", Range(0, 1)) = 0.4

        [Header(Rim Light)]
        _RimColor ("Rim Color (HDR)", Color) = (0.961, 0.929, 0.839, 1) // #F5EDD6 warm sunlight
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 0.3
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
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;

                float _StoneScale;
                float _StoneIntensity;
                float _StoneContrast;

                float _CracksScale;
                float _CracksIntensity;
                float _CracksWidth;
                float _CracksContrast;
                float4 _CracksColor;

                float _RoughnessScale;
                float _RoughnessIntensity;
                float _RoughnessNormalStrength;

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
            // PROCEDURAL NOISE FUNCTIONS
            // ----------------------------------------------------------------

            // Pseudo-random hash for 3D positions
            float Hash3D(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // Hash returning vec3 (for Voronoi cell offsets)
            float3 Hash3DVec(float3 p)
            {
                p = float3(
                    dot(p, float3(127.1, 311.7, 74.7)),
                    dot(p, float3(269.5, 183.3, 246.1)),
                    dot(p, float3(113.5, 271.9, 124.6))
                );
                return frac(sin(p) * 43758.5453);
            }

            // 3D value noise
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

            // FBM — 3 octaves of value noise for stone variation
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

            // Fine FBM for surface roughness — 2 octaves at finer scale
            // Returns value and approximate gradient for normal perturbation
            void RoughnessNoise(float3 p, out float value, out float3 gradient)
            {
                float eps = 0.01;

                // Sample center
                float n = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < 2; i++)
                {
                    n += ValueNoise3D(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }
                value = n;

                // Compute gradient via finite differences
                float nx = 0.0, ny = 0.0, nz = 0.0;
                amp = 0.5; freq = 1.0;
                for (int j = 0; j < 2; j++)
                {
                    nx += ValueNoise3D((p + float3(eps, 0, 0)) * freq) * amp;
                    ny += ValueNoise3D((p + float3(0, eps, 0)) * freq) * amp;
                    nz += ValueNoise3D((p + float3(0, 0, eps)) * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }

                gradient = float3(nx - n, ny - n, nz - n) / eps;
            }

            // Voronoi noise — returns distance to closest cell center.
            // We use distance to SECOND closest minus distance to closest = cell border distance.
            // This gives us crack-like lines along Voronoi cell boundaries.
            float VoronoiCracks(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);

                float minDist1 = 1.0;
                float minDist2 = 1.0;

                // Check 3x3x3 neighbor cells
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            float3 neighbor = float3(x, y, z);
                            float3 cellPos = neighbor + Hash3DVec(i + neighbor) - f;
                            float d = dot(cellPos, cellPos); // squared distance

                            if (d < minDist1)
                            {
                                minDist2 = minDist1;
                                minDist1 = d;
                            }
                            else if (d < minDist2)
                            {
                                minDist2 = d;
                            }
                        }
                    }
                }

                // Border distance: closer to 0 = on cell border = crack line
                return sqrt(minDist2) - sqrt(minDist1);
            }

            // ----------------------------------------------------------------
            // VERTEX SHADER
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
            // FRAGMENT SHADER
            // ----------------------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // ============================================================
                // BASE STONE VARIATION (tri-planar FBM)
                // ============================================================
                float3 stonePos = IN.positionWS * _StoneScale * 0.1;

                float stoneX = StoneNoise(stonePos.yzx);
                float stoneY = StoneNoise(stonePos.xzy);
                float stoneZ = StoneNoise(stonePos.xyz);

                float3 blendW = abs(normalWS);
                blendW = blendW / (blendW.x + blendW.y + blendW.z);

                float stoneNoise = stoneX * blendW.x + stoneY * blendW.y + stoneZ * blendW.z;
                stoneNoise = saturate(pow(stoneNoise, _StoneContrast));
                float stoneModulation = lerp(1.0 - _StoneIntensity, 1.0 + _StoneIntensity * 0.3, stoneNoise);

                // ============================================================
                // FINE ROUGHNESS (additional small-scale FBM with normal perturbation)
                // ============================================================
                float3 roughPos = IN.positionWS * _RoughnessScale * 0.1;

                // Tri-planar roughness with gradients
                float roughValueX, roughValueY, roughValueZ;
                float3 roughGradX, roughGradY, roughGradZ;
                RoughnessNoise(roughPos.yzx, roughValueX, roughGradX);
                RoughnessNoise(roughPos.xzy, roughValueY, roughGradY);
                RoughnessNoise(roughPos.xyz, roughValueZ, roughGradZ);

                float roughNoise = roughValueX * blendW.x + roughValueY * blendW.y + roughValueZ * blendW.z;
                float roughModulation = lerp(1.0 - _RoughnessIntensity, 1.0 + _RoughnessIntensity * 0.3, roughNoise);

                // Blend gradients and convert to world-space normal perturbation
                float3 roughGrad = roughGradX.zxy * blendW.x + roughGradY.xzy * blendW.y + roughGradZ * blendW.z;
                normalWS = normalize(normalWS + roughGrad * _RoughnessNormalStrength);

                // ============================================================
                // CRACKS (Voronoi cell boundaries)
                // ============================================================
                float3 crackPos = IN.positionWS * _CracksScale * 0.1;

                // Tri-planar voronoi
                float crackX = VoronoiCracks(crackPos.yzx);
                float crackY = VoronoiCracks(crackPos.xzy);
                float crackZ = VoronoiCracks(crackPos.xyz);
                float voronoiBorder = crackX * blendW.x + crackY * blendW.y + crackZ * blendW.z;

                // voronoiBorder is small (close to 0) at cell borders.
                // We want a thin band where it's near 0 — convert to crack mask.
                float crackMask = 1.0 - smoothstep(0.0, _CracksWidth, voronoiBorder);
                crackMask = pow(crackMask, _CracksContrast);
                crackMask *= _CracksIntensity;

                // Cracks also perturb normal (make them look like grooves)
                // Push normal away from crack direction
                float crackNormalStrength = crackMask * 0.5;
                float3 crackNormalOffset = float3(
                    ValueNoise3D(crackPos * 2.0) - 0.5,
                    0.0,
                    ValueNoise3D(crackPos.zyx * 2.0) - 0.5
                ) * crackNormalStrength;
                normalWS = normalize(normalWS + crackNormalOffset);

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
                // Base albedo: base color * stone variation * roughness variation
                float3 albedo = _BaseColor.rgb * stoneModulation * roughModulation;

                // Apply cracks: lerp from albedo toward dark crack color where crack mask is high
                albedo = lerp(albedo, _CracksColor.rgb, crackMask);

                // Lighting
                float3 lit = albedo * (mainLightColor + ambient);
                lit += specular * mainLight.color;
                lit += rimLight;
                lit += _EmissionColor.rgb;

                // Fog
                lit = MixFog(lit, IN.fogCoord);

                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        // ====================================================================
        // SHADOW CASTER PASS
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
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;
                float _StoneScale;
                float _StoneIntensity;
                float _StoneContrast;
                float _CracksScale;
                float _CracksIntensity;
                float _CracksWidth;
                float _CracksContrast;
                float4 _CracksColor;
                float _RoughnessScale;
                float _RoughnessIntensity;
                float _RoughnessNormalStrength;
                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;
                float4 _EmissionColor;
            CBUFFER_END

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

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
        // DEPTH ONLY PASS
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
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;
                float _StoneScale;
                float _StoneIntensity;
                float _StoneContrast;
                float _CracksScale;
                float _CracksIntensity;
                float _CracksWidth;
                float _CracksContrast;
                float4 _CracksColor;
                float _RoughnessScale;
                float _RoughnessIntensity;
                float _RoughnessNormalStrength;
                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;
                float4 _EmissionColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

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
        // DEPTH NORMALS PASS
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
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;
                float _StoneScale;
                float _StoneIntensity;
                float _StoneContrast;
                float _CracksScale;
                float _CracksIntensity;
                float _CracksWidth;
                float _CracksContrast;
                float4 _CracksColor;
                float _RoughnessScale;
                float _RoughnessIntensity;
                float _RoughnessNormalStrength;
                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;
                float4 _EmissionColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

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
