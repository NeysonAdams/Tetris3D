// ============================================================================
// WaterDistortion.shader
// ----------------------------------------------------------------------------
// Screen-space ripple distortion post-processing shader. Distorts the camera
// color buffer to simulate water waves emanating from world-space epicenters.
//
// Supports up to 4 simultaneous active waves (e.g. piece move + line clear
// can overlap without canceling each other).
//
// Each wave is parameterized by:
//   - origin (world position, projected to screen UV in C# before pass)
//   - elapsed time since wave start
//   - amplitude (current strength)
//   - speed (propagation speed across screen)
//   - wavelength (distance between crests)
//   - direction bias (Vector2: 0,0 = radial, otherwise directional)
//
// Wave function (per active wave):
//   distance = length(uv - origin)
//   phase = distance / wavelength - time * speed
//   wave_offset = sin(phase * 2π) * amplitude * decay(distance, time)
//   uv += wave_offset * direction_modulator
//
// Compatibility: Unity 6 + URP 17+ + Forward+.
// ============================================================================

Shader "Hidden/WaterDistortion"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    // Up to 4 simultaneous waves. Inactive waves have amplitude = 0.
    // Each wave packs parameters tightly to minimize uniform count.
    //
    // _WaveParamsA[i]: xy = origin UV, z = amplitude, w = elapsed time
    // _WaveParamsB[i]: x = speed, y = wavelength, zw = direction bias (xy)
    #define MAX_WAVES 4

    float4 _WaveParamsA[MAX_WAVES];
    float4 _WaveParamsB[MAX_WAVES];

    // Apply one wave to UV. Returns offset to add.
    float2 ApplyWave(float2 uv, float aspectRatio, int idx)
    {
        float4 paramsA = _WaveParamsA[idx];
        float4 paramsB = _WaveParamsB[idx];

        float amplitude = paramsA.z;
        if (amplitude <= 0.0001) return float2(0, 0);

        float2 origin     = paramsA.xy;
        float elapsedTime = paramsA.w;
        float speed       = paramsB.x;
        float wavelength  = paramsB.y;
        float2 dirBias    = paramsB.zw;

        // Distance from origin to current pixel (aspect-corrected so wave is round)
        float2 delta = uv - origin;
        delta.x *= aspectRatio;
        float dist = length(delta);

        // Wavefront has reached us only after time × speed
        float wavefront = elapsedTime * speed;
        if (dist > wavefront + 0.05) return float2(0, 0); // wave hasn't arrived

        // Phase for sin: distance / wavelength shifted by wavefront travel
        float phase = (dist - wavefront) / max(wavelength, 0.0001);
        float wave = sin(phase * 6.28318);

        // Falloff: attenuate by distance from wavefront (so only crest area distorts)
        float fromCrest = abs(dist - wavefront);
        float crestFalloff = saturate(1.0 - fromCrest / max(wavelength * 2.0, 0.001));
        crestFalloff = smoothstep(0.0, 1.0, crestFalloff);

        // Directional vs radial: if dirBias is non-zero, blend wave direction
        // 1 = pure radial outward, 0 = pure directional along dirBias
        float radialStrength = 1.0 - saturate(length(dirBias));
        float2 radialDir = normalize(delta + 0.0001);
        float2 finalDir = normalize(lerp(dirBias, radialDir, radialStrength) + 0.0001);

        return finalDir * wave * amplitude * crestFalloff;
    }

    half4 FragDistort(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = input.texcoord;

        // Aspect ratio for round waves regardless of viewport shape
        float aspectRatio = _ScreenParams.x / _ScreenParams.y;

        // Accumulate distortion from all active waves
        float2 totalOffset = float2(0, 0);
        UNITY_UNROLL
        for (int i = 0; i < MAX_WAVES; i++)
        {
            totalOffset += ApplyWave(uv, aspectRatio, i);
        }

        // Apply offset to sampling UV
        // Divide x by aspect to undo the aspect correction we did when computing distance
        totalOffset.x /= aspectRatio;
        float2 distortedUV = uv + totalOffset;

        // Sample the camera color at distorted UV
        half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, distortedUV);

        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "WaterDistortion"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDistort
            #pragma target 3.0
            ENDHLSL
        }
    }

    FallBack Off
}
