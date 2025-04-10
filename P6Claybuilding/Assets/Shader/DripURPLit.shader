Shader "Custom/URP_RippleTopOnly_Ripples_PBR"
{
    Properties
    {
        _BaseMap        ("Base Map", 2D) = "white" {}
        _BaseColor      ("Base Color", Color) = (1,1,1,1)
        _NormalMap      ("Normal Map", 2D) = "bump" {}
        _Metallic       ("Metallic", Range(0, 1)) = 0.0
        _Smoothness     ("Smoothness", Range(0, 1)) = 0.5

        _RippleStrength ("Ripple Strength", Range(0, 5)) = 0.3
        _WaveSpeed      ("Wave Speed", Range(0.1, 10)) = 3
        _FillAmount     ("Fill Amount", Range(0, 1)) = 0
        _ObjectHeight   ("Object Height", Float) = 1.0
        _RippleStart    ("Ripple Start", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            // -----------------------------------------
            //  PRAGMAS
            // -----------------------------------------
            #pragma vertex vert
            #pragma fragment frag

            // Include the URP Core & Lit pipeline
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            // -----------------------------------------
            //  STRUCTS
            // -----------------------------------------
            struct VertexInput
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;     // Needed for proper lighting
                float2 uv         : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;  // World-space normal
            };

            // -----------------------------------------
            //  UNIFORMS / PROPERTIES
            // -----------------------------------------
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            float4 _BaseColor;
            float  _Metallic;
            float  _Smoothness;

            float  _RippleStrength;
            float  _WaveSpeed;
            float  _FillAmount;
            float  _ObjectHeight;
            float  _RippleStart;

            // -----------------------------------------
            //  VERTEX SHADER
            // -----------------------------------------
            VertexOutput vert (VertexInput IN)
            {
                VertexOutput OUT;

                // Convert object-space position to world space
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                // Apply ripple effect only above a certain Y threshold
                float localY      = IN.positionOS.y;
                float thresholdY  = _RippleStart * _ObjectHeight;

                if (_FillAmount < 1.0 && localY >= thresholdY)
                {
                    float dist = length(worldPos.xz);

                    // Two sin waves for more interesting ripple
                    float sineWave1 = sin(dist * 5.0 + _Time.y * _WaveSpeed) * 0.15;
                    float sineWave2 = sin(worldPos.z * 15.0 + _Time.y * _WaveSpeed) * 0.15;
                    float combinedRipple = sineWave1 + sineWave2;

                    worldPos.y += combinedRipple * _RippleStrength;
                }

                // Transform to clip space
                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.uv         = IN.uv;
                OUT.worldPos   = worldPos;

                // Transform normal to world space for PBR shading
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                return OUT;
            }

            // -----------------------------------------
            //  FRAGMENT SHADER (PBR)
            // -----------------------------------------
            half4 frag (VertexOutput IN) : SV_Target
            {
                // 1) Create a SurfaceData struct
                SurfaceData surfaceData;
                InitializeSurfaceData(IN.uv, surfaceData);

                // 2) Albedo (color) from the main texture * color property
                surfaceData.Albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * _BaseColor.rgb;

                // 3) Metallic & Smoothness (standard URP PBR inputs)
                surfaceData.Metallic   = _Metallic;
                surfaceData.Smoothness = _Smoothness;

                // 4) Normal map (if you want to keep the normal-based detail)
                float3 normalTex = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));
                surfaceData.Normal = normalTex;

                // 5) Construct InputData for lighting
                InputData inputData;
                inputData.positionWS        = IN.worldPos;
                inputData.normalWS          = normalize(IN.normalWS);
                inputData.viewDirectionWS   = GetWorldSpaceViewDir(IN.worldPos);
                inputData.shadowCoord       = TransformWorldToShadowCoord(IN.worldPos);

                // 6) Let URP's PBR function handle final lighting
                return UniversalFragmentPBR(inputData, surfaceData);
            }

            ENDHLSL
        }
    }

    Fallback "UniversalForward"
}
