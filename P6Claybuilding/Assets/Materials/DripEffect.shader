Shader "Custom/URP_RippleTopOnly_Ripples"
{
    Properties
    {
        _BaseMap        ("Base Map", 2D) = "white" {}
        _BaseColor      ("Base Color", Color) = (1,1,1,1)
        _NormalMap      ("Normal Map", 2D) = "bump" {}
        _RippleStrength ("Ripple Strength", Range(0, 5)) = 0.3
        _WaveSpeed      ("Wave Speed", Range(0.1, 10)) = 3
        _FillAmount     ("Fill Amount", Range(0, 1)) = 0
        _ObjectHeight   ("Object Height", Float) = 1.0
        _RippleStart    ("Ripple Start", Range(0, 1)) = 0.3
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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            float4 _BaseColor;
            float _RippleStrength;
            float _WaveSpeed;
            float _FillAmount;
            float _ObjectHeight;
            float _RippleStart;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                float localY = IN.positionOS.y;
                float thresholdY = _RippleStart * _ObjectHeight;

                if (_FillAmount < 1.0 && localY >= thresholdY)
                {
                    float dist = length(worldPos.xz);
                    float sineWave1 = sin(dist * 5.0 + _Time.y * _WaveSpeed) * 0.15;
                    float sineWave2 = sin(worldPos.z * 15.0 + _Time.y * _WaveSpeed * 1.0) * 0.15;

                    float combinedRipple = sineWave1 + sineWave2;

                    worldPos.y += combinedRipple * _RippleStrength;
                }

                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.uv = IN.uv;
                OUT.worldPos = worldPos;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Optional: Sample normal map if you want lighting later
                float3 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv).xyz * 2.0 - 1.0;
                //For now we'll just use baseColor, no fancy normals yet.

                return half4(baseColor.rgb, 1);
            }

            ENDHLSL
        }
    }
}
