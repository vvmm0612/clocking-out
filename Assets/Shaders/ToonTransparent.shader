// Alpha-blended cel-shaded surface with optional effects, one shader for all see-through things:
//   water  : _Wave > 0 lifts the top vertices in slow swells, _Stripes adds drifting highlight bands on top faces
//   glass  : low alpha, high _Fresnel (edges more opaque) and a fixed glint band
//   ghost  : _Fresnel + _Flicker (a slow shimmer), rim from the level's rim colour
//   beam   : _Emission 1 (self-lit), _Fresnel fades the edges
// No outline, no shadow casting (the renderers turn shadows off too); receives main-light shadows.
Shader "ClockingOut/ToonTransparent"
{
    Properties
    {
        [MainColor] _BaseColor ("Color (alpha = opacity)", Color) = (1,1,1,0.5)
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        _Color ("Legacy Color (unused)", Color) = (1,1,1,1)
        _Emission ("Self-lit (0..1)", Range(0,1)) = 0
        _Rim ("Rim amount", Range(0,3)) = 1
        _Fresnel ("Edge opacity boost", Range(0,2)) = 0.3
        _Wave ("Swell height (world units)", Range(0,0.3)) = 0
        _Stripes ("Highlight stripes", Range(0,1)) = 0
        _Glint ("Glass glint", Range(0,1)) = 0
        _Flicker ("Ghost shimmer", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "ToonCore.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _Color;
                half _Emission;
                half _Rim;
                half _Fresnel;
                half _Wave;
                half _Stripes;
                half _Glint;
                half _Flicker;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                half3 nWS = TransformObjectToWorldNormal(v.normalOS);
                if (_Wave > 0.0 && v.positionOS.y > 0.2)
                {
                    float t = _Time.y;
                    float s = sin(t * 0.9 + posWS.x * 0.9 + posWS.z * 0.55) * 0.6 + sin(t * 1.4 - posWS.x * 0.5 + posWS.z * 1.3) * 0.4;
                    posWS.y += s * _Wave;
                }
                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = nWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;
                half alpha = tex.a * _BaseColor.a;
                half3 n = normalize(i.normalWS);
                half3 viewWS = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                half3 col = ToonLighting(albedo, i.positionWS, n, viewWS, shadowCoord, _Rim);
                col = lerp(col, albedo, _Emission);

                half fres = 1.0h - saturate(dot(n, viewWS));
                alpha = saturate(alpha + fres * fres * _Fresnel);

                float t = _Time.y;
                if (_Stripes > 0.0)
                {
                    // top faces: drifting bands; sides: a darker, deeper tone
                    half top = saturate(n.y * 2.0h - 1.0h);
                    // thin glints that drift and break up (a slow wobble along each band)
                    float wob = sin(i.positionWS.z * 3.7 + t * 0.9) * 0.35 + sin(i.positionWS.x * 2.9 - t * 0.6) * 0.25;
                    float band = sin(i.positionWS.x * 3.3 + i.positionWS.z * 1.9 + t * 0.7 + wob) * 0.5 + 0.5;
                    float band2 = sin(i.positionWS.x * -1.9 + i.positionWS.z * 3.6 - t * 0.45 + wob) * 0.5 + 0.5;
                    float breakup = smoothstep(0.2, 0.7, sin(i.positionWS.x * 0.9 - i.positionWS.z * 1.4 + t * 0.3) * 0.5 + 0.5);
                    half hi = (smoothstep(0.982h, 0.994h, band) + smoothstep(0.986h, 0.996h, band2) * 0.7h) * breakup;
                    col = lerp(col, half3(1.0h, 0.98h, 0.92h), saturate(hi) * top * _Stripes);
                    alpha = saturate(alpha + hi * top * _Stripes * 0.3h);
                    col *= lerp(0.78h, 1.0h, top);
                }
                if (_Glint > 0.0)
                {
                    float g = sin((i.positionWS.x + i.positionWS.y) * 3.0) * 0.5 + 0.5;
                    half glint = smoothstep(0.93h, 0.97h, g) * _Glint;
                    col += glint;
                    alpha = saturate(alpha + glint * 0.5h);
                }
                if (_Flicker > 0.0)
                {
                    half sh = sin(t * 3.1 + i.positionWS.y * 9.0) * 0.5h + 0.5h;
                    alpha *= lerp(1.0h, 0.7h + 0.3h * sh, _Flicker);
                    col += _CO_RimColor.rgb * fres * _Flicker * 0.6h;
                }
                col = ToonFog(col, i.positionWS);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
