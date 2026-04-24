// ============================================================================
// SG_SeaBedSand.shader
// ----------------------------------------------------------------------------
// Stylized procedural sand shader for 3D Tetris AR project (Atlantis shallow
// waters). Designed for sea bed floor plane. Completely procedural — no
// textures required.
//
// Features:
//   - FBM procedural sand variation (grain/color variation via noise).
//   - Directional sand ripples (sine-based wave pattern, not random noise).
//   - Edge highlight via fwidth (vestigial — floor usually has no edges).
//   - Rim lighting (Fresnel) for subtle edge glow under water light.
//   - Emission support via MaterialPropertyBlock (for VFX moments).
//   - GPU Instancing enabled.
//   - URP Lit lighting with stylized simplified shading.
//
// Caustics are NOT part of this shader by design. Caustics will be a separate
// system (Decal Projector or Light Cookie) affecting all underwater surfaces
// uniformly. See Style Guide > Caustics Architecture.
//
// Compatibility: Unity 6 + URP 17+ + Forward+ rendering path.
// Author: Created for 3D Tetris AR portfolio project (v2.0).
// ============================================================================

Shader "Custom/SG_SeaBedSand"
{
    Properties
    {
        [Header(Base Surface)]
        _BaseColor ("Base Color", Color) = (0.91, 0.835, 0.659, 1) // #E8D5A8 warm sand
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        [Header(Sand Variation FBM)]
        _SandScale ("Sand Noise Scale", Range(0.5, 30)) = 8.0
        _SandIntensity ("Sand Intensity", Range(0, 1)) = 0.25
        _SandContrast ("Sand Contrast", Range(0.5, 3)) = 1.3

        [Header(Sand Grain Roughness)]
        _GrainScale ("Grain Scale", Range(10, 200)) = 80.0
        _GrainIntensity ("Grain Color Intensity", Range(0, 1)) = 0.15
        _GrainNormalStrength ("Grain Bump Strength", Range(0, 1)) = 0.3

        [Header(Sand Ripples)]
        _RippleScale ("Ripple Scale", Range(1, 30)) = 12.0
        _RippleIntensity ("Ripple Intensity", Range(0, 1)) = 0.35
        _RippleSharpness ("Ripple Sharpness", Range(0.5, 5)) = 2.0
        _RippleDirection ("Ripple Direction", Vector) = (1, 0.3, 0, 0)

        [Header(Edge Highlight)]
        _EdgeColor ("Edge Highlight Color", Color) = (1, 1, 1, 1)
        _EdgeIntensity ("Edge Intensity", Range(0, 2)) = 0.0
        _EdgeThreshold ("Edge Threshold", Range(0.001, 0.05)) = 0.015

        [Header(Rim Light)]
        _RimColor ("Rim Color (HDR)", Color) = (0.961, 0.929, 0.839, 1) // #F5EDD6 warm sunlight
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 0.2
        _RimPower ("Rim Power", Range(0.5, 10)) = 3.0

        [Header(Emission via MaterialPropertyBlock)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)

        [Header(Distance Fade)]
        _ViewRadius ("View Radius", Range(1, 100)) = 20.0
        _GradientWidth ("Gradient Width", Range(1, 50)) = 10.0
        _MinAlpha ("Minimum Alpha", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        LOD 200

        // ====================================================================
        // FORWARD LIT PASS — main lighting + shadows + ambient
        // ====================================================================
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // ----------------------------------------------------------------
            // PROPERTIES — must match Properties block above
            // ----------------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;

                float _SandScale;
                float _SandIntensity;
                float _SandContrast;

                float _GrainScale;
                float _GrainIntensity;
                float _GrainNormalStrength;

                float _RippleScale;
                float _RippleIntensity;
                float _RippleSharpness;
                float4 _RippleDirection;

                float4 _EdgeColor;
                float _EdgeIntensity;
                float _EdgeThreshold;

                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;

                float4 _EmissionColor;

                float _ViewRadius;
                float _GradientWidth;
                float _MinAlpha;
            CBUFFER_END

            // ----------------------------------------------------------------
            // STRUCTS — data flowing between shader stages
            // ----------------------------------------------------------------
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

            // Hash for 2D coords — deterministic, returns [0, 1]
            float Hash2D(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            // 2D value noise — smooth interpolation between 4 corners
            float ValueNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep easing

                float n00 = Hash2D(i + float2(0, 0));
                float n10 = Hash2D(i + float2(1, 0));
                float n01 = Hash2D(i + float2(0, 1));
                float n11 = Hash2D(i + float2(1, 1));

                float nx0 = lerp(n00, n10, f.x);
                float nx1 = lerp(n01, n11, f.x);
                return lerp(nx0, nx1, f.y);
            }

            // FBM (Fractal Brownian Motion) — layered noise for sand grain variation
            float SandNoise(float2 p)
            {
                float n = 0.0;
                float amp = 0.5;
                float freq = 1.0;

                // 3 octaves — balanced quality/performance
                for (int i = 0; i < 3; i++)
                {
                    n += ValueNoise2D(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return n;
            }

            // High-frequency grain noise for sand particle look
            // Returns both value and gradient for normal perturbation
            void SandGrain(float2 p, out float value, out float2 gradient)
            {
                // Use Voronoi-like cellular noise for sand grains
                float2 i = floor(p);
                float2 f = frac(p);

                float minDist = 1.0;
                float2 closestOffset = float2(0, 0);

                // Check 3x3 neighborhood for closest point
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        // Random point position within cell
                        float2 pointPos = Hash2D(i + neighbor) * 0.8 + 0.1;
                        float2 diff = neighbor + pointPos - f;
                        float dist = length(diff);

                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestOffset = diff;
                        }
                    }
                }

                // Value is based on distance to nearest grain center
                value = minDist;

                // Gradient points towards grain center (for normal perturbation)
                gradient = normalize(closestOffset + 0.001) * (1.0 - minDist);
            }

            // Sand ripples — directional sine pattern with noise distortion
            // Creates realistic "dune-like" wave pattern, not random grain
            float SandRipples(float2 p, float2 direction, float scale, float sharpness)
            {
                // Normalize direction vector
                float2 dir = normalize(direction);

                // Project position onto direction (gives "distance along ripple axis")
                float t = dot(p, dir) * scale;

                // Add low-frequency noise distortion to ripple line
                // (so ripples curve slightly, not perfectly parallel)
                float distortion = ValueNoise2D(p * scale * 0.3) * 2.0 - 1.0;
                t += distortion * 0.8;

                // Sine gives smooth wave [-1, 1], remap to [0, 1]
                float wave = sin(t) * 0.5 + 0.5;

                // Sharpen wave peaks — higher sharpness = narrower crests
                wave = pow(wave, sharpness);

                return wave;
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

                // Normalize interpolated vectors
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // ============================================================
                // SAND VARIATION (FBM) — subtle color variation across surface
                // ============================================================
                // Use world-space XZ coords for horizontal floor plane
                float2 sandPos = IN.positionWS.xz * _SandScale * 0.1;
                float sandNoise = SandNoise(sandPos);

                // Apply contrast for more pronounced grain
                sandNoise = saturate(pow(sandNoise, _SandContrast));

                // Modulate base color — asymmetric (more darkening than brightening)
                float sandModulation = lerp(1.0 - _SandIntensity, 1.0 + _SandIntensity * 0.3, sandNoise);

                // ============================================================
                // SAND GRAIN ROUGHNESS — fine particle texture
                // ============================================================
                float2 grainPos = IN.positionWS.xz * _GrainScale * 0.1;
                float grainValue;
                float2 grainGradient;
                SandGrain(grainPos, grainValue, grainGradient);

                // Color variation from grain (small bright/dark spots)
                float grainColorMod = lerp(1.0 - _GrainIntensity * 0.5, 1.0 + _GrainIntensity * 0.3, grainValue);

                // Perturb normal for roughness feel (creates micro-shadows)
                float3 grainNormalOffset = float3(grainGradient.x, 0, grainGradient.y) * _GrainNormalStrength;
                normalWS = normalize(normalWS + grainNormalOffset);

                // ============================================================
                // SAND RIPPLES — directional wave pattern like real sea bed
                // ============================================================
                float2 ripplePos = IN.positionWS.xz * 0.1;
                float rippleValue = SandRipples(ripplePos, _RippleDirection.xy, _RippleScale, _RippleSharpness);

                // Ripples create light and dark stripes
                // Remap [0, 1] to [1 - intensity*0.5, 1 + intensity*0.2]
                // (more darkening in troughs, slight brightening on crests)
                float rippleModulation = lerp(1.0 - _RippleIntensity * 0.5, 1.0 + _RippleIntensity * 0.2, rippleValue);

                // ============================================================
                // EDGE HIGHLIGHT — detect sharp geometry edges via fwidth
                // (vestigial for floor — usually no edges, but kept for consistency)
                // ============================================================
                float3 normalDerivative = fwidth(normalWS);
                float edgeFactor = length(normalDerivative);
                edgeFactor = smoothstep(_EdgeThreshold, _EdgeThreshold * 3.0, edgeFactor);
                edgeFactor *= _EdgeIntensity;
                float3 edgeHighlight = _EdgeColor.rgb * edgeFactor;

                // ============================================================
                // LIGHTING — URP main light + shadows + ambient
                // ============================================================
                Light mainLight = GetMainLight(IN.shadowCoord, IN.positionWS, half4(1, 1, 1, 1));

                // Diffuse (Lambert) from main light
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 mainLightColor = mainLight.color * NdotL * mainLight.shadowAttenuation;

                // Ambient from SH (spherical harmonics — our Gradient ambient)
                float3 ambient = SampleSH(normalWS);

                // Simple stylized specular (Blinn-Phong)
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularPower = exp2(_Smoothness * 10 + 1);
                float specular = pow(NdotH, specularPower) * _Smoothness * NdotL * mainLight.shadowAttenuation;

                // ============================================================
                // RIM LIGHT (Fresnel) — subtle warm glow on surface edges
                // ============================================================
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _RimPower);
                float3 rimLight = _RimColor.rgb * fresnel * _RimIntensity;

                // ============================================================
                // COMPOSITE
                // ============================================================
                // Combine all sand modulations (multiplicative)
                float totalModulation = sandModulation * rippleModulation * grainColorMod;
                float3 albedo = _BaseColor.rgb * totalModulation;

                // Apply lighting
                float3 lit = albedo * (mainLightColor + ambient);
                lit += specular * mainLight.color;
                lit += edgeHighlight;
                lit += rimLight;
                lit += _EmissionColor.rgb;

                // Apply fog
                lit = MixFog(lit, IN.fogCoord);

                // ============================================================
                // DISTANCE FADE — radial alpha falloff from camera
                // ============================================================
                float distanceToCamera = length(IN.positionWS - _WorldSpaceCameraPos);
                // Fade starts at _ViewRadius, ends at _ViewRadius + _GradientWidth
                float fadeStart = _ViewRadius;
                float fadeEnd = _ViewRadius + _GradientWidth;
                float fadeAlpha = 1.0 - saturate((distanceToCamera - fadeStart) / _GradientWidth);
                fadeAlpha = lerp(_MinAlpha, 1.0, fadeAlpha);

                return half4(lit, fadeAlpha);
            }
            ENDHLSL
        }

        // ====================================================================
        // SHADOW CASTER PASS — for main light shadows
        // ====================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

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
                float _SandScale;
                float _SandIntensity;
                float _SandContrast;
                float _GrainScale;
                float _GrainIntensity;
                float _GrainNormalStrength;
                float _RippleScale;
                float _RippleIntensity;
                float _RippleSharpness;
                float4 _RippleDirection;
                float4 _EdgeColor;
                float _EdgeIntensity;
                float _EdgeThreshold;
                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;
                float4 _EmissionColor;
                float _ViewRadius;
                float _GradientWidth;
                float _MinAlpha;
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
        // DEPTH ONLY PASS — for depth texture (post-processing, SSAO, etc.)
        // ====================================================================
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

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
                float _SandScale;
                float _SandIntensity;
                float _SandContrast;
                float _GrainScale;
                float _GrainIntensity;
                float _GrainNormalStrength;
                float _RippleScale;
                float _RippleIntensity;
                float _RippleSharpness;
                float4 _RippleDirection;
                float4 _EdgeColor;
                float _EdgeIntensity;
                float _EdgeThreshold;
                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;
                float4 _EmissionColor;
                float _ViewRadius;
                float _GradientWidth;
                float _MinAlpha;
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
        // DEPTH NORMALS PASS — for SSAO and other normal-aware effects
        // ====================================================================
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

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
                float _SandScale;
                float _SandIntensity;
                float _SandContrast;
                float _GrainScale;
                float _GrainIntensity;
                float _GrainNormalStrength;
                float _RippleScale;
                float _RippleIntensity;
                float _RippleSharpness;
                float4 _RippleDirection;
                float4 _EdgeColor;
                float _EdgeIntensity;
                float _EdgeThreshold;
                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;
                float4 _EmissionColor;
                float _ViewRadius;
                float _GradientWidth;
                float _MinAlpha;
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
