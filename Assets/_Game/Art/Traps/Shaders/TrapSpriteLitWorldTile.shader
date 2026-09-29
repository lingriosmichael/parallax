// PAX-A13 (§12 R8): Sprite-Lit-Flip sampling its sprite in world space, so a disguised trap and its host draw the same pixels
// at the same place however they're cut. UV = the vertex's rest-pose world position / the tile size, where the rest pose is
// per renderer (_Rest: origin xy, scale zw, set by a MaterialPropertyBlock): an intact skin's rest pose is where it is, a
// crumble shard's is where it sat, so the shard keeps its own pixels as it falls and spins. An atlased sprite wraps inside
// its atlas rectangle (_UVRect); a whole-texture sprite wraps by the texture's Repeat mode.
// Based on a copy of URP 17's Sprite-Lit-Default whose normals pass mirrors the tangent with the sprite's flip, so a
// flipX'd sprite's normal map is lit from the correct side. The stock shader flips only positions: its tangent keeps
// pointing +x, so a flipped sprite's sideways shading comes from the wrong side (and a negative X scale fixes x but
// inverts y, since the bitangent isn't corrected for handedness). Everything else is URP's shader, unchanged.
Shader "Parallax/2D/Sprite-Lit-WorldTile"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0
        _WorldTile("World tile size (xy)", Vector) = (1,1,0,0)
        _Rest("Rest origin (xy) and scale (zw)", Vector) = (0,0,1,1)
        _UVRect("Sprite UV rect in its texture", Vector) = (0,0,1,1)

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
                float4 _WorldTile;
                float4 _Rest;
                float4 _UVRect;
            CBUFFER_END

            float2 WorldTileUV(float3 positionOS) { return (_Rest.xy + positionOS.xy * _Rest.zw) / _WorldTile.xy; }
            float2 WrapUV(float2 uv) { return (_UVRect.z < 0.9999 || _UVRect.w < 0.9999) ? _UVRect.xy + frac(uv) * _UVRect.zw : uv; }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);
                o.uv = WorldTileUV(input.positionOS);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                input.uv = WrapUV(input.uv);
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
                float4 _WorldTile;
                float4 _Rest;
                float4 _UVRect;
            CBUFFER_END

            float2 WorldTileUV(float3 positionOS) { return (_Rest.xy + positionOS.xy * _Rest.zw) / _WorldTile.xy; }
            float2 WrapUV(float2 uv) { return (_UVRect.z < 0.9999 || _UVRect.w < 0.9999) ? _UVRect.xy + frac(uv) * _UVRect.zw : uv; }

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
                o.uv = WorldTileUV(input.positionOS);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                input.uv = WrapUV(input.uv);
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
                float4 _WorldTile;
                float4 _Rest;
                float4 _UVRect;
            CBUFFER_END

            float2 WorldTileUV(float3 positionOS) { return (_Rest.xy + positionOS.xy * _Rest.zw) / _WorldTile.xy; }
            float2 WrapUV(float2 uv) { return (_UVRect.z < 0.9999 || _UVRect.w < 0.9999) ? _UVRect.xy + frac(uv) * _UVRect.zw : uv; }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.uv = WorldTileUV(input.positionOS);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                input.uv = WrapUV(input.uv);
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
