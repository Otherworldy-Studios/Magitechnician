Shader "PluginMaster/SnapBox"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"

            #define LINE_WIDTH 2.0

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2g
            {
                float4 clipPos  : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 localPos : TEXCOORD1;
            };

            struct g2f
            {
                float4 clipPos : SV_POSITION;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.localPos = v.vertex.xyz;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = wp.xyz;
                o.clipPos  = UnityObjectToClipPos(v.vertex);
                return o;
            }

            inline bool IsAxisAligned(float3 dir, float tol)
            {
                float3 n = normalize(dir);
                return (abs(n.x) > 1-tol && abs(n.y)<tol && abs(n.z)<tol)
                    || (abs(n.y) > 1-tol && abs(n.x)<tol && abs(n.z)<tol)
                    || (abs(n.z) > 1-tol && abs(n.x)<tol && abs(n.y)<tol);
            }

            void DrawThickLine(float3 pA, float3 pB, inout TriangleStream<g2f> triStream)
            {
                float4 cA = UnityWorldToClipPos(pA);
                float4 cB = UnityWorldToClipPos(pB);

                float2 ndcA = cA.xy / cA.w;
                float2 ndcB = cB.xy / cB.w;

                float2 dir = normalize(ndcB - ndcA);
                float2 perp = float2(-dir.y, dir.x);

                float2 offset = perp * (LINE_WIDTH / _ScreenParams.x * 2.0);


                float4 posA1 = float4((ndcA + offset)*cA.w, cA.zw);
                float4 posA2 = float4((ndcA - offset)*cA.w, cA.zw);
                float4 posB1 = float4((ndcB + offset)*cB.w, cB.zw);
                float4 posB2 = float4((ndcB - offset)*cB.w, cB.zw);

                g2f v0 = (g2f)0, v1 = (g2f)0, v2 = (g2f)0, v3 = (g2f)0;
                v0.clipPos = posA1;
                v1.clipPos = posA2;
                v2.clipPos = posB1;
                v3.clipPos = posB2;

                triStream.Append(v0);
                triStream.Append(v1);
                triStream.Append(v2);
                triStream.RestartStrip();

                triStream.Append(v2);
                triStream.Append(v1);
                triStream.Append(v3);
                triStream.RestartStrip();
            }

            [maxvertexcount(12)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                float3 w0 = input[0].worldPos;
                float3 w1 = input[1].worldPos;
                float3 w2 = input[2].worldPos;
                float3 l0 = input[0].localPos;
                float3 l1 = input[1].localPos;
                float3 l2 = input[2].localPos;

                if (IsAxisAligned(l1 - l0, 0.01)) DrawThickLine(w0, w1, triStream);
                if (IsAxisAligned(l2 - l1, 0.01)) DrawThickLine(w1, w2, triStream);
                if (IsAxisAligned(l0 - l2, 0.01)) DrawThickLine(w2, w0, triStream);
            }

            float4 frag(g2f IN) : SV_Target
            {
                return float4(1, 0.5, 0.8, 1);
            }
            ENDCG
        }
    }
}