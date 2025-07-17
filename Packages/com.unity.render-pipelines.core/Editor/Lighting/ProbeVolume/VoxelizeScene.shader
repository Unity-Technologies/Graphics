Shader "Hidden/ProbeVolume/VoxelizeScene"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        HLSLINCLUDE
        #define EPSILON (1e-10)
        ENDHLSL

        Pass
        {
            Name "VoxelizeTerrain"

            Cull Off
            // ColorMask 0
            ZTest Off
            ZWrite Off
            ZClip Off

            HLSLPROGRAM
            #pragma vertex TerrainVert
            #pragma fragment TerrainFrag
            #pragma require randomwrite
            // #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VoxelizeScene.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "VoxelizeMesh"

            Cull Off
            ColorMask 0
            ZWrite Off
            ZClip Off
            ZTest Off

            HLSLPROGRAM
            #pragma vertex MeshVert
            #pragma fragment MeshFrag
            #pragma require randomwrite

            #include "Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VoxelizeScene.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "VoxelizeMeshInstanced"

            Cull Off
            ColorMask 0
            ZWrite Off
            ZClip Off
            ZTest Off

            HLSLPROGRAM
            #pragma vertex MeshVert
            #pragma fragment MeshFrag
            #pragma require randomwrite

            #define USE_INSTANCE_TRANSFORMS 1
            #include "Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VoxelizeScene.hlsl"

            ENDHLSL
        }
    }
}
