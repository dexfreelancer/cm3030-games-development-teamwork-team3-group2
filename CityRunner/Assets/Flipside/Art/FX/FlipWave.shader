Shader "Flipside/FlipWave"
{
    Properties
    {
        _Center ("Center (viewport)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0
        _Aspect ("Aspect", Float) = 1.78
        _Inside ("Inside is orange", Float) = 1
        _Outside ("Outside is orange", Float) = 0
        _Ring ("Ring width", Float) = 0.035
        _Intensity ("Orange intensity", Range(0, 1)) = 0.5
        _Distort ("Ring distortion", Float) = 0.015
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "FlipWave"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag

            float4 _Center;
            float _Radius, _Aspect, _Inside, _Outside, _Ring, _Intensity, _Distort;

            // Warm duotone: shadows go deep plum, mid-tones orange, highlights pale gold.
            float3 Orange(float3 c)
            {
                float l = dot(c, float3(0.299, 0.587, 0.114));
                float3 dark = float3(0.10, 0.02, 0.06);
                float3 mid = float3(0.98, 0.45, 0.07);
                float3 hi = float3(1.0, 0.92, 0.62);
                float3 duo = l < 0.5 ? lerp(dark, mid, l * 2.0) : lerp(mid, hi, (l - 0.5) * 2.0);
                return lerp(c, duo, _Intensity);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                float2 d = uv - _Center.xy;
                d.x *= _Aspect;
                float dist = length(d);

                // Slight refraction on the ring so the wave reads as a shockwave.
                float ringMask = 1.0 - saturate(abs(dist - _Radius) / _Ring);
                float active = step(0.001, _Radius);
                float2 dir = dist > 0.0001 ? d / dist : float2(0, 0);
                dir.x /= _Aspect;
                float2 sampleUv = uv + dir * ringMask * ringMask * _Distort * active;
                float3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(sampleUv)).rgb;

                float inside = 1.0 - smoothstep(_Radius - 0.004, _Radius + 0.004, dist);
                float orange = lerp(_Outside, _Inside, inside);
                float3 col = lerp(c, Orange(c), orange);

                float glow = ringMask * ringMask * active;
                col += float3(1.0, 0.62, 0.22) * glow * 1.1;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
