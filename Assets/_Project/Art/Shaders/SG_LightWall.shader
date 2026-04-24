// ============================================================================
// SG_LightWall.shader
// ----------------------------------------------------------------------------
// Light effect shader - mesh is completely invisible, only light effect visible.
// ============================================================================

Shader "Custom/SG_LightWall"
{
    Properties
    {
        [Header(Light Color and Intensity)]
        [HDR] _LightColor ("Light Color (HDR)", Color) = (0.961, 0.929, 0.839, 1)
        _Intensity ("Master Intensity", Range(0, 5)) = 1.5

        [Header(Vertical Height Fade)]
        _HeightFadeStart ("Fade Start Y (world)", Float) = 0.0
        _HeightFadeEnd ("Fade End Y (world)", Float) = 8.0
        _HeightFadePower ("Fade Power", Range(1, 10)) = 4.0
        _BaseAlphaMax ("Base Alpha", Range(0, 1)) = 0.5

        [Header(View Angle Fade)]
        _ViewFadeAmount ("View Fade Amount", Range(0, 1)) = 0.7
        _ViewFadePower ("View Fade Power", Range(0.5, 5)) = 2.0

        [Header(Mesh Edge Dissolve)]
        _EdgeFade ("Edge Fade (all sides)", Range(0.01, 0.5)) = 0.2

        [Header(Transparency)]
        _BrightnessThreshold ("Brightness Threshold", Range(0, 0.5)) = 0.05

        [Header(Pulse Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.4

        [Header(Vertical Flow Animation)]
        _FlowSpeed ("Flow Speed", Range(0, 10)) = 2.0
        _FlowFreq ("Flow Frequency", Range(0.5, 30)) = 8.0
        _FlowAmount ("Flow Amount", Range(0, 1)) = 0.5
        _FlowSharpness ("Flow Sharpness", Range(1, 5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
        }

        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _LightColor;
                float _Intensity;
                float _HeightFadeStart;
                float _HeightFadeEnd;
                float _HeightFadePower;
                float _BaseAlphaMax;
                float _ViewFadeAmount;
                float _ViewFadePower;
                float _EdgeFade;
                float _BrightnessThreshold;
                float _PulseSpeed;
                float _PulseAmount;
                float _FlowSpeed;
                float _FlowFreq;
                float _FlowAmount;
                float _FlowSharpness;
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

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
                OUT.uv = IN.uv;
                OUT.fogCoord = ComputeFogFactor(positions.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // ============================================================
                // MESH EDGE DISSOLVE — hide all geometry edges
                // ============================================================
                float edgeL = smoothstep(0.0, _EdgeFade, IN.uv.x);
                float edgeR = smoothstep(0.0, _EdgeFade, 1.0 - IN.uv.x);
                float edgeB = smoothstep(0.0, _EdgeFade, IN.uv.y);
                float edgeT = smoothstep(0.0, _EdgeFade, 1.0 - IN.uv.y);
                float meshMask = edgeL * edgeR * edgeB * edgeT;

                // ============================================================
                // HEIGHT FADE
                // ============================================================
                float heightT = saturate(
                    (IN.positionWS.y - _HeightFadeStart) /
                    max(_HeightFadeEnd - _HeightFadeStart, 0.001)
                );
                float heightFade = pow(1.0 - heightT, _HeightFadePower);

                // ============================================================
                // VIEW ANGLE FADE
                // ============================================================
                float NdotV = abs(dot(normalWS, viewDirWS));
                float viewFade = 1.0 - _ViewFadeAmount * pow(NdotV, _ViewFadePower);
                viewFade = saturate(viewFade);

                // ============================================================
                // PULSE ANIMATION
                // ============================================================
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                pulse = pow(pulse, 0.7);
                float pulseModulation = lerp(1.0 - _PulseAmount, 1.0 + _PulseAmount * 0.5, pulse);

                // ============================================================
                // VERTICAL FLOW ANIMATION
                // ============================================================
                float flowWave = sin(IN.uv.y * _FlowFreq * 6.28318 - _Time.y * _FlowSpeed);
                flowWave = flowWave * 0.5 + 0.5;
                flowWave = pow(flowWave, _FlowSharpness);
                float flowModulation = lerp(1.0 - _FlowAmount, 1.0 + _FlowAmount * 0.3, flowWave);

                // ============================================================
                // COMPOSITE — transparent background, only bright light visible
                // ============================================================
                float alpha = meshMask * heightFade * viewFade * _BaseAlphaMax;

                float3 lightColor = _LightColor.rgb * _Intensity * pulseModulation * flowModulation;
                lightColor *= alpha;

                // Calculate brightness
                float brightness = dot(lightColor, float3(0.299, 0.587, 0.114));

                // Below threshold = completely transparent (no background visible)
                float visibilityMask = smoothstep(_BrightnessThreshold * 0.5, _BrightnessThreshold, brightness);

                // Apply mask - dark areas become fully transparent
                float3 finalColor = lightColor * visibilityMask;

                finalColor = MixFog(finalColor, IN.fogCoord);

                return half4(finalColor, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
