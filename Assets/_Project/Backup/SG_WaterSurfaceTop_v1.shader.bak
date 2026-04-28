// ============================================================================
// SG_WaterSurfaceTop.shader
// ----------------------------------------------------------------------------
// Realistic animated water surface with multiple wave layers, vertex displacement,
// caustics, fresnel, and visible ripple animation.
// ============================================================================

Shader "Custom/SG_WaterSurfaceTop"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Color", Color) = (0.4, 0.75, 0.85, 1)
        _DeepColor ("Deep Color", Color) = (0.1, 0.35, 0.55, 1)
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.7

        [Header(Wave Layer 1 Large)]
        _WaveScale1 ("Wave Scale", Range(0.1, 5)) = 1.0
        _WaveSpeed1 ("Wave Speed", Range(0, 3)) = 0.4
        _WaveHeight1 ("Wave Height", Range(0, 2)) = 0.3
        _WaveDirection1 ("Wave Direction", Vector) = (1, 0, 0.5, 0)

        [Header(Wave Layer 2 Medium)]
        _WaveScale2 ("Wave Scale", Range(0.5, 10)) = 2.5
        _WaveSpeed2 ("Wave Speed", Range(0, 3)) = 0.7
        _WaveHeight2 ("Wave Height", Range(0, 1)) = 0.15
        _WaveDirection2 ("Wave Direction", Vector) = (-0.7, 0, 1, 0)

        [Header(Wave Layer 3 Small Detail)]
        _WaveScale3 ("Wave Scale", Range(1, 20)) = 6.0
        _WaveSpeed3 ("Wave Speed", Range(0, 5)) = 1.2
        _WaveHeight3 ("Wave Height", Range(0, 0.5)) = 0.05
        _WaveDirection3 ("Wave Direction", Vector) = (0.3, 0, -1, 0)

        [Header(Ripple Detail)]
        _RippleScale ("Ripple Scale", Range(5, 50)) = 15.0
        _RippleSpeed ("Ripple Speed", Range(0, 3)) = 0.8
        _RippleIntensity ("Ripple Intensity", Range(0, 2)) = 0.6

        [Header(Caustics)]
        _CausticsScale ("Caustics Scale", Range(1, 20)) = 8.0
        _CausticsSpeed ("Caustics Speed", Range(0, 3)) = 0.5
        _CausticsIntensity ("Caustics Intensity", Range(0, 3)) = 1.5

        [Header(Fresnel and Specular)]
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 0.8
        _SpecularPower ("Specular Power", Range(10, 500)) = 120.0
        _SpecularIntensity ("Specular Intensity", Range(0, 5)) = 2.0

        [Header(Distance Fade)]
        _FadeStart ("Fade Start Distance", Range(5, 200)) = 30.0
        _FadeEnd ("Fade End Distance", Range(10, 500)) = 120.0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _BaseAlpha;
                float _WaveScale1;
                float _WaveSpeed1;
                float _WaveHeight1;
                float4 _WaveDirection1;
                float _WaveScale2;
                float _WaveSpeed2;
                float _WaveHeight2;
                float4 _WaveDirection2;
                float _WaveScale3;
                float _WaveSpeed3;
                float _WaveHeight3;
                float4 _WaveDirection3;
                float _RippleScale;
                float _RippleSpeed;
                float _RippleIntensity;
                float _CausticsScale;
                float _CausticsSpeed;
                float _CausticsIntensity;
                float _FresnelPower;
                float _FresnelIntensity;
                float _SpecularPower;
                float _SpecularIntensity;
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
                float2 uv          : TEXCOORD3;
                float fogCoord     : TEXCOORD4;
                float waveHeight   : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ----------------------------------------------------------------
            // NOISE FUNCTIONS
            // ----------------------------------------------------------------
            float Hash2D(float2 p)
            {
                p = frac(p * float2(443.8975, 397.2973));
                p += dot(p, p.yx + 19.19);
                return frac((p.x + p.y) * p.x);
            }

            float2 Hash2D2(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float ValueNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n00 = Hash2D(i);
                float n10 = Hash2D(i + float2(1, 0));
                float n01 = Hash2D(i + float2(0, 1));
                float n11 = Hash2D(i + float2(1, 1));

                return lerp(lerp(n00, n10, f.x), lerp(n01, n11, f.x), f.y);
            }

            // Gradient noise for smoother waves
            float GradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float2 ga = Hash2D2(i) * 2.0 - 1.0;
                float2 gb = Hash2D2(i + float2(1, 0)) * 2.0 - 1.0;
                float2 gc = Hash2D2(i + float2(0, 1)) * 2.0 - 1.0;
                float2 gd = Hash2D2(i + float2(1, 1)) * 2.0 - 1.0;

                float va = dot(ga, f);
                float vb = dot(gb, f - float2(1, 0));
                float vc = dot(gc, f - float2(0, 1));
                float vd = dot(gd, f - float2(1, 1));

                return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y) + 0.5;
            }

            // FBM with 4 octaves
            float WaveFBM(float2 p)
            {
                float n = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                float2x2 rot = float2x2(0.8, -0.6, 0.6, 0.8);
                for (int i = 0; i < 4; i++)
                {
                    n += GradientNoise(p * freq) * amp;
                    p = mul(rot, p);
                    freq *= 2.1;
                    amp *= 0.5;
                }
                return n;
            }

            // Gerstner wave function
            float3 GerstnerWave(float2 pos, float2 dir, float steepness, float wavelength, float speed, out float3 tangent, out float3 binormal)
            {
                float k = 6.28318 / wavelength;
                float c = sqrt(9.8 / k);
                float2 d = normalize(dir);
                float f = k * (dot(d, pos) - c * speed * _Time.y);
                float a = steepness / k;

                tangent = float3(
                    -d.x * d.x * steepness * sin(f),
                    d.x * steepness * cos(f),
                    -d.x * d.y * steepness * sin(f)
                );
                binormal = float3(
                    -d.x * d.y * steepness * sin(f),
                    d.y * steepness * cos(f),
                    -d.y * d.y * steepness * sin(f)
                );

                return float3(
                    d.x * a * cos(f),
                    a * sin(f),
                    d.y * a * cos(f)
                );
            }

            // Caustics pattern
            float Caustics(float2 uv)
            {
                float2 p = uv;
                float c = 0.0;
                for (int i = 0; i < 3; i++)
                {
                    float2 offset = float2(
                        sin(_Time.y * _CausticsSpeed * (0.5 + i * 0.2) + p.y * 3.0),
                        cos(_Time.y * _CausticsSpeed * (0.7 + i * 0.15) + p.x * 2.5)
                    ) * 0.3;
                    c += GradientNoise((p + offset) * (1.0 + i * 0.5));
                }
                c = pow(saturate(c * 0.5), 2.0);
                return c;
            }

            // ----------------------------------------------------------------
            // VERTEX — with wave displacement
            // ----------------------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posOS = IN.positionOS.xyz;
                float3 posWS = TransformObjectToWorld(posOS);

                // Calculate wave displacement
                float3 t1, b1, t2, b2, t3, b3;
                float3 wave1 = GerstnerWave(posWS.xz, _WaveDirection1.xz, 0.2, _WaveScale1, _WaveSpeed1, t1, b1) * _WaveHeight1;
                float3 wave2 = GerstnerWave(posWS.xz, _WaveDirection2.xz, 0.15, _WaveScale2, _WaveSpeed2, t2, b2) * _WaveHeight2;
                float3 wave3 = GerstnerWave(posWS.xz, _WaveDirection3.xz, 0.1, _WaveScale3, _WaveSpeed3, t3, b3) * _WaveHeight3;

                float3 waveOffset = wave1 + wave2 + wave3;
                posWS += waveOffset;

                // Calculate normal from wave tangents
                float3 tangent = float3(1, 0, 0) + t1 + t2 + t3;
                float3 binormal = float3(0, 0, 1) + b1 + b2 + b3;
                float3 normalWS = normalize(cross(binormal, tangent));

                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.positionWS = posWS;
                OUT.normalWS = normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(posWS);
                OUT.uv = IN.uv;
                OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);
                OUT.waveHeight = waveOffset.y;

                return OUT;
            }

            // ----------------------------------------------------------------
            // FRAGMENT
            // ----------------------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;

                // ============================================================
                // SURFACE RIPPLES (detail normal perturbation)
                // ============================================================
                float2 rippleUV1 = IN.positionWS.xz * _RippleScale * 0.1;
                float2 rippleUV2 = IN.positionWS.xz * _RippleScale * 0.15;
                float2 offset1 = float2(_Time.y * _RippleSpeed * 0.7, _Time.y * _RippleSpeed * 0.5);
                float2 offset2 = float2(-_Time.y * _RippleSpeed * 0.5, _Time.y * _RippleSpeed * 0.8);

                float ripple1 = WaveFBM(rippleUV1 + offset1);
                float ripple2 = WaveFBM(rippleUV2 + offset2);
                float rippleCombined = (ripple1 + ripple2) * 0.5;

                // Perturb normal with ripples
                float3 rippleNormal = normalWS;
                rippleNormal.xz += (rippleCombined - 0.5) * _RippleIntensity * 0.5;
                rippleNormal = normalize(rippleNormal);

                // ============================================================
                // FRESNEL
                // ============================================================
                float NdotV = saturate(dot(rippleNormal, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;

                // ============================================================
                // SPECULAR HIGHLIGHT
                // ============================================================
                float3 halfDir = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(rippleNormal, halfDir));
                float specular = pow(NdotH, _SpecularPower) * _SpecularIntensity;

                // ============================================================
                // CAUSTICS
                // ============================================================
                float2 causticsUV = IN.positionWS.xz * _CausticsScale * 0.1;
                float caustics = Caustics(causticsUV) * _CausticsIntensity;

                // ============================================================
                // COLOR BLENDING
                // ============================================================
                // Blend between shallow and deep based on wave height and view angle
                float depthFactor = saturate(IN.waveHeight * 2.0 + 0.5 + fresnel * 0.3);
                float3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, depthFactor);

                // Add caustics
                waterColor += caustics * _ShallowColor.rgb * 0.5;

                // Add specular
                waterColor += specular * mainLight.color;

                // Add fresnel rim
                waterColor += fresnel * _ShallowColor.rgb * 0.3;

                // ============================================================
                // ALPHA
                // ============================================================
                float alpha = _BaseAlpha;
                alpha += fresnel * 0.2;
                alpha += rippleCombined * _RippleIntensity * 0.2;

                // Distance fade
                float distToCamera = length(IN.positionWS - _WorldSpaceCameraPos);
                float fadeT = saturate((distToCamera - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));
                fadeT = smoothstep(0.0, 1.0, fadeT);
                alpha *= (1.0 - fadeT);

                alpha = saturate(alpha);

                // Apply fog
                waterColor = MixFog(waterColor, IN.fogCoord);

                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
