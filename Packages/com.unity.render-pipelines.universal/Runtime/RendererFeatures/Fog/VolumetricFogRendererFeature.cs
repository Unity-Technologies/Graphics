#if VOLUMETRIC_FOG

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Renderer feature that drives URP's volumetric fog: voxelizes global and local fog
    /// volumes, ray-marches lit volume slices, and composites the result before transparents.
    /// </summary>
    [SupportedOnRenderer(typeof(UniversalRendererData))]
    [DisallowMultipleRendererFeature("Volumetric Fog")]
    [Tooltip("Renders volumetric fog by voxelizing global and local fog volumes, ray-marching lit volume slices, and compositing the result before transparents.")]
    public sealed class VolumetricFogRendererFeature : ScriptableRendererFeature
    {
        // IDs used by more than one pass live here. Single-callsite IDs are declared
        // in a private ShaderIDs class on the pass that uses them.
        internal static class ShaderIDs
        {
            public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            public static readonly int _CameraNearPlane = Shader.PropertyToID("_CameraNearPlane");
            public static readonly int _CameraPositionWS = Shader.PropertyToID("_CameraPositionWS");
            public static readonly int _DepthDecodingParams = Shader.PropertyToID("_DepthDecodingParams");
            public static readonly int _DepthEncodingParams = Shader.PropertyToID("_DepthEncodingParams");
            public static readonly int _VBuffer = Shader.PropertyToID("_VBuffer");
            public static readonly int _VBufferCoordToViewDirWS = Shader.PropertyToID("_VBufferCoordToViewDirWS");
            public static readonly int _VBufferDensity = Shader.PropertyToID("_VBufferDensity");
            public static readonly int _VBufferRcpSliceCount = Shader.PropertyToID("_VBufferRcpSliceCount");
            public static readonly int _VBufferSize = Shader.PropertyToID("_VBufferSize");
            public static readonly int _VBufferSliceCount = Shader.PropertyToID("_VBufferSliceCount");
            public static readonly int _ZBufferParams = Shader.PropertyToID("_ZBufferParams");
        }

        VolumetricFogGlobalsPass m_GlobalsPass;
        GenerateMaxZMaskPass m_MaxZPass;
        VolumeVoxelizationPass m_VoxelizationPass;
        VolumetricLightingPass m_LightingPass;
        VolumetricFilteringPass m_FilteringPass;
        ApplyVolumetricFogPass m_ApplyPass;
        ScreenSpaceMultipleScatteringPass m_SSMSPass;

        public override void Create()
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<VolumetricFogResources>(out _))
                return;

            // Tear down any passes from a previous call
            Dispose(true);

            m_GlobalsPass = new VolumetricFogGlobalsPass { renderPassEvent = RenderPassEvent.BeforeRenderingShadows };
            m_MaxZPass = new GenerateMaxZMaskPass { renderPassEvent = RenderPassEvent.AfterRenderingSkybox };
            m_VoxelizationPass = new VolumeVoxelizationPass { renderPassEvent = RenderPassEvent.AfterRenderingSkybox };
            m_LightingPass = new VolumetricLightingPass { renderPassEvent = RenderPassEvent.AfterRenderingSkybox };
            m_FilteringPass = new VolumetricFilteringPass { renderPassEvent = RenderPassEvent.AfterRenderingSkybox };
            m_ApplyPass = new ApplyVolumetricFogPass { renderPassEvent = RenderPassEvent.AfterRenderingSkybox };
            m_SSMSPass = new ScreenSpaceMultipleScatteringPass { renderPassEvent = RenderPassEvent.AfterRenderingSkybox };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_GlobalsPass == null)
                return;

            var fog = VolumeManager.instance.stack.GetComponent<FogVolumeComponent>();

            if (fog == null || !fog.IsActive() || renderingData.cameraData.camera.orthographic ||
                renderingData.cameraData.renderType == CameraRenderType.Overlay)
                return;

            m_MaxZPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            m_LightingPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            m_ApplyPass.ConfigureInput(ScriptableRenderPassInput.Depth);

            renderer.EnqueuePass(m_GlobalsPass);
            renderer.EnqueuePass(m_MaxZPass);
            renderer.EnqueuePass(m_VoxelizationPass);
            renderer.EnqueuePass(m_LightingPass);
            // Always enqueued; the pass internally no-ops when the Gaussian bit isn't set.
            renderer.EnqueuePass(m_FilteringPass);
            renderer.EnqueuePass(m_ApplyPass);
            // Always enqueued, but no-ops when multiple scattering intensity is 0 (the default).
            // That's the same condition under which the apply pass skips writing the opacity
            // buffer this pass would consume, so the two stay in lockstep.
            renderer.EnqueuePass(m_SSMSPass);
        }

        protected override void Dispose(bool disposing)
        {
            if (m_GlobalsPass == null)
                return;

            m_LightingPass.Dispose();
            m_ApplyPass.Dispose();
            m_SSMSPass.Dispose();

            m_GlobalsPass = null;
            m_MaxZPass = null;
            m_VoxelizationPass = null;
            m_LightingPass = null;
            m_FilteringPass = null;
            m_ApplyPass = null;
            m_SSMSPass = null;
        }
    }
}

#endif
