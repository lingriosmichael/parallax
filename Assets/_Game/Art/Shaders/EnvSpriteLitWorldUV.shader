// PAX-A15: the environment kit's fixed fills and trims. A copy of PAX-A13's Sprite-Lit-WorldTile (itself URP 17's
// Sprite-Lit-Default with the flip-correct normals pass) whose UV comes from the vertex's world position instead of a
// per-renderer rest pose: no MaterialPropertyBlock, so renderers sharing a material and texture batch. _Tile is the
// tile's world size (the sprite's own size); _Axis picks the world-anchored axes: 0 both (fills), 1 across (caps,
// undersides, slabs, water: u from world x, v from the sprite), 2 down (wall faces, posts: u from the sprite, v from world
// y). At rest it samples exactly what Sprite-Lit-WorldTile samples for the same tile, so a disguised trap (which keeps
// that shader and its rest pose, so its pixels travel with it) matches its host pixel for pixel (P10).
Shader "Parallax/2D/Env-Sprite-Lit-WorldUV"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0
        _Tile("World tile size (xy)", Vector) = (1,1,0,0)
        _Axis("World axes: 0 both, 1 across (v from the sprite), 2 down (u from the sprite), 3 none (the sprite's UV)", Float) = 0

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
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Tile;
                float _Axis;
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

                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                return CommonLitFragment(input, input.color);
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
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                float4 _Tile;
                float _Axis;
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

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                return CommonNormalsFragment(input, input.color);
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
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Tile;
                float _Axis;
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
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
