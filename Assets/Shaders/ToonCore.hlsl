// Shared lighting for the Clocking Out toon shaders (URP 14, WebGL2-safe: no compute, no texture arrays).
// Per-level look comes from a handful of globals that GameRoot.ApplyLighting sets (linear colors):
//   _CO_ShadowTint   rgb = colour the shaded side is multiplied by (cool violet, never black)
//   _CO_RimColor     rgb = rim light colour, a = global rim strength
//   _CO_FogColor     rgb = fog / horizon colour, a = max fog amount
//   _CO_FogParams    x = fog start distance, y = fog end distance, z = height fog top (world y), w = height fog range
//   _CO_OutlineTint  rgb = level "dark" colour, a = how much the outline leans toward it (vs. the darkened albedo)
//   _CO_OutlinePx    outline width in pixels at 720p (scales with the window height)
#ifndef CO_TOON_CORE_INCLUDED
#define CO_TOON_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float4 _CO_ShadowTint;
float4 _CO_RimColor;
float4 _CO_FogColor;
float4 _CO_FogParams;
float4 _CO_OutlineTint;
float _CO_OutlinePx;

// 3 bands from the main light: lit / half / shade. Cast shadows push lit surfaces into "shade".
half ToonRamp(half ndl, half shadow)
{
    half lit = smoothstep(0.30h, 0.36h, ndl);
    half mid = smoothstep(-0.06h, 0.0h, ndl);
    half s = smoothstep(0.35h, 0.65h, shadow);
    return (mid * 0.45h + lit * 0.55h) * s;
}

half3 ToonFog(half3 col, float3 positionWS)
{
    float dist = distance(positionWS, _WorldSpaceCameraPos.xyz);
    float range = max(0.001, _CO_FogParams.y - _CO_FogParams.x);
    float f = saturate((dist - _CO_FogParams.x) / range);
    float h = saturate((_CO_FogParams.z - positionWS.y) / max(0.001, _CO_FogParams.w));
    f = max(f * f, h) * _CO_FogColor.a;
    return lerp(col, _CO_FogColor.rgb, f);
}

half3 ToonLighting(half3 albedo, float3 positionWS, half3 normalWS, half3 viewWS, float4 shadowCoord, half rimAmount)
{
    Light mainLight = GetMainLight(shadowCoord);
    half ndl = dot(normalWS, mainLight.direction);
    half ramp = ToonRamp(ndl, mainLight.shadowAttenuation);
    half3 shade = albedo * _CO_ShadowTint.rgb;
    half3 lit = albedo * mainLight.color;
    half3 col = lerp(shade, max(lit, shade), ramp);

#if defined(_ADDITIONAL_LIGHTS)
    uint count = GetAdditionalLightsCount();
    for (uint i = 0u; i < count; ++i)
    {
        Light l = GetAdditionalLight(i, positionWS);
        half a = l.distanceAttenuation * saturate(dot(normalWS, l.direction) * 0.8h + 0.2h);
        // two soft steps keep lamp pools readable without banding into rings
        half k = smoothstep(0.02h, 0.10h, a) * 0.55h + saturate(a) * 0.45h;
        col += albedo * l.color * k * 0.6h;
    }
#endif

    half fres = 1.0h - saturate(dot(normalWS, viewWS));
    half rim = smoothstep(0.58h, 0.78h, fres) * _CO_RimColor.a * rimAmount;
    // rim mostly on the side facing the light, a little everywhere so silhouettes pop on dark skies
    rim *= 0.35h + 0.65h * saturate(ndl * 0.5h + 0.5h);
    col += _CO_RimColor.rgb * rim;
    return col;
}

// Outline helpers: rotation-only object->world for a direction (non-uniform scale would squash the shell).
float3 RotateObjectDir(float3 dirOS)
{
    float4x4 m = GetObjectToWorldMatrix();
    float3 c0 = float3(m._m00, m._m10, m._m20);
    float3 c1 = float3(m._m01, m._m11, m._m21);
    float3 c2 = float3(m._m02, m._m12, m._m22);
    float3 s = float3(length(c0), length(c1), length(c2));
    s = max(s, 1e-5);
    return normalize(c0 * (dirOS.x / s.x) + c1 * (dirOS.y / s.y) + c2 * (dirOS.z / s.z));
}

// World units per pixel (at 720p) at this world position: keeps the outline width roughly constant on screen.
float WorldPerPixel720(float3 positionWS)
{
    float dist = distance(positionWS, _WorldSpaceCameraPos.xyz);
    float p11 = abs(UNITY_MATRIX_P._m11);
    if (unity_OrthoParams.w > 0.5) return 2.0 * unity_OrthoParams.y / 720.0;
    return 2.0 * dist / (max(p11, 1e-4) * 720.0);
}

#endif
