Shader "Hidden/VoxelizeShader"
{
	Properties
    {
        _ColorMask ("Color Mask", Range(0,15)) = 15
    }
    
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

        CGINCLUDE

        struct appdata
        {
            float4 vertex : POSITION;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
        };
        
        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            return o;
        }
        
        fixed4 fragONE (v2f i) : SV_Target
        {
            return fixed4(1,1,1,1);
        }
        
        fixed4 fragZERO (v2f i) : SV_Target
        {
            return fixed4(0,0,0,1);
        }

        ENDCG

        Pass // ONE Inside
        {
            Cull Front
            
            ZWrite On
            ZTest LEqual

            ColorMask [_ColorMask]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragONE
            
            #include "UnityCG.cginc"
            ENDCG
        }

        Pass // ZERO Outside
        {
            Cull Back
            
            ZWrite On
            ZTest LEqual

            ColorMask [_ColorMask]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragZERO
            
            #include "UnityCG.cginc"
            ENDCG
        }
	}
}
