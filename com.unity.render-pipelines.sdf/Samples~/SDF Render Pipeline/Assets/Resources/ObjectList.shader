Shader "Unlit/ObjectList"
{
    Properties
    {
        [PerRendererData]_SdfID ("SDF ID", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #include "UnityCG.cginc"

            #define MAX_OBJECTS_IN_SCENE 50

            struct TileDataHeader
            {
                int  offset;
                int  numObjects;
                int2 pad;
            };

            // RWStructuredBuffer<TileDataHeader> _TileHeaderData : register(u1);
            RWStructuredBuffer<int> _TileFlagsData : register(u1);
            uniform int _SdfID;

            struct appdata
            {
                float4 pos : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Assuming UAV buffer of size = ceil(_ScreenParams.x / 8) * ceil(_ScreenParams.y / 8) * MAX_OBJECTS_IN_SCENE

                // TODO: Do this once outside and then pass into shader
                // const float debugVisMultiplier = 16.0f;
                const float tileSize = 8.0f;// * debugVisMultiplier;
                float totalLength = _ScreenParams.x * _ScreenParams.y;
                float tileSizeSquared = tileSize * tileSize;
                int totalBins = ceil(_ScreenParams.x / tileSize) * ceil(_ScreenParams.y / tileSize);

                // 'Flatten' position: x + y * screenWidth
                float binIndex = floor(i.pos.x / tileSize) + floor(i.pos.y / tileSize) * ceil(_ScreenParams.x / tileSize);

                _TileFlagsData[MAX_OBJECTS_IN_SCENE * binIndex + _SdfID] = 1;                

                // Only for debug visualization
                return fixed4(binIndex / totalBins, 0, 0, 1);
            }
            ENDCG
        }
    }
}
