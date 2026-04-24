Shader "Custom/SG_BlockStylized"
{
    Properties
    {
        [Header(Base Surface)]
        _BaseColor ("Base Color", Color) = (0.29, 0.565, 0.761, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.35
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        
        [Header(Stone Surface)]
        _StoneScale ("Stone Noise Scale", Range(0.5, 20)) = 6.0
        _StoneIntensity ("Stone Intensity", Range(0, 1)) = 0.45
        _StoneContrast ("Stone Contrast", Range(0.5, 3)) = 1.8
        
        [Header(Rim Light)]
        _RimColor ("Rim Color (HDR)", Color) = (0.498, 0.831, 0.910, 1)
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 0.4
        _RimPower ("Rim Power", Range(0.5, 10)) = 3.0

        [Header(Emission via MaterialPropertyBlock)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)
        
        [Header(Edge Highlight)]
        _EdgeColor ("Edge Highlight Color", Color) = (1, 1, 1, 1)
        _EdgeIntensity ("Edge Intensity", Range(0, 2)) = 0.6
        _EdgeThreshold ("Edge Threshold", Range(0.001, 0.05)) = 0.015
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry" 
        }
        Pass
        {
            Name "ForwardLit"
            Tags {"LitMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

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

                float4 _EdgeColor;
                float _EdgeIntensity;
                float _EdgeThreshold;

                float4 _RimColor;
                float _RimIntensity;
                float _RimPower;

                float4 _EmissionColor;
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
                float3 positionOS  : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
             // ----------------------------------------------------------------
            // PROCEDURAL NOISE FUNCTIONS
            // ----------------------------------------------------------------

            // Pseudo-random hash — deterministic per 3D position
            float Hash3D(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // 3D value noise — smooth interpolation between random values at unit cube corners
            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep easing

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
            
            // FBM (Fractal Brownian Motion) — layered noise for stone-like texture
            float StoneNoise(float3 p)
            {
                float n = 0.0;
                float amp = 0.5;
                float freq = 1.0;

                // 3 octaves of noise — balanced quality/performance
                for (int i = 0; i < 3; i++)
                {
                    n += ValueNoise3D(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return n;
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
                OUT.positionOS  = IN.positionOS.xyz;

                return OUT;
            }
            
            // ----------------------------------------------------------------
            // FRAGMENT SHADER
            // ----------------------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Normalize interpolated vectors — critical!
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // ============================================================
                // STONE SURFACE — procedural tri-planar noise
                // ============================================================
                float3 noisePos = IN.positionWS * _StoneScale;

                float noiseX = StoneNoise(noisePos.yzx);
                float noiseY = StoneNoise(noisePos.xzy);
                float noiseZ = StoneNoise(noisePos.xyz);
                
                // Tri-planar blend weights from normal
                float3 blendWeights = abs(normalWS);
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

                float stoneNoise = noiseX * blendWeights.x + noiseY * blendWeights.y + noiseZ * blendWeights.z;

                // Apply contrast for more pronounced "stone spots"
                stoneNoise = saturate(pow(stoneNoise, _StoneContrast));

                // Remap noise from [0,1] to [1-intensity, 1+intensity*0.3] to darken/lighten base
                float stoneModulation = lerp(1.0 - _StoneIntensity, 1.0 + _StoneIntensity * 0.3, stoneNoise);
                
                // ============================================================
                // EDGE HIGHLIGHT — detect edges using object space position
                // ============================================================
                // For a unit cube centered at origin, edges are where position is near ±0.5
                // We check distance from edge on each axis perpendicular to the face normal

                float3 absPos = abs(IN.positionOS);
                float3 absNormal = abs(normalWS);

                // Find which axes are "along the face" (perpendicular to normal)
                // For those axes, check if we're near the edge (close to 0.5)
                float edgeDist = 1.0;

                // X-facing face: check Y and Z edges
                if (absNormal.x > 0.5)
                {
                    edgeDist = min(0.5 - absPos.y, 0.5 - absPos.z);
                }
                // Y-facing face: check X and Z edges
                else if (absNormal.y > 0.5)
                {
                    edgeDist = min(0.5 - absPos.x, 0.5 - absPos.z);
                }
                // Z-facing face: check X and Y edges
                else
                {
                    edgeDist = min(0.5 - absPos.x, 0.5 - absPos.y);
                }

                // Convert distance to edge factor (closer to edge = stronger highlight)
                float edgeFactor = 1.0 - smoothstep(0.0, _EdgeThreshold, edgeDist);
                edgeFactor *= _EdgeIntensity;

                float3 edgeHighlight = _EdgeColor.rgb * edgeFactor;

                // ============================================================
                // LIGHTING — URP main light + shadows + ambient
                // ============================================================
                Light mainLight = GetMainLight(IN.shadowCoord, IN.positionWS, half4(1,1,1,1));

                // Diffuse (Lambert) from main light
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 mainLightColor = mainLight.color * NdotL * mainLight.shadowAttenuation;

                // Ambient from SH (our Gradient ambient settings)
                float3 ambient = SampleSH(normalWS);

                // Simple stylized specular (non-PBR, but consistent)
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularPower = exp2(_Smoothness * 10 + 1);
                float specular = pow(NdotH, specularPower) * _Smoothness * NdotL * mainLight.shadowAttenuation;

                // ============================================================
                // COMPOSITE
                // ============================================================
                float3 albedo = _BaseColor.rgb * stoneModulation;
                float3 lit = albedo * (mainLightColor + ambient);
                lit += specular * mainLight.color;
                lit += edgeHighlight;
                lit += _EmissionColor.rgb;

                return half4(lit, 1.0);
            }
            
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/Lit"
}