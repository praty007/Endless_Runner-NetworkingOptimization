Shader "Custom/PeriscopicPattern"
{
    Properties
    {
        [HDR]_MainColor ("Main Color", Color) = (1,1,1,1)
        [HDR]_SecondaryColor ("Secondary Color", Color) = (0,0,0,1)
        _PatternDensity ("Pattern Density", Range(1, 50)) = 10
        _AnimationSpeed ("Animation Speed", Range(0, 5)) = 1
        _PatternWidth ("Pattern Width", Range(0, 1)) = 0.5
        _LineThickness ("Line Thickness", Range(0.01, 0.2)) = 0.05
        _LineFrequency ("Line Frequency", Range(1, 20)) = 5
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainColor;
            float4 _SecondaryColor;
            float _PatternDensity;
            float _AnimationSpeed;
            float _PatternWidth;
            float _LineThickness;
            float _LineFrequency;
            float _EmissionIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Center UV coordinates
                float2 centeredUV = i.uv - 0.5;
                
                // Calculate distance from center
                float dist = length(centeredUV);
                
                // Create animated circular pattern
                float time = _Time.y * _AnimationSpeed;
                float circularPattern = sin(dist * _PatternDensity - time);
                
                // Create line pattern
                float angle = atan2(centeredUV.y, centeredUV.x);
                float linePattern = sin(angle * _LineFrequency + time);
                
                // Combine patterns
                float combinedPattern = circularPattern * linePattern;
                
                // Create sharp lines
                float lines = abs(frac(centeredUV.x * 10 + centeredUV.y * 10 + time * 0.5) * 2 - 1);
                lines = smoothstep(_LineThickness, 0, lines);
                
                // Combine all patterns
                float finalPattern = max(smoothstep(0, _PatternWidth, combinedPattern), lines);
                
                // Apply emission
                fixed4 col = lerp(_SecondaryColor, _MainColor, finalPattern) * _EmissionIntensity;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}