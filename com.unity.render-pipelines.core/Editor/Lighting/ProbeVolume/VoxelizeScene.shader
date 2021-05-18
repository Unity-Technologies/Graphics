Shader "Hidden/ProbeVolume/VoxelizeScene"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "Voxelize Mesh"

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
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

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

                float3 cellPos = mul(GetRawUnityObjectToWorld(), vertex).xyz;
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

                // trasnform pos between 0 1 to -1 1
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

        Pass
        {
            Name "Voxelize Terrain"

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
            TEXTURE2D(_TerrainHeightmapTexture);
            TEXTURE2D(_TerrainHolesTexture);
            float4              _TerrainSize;
            float               _TerrainHeightmapResolution;

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                float3 cellPos01 : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            VertexToFragment vert(uint vertexID : SV_VERTEXID, uint instanceID : SV_InstanceID)
            {
                VertexToFragment o;

                uint quadID = vertexID / 4;
                uint2 quadPos = uint2(quadID % uint(_TerrainHeightmapResolution), quadID / uint(_TerrainHeightmapResolution));
                float4 vertex = GetQuadVertexPosition(vertexID % 4);

                // flip quad to xz axis (default terrain orientation without rotation)
                vertex = float4(vertex.x, 0, vertex.y, 1);

                // Offset quad to create the plane terrain
                vertex.xz += (float2(quadPos) / float(_TerrainHeightmapResolution)) * _TerrainSize.xz;

                uint2 id = (quadPos / _TerrainSize.xz) * _TerrainHeightmapResolution;
                float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(uint3(quadPos, 0)));
                vertex.y += height * _TerrainSize.y * 2;

                o.uv = vertex.xz;
                // TODO: multiply by terrain size
                // vertex.xyz *= _TerrainSize.xz;

                float3 cellPos = mul(GetRawUnityObjectToWorld(), vertex).xyz;
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

                // trasnform pos between 0 1 to -1 1
                o.vertex.xyz = o.vertex.xyz * 2 - 1;

                return o;
            }

            float4 frag(VertexToFragment i) : COLOR
            {
                if (any(i.cellPos01 < 0) || any(i.cellPos01 >= 1))
                    return 0;

                // Offset the cellposition with the heightmap
                // float height = UnpackHeightmap(_TerrainHeightmapTexture.Sample(s_point_clamp_sampler, float3(i.uv, 0)));

                // i.cellPos01.y += height * _TerrainSize.y;

                // TODO: discard pixels above a hole
                // float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, uv).r;
                // clip(hole == 0.0f ? -1 : 1);

                uint3 pos = uint3(i.cellPos01 * _OutputSize);

                _Output[pos] = 1;

                return float4(i.cellPos01.xyz, 1);
            }
            ENDHLSL
        }
    }
}
