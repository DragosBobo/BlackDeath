Shader "Unlit/RingFill"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,1)
        _Inner ("Inner Radius (0-0.5)", Range(0,0.5)) = 0.18
        _Outer ("Outer Radius (0-0.5)", Range(0,0.5)) = 0.18
        _Soft  ("Edge Softness", Range(0.0001,0.05)) = 0.01
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Inner, _Outer, _Soft;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv - 0.5;           // center
                float r = length(p);             // 0..~0.707 on a quad
                float inner = smoothstep(_Inner, _Inner + _Soft, r);
                float outer = 1.0 - smoothstep(_Outer, _Outer + _Soft, r);
                float a = saturate(inner * outer);

                fixed4 col = _Color;
                col.a *= a;
                return col;
            }
            ENDCG
        }
    }
}
