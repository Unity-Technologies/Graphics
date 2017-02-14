using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDRenderPipelineSetup : ScriptableObject
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("RenderPipeline/CreateHDPipelineSetup")]
        static void CreateHDRenderPipelineSetup()
        {
            var instance = ScriptableObject.CreateInstance<HDRenderPipelineSetup>();
            UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/HDRenderPipelineSetup.asset");
        }
#endif
        public ComputeShader buildScreenAABBShader = null;
        public ComputeShader buildPerTileLightListShader = null;     // FPTL
        public ComputeShader buildPerBigTileLightListShader = null;
        public ComputeShader buildPerVoxelLightListShader = null;    // clustered
        public ComputeShader shadeOpaqueShader = null;

        // Various set of material use in render loop
        public Shader m_DebugViewMaterialGBuffer;

        // For image based lighting
        public Shader m_InitPreFGD;

    }
}
