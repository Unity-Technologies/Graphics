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
            ZWrite Off
            ZClip Off

            HLSLPROGRAM
            #pragma vertex TerrainVert
            #pragma fragment TerrainFrag
            #pragma target 4.5
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
            Conservative True

            HLSLPROGRAM
            #pragma vertex ConservativeVertex
            #pragma geometry ConservativeGeom
            #pragma fragment ConservativeFrag
            #pragma target 4.5
            #pragma require geometry
            // #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VoxelizeScene.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "VoxelizeTree"

            Cull Off
            ColorMask 0
            ZWrite Off
            ZClip Off
            Conservative True

            HLSLPROGRAM
            #pragma vertex ConservativeVertex
            #pragma geometry ConservativeGeom
            #pragma fragment ConservativeFrag
            #pragma target 4.5
            #pragma require geometry
            //#pragma enable_d3d11_debug_symbols

            #pragma multi_compile _ PROCEDURAL_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VoxelizeScene.hlsl"
            ENDHLSL
        }
    }

    // Fallback subshader for platform that don't support geometry shaders
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
            ZWrite Off
            ZClip Off

            HLSLPROGRAM
            #pragma vertex TerrainVert
            #pragma fragment TerrainFrag
            #pragma target 4.5
            // #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VoxelizeScene.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "VoxelizeMeshFallback"

            Cull Off
            ColorMask 0
            ZWrite Off
            ZClip Off

            HLSLPROGRAM
            #pragma vertex MeshVert
            #pragma fragment MeshFrag
            #pragma target 4.5
            // #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VoxelizeScene.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "VoxelizeTreeFallback"

            Cull Off
            ColorMask 0
            ZWrite Off
            ZClip Off

            HLSLPROGRAM
            #pragma vertex MeshVert
            #pragma fragment MeshFrag
            #pragma target 4.5
            //#pragma enable_d3d11_debug_symbols

            #pragma multi_compile _ PROCEDURAL_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VoxelizeScene.hlsl"

            ENDHLSL
        }
    }
}
