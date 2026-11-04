// PAX-A18 W3 (2026-10-17): the room's stone shadow, read from its StoneShade map (Parallax.Presentation.StoneShade) at the
// fragment's world position, so every piece of stone at one place takes the same shadow: red, the face shadow under a block's
// lit lip; green, the outside masonry's. Each is a colour laid over the albedo at its strength (as the shade quads were);
// with no StoneShade in the scene both strengths are zero and nothing changes.
#ifndef PARALLAX_STONE_SHADE
#define PARALLAX_STONE_SHADE

TEXTURE2D(_PaxShadeMap);
SAMPLER(sampler_PaxShadeMap);
float4 _PaxShadeRect;      // xy: the map's world minimum, zw: one over its world size
half4 _PaxFaceShade;       // rgb: the face shadow's colour, a: its full strength
half4 _PaxOutsideShade;    // rgb: the outside's shade colour, a: its full strength
half4 _PaxStoneLight;      // rgb: the level's light on all its stone and plants (D-304), a: 1 where a level sets it, 0 leaves them as painted
half _PaxShadeMul;         // 1: both colours multiply the stone, so its seams stay (D-301); 0: the stone blends to them

float2 PaxShadeUV(float3 positionOS)
{
    return (TransformObjectToWorld(positionOS).xy - _PaxShadeRect.xy) * _PaxShadeRect.zw;
}

half3 PaxShade(half3 albedo, float2 shadeUV)
{
    albedo = lerp(albedo, albedo * _PaxStoneLight.rgb, _PaxStoneLight.a);
    half2 s = SAMPLE_TEXTURE2D(_PaxShadeMap, sampler_PaxShadeMap, shadeUV).rg;
    albedo = lerp(albedo, lerp(_PaxFaceShade.rgb, albedo * _PaxFaceShade.rgb, _PaxShadeMul), s.r * _PaxFaceShade.a);
    return lerp(albedo, lerp(_PaxOutsideShade.rgb, albedo * _PaxOutsideShade.rgb, _PaxShadeMul), s.g * _PaxOutsideShade.a);
}

#endif
