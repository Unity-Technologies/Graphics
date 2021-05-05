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
            // ColorMask 0
            ZWrite Off
            ZClip Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            RWTexture3D<float>  _Output : register(u4);
            float3              _OutputSize;
            float3              _VolumeWorldOffset;
            float3              _VolumeSize;
            uint                _AxisSwizzle;

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            VertexToFragment vert(float4 vertex : POSITION)
            {
                VertexToFragment o;

                o.worldPos = mul(GetRawUnityObjectToWorld(), vertex).xyz;
                o.worldPos -= _VolumeWorldOffset;

                float4 p = float4(o.worldPos, 1);

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
                float3 cellPos = i.worldPos;
                float3 cellPos01 = cellPos / _VolumeSize;
                float3 pos = (cellPos01 * _OutputSize);

                if (any(pos < 0) || any(pos > _OutputSize))
                    return 0;

                _Output[uint3(pos)] = 1;

                return 1;
            }
            ENDHLSL
        }
    }
}
