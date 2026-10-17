// PAX-A18 W3: the broken stone's edge strips. A copy of Env-Sprite-Lit-WorldUV whose colour comes from the room's fill
// (_FillTex, sampled in world space at the fill's own tile size _FillTile, exactly as the fill quad samples it), times the
// strip's own texture read as a shade (rgb 0.5 = unchanged, so 0..2), with the strip's alpha as the broken outline. The edge
// is cut out of the very stone it borders: no second copy of the stone, no seam.
// Below: PAX-A15: the environment kit's fixed fills and trims. A copy of PAX-A13's Sprite-Lit-WorldTile (itself URP 17's
// Sprite-Lit-Default with the flip-correct normals pass) whose UV comes from the vertex's world position instead of a
// per-renderer rest pose: no MaterialPropertyBlock, so renderers sharing a material and texture batch. _Tile is the
// tile's world size (the sprite's own size); _Axis picks the world-anchored axes: 0 both (fills), 1 across (caps,
// undersides, slabs, water: u from world x, v from the sprite), 2 down (wall faces, posts: u from the sprite, v from world
// y). At rest it samples exactly what Sprite-Lit-WorldTile samples for the same tile, so a disguised trap (which keeps
// that shader and its rest pose, so its pixels travel with it) matches its host pixel for pixel (P10).
Shader "Parallax/2D/Env-Sprite-Lit-EdgeMask"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0
        _Tile("World tile size (xy)", Vector) = (1,1,0,0)
        _Axis("World axes: 0 both, 1 across (v from the sprite), 2 down (u from the sprite), 3 none (the sprite's UV)", Float) = 0
        _FillTex("Fill (world tiled)", 2D) = "white" {}
        _FillTile("Fill world tile size (xy)", Vector) = (1,1,0,0)
        _FillNormal("Fill normal map (world tiled)", 2D) = "bump" {}

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
                float2 fillUV      : TEXCOORD5;
                float2 shadeUV     : TEXCOORD6;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
            #include "Assets/_Game/Art/Shaders/StoneShade.hlsl"

            TEXTURE2D(_FillTex);
            SAMPLER(sampler_FillTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Tile;
                float _Axis;
                float4 _FillTile;
            CBUFFER_END

            // PAX-A15: world-anchored along the tiling axis (the texture's Repeat mode wraps it), the sprite's own UV across.
            // PAX-A16: _Axis 3 is the sprite's own UV (dressing, chains, fringes: plain sprites on the same shader as the trims, so
            // the whole static play layer batches without a shader switch).
            float2 WorldUV(float3 positionOS, float2 spriteUV)
            {
                if (_Axis > 2.5) return spriteUV;
                float2 w = TransformObjectToWorld(positionOS).xy / _Tile.xy;
                return float2(_Axis > 1.5 ? spriteUV.x : w.x, (_Axis > 0.5 && _Axis < 1.5) ? spriteUV.y : w.y);
            }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);
                o.uv = WorldUV(input.positionOS, input.uv);
                o.color = input.color * _Color * unity_SpriteColor;
                o.fillUV = TransformObjectToWorld(input.positionOS).xy / _FillTile.xy;
                o.shadeUV = PaxShadeUV(input.positionOS);

                return o;
            }

            // CommonLitFragment (URP 17, Lit2DCommon.hlsl) with the albedo from the fill, shaded by the strip.
            half4 LitFragment(Varyings input) : SV_Target
            {
                const half4 edge = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                const half4 fill = SAMPLE_TEXTURE2D(_FillTex, sampler_FillTex, input.fillUV);
                half4 main = input.color * half4(fill.rgb * edge.rgb * 2.0h, edge.a);
                main.rgb = PaxShade(main.rgb, input.shadeUV);   // 2026-10-17: the room's stone shadow (StoneShade.hlsl)
                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
            #if defined(DEBUG_DISPLAY)
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                surfaceData.normalWS = input.normalWS;
            #endif
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
                float2  fillUV          : TEXCOORD5;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            TEXTURE2D(_FillNormal);
            SAMPLER(sampler_FillNormal);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                float4 _Tile;
                float _Axis;
                float4 _FillTile;
            CBUFFER_END

            // PAX-A15: world-anchored along the tiling axis (the texture's Repeat mode wraps it), the sprite's own UV across.
            // PAX-A16: _Axis 3 is the sprite's own UV (dressing, chains, fringes: plain sprites on the same shader as the trims, so
            // the whole static play layer batches without a shader switch).
            float2 WorldUV(float3 positionOS, float2 spriteUV)
            {
                if (_Axis > 2.5) return spriteUV;
                float2 w = TransformObjectToWorld(positionOS).xy / _Tile.xy;
                return float2(_Axis > 1.5 ? spriteUV.x : w.x, (_Axis > 0.5 && _Axis < 1.5) ? spriteUV.y : w.y);
            }

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                // PAX-A13: the tangent turns with a horizontal flip, and the handedness with either flip, so the
                // bitangent (cross(normal, tangent) * w) keeps pointing up.
                input.tangent.x *= unity_SpriteProps.x;
                input.tangent.w *= unity_SpriteProps.x * unity_SpriteProps.y;

                Varyings o = CommonNormalsVertex(input);
                o.uv = WorldUV(input.positionOS, input.uv);
                o.color = input.color * _Color * unity_SpriteColor;
                o.fillUV = TransformObjectToWorld(input.positionOS).xy / _FillTile.xy;

                return o;
            }

            // The fill's own normals (so the strip lights exactly as the stone it's cut from), plus the strip's bevel at its edge.
            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                const half4 mainTex = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                const half3 fillN = UnpackNormal(SAMPLE_TEXTURE2D(_FillNormal, sampler_FillNormal, input.fillUV));
                const half3 edgeN = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                const half3 normalTS = normalize(half3(fillN.xy + edgeN.xy, fillN.z));
                return NormalsRenderingShared(mainTex, normalTS, input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 fillUV : TEXCOORD5;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            TEXTURE2D(_FillTex);
            SAMPLER(sampler_FillTex);
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Tile;
                float _Axis;
                float4 _FillTile;
            CBUFFER_END

            // PAX-A15: world-anchored along the tiling axis (the texture's Repeat mode wraps it), the sprite's own UV across.
            // PAX-A16: _Axis 3 is the sprite's own UV (dressing, chains, fringes: plain sprites on the same shader as the trims, so
            // the whole static play layer batches without a shader switch).
            float2 WorldUV(float3 positionOS, float2 spriteUV)
            {
                if (_Axis > 2.5) return spriteUV;
                float2 w = TransformObjectToWorld(positionOS).xy / _Tile.xy;
                return float2(_Axis > 1.5 ? spriteUV.x : w.x, (_Axis > 0.5 && _Axis < 1.5) ? spriteUV.y : w.y);
            }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.uv = WorldUV(input.positionOS, input.uv);
                o.color = input.color *_Color * unity_SpriteColor;
                o.fillUV = TransformObjectToWorld(input.positionOS).xy / _FillTile.xy;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                const half4 edge = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                const half4 fill = SAMPLE_TEXTURE2D(_FillTex, sampler_FillTex, input.fillUV);
                return input.color * half4(fill.rgb * edge.rgb * 2.0h, edge.a);
            }
            ENDHLSL
        }
    }
}
