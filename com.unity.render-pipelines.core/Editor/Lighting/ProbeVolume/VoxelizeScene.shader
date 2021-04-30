Shader "Hidden/ProbeVolume/VoxelizeScene"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off
            ColorMask 0
            ZWrite Off
            ZClip Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma enable_d3d11_debug_symbols

            // TODO: remove GCinc
            #include "UnityCG.cginc"

            RWTexture3D<float>  _Output : register(u4);
            float3              _OutputSize;
            float3              _VolumeWorldOffset;
            float3              _VolumeSize;
            float4x4            _CameraMatrix;

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            VertexToFragment vert(float4 vertex : POSITION)
            {
                VertexToFragment o;

                o.vertex = mul(_CameraMatrix, float4(vertex.xyz, 1.0));
                o.worldPos = mul (unity_ObjectToWorld, vertex);

                return o;
            }

            fixed4 frag(VertexToFragment i) : COLOR
            {
                float3 cellPos = i.worldPos - _VolumeWorldOffset;
                float3 cellPos01 = cellPos / _VolumeSize;
                float3 pos = (cellPos01 * _OutputSize);

                if (any(pos < 0) || any(pos > _OutputSize))
                    return 0;

                _Output[uint3(pos)] = 1;

                return 0;
            }
            ENDHLSL
        }
    }
}
