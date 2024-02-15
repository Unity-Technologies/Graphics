using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing debug shader resources used in URP.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Debug Shaders", Order = 1000), HideInInspector]
    public class UniversalRenderPipelineDebugShaders : IRenderPipelineResources
    {
        /// <summary>
        /// Version of the Debug Shader resources
        /// </summary>
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild =>
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            true;
#else
            false;
#endif

        [SerializeField]
        [ResourcePath("Shaders/Debug/DebugReplacement.shader")]
        Shader m_DebugReplacementPS;

        /// <summary>
        /// Debug shader used to output interpolated vertex attributes.
        /// </summary>
        public Shader debugReplacementPS
        {
            get => m_DebugReplacementPS;
            set => this.SetValueAndNotify(ref m_DebugReplacementPS, value, nameof(m_DebugReplacementPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/Debug/HDRDebugView.shader")]
        Shader m_HdrDebugViewPS;

        /// <summary>
        /// Debug shader used to output HDR Chromacity mapping.
        /// </summary>
        public Shader hdrDebugViewPS
        {
            get => m_HdrDebugViewPS;
            set => this.SetValueAndNotify(ref m_HdrDebugViewPS, value, nameof(m_HdrDebugViewPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/Debug/ProbeVolumeSamplingDebugPositionNormal.compute")]
        ComputeShader m_ProbeVolumeSamplingDebugComputeShader;

        /// <summary>
        /// Debug shader used to output world position and world normal for the pixel under the cursor.
        /// </summary>
        public ComputeShader probeVolumeSamplingDebugComputeShader
        {
            get => m_ProbeVolumeSamplingDebugComputeShader;
            set => this.SetValueAndNotify(ref m_ProbeVolumeSamplingDebugComputeShader, value, nameof(m_ProbeVolumeSamplingDebugComputeShader));
        }
    }
}
