Shader "Custom/URP_RippleTopOnly"
{
    Properties
    {
        _BaseColor    ("Base Color", Color)            = (0.8, 0.6, 0.4, 1)
        _RippleStrength("Ripple Strength", Range(0, 1))  = 0.3
        _WaveSpeed    ("Wave Speed", Range(0.1, 10))     = 3
        _FillAmount   ("Fill Amount", Range(0, 1))       = 0
        _ObjectHeight ("Object Height", Float)           = 1.0
        // _RippleStart represents the fraction of the object's height (from the bottom) 
        // that must be exceeded before the ripple effect is applied.
        _RippleStart  ("Ripple Start", Range(0, 1))       = 0.3
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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
            };

            float4 _BaseColor;
            float   _RippleStrength;
            float   _WaveSpeed;
            float   _FillAmount;
            float   _ObjectHeight;
            float   _RippleStart;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                // Compute the world position of the vertex.
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                // Use the local y coordinate from IN.positionOS (which is in object space).
                float localY = IN.positionOS.y;
                // Calculate the threshold. (Assumes the object's pivot is at its bottom.)
                float thresholdY = _RippleStart * _ObjectHeight;

                // Apply ripple only if fill is not complete and vertex is above the threshold.
                if (_FillAmount < 1.0 && localY >= thresholdY)
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
