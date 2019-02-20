using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    [Serializable, VolumeComponentMenu("Lighting/Ambient Occlusion")]
    public sealed class AmbientOcclusion : VolumeComponent
    {
        [Tooltip("Degree of darkness added by ambient occlusion.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);

        [Tooltip("Modifies thickness of occluders. This increases dark areas but also introduces dark halo around objects.")]
        public ClampedFloatParameter thicknessModifier = new ClampedFloatParameter(1f, 1f, 10f);

        [Tooltip("Defines how much of the occlusion should be affected by ambient lighting.")]
        public ClampedFloatParameter directLightingStrength = new ClampedFloatParameter(0f, 0f, 1f);

        // Hidden parameters
        [HideInInspector] public ClampedFloatParameter noiseFilterTolerance = new ClampedFloatParameter(0f, -8f, 0f);
        [HideInInspector] public ClampedFloatParameter blurTolerance = new ClampedFloatParameter(-4.6f, -8f, 1f);
        [HideInInspector] public ClampedFloatParameter upsampleTolerance = new ClampedFloatParameter(-12f, -12f, -1f);
    }

    public class AmbientOcclusionSystem
    {
        enum MipLevel { Original, L1, L2, L3, L4, L5, L6, Count }

        RenderPipelineResources m_Resources;
        RenderPipelineSettings m_Settings;

        // The arrays below are reused between frames to reduce GC allocation.
        readonly float[] m_SampleThickness =
        {
            Mathf.Sqrt(1f - 0.2f * 0.2f),
            Mathf.Sqrt(1f - 0.4f * 0.4f),
            Mathf.Sqrt(1f - 0.6f * 0.6f),
            Mathf.Sqrt(1f - 0.8f * 0.8f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.2f * 0.2f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.4f * 0.4f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.6f * 0.6f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.8f * 0.8f),
            Mathf.Sqrt(1f - 0.4f * 0.4f - 0.4f * 0.4f),
            Mathf.Sqrt(1f - 0.4f * 0.4f - 0.6f * 0.6f),
            Mathf.Sqrt(1f - 0.4f * 0.4f - 0.8f * 0.8f),
            Mathf.Sqrt(1f - 0.6f * 0.6f - 0.6f * 0.6f)
        };

        readonly float[] m_InvThicknessTable = new float[12];
        readonly float[] m_SampleWeightTable = new float[12];

        readonly int[] m_Widths = new int[7];
        readonly int[] m_Heights = new int[7];

        readonly RTHandle m_AmbientOcclusionTex;

        // All the targets needed are pre-allocated and only released on cleanup for now to avoid
        // having to constantly allo/dealloc on every frame
        readonly RTHandle m_LinearDepthTex;

        readonly RTHandle m_LowDepth1Tex;
        readonly RTHandle m_LowDepth2Tex;
        readonly RTHandle m_LowDepth3Tex;
        readonly RTHandle m_LowDepth4Tex;

        readonly RTHandle m_TiledDepth1Tex;
        readonly RTHandle m_TiledDepth2Tex;
        readonly RTHandle m_TiledDepth3Tex;
        readonly RTHandle m_TiledDepth4Tex;

        readonly RTHandle m_Occlusion1Tex;
        readonly RTHandle m_Occlusion2Tex;
        readonly RTHandle m_Occlusion3Tex;
        readonly RTHandle m_Occlusion4Tex;

        readonly RTHandle m_Combined1Tex;
        readonly RTHandle m_Combined2Tex;
        readonly RTHandle m_Combined3Tex;

        readonly ScaleFunc[] m_ScaleFunctors;

        // MSAA-specifics
        readonly RTHandle m_MultiAmbientOcclusionTex;
        readonly MaterialPropertyBlock m_ResolvePropertyBlock;
        readonly Material m_ResolveMaterial;

#if ENABLE_RAYTRACING
        public HDRaytracingManager m_RayTracingManager = new HDRaytracingManager();
        readonly HDRaytracingAmbientOcclusion m_RaytracingAmbientOcclusion = new HDRaytracingAmbientOcclusion();
#endif

        public AmbientOcclusionSystem(HDRenderPipelineAsset hdAsset)
        {
            m_Settings = hdAsset.currentPlatformRenderPipelineSettings;
            m_Resources = hdAsset.renderPipelineResources;

            if (!hdAsset.currentPlatformRenderPipelineSettings.supportSSAO)
                return;

            bool supportMSAA = hdAsset.currentPlatformRenderPipelineSettings.supportMSAA;

            // Destination targets
            m_AmbientOcclusionTex = RTHandles.Alloc(Vector2.one,
                filterMode: FilterMode.Bilinear,
                colorFormat: GraphicsFormat.R8_UNorm,
                enableRandomWrite: true,
                xrInstancing: true,
                useDynamicScale: true,
                name: "Ambient Occlusion"
            );

            if (supportMSAA)
            {
                m_MultiAmbientOcclusionTex = RTHandles.Alloc(Vector2.one,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: GraphicsFormat.R8G8_UNorm,
                    enableRandomWrite: true,
                    xrInstancing: true,
                    useDynamicScale: true,
                    name: "Ambient Occlusion MSAA"
                );

                m_ResolveMaterial = CoreUtils.CreateEngineMaterial(m_Resources.shaders.aoResolvePS);
                m_ResolvePropertyBlock = new MaterialPropertyBlock();
            }

            // Prepare scale functors
            m_ScaleFunctors = new ScaleFunc[(int)MipLevel.Count];
            m_ScaleFunctors[0] = size => size; // 0 is original size (mip0)

            for (int i = 1; i < m_ScaleFunctors.Length; i++)
            {
                int mult = i;
                m_ScaleFunctors[i] = size =>
                {
                    int div = 1 << mult;
                    return new Vector2Int(
                        (size.x + (div - 1)) / div,
                        (size.y + (div - 1)) / div
                    );
                };
            }

            var fmtFP16 = supportMSAA ? GraphicsFormat.R16G16_SFloat  : GraphicsFormat.R16_SFloat;
            var fmtFP32 = supportMSAA ? GraphicsFormat.R32G32_SFloat : GraphicsFormat.R32_SFloat;
            var fmtFX8  = supportMSAA ? GraphicsFormat.R8G8_UNorm    : GraphicsFormat.R8_UNorm;

            // All of these are pre-allocated to 1x1 and will be automatically scaled properly by
            // the internal RTHandle system
            Alloc(out m_LinearDepthTex, MipLevel.Original, fmtFP16, true, "AOLinearDepth");

            Alloc(out m_LowDepth1Tex, MipLevel.L1, fmtFP32, true, "AOLowDepth1");
            Alloc(out m_LowDepth2Tex, MipLevel.L2, fmtFP32, true, "AOLowDepth2");
            Alloc(out m_LowDepth3Tex, MipLevel.L3, fmtFP32, true, "AOLowDepth3");
            Alloc(out m_LowDepth4Tex, MipLevel.L4, fmtFP32, true, "AOLowDepth4");

            AllocArray(out m_TiledDepth1Tex, MipLevel.L3, fmtFP16, true, "AOTiledDepth1");
            AllocArray(out m_TiledDepth2Tex, MipLevel.L4, fmtFP16, true, "AOTiledDepth2");
            AllocArray(out m_TiledDepth3Tex, MipLevel.L5, fmtFP16, true, "AOTiledDepth3");
            AllocArray(out m_TiledDepth4Tex, MipLevel.L6, fmtFP16, true, "AOTiledDepth4");

            Alloc(out m_Occlusion1Tex, MipLevel.L1, fmtFX8, true, "AOOcclusion1");
            Alloc(out m_Occlusion2Tex, MipLevel.L2, fmtFX8, true, "AOOcclusion2");
            Alloc(out m_Occlusion3Tex, MipLevel.L3, fmtFX8, true, "AOOcclusion3");
            Alloc(out m_Occlusion4Tex, MipLevel.L4, fmtFX8, true, "AOOcclusion4");

            Alloc(out m_Combined1Tex, MipLevel.L1, fmtFX8, true, "AOCombined1");
            Alloc(out m_Combined2Tex, MipLevel.L2, fmtFX8, true, "AOCombined2");
            Alloc(out m_Combined3Tex, MipLevel.L3, fmtFX8, true, "AOCombined3");
        }

        public void Cleanup()
        {
#if ENABLE_RAYTRACING
            m_RaytracingAmbientOcclusion.Release();
#endif

            CoreUtils.Destroy(m_ResolveMaterial);

            RTHandles.Release(m_AmbientOcclusionTex);
            RTHandles.Release(m_MultiAmbientOcclusionTex);

            RTHandles.Release(m_LinearDepthTex);

            RTHandles.Release(m_LowDepth1Tex);
            RTHandles.Release(m_LowDepth2Tex);
            RTHandles.Release(m_LowDepth3Tex);
            RTHandles.Release(m_LowDepth4Tex);

            RTHandles.Release(m_TiledDepth1Tex);
            RTHandles.Release(m_TiledDepth2Tex);
            RTHandles.Release(m_TiledDepth3Tex);
            RTHandles.Release(m_TiledDepth4Tex);

            RTHandles.Release(m_Occlusion1Tex);
            RTHandles.Release(m_Occlusion2Tex);
            RTHandles.Release(m_Occlusion3Tex);
            RTHandles.Release(m_Occlusion4Tex);

            RTHandles.Release(m_Combined1Tex);
            RTHandles.Release(m_Combined2Tex);
            RTHandles.Release(m_Combined3Tex);
        }

#if ENABLE_RAYTRACING
        public void InitRaytracing(HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager)
        {
            m_RayTracingManager = raytracingManager;
            m_RaytracingAmbientOcclusion.Init(m_Resources, m_Settings, m_RayTracingManager, sharedRTManager);
        }
#endif

        public bool IsActive(HDCamera camera, AmbientOcclusion settings) => camera.frameSettings.IsEnabled(FrameSettingsField.SSAO) && settings.intensity.value > 0f;

        public void Render(CommandBuffer cmd, HDCamera camera, SharedRTManager sharedRTManager, ScriptableRenderContext renderContext, uint frameCount)
        {

#if ENABLE_RAYTRACING
            HDRaytracingEnvironment rtEnvironement = m_RayTracingManager.CurrentEnvironment();
            if (rtEnvironement != null && rtEnvironement.raytracedAO)
                m_RaytracingAmbientOcclusion.RenderAO(camera, cmd, m_AmbientOcclusionTex, renderContext, frameCount);
            else
#endif
            {
                Dispatch(cmd, camera, sharedRTManager);
                PostDispatchWork(cmd, camera, sharedRTManager);
            }
        }

        public void Dispatch(CommandBuffer cmd, HDCamera camera, SharedRTManager sharedRTManager)
        {
            // Grab current settings
            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            if (!IsActive(camera, settings))
                return;

            using (new ProfilingSample(cmd, "Render SSAO", CustomSamplerId.RenderSSAO.GetSampler()))
            {
                // Base size
                m_Widths[0] = camera.actualWidth;
                m_Heights[0] = camera.actualHeight;

                // L1 -> L6 sizes
                // We need to recalculate these on every frame, we can't rely on RTHandle width/height
                // values as they may have been rescaled and not the actual size we want
                for (int i = 1; i < (int)MipLevel.Count; i++)
                {
                    int div = 1 << i;
                    m_Widths[i] = (m_Widths[0] + (div - 1)) / div;
                    m_Heights[i] = (m_Heights[0] + (div - 1)) / div;
                }

                // Grab current viewport scale factor - needed to handle RTHandle auto resizing
                var viewport = camera.viewportScale;

                // Textures used for rendering
                RTHandle depthMap, destination;
                bool msaa = camera.frameSettings.IsEnabled(FrameSettingsField.MSAA);

                if (msaa)
                {
                    depthMap = sharedRTManager.GetDepthValuesTexture();
                    destination = m_MultiAmbientOcclusionTex;
                }
                else
                {
                    depthMap = sharedRTManager.GetDepthTexture();
                    destination = m_AmbientOcclusionTex;
                }

                // Render logic
                PushDownsampleCommands(cmd, depthMap, msaa);

                float tanHalfFovH = CalculateTanHalfFovHeight(camera);
                PushRenderCommands(cmd, viewport, m_TiledDepth1Tex, m_Occlusion1Tex, settings, GetSizeArray(MipLevel.L3), tanHalfFovH, msaa);
                PushRenderCommands(cmd, viewport, m_TiledDepth2Tex, m_Occlusion2Tex, settings, GetSizeArray(MipLevel.L4), tanHalfFovH, msaa);
                PushRenderCommands(cmd, viewport, m_TiledDepth3Tex, m_Occlusion3Tex, settings, GetSizeArray(MipLevel.L5), tanHalfFovH, msaa);
                PushRenderCommands(cmd, viewport, m_TiledDepth4Tex, m_Occlusion4Tex, settings, GetSizeArray(MipLevel.L6), tanHalfFovH, msaa);

                PushUpsampleCommands(cmd, viewport, m_LowDepth4Tex, m_Occlusion4Tex, m_LowDepth3Tex,   m_Occlusion3Tex, m_Combined3Tex, settings, GetSize(MipLevel.L4), GetSize(MipLevel.L3),       msaa);
                PushUpsampleCommands(cmd, viewport, m_LowDepth3Tex, m_Combined3Tex,  m_LowDepth2Tex,   m_Occlusion2Tex, m_Combined2Tex, settings, GetSize(MipLevel.L3), GetSize(MipLevel.L2),       msaa);
                PushUpsampleCommands(cmd, viewport, m_LowDepth2Tex, m_Combined2Tex,  m_LowDepth1Tex,   m_Occlusion1Tex, m_Combined1Tex, settings, GetSize(MipLevel.L2), GetSize(MipLevel.L1),       msaa);
                PushUpsampleCommands(cmd, viewport, m_LowDepth1Tex, m_Combined1Tex,  m_LinearDepthTex, null,            destination,    settings, GetSize(MipLevel.L1), GetSize(MipLevel.Original), msaa);
            }
        }

        public void PostDispatchWork(CommandBuffer cmd, HDCamera camera, SharedRTManager sharedRTManager)
        {
            // Grab current settings
            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            if (!IsActive(camera, settings))
            {
                // No AO applied - neutral is black, see the comment in the shaders
                cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, TextureXR.GetBlackTexture());
                cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, Vector4.zero);
                return;
            }

            // MSAA Resolve
            if (camera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                using (new ProfilingSample(cmd, "Resolve AO Buffer", CustomSamplerId.ResolveSSAO.GetSampler()))
                {
                    HDUtils.SetRenderTarget(cmd, camera, m_AmbientOcclusionTex);
                    m_ResolvePropertyBlock.SetTexture(HDShaderIDs._DepthValuesTexture, sharedRTManager.GetDepthValuesTexture());
                    m_ResolvePropertyBlock.SetTexture(HDShaderIDs._MultiAmbientOcclusionTexture, m_MultiAmbientOcclusionTex);
                    cmd.DrawProcedural(Matrix4x4.identity, m_ResolveMaterial, 0, MeshTopology.Triangles, 3, 1, m_ResolvePropertyBlock);
                }
            }

            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, m_AmbientOcclusionTex);
            cmd.SetGlobalVector(HDShaderIDs._AmbientOcclusionParam, new Vector4(0f, 0f, 0f, settings.directLightingStrength.value));

            // TODO: All the pushdebug stuff should be centralized somewhere
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(camera, cmd, m_AmbientOcclusionTex, FullScreenDebugMode.SSAO);
        }

        void Alloc(out RTHandle rt, MipLevel size, GraphicsFormat format, bool uav, string name)
        {
            rt = RTHandles.Alloc(
                scaleFunc: m_ScaleFunctors[(int)size],
                dimension: TextureDimension.Tex2D,
                colorFormat: format,
                depthBufferBits: DepthBits.None,
                autoGenerateMips: false,
                enableMSAA: false,
                useDynamicScale: true,
                enableRandomWrite: uav,
                filterMode: FilterMode.Point,
                xrInstancing: true,
                name: name
            );
        }

        void AllocArray(out RTHandle rt, MipLevel size, GraphicsFormat format, bool uav, string name)
        {
            rt = RTHandles.Alloc(
                scaleFunc: m_ScaleFunctors[(int)size],
                dimension: TextureDimension.Tex2DArray,
                colorFormat: format,
                depthBufferBits: DepthBits.None,
                slices: 16,
                autoGenerateMips: false,
                enableMSAA: false,
                useDynamicScale: true,
                enableRandomWrite: uav,
                filterMode: FilterMode.Point,
                xrInstancing: true,
                name: name
            );
        }

        float CalculateTanHalfFovHeight(HDCamera camera)
        {
            return 1f / camera.projMatrix[0, 0];
        }

        Vector2 GetSize(MipLevel mip)
        {
            return new Vector2(m_Widths[(int)mip], m_Heights[(int)mip]);
        }

        Vector3 GetSizeArray(MipLevel mip)
        {
            return new Vector3(m_Widths[(int)mip], m_Heights[(int)mip], 16);
        }

        void PushDownsampleCommands(CommandBuffer cmd, RTHandle depthMap, bool msaa)
        {
            var kernelName = msaa ? "KMain_MSAA" : "KMain";

            // 1st downsampling pass.
            var cs = m_Resources.shaders.aoDownsample1CS;
            int kernel = cs.FindKernel(kernelName);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._LinearZ, m_LinearDepthTex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DS2x, m_LowDepth1Tex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DS4x, m_LowDepth2Tex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DS2xAtlas, m_TiledDepth1Tex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DS4xAtlas, m_TiledDepth2Tex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Depth, depthMap, 0);

            cmd.DispatchCompute(cs, kernel, m_Widths[(int)MipLevel.L4], m_Heights[(int)MipLevel.L4], XRGraphics.computePassCount);

            // 2nd downsampling pass.
            cs = m_Resources.shaders.aoDownsample2CS;
            kernel = cs.FindKernel(kernelName);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DS4x, m_LowDepth2Tex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DS8x, m_LowDepth3Tex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DS16x, m_LowDepth4Tex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DS8xAtlas, m_TiledDepth3Tex);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._DS16xAtlas, m_TiledDepth4Tex);

            cmd.DispatchCompute(cs, kernel, m_Widths[(int)MipLevel.L6], m_Heights[(int)MipLevel.L6], XRGraphics.computePassCount);
        }

        void PushRenderCommands(CommandBuffer cmd, in Vector4 viewport, RTHandle source, RTHandle destination, AmbientOcclusion settings, in Vector3 sourceSize, float tanHalfFovH, bool msaa)
        {
            // Here we compute multipliers that convert the center depth value into (the reciprocal
            // of) sphere thicknesses at each sample location. This assumes a maximum sample radius
            // of 5 units, but since a sphere has no thickness at its extent, we don't need to
            // sample that far out. Only samples whole integer offsets with distance less than 25
            // are used. This means that there is no sample at (3, 4) because its distance is
            // exactly 25 (and has a thickness of 0.)

            // The shaders are set up to sample a circular region within a 5-pixel radius.
            const float kScreenspaceDiameter = 10f;

            // SphereDiameter = CenterDepth * ThicknessMultiplier. This will compute the thickness
            // of a sphere centered at a specific depth. The ellipsoid scale can stretch a sphere
            // into an ellipsoid, which changes the characteristics of the AO.
            // TanHalfFovH: Radius of sphere in depth units if its center lies at Z = 1
            // ScreenspaceDiameter: Diameter of sample sphere in pixel units
            // ScreenspaceDiameter / BufferWidth: Ratio of the screen width that the sphere actually covers
            float thicknessMultiplier = 2f * tanHalfFovH * kScreenspaceDiameter / sourceSize.x;

            // This will transform a depth value from [0, thickness] to [0, 1].
            float inverseRangeFactor = 1f / thicknessMultiplier;

            // The thicknesses are smaller for all off-center samples of the sphere. Compute
            // thicknesses relative to the center sample.
            for (int i = 0; i < 12; i++)
                m_InvThicknessTable[i] = inverseRangeFactor / m_SampleThickness[i];

            // These are the weights that are multiplied against the samples because not all samples
            // are equally important. The farther the sample is from the center location, the less
            // they matter. We use the thickness of the sphere to determine the weight.  The scalars
            // in front are the number of samples with this weight because we sum the samples
            // together before multiplying by the weight, so as an aggregate all of those samples
            // matter more. After generating this table, the weights are normalized.
            m_SampleWeightTable[ 0] = 4 * m_SampleThickness[ 0];    // Axial
            m_SampleWeightTable[ 1] = 4 * m_SampleThickness[ 1];    // Axial
            m_SampleWeightTable[ 2] = 4 * m_SampleThickness[ 2];    // Axial
            m_SampleWeightTable[ 3] = 4 * m_SampleThickness[ 3];    // Axial
            m_SampleWeightTable[ 4] = 4 * m_SampleThickness[ 4];    // Diagonal
            m_SampleWeightTable[ 5] = 8 * m_SampleThickness[ 5];    // L-shaped
            m_SampleWeightTable[ 6] = 8 * m_SampleThickness[ 6];    // L-shaped
            m_SampleWeightTable[ 7] = 8 * m_SampleThickness[ 7];    // L-shaped
            m_SampleWeightTable[ 8] = 4 * m_SampleThickness[ 8];    // Diagonal
            m_SampleWeightTable[ 9] = 8 * m_SampleThickness[ 9];    // L-shaped
            m_SampleWeightTable[10] = 8 * m_SampleThickness[10];    // L-shaped
            m_SampleWeightTable[11] = 4 * m_SampleThickness[11];    // Diagonal

            // Zero out the unused samples.
            // FIXME: should we support SAMPLE_EXHAUSTIVELY mode?
            m_SampleWeightTable[0] = 0;
            m_SampleWeightTable[2] = 0;
            m_SampleWeightTable[5] = 0;
            m_SampleWeightTable[7] = 0;
            m_SampleWeightTable[9] = 0;

            // Normalize the weights by dividing by the sum of all weights
            float totalWeight = 0f;

            foreach (float w in m_SampleWeightTable)
                totalWeight += w;

            for (int i = 0; i < m_SampleWeightTable.Length; i++)
                m_SampleWeightTable[i] /= totalWeight;

            // Set the arguments for the render kernel.
            var cs = m_Resources.shaders.aoRenderCS;
            int kernel = cs.FindKernel(msaa ? "KMainInterleaved_MSAA" : "KMainInterleaved");

            cmd.SetComputeFloatParams(cs, HDShaderIDs._InvThicknessTable, m_InvThicknessTable);
            cmd.SetComputeFloatParams(cs, HDShaderIDs._SampleWeightTable, m_SampleWeightTable);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._InvSliceDimension, new Vector2(1f / sourceSize.x * viewport.x, 1f / sourceSize.y * viewport.y));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdditionalParams, new Vector2(-1f / settings.thicknessModifier.value, settings.intensity.value));
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Depth, source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._Occlusion, destination);

            // Calculate the thread group count and add a dispatch command with them.
            cs.GetKernelThreadGroupSizes(kernel, out var xsize, out var ysize, out var zsize);

            cmd.DispatchCompute(
                cs, kernel,
                ((int)sourceSize.x + (int)xsize - 1) / (int)xsize,
                ((int)sourceSize.y + (int)ysize - 1) / (int)ysize,
                XRGraphics.computePassCount * ((int)sourceSize.z + (int)zsize - 1) / (int)zsize
            );
        }

        void PushUpsampleCommands(CommandBuffer cmd, in Vector4 viewport, RTHandle lowResDepth, RTHandle interleavedAO, RTHandle highResDepth, RTHandle highResAO, RTHandle dest, AmbientOcclusion settings, in Vector3 lowResDepthSize, in Vector2 highResDepthSize, bool msaa)
        {
            var cs = m_Resources.shaders.aoUpsampleCS;
            int kernel = msaa
                ? cs.FindKernel(highResAO == null ? "KMainInvert_MSAA" : "KMainBlendout_MSAA")
                : cs.FindKernel(highResAO == null ? "KMainInvert" : "KMainBlendout");

            float stepSize = 1920f / lowResDepthSize.x;
            float bTolerance = 1f - Mathf.Pow(10f, settings.blurTolerance.value) * stepSize;
            bTolerance *= bTolerance;
            float uTolerance = Mathf.Pow(10f, settings.upsampleTolerance.value);
            float noiseFilterWeight = 1f / (Mathf.Pow(10f, settings.noiseFilterTolerance.value) + uTolerance);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._InvLowResolution, new Vector2(1f / lowResDepthSize.x * viewport.x, 1f / lowResDepthSize.y * viewport.y));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._InvHighResolution, new Vector2(1f / highResDepthSize.x * viewport.x, 1f / highResDepthSize.y * viewport.y));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdditionalParams, new Vector4(noiseFilterWeight, stepSize, bTolerance, uTolerance));

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._LoResDB, lowResDepth);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._HiResDB, highResDepth);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._LoResAO1, interleavedAO);

            if (highResAO != null)
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._HiResAO, highResAO);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._AoResult, dest);

            int xcount = ((int)highResDepthSize.x + 17) / 16;
            int ycount = ((int)highResDepthSize.y + 17) / 16;
            cmd.DispatchCompute(cs, kernel, xcount, ycount, XRGraphics.computePassCount);
        }
    }
}
