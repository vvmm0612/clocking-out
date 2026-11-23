// Full-viewport backdrop drawn first (Background queue): a 3-stop vertical gradient, a soft glow around
// the horizon point, a vignette and a faint paper grain. The mesh is a unit quad whose UVs are written
// straight to clip space, so it always fills the camera's viewport whatever the camera does.
Shader "ClockingOut/Sky"
{
    Properties
    {
        _Top ("Top", Color) = (0.3,0.35,0.5,1)
        _Mid ("Horizon", Color) = (0.9,0.75,0.6,1)
        _Bottom ("Bottom", Color) = (0.2,0.2,0.25,1)
        _MidHeight ("Horizon height (0..1)", Range(0,1)) = 0.4
        _Glow ("Glow colour (a = strength)", Color) = (1,0.9,0.7,0.3)
        _GlowPos ("Glow position (xy, 0..1)", Vector) = (0.6,0.45,0,0)
        _Vignette ("Vignette", Range(0,1)) = 0.35
        _Grain ("Grain", Range(0,0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Background" "PreviewType"="Plane" }

        Pass
        {
            Name "Sky"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Top;
                half4 _Mid;
                half4 _Bottom;
                half _MidHeight;
                half4 _Glow;
                float4 _GlowPos;
                half _Vignette;
                half _Grain;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float2 p = v.uv * 2.0 - 1.0;
                // _ProjectionParams.x is -1 when URP renders with a flipped projection (into some render textures).
                o.positionCS = float4(p.x, p.y * _ProjectionParams.x, UNITY_RAW_FAR_CLIP_VALUE, 1.0);
                o.uv = v.uv;
                return o;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float y = i.uv.y;
                half3 col;
                if (y > _MidHeight) col = lerp(_Mid.rgb, _Top.rgb, smoothstep(0.0, 1.0, (y - _MidHeight) / max(0.001, 1.0 - _MidHeight)));
                else col = lerp(_Bottom.rgb, _Mid.rgb, smoothstep(0.0, 1.0, y / max(0.001, _MidHeight)));
                float2 d = (i.uv - _GlowPos.xy) * float2(1.6, 1.0);
                col += _Glow.rgb * _Glow.a * exp(-dot(d, d) * 6.0);
                float2 c = i.uv - 0.5;
                col *= 1.0 - _Vignette * saturate(dot(c, c) * 1.6);
                col += (Hash(floor(i.positionCS.xy * 0.5)) - 0.5) * _Grain;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
