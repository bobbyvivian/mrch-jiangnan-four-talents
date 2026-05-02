Shader "Unlit/MRCH/ARKitBackground"
{
    Properties
    {
        _textureY ("TextureY", 2D) = "white" {}
        _textureCbCr ("TextureCbCr", 2D) = "black" {}
        _HumanStencil ("HumanStencil", 2D) = "black" {}
        _HumanDepth ("HumanDepth", 2D) = "black" {}
        _EnvironmentDepth ("EnvironmentDepth", 2D) = "black" {}

        // ---------- Old TV / 1930s Effect ----------
        [Header(Old TV 1930s Effect)]
        [Toggle(ARKIT_OLD_TV_EFFECT)] _OldTVKeyword ("Enable Old TV Effect", Float) = 0
        _OldTVStrength      ("Effect Blend",                Range(0,1))      = 1
        _SepiaTint          ("Sepia (0=Gray 1=Sepia)",      Range(0,1))      = 0.7
        _Contrast           ("Contrast",                    Range(0,3))      = 1.4
        _Brightness         ("Brightness",                  Range(-1,1))     = -0.05
        _GrainStrength      ("Grain Strength",              Range(0,1))      = 0.45
        _GrainSize          ("Grain Size",                  Range(50,2000))  = 600
        _VignetteStrength   ("Vignette Strength",           Range(0,2))      = 1.2
        _VignetteSoftness   ("Vignette Softness",           Range(0.05,2))   = 0.55
        _ScanlineStrength   ("Scanline Strength",           Range(0,1))      = 0.25
        _ScanlineCount      ("Scanline Count",              Range(50,2000))  = 600
        _FlickerStrength    ("Flicker Strength",            Range(0,1))      = 0.18
        _FlickerSpeed       ("Flicker Speed",               Range(0,30))     = 14
        _ScratchStrength    ("Scratch Strength",            Range(0,1))      = 0.35
        _JitterStrength     ("Vertical Jitter Strength",    Range(0,0.05))   = 0.004
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite On
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local __ ARKIT_BACKGROUND_URP
            #pragma multi_compile_local __ ARKIT_HUMAN_SEGMENTATION_ENABLED ARKIT_ENVIRONMENT_DEPTH_ENABLED
            // Toggle the 1930s old-TV effect at runtime via material.EnableKeyword("ARKIT_OLD_TV_EFFECT")
            #pragma multi_compile_local __ ARKIT_OLD_TV_EFFECT


#if ARKIT_BACKGROUND_URP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            #define ARKIT_TEXTURE2D_HALF(texture) TEXTURE2D(texture)
            #define ARKIT_SAMPLER_HALF(sampler) SAMPLER(sampler)
            #define ARKIT_TEXTURE2D_FLOAT(texture) TEXTURE2D(texture)
            #define ARKIT_SAMPLER_FLOAT(sampler) SAMPLER(sampler)
            #define ARKIT_SAMPLE_TEXTURE2D(texture,sampler,texcoord) SAMPLE_TEXTURE2D(texture,sampler,texcoord)

#else // Legacy RP

            #include "UnityCG.cginc"

            #define real4 half4
            #define real3 half3
            #define real  half
            #define real4x4 half4x4
            #define TransformObjectToHClip UnityObjectToClipPos
            #define FastSRGBToLinear GammaToLinearSpace

            #define ARKIT_TEXTURE2D_HALF(texture) UNITY_DECLARE_TEX2D_HALF(texture)
            #define ARKIT_SAMPLER_HALF(sampler)
            #define ARKIT_TEXTURE2D_FLOAT(texture) UNITY_DECLARE_TEX2D_FLOAT(texture)
            #define ARKIT_SAMPLER_FLOAT(sampler)
            #define ARKIT_SAMPLE_TEXTURE2D(texture,sampler,texcoord) UNITY_SAMPLE_TEX2D(texture,texcoord)

#endif


            struct appdata
            {
                float3 position : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct fragment_output
            {
                real4 color : SV_Target;
                float depth : SV_Depth;
            };


            CBUFFER_START(UnityARFoundationPerFrame)
            // Device display transform is provided by the AR Foundation camera background renderer.
            float4x4 _UnityDisplayTransform;
            float _UnityCameraForwardScale;
            CBUFFER_END


            v2f vert (appdata v)
            {
                // Transform the position from object space to clip space.
                float4 position = TransformObjectToHClip(v.position);

                // Remap the texture coordinates based on the device rotation.
                float2 texcoord = mul(float4(v.texcoord, 1.0f, 1.0f), _UnityDisplayTransform).xy;

                v2f o;
                o.position = position;
                o.texcoord = texcoord;
                return o;
            }


            CBUFFER_START(ARKitColorTransformations)
            static const real4x4 s_YCbCrToSRGB = real4x4(
                real4(1.0h,  0.0000h,  1.4020h, -0.7010h),
                real4(1.0h, -0.3441h, -0.7141h,  0.5291h),
                real4(1.0h,  1.7720h,  0.0000h, -0.8860h),
                real4(0.0h,  0.0000h,  0.0000h,  1.0000h)
            );
            CBUFFER_END


            // ---------- Old TV effect uniforms ----------
            CBUFFER_START(ARKitOldTVProperties)
            float _OldTVStrength;
            float _SepiaTint;
            float _Contrast;
            float _Brightness;
            float _GrainStrength;
            float _GrainSize;
            float _VignetteStrength;
            float _VignetteSoftness;
            float _ScanlineStrength;
            float _ScanlineCount;
            float _FlickerStrength;
            float _FlickerSpeed;
            float _ScratchStrength;
            float _JitterStrength;
            CBUFFER_END


            inline float ConvertDistanceToDepth(float d)
            {
                // Account for scale
                d = _UnityCameraForwardScale > 0.0 ? _UnityCameraForwardScale * d : d;

                // Clip any distances smaller than the near clip plane, and compute the depth value from the distance.
                return (d < _ProjectionParams.y) ? 0.0f : ((1.0f / _ZBufferParams.z) * ((1.0f / d) - _ZBufferParams.w));
            }


            ARKIT_TEXTURE2D_HALF(_textureY);
            ARKIT_SAMPLER_HALF(sampler_textureY);
            ARKIT_TEXTURE2D_HALF(_textureCbCr);
            ARKIT_SAMPLER_HALF(sampler_textureCbCr);
#if ARKIT_ENVIRONMENT_DEPTH_ENABLED
            ARKIT_TEXTURE2D_FLOAT(_EnvironmentDepth);
            ARKIT_SAMPLER_FLOAT(sampler_EnvironmentDepth);
#elif ARKIT_HUMAN_SEGMENTATION_ENABLED
            ARKIT_TEXTURE2D_HALF(_HumanStencil);
            ARKIT_SAMPLER_HALF(sampler_HumanStencil);
            ARKIT_TEXTURE2D_FLOAT(_HumanDepth);
            ARKIT_SAMPLER_FLOAT(sampler_HumanDepth);
#endif // ARKIT_HUMAN_SEGMENTATION_ENABLED


#if ARKIT_OLD_TV_EFFECT
            // Cheap hash, deterministic per (uv, seed)
            inline float OldTV_Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            inline real3 OldTV_Apply(real3 color, float2 uv, float t)
            {
                // 1) Luminance -> mono / sepia
                real luma = dot(color, real3(0.299, 0.587, 0.114));
                real3 mono  = real3(luma, luma, luma);
                real3 sepia = real3(luma * 1.07, luma * 0.78, luma * 0.55);
                real3 toned = lerp(mono, sepia, _SepiaTint);

                // 2) Contrast / brightness
                toned = saturate((toned - 0.5h) * _Contrast + 0.5h + _Brightness);

                // 3) Vignette (radial)
                float2 vUV = uv - 0.5;
                float vD   = length(vUV);
                float vig  = 1.0 - smoothstep(_VignetteSoftness * 0.5,
                                              _VignetteSoftness * 0.5 + 0.5, vD);
                vig        = lerp(1.0, vig, _VignetteStrength);
                toned     *= vig;

                // 4) Horizontal scanlines (CRT feel)
                float scan = sin(uv.y * _ScanlineCount * 3.14159265) * 0.5 + 0.5;
                toned *= lerp(1.0, scan, _ScanlineStrength);

                // 5) Animated film grain (~24 fps stepped seed feels filmic)
                float grainSeed = floor(t * 24.0);
                float grain     = OldTV_Hash21(uv * _GrainSize + grainSeed * 17.13) - 0.5;
                toned += grain * _GrainStrength;

                // 6) Flicker (sine + per-frame jitter)
                float fSine  = sin(t * _FlickerSpeed) * 0.5;
                float fNoise = OldTV_Hash21(float2(floor(t * 24.0), 1.7)) - 0.5;
                toned *= 1.0 + (fSine + fNoise) * _FlickerStrength;

                // 7) Vertical scratches (occasional bright lines, faded at top/bottom)
                float scratchSeed = floor(t * 6.0);
                float scratchX    = floor(uv.x * 300.0);
                float scratchHash = OldTV_Hash21(float2(scratchX, scratchSeed));
                float scratch     = step(0.992, scratchHash);
                float scratchMask = smoothstep(0.0, 0.1, uv.y) * smoothstep(1.0, 0.9, uv.y);
                toned += scratch * scratchMask * _ScratchStrength;

                return saturate(toned);
            }
#endif // ARKIT_OLD_TV_EFFECT


            fragment_output frag (v2f i)
            {
                // ---- Optional vertical jitter on the sampling UV ----
                float2 sampleUV = i.texcoord;

#if ARKIT_OLD_TV_EFFECT
                {
                    float jSeed  = floor(_Time.y * 12.0);
                    float jitter = (OldTV_Hash21(float2(jSeed, jSeed * 0.31)) - 0.5)
                                 * _JitterStrength * _OldTVStrength;
                    sampleUV.y += jitter;
                }
#endif

                // Sample the video textures (in YCbCr).
                real4 ycbcr = real4(ARKIT_SAMPLE_TEXTURE2D(_textureY,    sampler_textureY,    sampleUV).r,
                                    ARKIT_SAMPLE_TEXTURE2D(_textureCbCr, sampler_textureCbCr, sampleUV).rg,
                                    1.0h);

                // Convert from YCbCr to sRGB.
                real4 videoColor = mul(s_YCbCrToSRGB, ycbcr);

#if !UNITY_COLORSPACE_GAMMA
                // If rendering in linear color space, convert from sRGB to RGB.
                videoColor.xyz = FastSRGBToLinear(videoColor.xyz);
#endif // !UNITY_COLORSPACE_GAMMA

                // Assume the background depth is the back of the depth clipping volume.
                float depthValue = 0.0f;

#if ARKIT_ENVIRONMENT_DEPTH_ENABLED
                // Sample the environment depth (in meters). Use original texcoord for depth alignment.
                float envDistance = ARKIT_SAMPLE_TEXTURE2D(_EnvironmentDepth, sampler_EnvironmentDepth, i.texcoord).r;

                // Convert the distance to depth.
                depthValue = ConvertDistanceToDepth(envDistance);
#elif ARKIT_HUMAN_SEGMENTATION_ENABLED
                // Check the human stencil, and skip non-human pixels.
                if (ARKIT_SAMPLE_TEXTURE2D(_HumanStencil, sampler_HumanStencil, i.texcoord).r > 0.5h)
                {
                    // Sample the human depth (in meters).
                    float humanDistance = ARKIT_SAMPLE_TEXTURE2D(_HumanDepth, sampler_HumanDepth, i.texcoord).r;

                    // Convert the distance to depth.
                    depthValue = ConvertDistanceToDepth(humanDistance);
                }
#endif // ARKIT_HUMAN_SEGMENTATION_ENABLED

#if ARKIT_OLD_TV_EFFECT
                // Apply 1930s look, then blend with original by _OldTVStrength
                real3 oldTV = OldTV_Apply(videoColor.rgb, i.texcoord, _Time.y);
                videoColor.rgb = lerp(videoColor.rgb, oldTV, (real)_OldTVStrength);
#endif

                fragment_output o;
                o.color = videoColor;
                o.depth = depthValue;
                return o;
            }

            ENDHLSL
        }
    }
}
