Shader "Custom/PortalVortex"
{
    Properties
    {
        _SwirlStrength ("Swirl Strength", Float) = 6.0
        _Radius ("Effect Radius (local UV)", Float) = 0.5
        _PullStrength ("Pull Strength", Float) = 0.15
        _UnscaledTime ("Unscaled Time", Float) = 0.0
        _EffectAlpha ("Effect Alpha", Range(0,1)) = 1.0
        _DebugMode ("Debug Mode (0=Normal,1=SolidMagenta,2=RawGrabTexture)", Float) = 0.0
        _DistortionScale ("Distortion Scale (screen-space)", Float) = 0.05
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
                float _DebugMode;
                float _DistortionScale;
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
                // pullOffset is computed in quad-local UV space (toCenter ~ [-0.5, 0.5]) but
                // screenUV is full-screen normalized [0,1]. Adding pullOffset directly caused
                // the sample point to jump up to ~100% of the screen away from the portal quad's
                // own footprint (mirror-like smear instead of a localized sucking-in swirl).
                // _DistortionScale rescales the local-UV offset into a small screen-space nudge.
                float2 distortedUV = screenUV + pullOffset * _DistortionScale;

                // Debug 2: raw, undistorted, fully-opaque grab-pass sample.
                // Isolates whether _CameraSortingLayerTexture actually contains valid
                // captured scene content (vs. this pass's own UV/mask math being at fault).
                if (_DebugMode > 1.5)
                {
                    half4 raw = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, screenUV);
                    raw.a = 1.0;
                    return raw;
                }

                // Debug 1: solid opaque magenta, no texture sampling at all.
                // Isolates whether this Pass draws at all (Sorting Layer assignment /
                // "LightMode"="Universal2D" tag matching / SRP Batcher wiring).
                if (_DebugMode > 0.5)
                    return half4(1.0, 0.0, 1.0, 1.0);

                half4 col = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, distortedUV);
                // NOTE: smoothstep(edge0, edge1, x) requires edge0 < edge1 -- HLSL/GLSL spec
                // leaves edge0 >= edge1 undefined (compiler/GPU-vendor dependent). The previous
                // version passed (_Radius, _Radius*0.4) i.e. edge0 > edge1, which is technically
                // undefined behavior even though it happens to numerically resolve via the
                // canonical saturate((x-edge0)/(edge1-edge0)) formula on some compilers. Fixed to
                // pass edges in ascending order and invert the result instead, which is
                // well-defined on every platform (including the Android/GLES/Vulkan target).
                half edgeMask = 1.0 - smoothstep(_Radius * 0.4, _Radius, dist);
                col.a *= edgeMask * _EffectAlpha;
                return col;
            }
            ENDHLSL
        }
    }
}
