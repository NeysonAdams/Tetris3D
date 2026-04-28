// ============================================================================
// SG_SeaBedSand_Opaque.shader
// ----------------------------------------------------------------------------
// Opaque version of the procedural sand shader. No transparency, supports
// DBuffer decals properly.
//
// Compatibility: Unity 6 + URP 17+ + Forward+ rendering path.
// ============================================================================

Shader "Custom/SG_SeaBedSand_Opaque"
{
    Properties
    {
        [Header(Base Surface)]
        _BaseColor ("Base Color", Color) = (0.91, 0.835, 0.659, 1)
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
        _RimColor ("Rim Color (HDR)", Color) = (0.961, 0.929, 0.839, 1)
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 0.2
        _RimPower ("Rim Power", Range(0.5, 10)) = 3.0

        [Header(Emission)]
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
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On

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
            // Decal support
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

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

            float SandNoise(float2 p)
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

            void SandGrain(float2 p, out float value, out float2 gradient)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float minDist = 1.0;
                float2 closestOffset = float2(0, 0);

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
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

                value = minDist;
                gradient = normalize(closestOffset + 0.001) * (1.0 - minDist);
            }

            float SandRipples(float2 p, float2 direction, float scale, float sharpness)
            {
                float2 dir = normalize(direction);
                float t = dot(p, dir) * scale;
                float distortion = ValueNoise2D(p * scale * 0.3) * 2.0 - 1.0;
                t += distortion * 0.8;
                float wave = sin(t) * 0.5 + 0.5;
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

                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // Sand variation
                float2 sandPos = IN.positionWS.xz * _SandScale * 0.1;
                float sandNoise = SandNoise(sandPos);
                sandNoise = saturate(pow(sandNoise, _SandContrast));
                float sandModulation = lerp(1.0 - _SandIntensity, 1.0 + _SandIntensity * 0.3, sandNoise);

                // Sand grain
                float2 grainPos = IN.positionWS.xz * _GrainScale * 0.1;
                float grainValue;
                float2 grainGradient;
                SandGrain(grainPos, grainValue, grainGradient);
                float grainColorMod = lerp(1.0 - _GrainIntensity * 0.5, 1.0 + _GrainIntensity * 0.3, grainValue);
                float3 grainNormalOffset = float3(grainGradient.x, 0, grainGradient.y) * _GrainNormalStrength;
                normalWS = normalize(normalWS + grainNormalOffset);

                // Sand ripples
                float2 ripplePos = IN.positionWS.xz * 0.1;
                float rippleValue = SandRipples(ripplePos, _RippleDirection.xy, _RippleScale, _RippleSharpness);
                float rippleModulation = lerp(1.0 - _RippleIntensity * 0.5, 1.0 + _RippleIntensity * 0.2, rippleValue);

                // Edge highlight
                float3 normalDerivative = fwidth(normalWS);
                float edgeFactor = length(normalDerivative);
                edgeFactor = smoothstep(_EdgeThreshold, _EdgeThreshold * 3.0, edgeFactor);
                edgeFactor *= _EdgeIntensity;
                float3 edgeHighlight = _EdgeColor.rgb * edgeFactor;

                // Lighting
                Light mainLight = GetMainLight(IN.shadowCoord, IN.positionWS, half4(1, 1, 1, 1));
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 mainLightColor = mainLight.color * NdotL * mainLight.shadowAttenuation;
                float3 ambient = SampleSH(normalWS);

                // Specular
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularPower = exp2(_Smoothness * 10 + 1);
                float specular = pow(NdotH, specularPower) * _Smoothness * NdotL * mainLight.shadowAttenuation;

                // Rim light
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _RimPower);
                float3 rimLight = _RimColor.rgb * fresnel * _RimIntensity;

                // Composite albedo
                float totalModulation = sandModulation * rippleModulation * grainColorMod;
                float3 albedo = _BaseColor.rgb * totalModulation;

                // ============================================================
                // APPLY DECALS (DBuffer)
                // ============================================================
                #if defined(_DBUFFER_MRT1) || defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
                {
                    float2 positionSS = IN.positionCS.xy;
                    FETCH_DBUFFER(DBuffer, _DBufferTexture, int2(positionSS));

                    DecalSurfaceData decalSurfaceData;
                    DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);

                    albedo = albedo * decalSurfaceData.baseColor.a + decalSurfaceData.baseColor.rgb;

                    #if defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
                    normalWS = normalize(normalWS * decalSurfaceData.normalWS.a + decalSurfaceData.normalWS.xyz);
                    #endif
                }
                #endif

                // Final lighting
                float3 lit = albedo * (mainLightColor + ambient);
                lit += specular * mainLight.color;
                lit += edgeHighlight;
                lit += rimLight;
                lit += _EmissionColor.rgb;

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
