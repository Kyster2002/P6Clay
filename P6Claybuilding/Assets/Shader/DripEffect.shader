Shader "Custom/URP_FullFixTopSidesLiquid"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.6, 0.4, 1)
        _RippleStrength ("Ripple Strength", Range(0, 0.5)) = 0.3
        _WaveSpeed ("Wave Speed", Range(0.1, 10)) = 3
        _FillAmount ("Fill Amount", Range(0, 1)) = 0
        _ObjectHeight ("Object Height", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            float4 _BaseColor;
            float _RippleStrength;
            float _WaveSpeed;
            float _FillAmount;
            float _ObjectHeight;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float fillHeight = _FillAmount * _ObjectHeight;

                // ✅ Allow ripples at the bottom for 0.5s after filling
                float bottomFactor = smoothstep(0.0, 0.5, 1.0 - _FillAmount); 

                // ✅ Detect height to stop bottom ripple after filling
                float heightFactor = smoothstep(fillHeight - 0.1, fillHeight + 0.3, worldPos.y);

                // ✅ Only allow the bottom to ripple for a short time, then stop
                float waveIntensity = heightFactor * bottomFactor; 

                if (worldPos.y >= fillHeight)
{
    float distanceToCenter = length(worldPos.xz);
    float wave = sin(distanceToCenter * 8 + _Time.y * _WaveSpeed) * 
                 cos(distanceToCenter * 8 + _Time.y * _WaveSpeed) * 
                 _RippleStrength;

    worldPos.y += wave; // ✅ Forces ripple movement
}

                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.uv = IN.uv;
                OUT.worldPos = worldPos;

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return half4(_BaseColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}
