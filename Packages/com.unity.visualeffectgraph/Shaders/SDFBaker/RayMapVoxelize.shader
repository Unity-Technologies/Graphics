Shader "Hidden/VoxelizeShader"
{

    SubShader
    {

        HLSLINCLUDE
        #include "Packages/com.unity.visualeffectgraph/Shaders/SDFBaker/SdfUtils.hlsl"
        #define AABB_EPS 1e-5

    // Vertex input attributes
    struct Attributes
    {
        uint vertexId : SV_VertexID;
    };

    // Fragment varyings
    struct Varyings
    {
        float4 position : SV_POSITION;
        uint triangleID : TEXCOORD0;
    };
    StructuredBuffer<float4> vertices;
    StructuredBuffer<int> coordFlip;

    RWStructuredBuffer<float4> voxels : register(u1);
    RWStructuredBuffer<float4> aabb : register(u4);
    RWStructuredBuffer<uint> counter : register(u2);
    RWStructuredBuffer<uint> triangleIDs : register(u3);

    int currentAxis;

    int dimX, dimY, dimZ;

    uint id3(int i, int j, int k)
    {
        return (uint)(i + dimX * j + dimX * dimY * k);
    }
    uint id3(int3 coord)
    {
        return id3(coord.x, coord.y, coord.z);
    }


    float2 GetCustomScreenParams()
    {
        float2 myScreenParams;
        if (currentAxis == 1)
        {
            myScreenParams = float2(dimZ, dimX);
        }
        else if (currentAxis == 2)
        {
            myScreenParams = float2(dimY, dimZ);
        }
        else
        {
            myScreenParams = float2(dimX, dimY);
        }
        return myScreenParams;
    }
    void ScreenToUV(inout float4 pos, float2 myScreenParams)
    {
        #if UNITY_REVERSED_Z
            pos.z = 1.0f - pos.z;
        #endif
        pos.xy = pos.xy / myScreenParams;
        #if UNITY_UV_STARTS_AT_TOP
            pos.y = 1 - pos.y;
        #endif
    }
    void CullWithAABB(float4 pos, int triangleID)
    {
        float2 ndc_pos = pos.xy * 2.0f - 1.0f;
        if (ndc_pos.x < aabb[triangleID].x - AABB_EPS ||
        ndc_pos.y < aabb[triangleID].y - AABB_EPS ||
        ndc_pos.x > aabb[triangleID].z + AABB_EPS ||
        ndc_pos.y > aabb[triangleID].w + AABB_EPS)
        {
            discard;
        }
    }

    void ComputeCoordAndDepthStep(float2 myScreenParams, float4 pos, out int3 coord, out int3 depthStep, out bool stepMinus, out bool stepPlus)
    {
        stepPlus = true;
        stepMinus = true; //TODO : Now we're conservative about how we share triangle data across neighbouring cells, to fix visible artefacts
        if (currentAxis == 1)
        {
            coord = (pos.xyz * float3(myScreenParams, dimY));
            coord.xyz = coord.yzx;
            depthStep = int3(0, 1, 0);
            if (coord.y == 0)
            {
                stepMinus = false;
            }
            if (coord.y == dimY - 1)
            {
                stepPlus = false;
            }
        }
        else if (currentAxis == 2)
        {
            coord = (pos.xyz * float3(myScreenParams, dimX));
            coord.xyz = coord.zxy;
            depthStep = int3(1, 0, 0);
            if (coord.x == 0)
            {
                stepMinus = false;
            }
            if (coord.x == dimX - 1)
            {
                stepPlus = false;
            }

        }
        else
        {
            coord = (pos.xyz * float3(myScreenParams, dimZ));
            depthStep = int3(0, 0, 1);
            if (coord.z == 0)
            {
                stepMinus = false;
            }
            if (coord.z == dimZ - 1)
            {
                stepPlus = false;
            }
        }
    }

    void GetCellCoordinatesData(Varyings input, out int3 coord, out int3 depthStep, out bool stepMinus, out bool stepPlus)
    {
        float4 pos = input.position;

        float2 myScreenParams = GetCustomScreenParams();
        ScreenToUV(pos, myScreenParams);

        CullWithAABB(pos, input.triangleID);

        ComputeCoordAndDepthStep(myScreenParams, pos, coord, depthStep, stepMinus, stepPlus);
    }



    Varyings Vertex(Attributes input)
    {
        Varyings o = (Varyings)(0);
        float4 pos = vertices[input.vertexId];
        o.triangleID = input.vertexId / 3;
        if (coordFlip[o.triangleID] != currentAxis)
        {
            o.position = float4(-1,-1,-1,-1);
            return o;
        }
        o.position = pos;
        return o;
    }

    ENDHLSL
        // Shader code
        Pass{

            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 5.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Fragment(Varyings input) : SV_Target{

                int3 depthStep, coord;
                bool stepMinus, stepPlus;

                GetCellCoordinatesData(input, coord, depthStep, stepMinus, stepPlus);

                float3 voxelUV = ((float3(coord)+float3(0.5f, 0.5f, 0.5f)) / Max3(dimX, dimY, dimZ));
                voxels[id3(coord)] = float4(voxelUV, 1.0f);
                InterlockedAdd(counter[id3(coord)], 1u);
                if (stepPlus)
                {
                    voxels[id3(coord + depthStep)] = float4(voxelUV, 1.0f);
                    InterlockedAdd(counter[id3(coord + depthStep)], 1u);
                }
                if (stepMinus)
                {
                    voxels[id3(coord - depthStep)] = float4(voxelUV, 1.0f);
                    InterlockedAdd(counter[id3(coord - depthStep)], 1u);
                }

                return float4(voxelUV,1);
            }
            ENDHLSL
        }

        Pass
        {

            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 5.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            float4
            Fragment(Varyings input) : SV_Target
            {
                int3 depthStep, coord;
                bool stepMinus, stepPlus;

                GetCellCoordinatesData(input, coord, depthStep, stepMinus, stepPlus);

                uint indexTri = 0u;

                InterlockedAdd(counter[id3(coord)], 1, indexTri);
                triangleIDs[indexTri] = input.triangleID;
                if (stepPlus)
                {
                    InterlockedAdd(counter[id3(coord + depthStep)], 1, indexTri);
                    triangleIDs[indexTri] = input.triangleID;
                }
                if (stepMinus)
                {
                    InterlockedAdd(counter[id3(coord - depthStep)], 1, indexTri);
                    triangleIDs[indexTri] = input.triangleID;
                }
                return input.position;
            }
            ENDHLSL
        }
    }
}
