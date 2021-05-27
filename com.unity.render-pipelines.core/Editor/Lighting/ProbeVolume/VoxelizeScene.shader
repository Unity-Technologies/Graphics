Shader "Hidden/ProbeVolume/VoxelizeScene"
{
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
            // #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            float4x4 unity_ObjectToWorld;

            RWTexture3D<float>  _Output : register(u4);
            float3              _OutputSize;
            float3              _VolumeWorldOffset;
            float3              _VolumeSize;
            uint                _AxisSwizzle;

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                float3 cellPos01 : TEXCOORD0;
            };

            VertexToFragment vert(float4 vertex : POSITION)
            {
                VertexToFragment o;

                float3 cellPos = mul(unity_ObjectToWorld, vertex).xyz;
                cellPos -= _VolumeWorldOffset;
                o.cellPos01 = (cellPos / _VolumeSize);

                float4 p = float4(cellPos, 1);

                switch (_AxisSwizzle)
                {
                    default:
                    case 0: // top
                        p.xyz = p.zxy;
                        break;
                    case 1: // right
                        p.xyz = p.yzx;
                        break;
                    case 2: // forward
                        p.xyz = p.xyz;
                        break;
                }
                o.vertex = float4(p.xyz / _VolumeSize, 1);

                // turn pos betwee 0 1 to -1 1
                o.vertex.xyz = o.vertex.xyz * 2 - 1;

                return o;
            }

            float4 frag(VertexToFragment i) : COLOR
            {
                if (any(i.cellPos01 < 0) || any(i.cellPos01 >= 1))
                    return 0;

                uint3 pos = uint3(i.cellPos01 * _OutputSize);

                _Output[pos] = 1;

                return float4(i.cellPos01, 1);
            }
            ENDHLSL
        }
    }
}
