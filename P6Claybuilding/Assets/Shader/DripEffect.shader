Shader "Custom/URP_FullFixTopSidesLiquid"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.6, 0.4, 1)
        _RippleStrength ("Ripple Strength", Range(0, 0.5)) = 0.3
        _WaveSpeed ("Wave Speed", Range(0.1, 10)) = 3
        _FillAmount ("Fill Amount", Range(0, 1)) = 0
        _ObjectHeight ("Object Height", Float) = 1.0
        _RippleStart ("Ripple Start", Range(0, 1)) = 0.2  // When ripple should start
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
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
            float _RippleStart;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                // Transform vertex to world space.
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                // Compute a fixed threshold based on _RippleStart.
                float rippleThreshold = _RippleStart * _ObjectHeight;
                // Use step: if worldPos.y is above the threshold, then rippleClip is 1; otherwise, 0.
                float rippleClip = step(rippleThreshold, worldPos.y);
                
                // If fill isn't complete and rippleClip is active, apply the ripple.
                if (_FillAmount < 1.0 && rippleClip > 0.0)
                {
                    float distanceToCenter = length(worldPos.xz);
                    float wave = sin(distanceToCenter * 8 + _Time.y * _WaveSpeed) *
                                 cos(distanceToCenter * 8 + _Time.y * _WaveSpeed) *
                                 _RippleStrength;
                    worldPos.y += wave;
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
