Shader "PluginMaster/Grid"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector]_Color ("Color", Color) = (1,1,1,1)
        [HideInInspector]_Tiling ("Tiling", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _Tiling;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 screenUV : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenUV = (o.vertex.xy / o.vertex.w) * 0.5 + 0.5;
                o.uv = v.uv * _Tiling + float2(0.5, 0.5);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                color.rgb *= 2.0 * color.a;
                color *= _Color;
                return color; 
            }
            ENDCG
        }
    }
}