using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    [Serializable]
    [SupportedOnRenderPipeline()]
    [Category("Probe Volume")]
    [HideInInspector]
    class ProbeVolumeRuntimeResources : IRenderPipelineResources
    {
        [SerializeField, HideInInspector]
        int m_Version = 1;

        public int version { get => m_Version; }

        [ResourcePath("Runtime/Lighting/ProbeVolume/ProbeVolumeBlendStates.compute")]
        public ComputeShader probeVolumeBlendStatesCS;
        [ResourcePath("Runtime/Lighting/ProbeVolume/ProbeVolumeUploadData.compute")]
        public ComputeShader probeVolumeUploadDataCS;
        [ResourcePath("Runtime/Lighting/ProbeVolume/ProbeVolumeUploadDataL2.compute")]
        public ComputeShader probeVolumeUploadDataL2CS;
    }

    [Serializable]
    [SupportedOnRenderPipeline()]
    [Category("Probe Volume")]
    [HideInInspector]
    class ProbeVolumeDebugResources : IRenderPipelineResources
    {
        [SerializeField, HideInInspector]
        int m_Version = 1;

        public int version { get => m_Version; }

        [ResourcePath("Runtime/Debug/ProbeVolumeDebug.shader")]
        public Shader probeVolumeDebugShader;
        [ResourcePath("Runtime/Debug/ProbeVolumeFragmentationDebug.shader")]
        public Shader probeVolumeFragmentationDebugShader;
        [ResourcePath("Runtime/Debug/ProbeVolumeSamplingDebug.shader")]
        public Shader probeVolumeSamplingDebugShader;
        [ResourcePath("Runtime/Debug/ProbeVolumeOffsetDebug.shader")]
        public Shader probeVolumeOffsetDebugShader;
        [ResourcePath("Runtime/Debug/ProbeSamplingDebugMesh.fbx")]
        public Mesh probeSamplingDebugMesh;
        [ResourcePath("Runtime/Debug/ProbeVolumeNumbersDisplayTex.png")]
        public Texture2D numbersDisplayTex;
    }


    [Serializable]
    [SupportedOnRenderPipeline()]
    [Category("Probe Volume")]
    class ProbeVolumeGlobalSettings : IRenderPipelineGraphicsSettings
    {
        [SerializeField, HideInInspector]
        int m_Version = 1;
        [SerializeField, Tooltip("Enabling this will make APV baked data assets compatible with Addressables and Asset Bundles. This will also make Disk Streaming unavailable.")]
        bool m_ProbeVolumeDisableStreamingAssets;

        public int version { get => m_Version; }

        public bool probeVolumeDisableStreamingAssets
        {
            get => m_ProbeVolumeDisableStreamingAssets;
            set => this.SetValueAndNotify(ref m_ProbeVolumeDisableStreamingAssets, value, nameof(m_ProbeVolumeDisableStreamingAssets));
        }
    }
}
