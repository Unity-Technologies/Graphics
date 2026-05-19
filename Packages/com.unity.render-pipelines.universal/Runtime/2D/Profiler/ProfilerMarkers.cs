using System;
using Unity.Profiling;

namespace UnityEngine.Rendering.Universal.U2D.Profiler
{
    static class ProfilerMarkers
    {
        public static readonly Guid k_2DGraphicProfilerProjectId = new Guid("a7f8c3d4-9e2b-4a1c-8f6d-3b9e7a5c2d1f");

        // Light Pass Markers
        public static readonly string s_LightPass = "Light2D Pass";
        public static readonly string s_LightSRTPass = "Light2D SRT Pass";
        public static readonly string s_LightVolumetricPass = "Light2D Volumetric Pass";
        public static readonly string s_Light2DBatcher = "Light2D Batcher";

        public static readonly ProfilingSampler s_ProfilingSampler = new ProfilingSampler(s_LightPass);
        public static readonly ProfilingSampler s_ProfilingSampleSRT = new ProfilingSampler(s_LightSRTPass);
        public static readonly ProfilingSampler s_ProfilingSamplerVolume = new ProfilingSampler(s_LightVolumetricPass);
        public static readonly ProfilingSampler s_ProfilingDrawBatched = new ProfilingSampler(s_Light2DBatcher);

        // Shadow Pass Markers
        public static readonly string s_ShadowTexture = "Draw 2D Shadow Texture";

        public static readonly ProfilingSampler s_ProfilingSamplerShadows = new ProfilingSampler(s_ShadowTexture);

        // Render Pass Markers
        public static readonly string s_SetGlobalProperties = "SetGlobalProperties";
        public static readonly string s_RenderPass = "Renderer2D Pass";
        public static readonly string s_SetLightBlendTexture = "SetLightBlendTextures";
        public static readonly string s_UpscalePass = "Upscale2D Pass";
        public static readonly string s_DrawUpscale = "Draw Upscale";
        public static readonly string s_MormalPass = "Normal2D Pass";
        public static readonly string s_ShadowPass = "Shadow2D UnsafePass";
        public static readonly string s_ShadowVolumetricPass = "Shadow2D Volumetric UnsafePass";
        public static readonly string s_CopyCameraSortingLayerPass = "CopyCameraSortingLayer Pass";
        public static readonly string s_Copy = "Copy";

        public static readonly ProfilingSampler s_ProfilingSamplerSetGlobalProperties = new ProfilingSampler(s_SetGlobalProperties);
        public static readonly ProfilingSampler s_ProfilingSamplerRenderPass = new ProfilingSampler(s_RenderPass);
        public static readonly ProfilingSampler s_ProfilingSamplerSetLightBlendTexture = new ProfilingSampler(s_SetLightBlendTexture);
        public static readonly ProfilingSampler s_ProfilingSamplerUpscalePass = new ProfilingSampler(s_UpscalePass);
        public static readonly ProfilingSampler s_ProfilingSamplerDrawUpscale = new ProfilingSampler(s_DrawUpscale);
        public static readonly ProfilingSampler s_ProfilingSamplerNormalPass = new ProfilingSampler(s_MormalPass);
        public static readonly ProfilingSampler s_ProfilingSamplerShadowPass = new ProfilingSampler(s_ShadowPass);
        public static readonly ProfilingSampler s_ProfilingSamplerShadowVolumetricPass = new ProfilingSampler(s_ShadowVolumetricPass);
        public static readonly ProfilingSampler s_ProfilingSamplerCopyCameraSortingLayerPass = new ProfilingSampler(s_CopyCameraSortingLayerPass);
        public static readonly ProfilingSampler s_ProfilingSamplerCopy = new ProfilingSampler(s_Copy);

        public const string k_U2DNormalMapProfilerCounterName = "Normal Textures";
        public const string k_U2DLightProfilerCounterName = "Light Textures";
        public const string k_U2DShadowProfilerCounterName = "Shadow Textures";
        public const string k_U2DShadowCasterCounterName = "Shadow Casters Draw Calls";
        public const string k_U2DShadowVerticesCounterName = "Shadow Triangles";
        public const string k_U2DLightBatchCounterName = "Light Batches";
        public const string k_U2DLightTriangleCounterName = "Light Triangles";
#if ENABLE_PROFILER && PROFILER_INSTALLED
        public enum ProfilerFrameDataTag
        {
            ShadowFrameData = 0,
            LightFrameData = 1,
            LightRenderFrameData = 2,
            ShadowRenderFrameData = 3
        }

        public static readonly ProfilerCounterValue<int> s_U2DNormalMapProfilerCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.U2D, k_U2DNormalMapProfilerCounterName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static readonly ProfilerCounterValue<int> s_U2DLightProfilerCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.U2D, k_U2DLightProfilerCounterName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static readonly ProfilerCounterValue<int> s_U2DShadowProfilerCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.U2D, k_U2DShadowProfilerCounterName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static readonly ProfilerCounterValue<int> s_U2DShadowCasterCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.U2D, k_U2DShadowCasterCounterName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static readonly ProfilerCounterValue<int> s_U2DShadowVerticesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.U2D, k_U2DShadowVerticesCounterName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static readonly ProfilerCounterValue<int> s_U2DLightBatchCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.U2D, k_U2DLightBatchCounterName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static readonly ProfilerCounterValue<int> s_U2DLightTriangleCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.U2D, k_U2DLightTriangleCounterName, ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static MeshFrameDataProfilerEmitter s_LightMeshFrameData = new ((int)ProfilerFrameDataTag.LightFrameData, s_U2DLightTriangleCounterValue);
        public static MeshRenderFrameDataProfilerEmitter s_LightRenderFrameData = new ((int)ProfilerFrameDataTag.LightRenderFrameData);
        public static MeshFrameDataProfilerEmitter s_ShadowMeshFrameData = new ((int)ProfilerFrameDataTag.ShadowFrameData, s_U2DShadowVerticesCounterValue);
        public static MeshRenderFrameDataProfilerEmitter s_ShadowRenderFrameData = new ((int)ProfilerFrameDataTag.ShadowRenderFrameData);
#endif
    }
}
