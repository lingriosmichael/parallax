Shader "PARALLAX/SpriteOutlineUnlit"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width (texels)", Float) = 1.5
        _RimTop ("Rim from above (0 even outline, 1 top only)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _RimTop;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            float SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _OutlineWidth;
                float centerAlpha = SampleAlpha(IN.uv);

                if (centerAlpha > 0.05)
                {
                    return half4(0, 0, 0, 0);
                }

                float maxNeighbourAlpha = 0.0;
                maxNeighbourAlpha = max(maxNeighbourAlpha, SampleAlpha(IN.uv + float2( texel.x,  0)));
                maxNeighbourAlpha = max(maxNeighbourAlpha, SampleAlpha(IN.uv + float2(-texel.x,  0)));
                maxNeighbourAlpha = max(maxNeighbourAlpha, SampleAlpha(IN.uv + float2( 0,  texel.y)));
                maxNeighbourAlpha = max(maxNeighbourAlpha, SampleAlpha(IN.uv + float2( 0, -texel.y)));
                maxNeighbourAlpha = max(maxNeighbourAlpha, SampleAlpha(IN.uv + float2( texel.x,  texel.y)));
                maxNeighbourAlpha = max(maxNeighbourAlpha, SampleAlpha(IN.uv + float2(-texel.x,  texel.y)));
                maxNeighbourAlpha = max(maxNeighbourAlpha, SampleAlpha(IN.uv + float2( texel.x, -texel.y)));
                maxNeighbourAlpha = max(maxNeighbourAlpha, SampleAlpha(IN.uv + float2(-texel.x, -texel.y)));

                if (maxNeighbourAlpha <= 0.05)
                {
                    return half4(0, 0, 0, 0);
                }

                // PAX-A16 gauntlet: a rim lit from above. Where the body lies below this pixel (its top edge) the rim is full; elsewhere
                // it fades to (1 - _RimTop) of it, so the outline stays all round for readability but reads as backlight.
                float below = max(SampleAlpha(IN.uv + float2(0, -texel.y)), max(SampleAlpha(IN.uv + float2(texel.x, -texel.y)), SampleAlpha(IN.uv + float2(-texel.x, -texel.y))));
                float rim = lerp(1.0, below > 0.05 ? 1.0 : 0.0, _RimTop);
                return half4(_OutlineColor.rgb * IN.color.rgb, _OutlineColor.a * IN.color.a * rim);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
