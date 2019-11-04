Shader "Hidden/LookDev/CubeToLatlong"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Cubemap", Any) = "grey" {}
        _CubeToLatLongParams ("Parameters", Vector) = (0.0, 0.0, 0.0, 0.0)
        _WindowParams("Window params", Vector) = (0.0, 0.0, 0.0, 0.0)
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    uniform float4 _MainTex_HDR;
    uniform float4 _MainTex_ST;
    UNITY_DECLARE_TEXCUBE(_MainTex);
    uniform float4 _CubeToLatLongParams;    // x angle offset, y alpha, z intensity w lod to use
    uniform float4 _WindowParams;           // x Editor windows height, y Environment windows posY, z margin (constant of 2), w PixelsPerPoint
    uniform bool _ManualTex2SRGB;

    #define OutputAlpha _CubeToLatLongParams.y
    #define Intensity _CubeToLatLongParams.z
    #define CurrentLOD _CubeToLatLongParams.w

    struct appdata_t
    {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f
    {
        float2 texcoord : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    v2f vert(appdata_t IN)
    {
        v2f OUT;
        OUT.vertex = UnityObjectToClipPos(IN.vertex);
        OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);

        return OUT;
    }

    float4 frag(    float2 texcoord : TEXCOORD0,
                    UNITY_VPOS_TYPE vpos : VPOS
                    ) : COLOR
    {
        float2 texCoord = texcoord.xy;
        float theta = texCoord.y * UNITY_PI;
        float phi = (texCoord.x * 2.f * UNITY_PI - UNITY_PI*0.5f) - _CubeToLatLongParams.x;

        float cosTheta = cos(theta);
        float sinTheta = sqrt(1.0f - min(1.0f, cosTheta*cosTheta));
        float cosPhi = cos(phi);
        float sinPhi = sin(phi);

        float3 direction = float3(sinTheta*cosPhi, cosTheta, sinTheta*sinPhi);
        direction.xy *= -1.0;
        float4 ret = float4(DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(_MainTex, direction, CurrentLOD), _MainTex_HDR) * Intensity, OutputAlpha);
        if (_ManualTex2SRGB)
            ret.rgb = LinearToGammaSpace(ret.rgb);

        // Clip outside of the library window

        // Editor windows is like this:
        //------
        // Margin (2)
        // Scene - Game - Asset Store   <= What we call tab size
        //------
        // Settings - Views <= what we call menu size
        //----
        // View size with Environment windows)
        //
        // _WindowParams.x contain the height of the editor windows
        // _WindowParams.y contain the start of the windows environment in the windows editor, i.e the menu size + tab size
        // _WindowParams.z contain a constant margin of 2 (don't know how to retrieve that)
        // _WindowParams.w is PixelsPerPoin (To handle retina display on OSX))

        // We use VPOS register to clip, VPOS is dependent on the API. It is reversed in openGL.
        // There is no need to clip when y is above height because the editor windows will clip it
        // vertex.y is relative to editor windows
#if UNITY_UV_STARTS_AT_TOP
        if ((vpos.y / _WindowParams.w) < (_WindowParams.y + _WindowParams.z))
#else
        // vertex.y is reversed (start from bottom of the editor windsows)
        vpos.y = _WindowParams.x - (vpos.y / _WindowParams.w);
        if (vpos.y < _WindowParams.z)
#endif
        {
            clip(-1);
        }
        return ret;
    }
    ENDCG

    SubShader
    {
        Tags
        {
            "ForceSupported"="True"
        }

        Lighting Off
        Cull Off
        ZTest Always
        ZWrite Off

        Pass
        {
            Blend One Zero

            CGPROGRAM
            #pragma fragment frag
            #pragma vertex vert
            #pragma target 3.0
            ENDCG
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma fragment frag
            #pragma vertex vert
            #pragma target 3.0
            ENDCG
        }

    }
}
