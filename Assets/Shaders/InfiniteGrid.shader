Shader "Custom/InfiniteGrid"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _GridScale ("Grid Scale", Float) = 1.0
        _LineThickness ("Line Thickness", Range(0.0, 0.1)) = 0.02
        _GridColor ("Grid Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _FadeDistance ("Fade Distance", Float) = 100.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 worldPos : TEXCOORD0;
            };

            float4 _Color;
            float _GridScale;
            float _LineThickness;
            float4 _GridColor;
            float _FadeDistance;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xz;
                return o;
            }

            float GridLine(float position)
            {
                float grid = abs(frac(position - 0.5) - 0.5) / fwidth(position);
                return saturate((1.0 - grid) / _LineThickness);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate grid
                float2 pos = i.worldPos * _GridScale;
                float x = GridLine(pos.x);
                float z = GridLine(pos.y);
                float grid = max(x, z);

                // Calculate distance fade
                float dist = length(_WorldSpaceCameraPos.xz - i.worldPos);
                float fade = 1.0 - saturate(dist / _FadeDistance);

                // Apply color and transparency
                float4 color = _GridColor;
                color.a *= grid * fade;

                // Blend with base color
                return lerp(_Color, color, color.a);
            }
            ENDCG
        }
    }
}