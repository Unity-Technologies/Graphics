using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Modes for improved draw submission.
    /// </summary>
    public enum GPUResidentDrawerMode : byte
    {
        /// <summary>
        /// Default mode, GPU resident drawer will be disabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// If used, the BatchRendererGroup will be used for draw submission whenever possible.
        /// </summary>
        InstancedDrawing
    }

    /// <summary>
    /// Utility struct to pass GPU resident drawer settings together
    /// </summary>
    public struct GPUResidentDrawerSettings
    {
        /// <summary>
        /// Serialized settings of GPUResidentDrawer
        /// </summary>
        public GPUResidentDrawerMode mode;

        /// <summary>
        /// Does the implementor support dithered crossfade
        /// </summary>
        public bool supportDitheringCrossFade;

        /// <summary>
        /// Enable GPU data for occlusion culling
        /// </summary>
        public bool enableOcclusionCulling;

        /// <summary>
        /// Allows the GPU Resident Drawer to run in edit mode
        /// </summary>
        public bool allowInEditMode;

        /// <summary>
        /// If set to true, we opt-out from binding lightmaps as texture
        /// arrays. In this case, we bind them as individual textures, which
        /// breaks the batch every time lightmaps are changed. This is
        /// well-supported on all GPUs.
        /// If set to false, we opt for lightmap texture arrays. This minimizes
        /// batch breakages, but texture arrays aren't supported in a performant
        /// way on all GPUs.
        /// </summary>
        public bool useLegacyLightmaps;

		/// <summary>
        /// Default minimum screen percentage (0-20%) gpu-driven Renderers can cover before getting culled.
        /// </summary>
        public float smallMeshScreenPercentage;

#if UNITY_EDITOR
        /// <summary>
        /// Shader used if no custom picking pass has been implemented
        /// </summary>
        public Shader pickingShader;
#endif

        /// <summary>
        /// Shader used when an error is detected
        /// </summary>
        public Shader errorShader;

        /// <summary>
        /// Shader used while compiling shaders
        /// </summary>
        public Shader loadingShader;
    }

    /// <summary>
    /// Interface that can be added to a RenderPipelineAsset
    /// which indicates that it can support the GPUResidentDrawer.
    /// </summary>
    public interface IGPUResidentRenderPipeline
    {
        /// <summary>
        /// Gets the GPU resident drawer settings
        /// </summary>
        GPUResidentDrawerSettings gpuResidentDrawerSettings { get; }

        /// <summary>
        /// The mode the GPUResidentDrawer is configured for on this RenderPipeline
        /// </summary>
        GPUResidentDrawerMode gpuResidentDrawerMode
        {
            get;
            set;
        }

        /// <summary>
        /// Callback for use when the GPUResidentDrawer needs to be reinitialized.
        /// </summary>
        static void ReinitializeGPUResidentDrawer()
        {
            GPUResidentDrawer.Reinitialize();
        }

        /// <summary>
        /// Is the GPU resident drawer supported on this render pipeline.
        /// </summary>
        /// <param name="logReason">Should the reason for non support be logged?</param>
        /// <returns>true if supported</returns>
        bool IsGPUResidentDrawerSupportedBySRP(bool logReason = false)
        {
            return true;
        }

        /// <summary>
        /// Is GPUResidentDrawer supported on this current configuration?
        /// </summary>
        /// <param name="logReason">Should the reason for non support be logged?</param>
        /// <returns>true if supported</returns>
        static bool IsGPUResidentDrawerSupportedByProjectConfiguration(bool logReason = false)
        {
            return GPUResidentDrawer.IsProjectSupported(logReason);
        }

        /// <summary>
        /// Is GPUResidentDrawer currently enabled
        /// </summary>
        /// <returns>true if enabled</returns>
        static bool IsGPUResidentDrawerEnabled()
        {
            return GPUResidentDrawer.IsEnabled();
        }
    }
}
