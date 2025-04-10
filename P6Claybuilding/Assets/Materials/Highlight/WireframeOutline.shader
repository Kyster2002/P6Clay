Shader "Custom/WireframeOnlyVertical"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,0,1) // Yellow
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float3 worldPos : TEXCOORD0;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
            };

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            [maxvertexcount(6)]
            void geom(triangle v2g input[3], inout LineStream<g2f> stream)
            {
                for (int i = 0; i < 3; i++)
                {
                    float3 p0 = input[i].worldPos;
                    float3 p1 = input[(i + 1) % 3].worldPos;

                    float3 dir = normalize(p1 - p0);

                    // Check if the edge is mostly vertical
                    if (abs(dir.y) > abs(dir.x) && abs(dir.y) > abs(dir.z))
                    {
                        g2f o;
                        o.vertex = input[i].vertex;
                        g2f o2;
                        o2.vertex = input[(i + 1) % 3].vertex;
                        stream.Append(o);
                        stream.Append(o2);
                    }
                }
            }

            fixed4 frag (g2f i) : SV_Target
            {
                return fixed4(1, 1, 0, 1); // Yellow
            }
            ENDCG
        }
    }
}
