Shader "Custom/PortalVortex"
{
    Properties
    {
        _SwirlStrength ("Swirl Strength", Float) = 6.0
        _Radius ("Effect Radius (local UV)", Float) = 0.5
        _PullStrength ("Pull Strength", Float) = 0.15
        _UnscaledTime ("Unscaled Time", Float) = 0.0
        _EffectAlpha ("Effect Alpha", Range(0,1)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "PortalVortex"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv        : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
            };

            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float _SwirlStrength;
                float _Radius;
                float _PullStrength;
                float _UnscaledTime;
                float _EffectAlpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 toCenter = IN.uv - 0.5;
                float dist = length(toCenter);
                float falloff = saturate(1.0 - dist / _Radius);

                float angle = _SwirlStrength * falloff * falloff * _UnscaledTime;
                float s = sin(angle);
                float c = cos(angle);
                float2 swirled = float2(
                    toCenter.x * c - toCenter.y * s,
                    toCenter.x * s + toCenter.y * c);

                float2 pullOffset = (swirled - toCenter) - toCenter * falloff * _PullStrength;
                float2 distortedUV = screenUV + pullOffset;

                half4 col = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, distortedUV);
                half edgeMask = smoothstep(_Radius, _Radius * 0.4, dist);
                col.a *= edgeMask * _EffectAlpha;
                return col;
            }
            ENDHLSL
        }
    }
}
