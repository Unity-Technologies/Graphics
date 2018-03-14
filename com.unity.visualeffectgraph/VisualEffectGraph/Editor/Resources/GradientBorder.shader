Shader "Hidden/VFX/GradientBorder"
{
    Properties
    {
        _Border("Border",float) = 1
        _Radius("Radius",float) = 1
        _PixelScale("PixelScale",float) = 1
        _Size("Size",Vector) = (100,100,0,0)
        _ColorStart("ColorStart",Color) = (1,1,0,1)
        _ColorEnd("ColorEnd", Color) = (0,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100
        Cull Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

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
                float4 pos : TEXCOORD2;
                float2 clipUV : TEXCOORD1;
                float height : TEXCOORD3;
            };

            float _Border;
            float _Radius;
            float _PixelScale;
            float2 _Size;
            float3 _ColorStart;
            float3 _ColorEnd;

            uniform float4x4 unity_GUIClipTextureMatrix;
            sampler2D _GUIClipTexture;

            v2f vert (appdata v)
            {
                v2f o;

                float margingScale = 1 + (_Border/_Radius /_PixelScale);

                o.height = _Size.y + _Radius;

                o.pos = float4(v.vertex.xy * _Size + v.uv* margingScale * v.vertex.xy* _Radius, 0, 0);
                o.vertex = UnityObjectToClipPos(o.pos);
                o.uv = v.uv*margingScale;
                float3 eyePos = UnityObjectToViewPos(o.pos );
                o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float pixelScale = _PixelScale;

                float realBorder = _Border;
                float2 uvMid = i.uv * (_Radius + realBorder); // coordinate to center of circle in pixels of middle of border
                float dist = uvMid.x * uvMid.x + uvMid.y * uvMid.y; // distance to center of circle in pixels
                float a = 1;

                float distToBorder = abs(_Radius - sqrt(dist))*pixelScale; // distance to the border center
                float onePixelAfterBorder = distToBorder - realBorder * 0.5f*pixelScale; //distance to edge of border

                a = 1-saturate(onePixelAfterBorder / _Border);

                float clipA = tex2D(_GUIClipTexture, i.clipUV).a;

                float height = 0.5f + i.pos.y / i.height * 0.5f;
                return float4(lerp(_ColorStart, _ColorEnd,height),a*clipA);
                //return float4(height,height,height , a*clipA);
                //return float4(_ColorEnd,1);
                //return float4(1, 1, 0, 1);

            }
            ENDCG
        }
    }
}
