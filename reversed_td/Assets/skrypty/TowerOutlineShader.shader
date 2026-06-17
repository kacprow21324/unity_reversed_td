// Własny shader obramówki wież — metoda Inverted Hull.
// Nie używa stencil buffer, działa niezależnie od ray tracingu i shadera modelu.
Shader "Custom/TowerOutline"
{
    Properties
    {
        _OutlineColor ("Kolor obramówki", Color) = (0.3, 0.85, 1, 1)
        _OutlineWidth ("Szerokość (metry)", Float) = 0.04
    }

    // ── URP ───────────────────────────────────────────────────────────────
    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry+1"
        }

        Pass
        {
            Name "TowerOutline_URP"
            Cull  Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                posWS          += normalWS * _OutlineWidth;
                OUT.positionHCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }

    // ── Built-in RP (fallback) ────────────────────────────────────────────
    SubShader
    {
        Tags { "Queue" = "Geometry+1" }

        Pass
        {
            Name "TowerOutline_Builtin"
            Cull  Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float  _OutlineWidth;

            struct a2v { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(a2v i)
            {
                v2f o;
                float3 wn = UnityObjectToWorldNormal(i.normal);
                float3 wp = mul(unity_ObjectToWorld, i.vertex).xyz + normalize(wn) * _OutlineWidth;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
}
