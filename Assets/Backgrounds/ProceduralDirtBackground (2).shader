// Property notes (kept here since ShaderLab's parser can choke on
// parentheses/slashes inside quoted display names, even quoted):
// - NoiseScale: bigger = smaller blotches
// - Octaves: number of detail layers, 1-6
// - Lacunarity: frequency growth per octave
// - Gain: strength falloff per octave
// - SpeckleDensity: 0-1, higher = more specks
// - Seed: change this to get a different pattern variation
Shader "Custom/ProceduralDirtBackground"
{
    Properties
    {
        [Header(Base Colors)]
        _ColorDark  ("Dark Dirt Color", Color) = (0.20, 0.13, 0.08, 1)
        _ColorMid   ("Mid Dirt Color",  Color) = (0.35, 0.24, 0.14, 1)
        _ColorLight ("Light Dirt Color", Color) = (0.50, 0.36, 0.22, 1)

        [Header(Base Noise)]
        _NoiseScale ("Noise Scale", Float) = 6.0
        _Octaves    ("Octaves", Range(1,6)) = 4
        _Lacunarity ("Lacunarity", Float) = 2.0
        _Gain       ("Gain", Range(0,1)) = 0.5

        [Header(Speckle Grain)]
        _SpeckleScale   ("Speckle Grid Scale", Float) = 40.0
        _SpeckleDensity ("Speckle Density", Range(0,1)) = 0.15
        _SpeckleColor   ("Speckle Color", Color) = (0.08, 0.05, 0.03, 1)
        _SpeckleStrength("Speckle Blend Strength", Range(0,1)) = 0.6

        [Header(Misc)]
        _Seed ("Pattern Seed", Float) = 0.0
    }

    SubShader
    {
        // "UniversalPipeline" is what makes this render correctly under URP.
        // Without it URP will treat this as an incompatible shader and
        // fall back to pink/magenta.
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP's shader library instead of Built-in's UnityCG.cginc
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionHCS  : SV_POSITION;
            };

            // CBUFFER wrapping is required for SRP Batcher compatibility --
            // without it the material won't batch efficiently under URP.
            CBUFFER_START(UnityPerMaterial)
                float4 _ColorDark;
                float4 _ColorMid;
                float4 _ColorLight;

                float _NoiseScale;
                float _Octaves;
                float _Lacunarity;
                float _Gain;

                float _SpeckleScale;
                float _SpeckleDensity;
                float4 _SpeckleColor;
                float _SpeckleStrength;

                float _Seed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // Cheap 2D hash: turns a coordinate into a pseudo-random 0-1 value.
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21) + _Seed);
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth value noise: hashes the 4 corners of a grid cell and
            // interpolates between them with a smoothstep-like curve so
            // there are no hard edges between cells.
            float valueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 f = frac(uv);

                float a = hash21(cell);
                float b = hash21(cell + float2(1, 0));
                float c = hash21(cell + float2(0, 1));
                float d = hash21(cell + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep curve

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Fractal Brownian Motion: stacks several octaves of noise at
            // increasing frequency and decreasing strength. This is what
            // gives the pattern organic-looking detail instead of one
            // smooth blob.
            float fbm(float2 uv)
            {
                float total = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                float maxTotal = 0.0;

                int octaveCount = (int)_Octaves;
                for (int i = 0; i < octaveCount; i++)
                {
                    total += valueNoise(uv * frequency) * amplitude;
                    maxTotal += amplitude;
                    frequency *= _Lacunarity;
                    amplitude *= _Gain;
                }

                return total / max(maxTotal, 0.0001); // normalize to 0-1
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * _NoiseScale;

                // --- Base blotchy color variation ---
                float n = fbm(uv);

                // Remap the noise value through three colors instead of
                // using it as raw brightness -- this is what makes it
                // read as "dirt" rather than gray static.
                float4 baseColor;
                if (n < 0.45)
                {
                    float t = smoothstep(0.0, 0.45, n);
                    baseColor = lerp(_ColorDark, _ColorMid, t);
                }
                else
                {
                    float t = smoothstep(0.45, 1.0, n);
                    baseColor = lerp(_ColorMid, _ColorLight, t);
                }

                // --- Fine speckle / grain layer ---
                float2 speckleUV = IN.uv * _SpeckleScale;
                float2 speckleCell = floor(speckleUV);
                float speckleRandom = hash21(speckleCell + 0.5);

                // Only cells below the density threshold become a speck --
                // this gives sparse scattered dots rather than a uniform
                // noisy layer.
                float speckleMask = step(speckleRandom, _SpeckleDensity);

                float4 finalColor = lerp(baseColor, _SpeckleColor, speckleMask * _SpeckleStrength);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
