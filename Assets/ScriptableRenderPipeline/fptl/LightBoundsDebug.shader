// Final compositing pass, just does gamma conversion for now.

Shader "Hidden/LightBoundsDebug"
{
    Properties {  }
    SubShader {
        Pass {
            ZTest Always
            Cull Off
            ZWrite Off
            Blend SrcAlpha One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "LightDefinitions.cs.hlsl"

            StructuredBuffer<SFiniteLightBound> g_data : register(t0);

            float4 vert(uint globalIndex : SV_VertexID) : SV_POSITION
            {
                uint lightIndex = globalIndex / 36;
                uint localIndex = globalIndex - lightIndex * 36;
                uint faceIndex = localIndex / 6;
                uint vertexIndex = localIndex - faceIndex * 6;
                int remapTrisToQuad[6] = { 0,1,2,2,3,0 };   // Remap tri indices to quad indices: 012345->012230
                vertexIndex = remapTrisToQuad[vertexIndex];

                uint faces[6][4] = {
                    {0, 2, 6, 4},   //-x
                    {1, 5, 7, 3},   //+x
                    {0, 4, 5, 1},   //-y
                    {2, 3, 7, 6},   //+y
                    {0, 1, 3, 2},   //-z
                    {4, 6, 7, 5},   //+z
                };

                SFiniteLightBound lightData = g_data[lightIndex];
                uint coordIndex = faces[faceIndex][vertexIndex];
                float3 coord = float3((coordIndex & 1) ? 1.0f : -1.0f, (coordIndex & 2) ? 1.0f : -1.0f, (coordIndex & 4) ? 1.0f : -1.0f);
                coord.xy *= (coordIndex >= 4) ? lightData.scaleXY : float2(1, 1);

                float3 viewPos = lightData.center + coord.x * lightData.boxAxisX.xyz + coord.y * lightData.boxAxisY.xyz + coord.z * -lightData.boxAxisZ.xyz;
#if USE_LEFTHAND_CAMERASPACE
                // not completely sure why this is necessary since the old stuff pretends camera coordinate system is also left-hand.
                // see: Camera::CalculateMatrixShaderProps()
                viewPos.z = -viewPos.z;
#endif
                return UnityViewToClipPos(viewPos);
            }

            fixed4 frag() : SV_Target
            {
                return float4(1.0f, 0.5f, 0.3f, 0.1f);
            }
            ENDCG
        }
    }
    Fallback Off
}
