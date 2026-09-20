// Property notes (kept as comments -- ShaderLab's parser can choke on
// parentheses/slashes inside quoted display names):
// - BrushStretch: how elongated the brush streaks are along one axis
// - SheenAngle: direction of the highlight streaks, in degrees
// - SheenSoftness: higher = thinner, sharper highlight bands
// - ScrollSpeed: 0 = static sheen, >0 = slowly animates/shimmers
Shader "Custom/SimpleBlueGreyMetalBackground"
{
    Properties
    {
        [Header(Base Color)]
        _BaseColor ("Base Color", Color) = (0.70, 0.75, 0.80, 1)
        _ShadeColor ("Shade Color", Color) = (0.55, 0.60, 0.66, 1)

        [Header(Brushed Streak Texture)]
        _BrushScale ("Brush Scale", Float) = 3.0
        _BrushStretch ("Brush Stretch", Float) = 12.0
        _BrushStrength ("Brush Strength", Range(0,1)) = 0.35

        [Header(Fake Sheen Highlight)]
        _SheenColor ("Sheen Color", Color) = (0.95, 0.98, 1.0, 1)
        _SheenAngle ("Sheen Angle", Range(0,360)) = 45
        _SheenFrequency ("Sheen Frequency", Float) = 1.5
        _SheenSoftness ("Sheen Softness", Range(1, 64)) = 12
        _SheenStrength ("Sheen Strength", Range(0,1)) = 0.5
        _ScrollSpeed ("Scroll Speed", Float) = 0.0

        [Header(Misc)]
        _Seed ("Pattern Seed", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float3 positionWS : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadeColor;

                float _BrushScale;
                float _BrushStretch;
                float _BrushStrength;

                float4 _SheenColor;
                float _SheenAngle;
                float _SheenFrequency;
                float _SheenSoftness;
                float _SheenStrength;
                float _ScrollSpeed;

                float _Seed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            // Cheap 2D hash: turns a coordinate into a pseudo-random 0-1 value.
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21) + _Seed);
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth value noise, same technique as the dirt shader.
            float valueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 f = frac(uv);

                float a = hash21(cell);
                float b = hash21(cell + float2(1, 0));
                float c = hash21(cell + float2(0, 1));
                float d = hash21(cell + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Two-octave fbm is plenty for a subtle brushed-metal streak
            // texture -- we don't want it as busy/detailed as the dirt.
            float fbm2(float2 uv)
            {
                float total = valueNoise(uv) * 0.6;
                total += valueNoise(uv * 2.0) * 0.4;
                return total / 1.0;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 worldPos = IN.positionWS.xy;

                // --- Brushed streak texture ---
                // Stretching one axis heavily before sampling noise turns
                // blobby noise into long thin streaks, like brushed metal
                // grain. World-space based, so it stays consistent across
                // any size/number of quads.
                float2 brushUV = worldPos * _BrushScale;
                brushUV.x /= _BrushStretch;
                float brushNoise = fbm2(brushUV);
                float3 baseColor = lerp(_ShadeColor.rgb, _BaseColor.rgb, brushNoise);
                baseColor = lerp(_BaseColor.rgb, baseColor, _BrushStrength);

                // --- Fake sheen highlight ---
                // A real specular highlight needs a varying surface normal,
                // which a flat 2D quad doesn't have. Instead, this fakes a
                // metallic sheen with a periodic soft band of light running
                // across the surface at a chosen angle. Being a periodic
                // function of world position, it also tiles seamlessly.
                float angleRad = radians(_SheenAngle);
                float2 sheenDir = float2(cos(angleRad), sin(angleRad));

                float proj = dot(worldPos, sheenDir) * _SheenFrequency;
                proj += _Time.y * _ScrollSpeed;

                float sheenWave = cos(proj); // -1 to 1
                float sheenMask = pow(saturate(sheenWave), _SheenSoftness);

                float3 finalColor = lerp(baseColor, _SheenColor.rgb, sheenMask * _SheenStrength);

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
