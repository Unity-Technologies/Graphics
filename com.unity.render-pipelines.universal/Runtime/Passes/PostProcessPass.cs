using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    // TODO: xmldoc
    public interface IPostProcessComponent
    {
        bool IsActive();
        bool IsTileCompatible();
    }

    // TODO: TAA
    // TODO: Motion blur
    internal class PostProcessPass : ScriptableRenderPass
    {
        RenderTextureDescriptor m_Descriptor;
        RenderTargetHandle m_Source;
        RenderTargetHandle m_Destination;
        RenderTargetHandle m_Depth;
        RenderTargetHandle m_InternalLut;

        const string k_RenderPostProcessingTag = "Render PostProcessing Effects";
        const string k_RenderFinalPostProcessingTag = "Render Final PostProcessing Pass";

        MaterialLibrary m_Materials;
        PostProcessData m_Data;

        // Builtin effects settings
        DepthOfField m_DepthOfField;
        MotionBlur m_MotionBlur;
        PaniniProjection m_PaniniProjection;
        Bloom m_Bloom;
        LensDistortion m_LensDistortion;
        ChromaticAberration m_ChromaticAberration;
        Vignette m_Vignette;
        ColorLookup m_ColorLookup;
        ColorAdjustments m_ColorAdjustments;
        Tonemapping m_Tonemapping;
        FilmGrain m_FilmGrain;

        // Misc
        const int k_MaxPyramidSize = 16;
        readonly GraphicsFormat m_DefaultHDRFormat;
        bool m_UseRGBM;
        readonly GraphicsFormat m_GaussianCoCFormat;
        Matrix4x4 m_PrevViewProjM = Matrix4x4.identity;
        bool m_ResetHistory;
        int m_DitheringTextureIndex;
        RenderTargetIdentifier[] m_MRT2;
        Vector4[] m_BokehKernel;
        int m_BokehHash;
        bool m_IsStereo;

        // True when this is the very last pass in the pipeline
        bool m_IsFinalPass;

        public PostProcessPass(RenderPassEvent evt, PostProcessData data)
        {
            renderPassEvent = evt;
            m_Data = data;
            m_Materials = new MaterialLibrary(data);

            // Texture format pre-lookup
            if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
            {
                m_DefaultHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                m_UseRGBM = false;
            }
            else
            {
                m_DefaultHDRFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;
                m_UseRGBM = true;
            }

            if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_UNorm, FormatUsage.Linear | FormatUsage.Render))
                m_GaussianCoCFormat = GraphicsFormat.R16_UNorm;
            else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, FormatUsage.Linear | FormatUsage.Render))
                m_GaussianCoCFormat = GraphicsFormat.R16_SFloat;
            else // Expect CoC banding
                m_GaussianCoCFormat = GraphicsFormat.R8_UNorm;

            // Bloom pyramid shader ids - can't use a simple stackalloc in the bloom function as we
            // unfortunately need to allocate strings
            ShaderConstants._BloomMipUp = new int[k_MaxPyramidSize];
            ShaderConstants._BloomMipDown = new int[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                ShaderConstants._BloomMipUp[i] = Shader.PropertyToID("_BloomMipUp" + i);
                ShaderConstants._BloomMipDown[i] = Shader.PropertyToID("_BloomMipDown" + i);
            }

            m_MRT2 = new RenderTargetIdentifier[2];
            m_ResetHistory = true;
        }

        public void Setup(in RenderTextureDescriptor baseDescriptor, in RenderTargetHandle source, in RenderTargetHandle destination, in RenderTargetHandle depth, in RenderTargetHandle internalLut)
        {
            m_Descriptor = baseDescriptor;
            m_Source = source;
            m_Destination = destination;
            m_Depth = depth;
            m_InternalLut = internalLut;
            m_IsFinalPass = false;
        }

        public void SetupFinalPass(in RenderTargetHandle source)
        {
            m_Source = source;
            m_Destination = RenderTargetHandle.CameraTarget;
            m_IsFinalPass = true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (m_Destination == RenderTargetHandle.CameraTarget)
                return;

            var desc = cameraTextureDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            cmd.GetTemporaryRT(m_Destination.id, desc, FilterMode.Point);
        }

        public void ResetHistory()
        {
            m_ResetHistory = true;
        }

        public bool CanRunOnTile()
        {
            // Check builtin & user effects here
            return false;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Start by pre-fetching all builtin effect settings we need
            // Some of the color-grading settings are only used in the color grading lut pass
            var stack = VolumeManager.instance.stack;
            m_DepthOfField        = stack.GetComponent<DepthOfField>();
            m_MotionBlur          = stack.GetComponent<MotionBlur>();
            m_PaniniProjection    = stack.GetComponent<PaniniProjection>();
            m_Bloom               = stack.GetComponent<Bloom>();
            m_LensDistortion      = stack.GetComponent<LensDistortion>();
            m_ChromaticAberration = stack.GetComponent<ChromaticAberration>();
            m_Vignette            = stack.GetComponent<Vignette>();
            m_ColorLookup         = stack.GetComponent<ColorLookup>();
            m_ColorAdjustments    = stack.GetComponent<ColorAdjustments>();
            m_Tonemapping         = stack.GetComponent<Tonemapping>();
            m_FilmGrain           = stack.GetComponent<FilmGrain>();

            if (m_IsFinalPass)
            {
                var cmd = CommandBufferPool.Get(k_RenderFinalPostProcessingTag);
                RenderFinalPass(cmd, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            else if (CanRunOnTile())
            {
                // TODO: Add a fast render path if only on-tile compatible effects are used and we're actually running on a platform that supports it
                // Note: we can still work on-tile if FXAA is enabled, it'd be part of the final pass
            }
            else
            {
                // Regular render path (not on-tile) - we do everything in a single command buffer as it
                // makes it easier to manage temporary targets' lifetime
                var cmd = CommandBufferPool.Get(k_RenderPostProcessingTag);
                Render(cmd, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            m_ResetHistory = false;
        }

        RenderTextureDescriptor GetStereoCompatibleDescriptor()
            => GetStereoCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat);

        RenderTextureDescriptor GetStereoCompatibleDescriptor(int width, int height, GraphicsFormat format)
        {
            // Inherit the VR setup from the camera descriptor
            var desc = m_Descriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            m_IsStereo = renderingData.cameraData.isStereoEnabled;

            // Don't use these directly unless you have a good reason to, use GetSource() and
            // GetDestination() instead
            int source = m_Source.id;
            int destination = -1;

            // Utilities to simplify intermediate target management
            int GetSource() => source;

            int GetDestination()
            {
                if (destination == -1)
                {
                    cmd.GetTemporaryRT(ShaderConstants._TempTarget, GetStereoCompatibleDescriptor(), FilterMode.Bilinear);
                    destination = ShaderConstants._TempTarget;
                }

                return destination;
            }

            void Swap() => CoreUtils.Swap(ref source, ref destination);

            // Optional NaN killer before post-processing kicks in
            if (cameraData.isStopNaNEnabled)
            {
                using (new ProfilingSample(cmd, "Stop NaN"))
                {
                    cmd.Blit(GetSource(), GetDestination(), m_Materials.stopNaN);
                    Swap();
                }
            }

            // Anti-aliasing
            if (cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2)
            {
                using (new ProfilingSample(cmd, "Sub-pixel Morphological Anti-aliasing"))
                {
                    DoSubpixelMorphologicalAntialiasing(ref cameraData, cmd, GetSource(), GetDestination());
                    Swap();
                }
            }

            // Depth of Field
            if (m_DepthOfField.IsActive() && !cameraData.isSceneViewCamera)
            {
                var markerName = m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian
                    ? "Gaussian Depth of Field"
                    : "Bokeh Depth of Field";

                using (new ProfilingSample(cmd, markerName))
                {
                    DoDepthOfField(cameraData.camera, cmd, GetSource(), GetDestination());
                    Swap();
                }
            }

            // Motion blur
            if (m_MotionBlur.IsActive() && !cameraData.isSceneViewCamera)
            {
                using (new ProfilingSample(cmd, "Motion Blur"))
                {
                    DoMotionBlur(cameraData.camera, cmd, GetSource(), GetDestination());
                    Swap();
                }
            }

            // Panini projection is done as a fullscreen pass after all depth-based effects are done
            // and before bloom kicks in
            if (m_PaniniProjection.IsActive() && !cameraData.isSceneViewCamera)
            {
                using (new ProfilingSample(cmd, "Panini Projection"))
                {
                    DoPaniniProjection(cameraData.camera, cmd, GetSource(), GetDestination());
                    Swap();
                }
            }

            // Combined post-processing stack
            using (new ProfilingSample(cmd, "Uber"))
            {
                // Reset uber keywords
                m_Materials.uber.shaderKeywords = null;

                // Bloom goes first
                bool bloomActive = m_Bloom.IsActive();
                if (bloomActive)
                {
                    using (new ProfilingSample(cmd, "Bloom"))
                        SetupBloom(cmd, GetSource(), m_Materials.uber);
                }

                // Setup other effects constants
                SetupLensDistortion(m_Materials.uber, cameraData.isSceneViewCamera);
                SetupChromaticAberration(m_Materials.uber);
                SetupVignette(m_Materials.uber);
                SetupColorGrading(cmd, ref renderingData, m_Materials.uber);

                // Only apply dithering & grain if we're the final pass
                SetupGrain(cameraData.camera, m_Materials.uber);
                SetupDithering(ref cameraData, m_Materials.uber);

                // Done with Uber, blit it
                cmd.SetGlobalTexture("_BlitTex", GetSource());

                if (m_IsStereo)
                {
                    Blit(cmd, GetSource(), m_Destination.Identifier(), m_Materials.uber);
                }
                else
                {
                    cmd.SetRenderTarget(m_Destination.Identifier());
                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                    cmd.SetViewport(cameraData.camera.pixelRect);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Materials.uber);
                    cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);
                }

                // Cleanup
                if (bloomActive)
                    cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipUp[0]);

                if (destination != -1)
                    cmd.ReleaseTemporaryRT(ShaderConstants._TempTarget);
            }
        }

        #region Sub-pixel Morphological Anti-aliasing

        // TODO: XR isn't working with SMAA
        void DoSubpixelMorphologicalAntialiasing(ref CameraData cameraData, CommandBuffer cmd, int source, int destination)
        {
            var camera = cameraData.camera;
            var material = m_Materials.subpixelMorphologicalAntialiasing;
            const int kStencilBit = 64;

            // Globals
            material.SetVector(ShaderConstants._Metrics, new Vector4(1f / m_Descriptor.width, 1f / m_Descriptor.height, m_Descriptor.width, m_Descriptor.height));
            material.SetTexture(ShaderConstants._AreaTexture, m_Data.textures.smaaAreaTex);
            material.SetTexture(ShaderConstants._SearchTexture, m_Data.textures.smaaSearchTex);
            material.SetInt(ShaderConstants._StencilRef, kStencilBit);
            material.SetInt(ShaderConstants._StencilMask, kStencilBit);

            // Quality presets
            material.shaderKeywords = null;

            switch (cameraData.antialiasingQuality)
            {
                case AntialiasingQuality.Low: material.EnableKeyword(ShaderKeywordStrings.SmaaLow);
                    break;
                case AntialiasingQuality.Medium: material.EnableKeyword(ShaderKeywordStrings.SmaaMedium);
                    break;
                case AntialiasingQuality.High: material.EnableKeyword(ShaderKeywordStrings.SmaaHigh);
                    break;
            }

            // Intermediate targets
            cmd.GetTemporaryRT(ShaderConstants._EdgeTexture, m_Descriptor.width, m_Descriptor.height, 0, FilterMode.Point, GraphicsFormat.R8G8B8A8_UNorm);
            cmd.GetTemporaryRT(ShaderConstants._BlendTexture, m_Descriptor.width, m_Descriptor.height, 0, FilterMode.Point, GraphicsFormat.R8G8B8A8_UNorm);

            // Prepare for manual blit
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetViewport(camera.pixelRect);

            // Pass 1: Edge detection
            cmd.SetRenderTarget(ShaderConstants._EdgeTexture, m_Depth.Identifier());
            cmd.ClearRenderTarget(true, true, Color.clear); // TODO: Explicitly clearing depth/stencil here but we shouldn't have to, FIXME /!\
            cmd.SetGlobalTexture(ShaderConstants._ColorTexture, source);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 0);

            // Pass 2: Blend weights
            cmd.SetRenderTarget(ShaderConstants._BlendTexture, m_Depth.Identifier());
            cmd.ClearRenderTarget(false, true, Color.clear);
            cmd.SetGlobalTexture(ShaderConstants._ColorTexture, ShaderConstants._EdgeTexture);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 1);

            // Pass 3: Neighborhood blending
            cmd.SetRenderTarget(destination);
            cmd.SetGlobalTexture(ShaderConstants._ColorTexture, source);
            cmd.SetGlobalTexture(ShaderConstants._BlendTexture, ShaderConstants._BlendTexture);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 2);

            // Cleanup
            cmd.ReleaseTemporaryRT(ShaderConstants._EdgeTexture);
            cmd.ReleaseTemporaryRT(ShaderConstants._BlendTexture);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        #endregion

        #region Depth Of Field

        // TODO: CoC reprojection once TAA gets in LW
        // TODO: Proper LDR/gamma support
        void DoDepthOfField(Camera camera, CommandBuffer cmd, int source, int destination)
        {
            if (m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian)
                DoGaussianDepthOfField(camera, cmd, source, destination);
            else if (m_DepthOfField.mode.value == DepthOfFieldMode.Bokeh)
                DoBokehDepthOfField(cmd, source, destination);
        }

        // TODO: XR isn't working with Gaussian DOF
        void DoGaussianDepthOfField(Camera camera, CommandBuffer cmd, int source, int destination)
        {
            var material = m_Materials.gaussianDepthOfField;
            int wh = m_Descriptor.width / 2;
            int hh = m_Descriptor.height / 2;
            float farStart = m_DepthOfField.gaussianStart.value;
            float farEnd = Mathf.Max(farStart, m_DepthOfField.gaussianEnd.value);

            // Assumes a radius of 1 is 1 at 1080p
            // Past a certain radius our gaussian kernel will look very bad so we'll clamp it for
            // very high resolutions (4K+).
            float maxRadius = m_DepthOfField.gaussianMaxRadius.value * (wh / 1080f);
            maxRadius = Mathf.Min(maxRadius, 2f);

            CoreUtils.SetKeyword(material, ShaderKeywordStrings.HighQualitySampling, m_DepthOfField.highQualitySampling.value);
            material.SetVector(ShaderConstants._CoCParams, new Vector3(farStart, farEnd, maxRadius));

            // Temporary textures
            cmd.GetTemporaryRT(ShaderConstants._FullCoCTexture, GetStereoCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_GaussianCoCFormat), FilterMode.Bilinear);
            cmd.GetTemporaryRT(ShaderConstants._HalfCoCTexture, GetStereoCompatibleDescriptor(wh, hh, m_GaussianCoCFormat), FilterMode.Bilinear);
            cmd.GetTemporaryRT(ShaderConstants._PingTexture, GetStereoCompatibleDescriptor(wh, hh, m_DefaultHDRFormat), FilterMode.Bilinear);
            cmd.GetTemporaryRT(ShaderConstants._PongTexture, GetStereoCompatibleDescriptor(wh, hh, m_DefaultHDRFormat), FilterMode.Bilinear);

            // Compute CoC
            cmd.Blit(source, ShaderConstants._FullCoCTexture, material, 0);

            // Downscale & prefilter color + coc
            m_MRT2[0] = ShaderConstants._HalfCoCTexture;
            m_MRT2[1] = ShaderConstants._PingTexture;

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetViewport(camera.pixelRect);
            cmd.SetGlobalTexture(ShaderConstants._ColorTexture, source);
            cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, ShaderConstants._FullCoCTexture);
            cmd.SetRenderTarget(m_MRT2, ShaderConstants._HalfCoCTexture);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 1);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            // Blur
            cmd.SetGlobalTexture(ShaderConstants._HalfCoCTexture, ShaderConstants._HalfCoCTexture);
            cmd.Blit(ShaderConstants._PingTexture, ShaderConstants._PongTexture, material, 2);
            cmd.Blit(ShaderConstants._PongTexture, ShaderConstants._PingTexture, material, 3);

            // Composite
            cmd.SetGlobalTexture(ShaderConstants._ColorTexture, ShaderConstants._PingTexture);
            cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, ShaderConstants._FullCoCTexture);
            cmd.Blit(source, destination, material, 4);

            // Cleanup
            cmd.ReleaseTemporaryRT(ShaderConstants._FullCoCTexture);
            cmd.ReleaseTemporaryRT(ShaderConstants._HalfCoCTexture);
            cmd.ReleaseTemporaryRT(ShaderConstants._PingTexture);
            cmd.ReleaseTemporaryRT(ShaderConstants._PongTexture);
        }

        void PrepareBokehKernel()
        {
            const int kRings = 4;
            const int kPointsPerRing = 7;

            // Check the existing array
            if (m_BokehKernel == null)
                m_BokehKernel = new Vector4[42];

            // Fill in sample points (concentric circles transformed to rotated N-Gon)
            int idx = 0;
            float bladeCount = m_DepthOfField.bladeCount.value;
            float curvature = 1f - m_DepthOfField.bladeCurvature.value;
            float rotation = m_DepthOfField.bladeRotation.value * Mathf.Deg2Rad;
            const float PI = Mathf.PI;
            const float TWO_PI = Mathf.PI * 2f;

            for (int ring = 1; ring < kRings; ring++)
            {
                float bias = 1f / kPointsPerRing;
                float radius = (ring + bias) / (kRings - 1f + bias);
                int points = ring * kPointsPerRing;

                for (int point = 0; point < points; point++)
                {
                    // Angle on ring
                    float phi = 2f * PI * point / points;

                    // Transform to rotated N-Gon
                    // Adapted from "CryEngine 3 Graphics Gems" [Sousa13]
                    float nt = Mathf.Cos(PI / bladeCount);
                    float dt = Mathf.Cos(phi - (TWO_PI / bladeCount) * Mathf.Floor((bladeCount * phi + Mathf.PI) / TWO_PI));
                    float r = radius * Mathf.Pow(nt / dt, curvature);
                    float u = r * Mathf.Cos(phi - rotation);
                    float v = r * Mathf.Sin(phi - rotation);

                    m_BokehKernel[idx] = new Vector4(u, v);
                    idx++;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetMaxBokehRadiusInPixels(float viewportHeight)
        {
            // Estimate the maximum radius of bokeh (empirically derived from the ring count)
            const float kRadiusInPixels = 14f;
            return Mathf.Min(0.05f, kRadiusInPixels / viewportHeight);
        }

        void DoBokehDepthOfField(CommandBuffer cmd, int source, int destination)
        {
            var material = m_Materials.bokehDepthOfField;
            int wh = m_Descriptor.width / 2;
            int hh = m_Descriptor.height / 2;

            // "A Lens and Aperture Camera Model for Synthetic Image Generation" [Potmesil81]
            float F = m_DepthOfField.focalLength.value / 1000f;
            float A = m_DepthOfField.focalLength.value / m_DepthOfField.aperture.value;
            float P = m_DepthOfField.focusDistance.value;
            float maxCoC = (A * F) / (P - F);
            float maxRadius = GetMaxBokehRadiusInPixels(m_Descriptor.height);
            float rcpAspect = 1f / (wh / (float)hh);

            cmd.SetGlobalVector(ShaderConstants._CoCParams, new Vector4(P, maxCoC, maxRadius, rcpAspect));

            // Prepare the bokeh kernel constant buffer
            int hash = m_DepthOfField.GetHashCode(); // TODO: GC fix
            if (hash != m_BokehHash)
            {
                m_BokehHash = hash;
                PrepareBokehKernel();
            }

            cmd.SetGlobalVectorArray(ShaderConstants._BokehKernel, m_BokehKernel);

            // Temporary textures
            cmd.GetTemporaryRT(ShaderConstants._FullCoCTexture, GetStereoCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8_UNorm), FilterMode.Bilinear);
            cmd.GetTemporaryRT(ShaderConstants._PingTexture, GetStereoCompatibleDescriptor(wh, hh, GraphicsFormat.R16G16B16A16_SFloat), FilterMode.Bilinear);
            cmd.GetTemporaryRT(ShaderConstants._PongTexture, GetStereoCompatibleDescriptor(wh, hh, GraphicsFormat.R16G16B16A16_SFloat), FilterMode.Bilinear);

            // Compute CoC
            cmd.Blit(source, ShaderConstants._FullCoCTexture, material, 0);
            cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, ShaderConstants._FullCoCTexture);

            // Downscale & prefilter color + coc
            cmd.Blit(source, ShaderConstants._PingTexture, material, 1);

            // Bokeh blur
            cmd.Blit(ShaderConstants._PingTexture, ShaderConstants._PongTexture, material, 2);

            // Post-filtering
            cmd.Blit(ShaderConstants._PongTexture, ShaderConstants._PingTexture, material, 3);

            // Composite
            cmd.SetGlobalTexture(ShaderConstants._DofTexture, ShaderConstants._PingTexture);
            cmd.Blit(source, destination, material, 4);

            // Cleanup
            cmd.ReleaseTemporaryRT(ShaderConstants._FullCoCTexture);
            cmd.ReleaseTemporaryRT(ShaderConstants._PingTexture);
            cmd.ReleaseTemporaryRT(ShaderConstants._PongTexture);
        }

        #endregion

        #region Motion Blur

        void DoMotionBlur(Camera camera, CommandBuffer cmd, int source, int destination)
        {
            var material = m_Materials.cameraMotionBlur;

            // This is needed because Blit will reset viewproj matrices to identity and LW currently
            // relies on SetupCameraProperties instead of handling its own matrices.
            // TODO: We need get rid of SetupCameraProperties and setup camera matrices in Universal
            var proj = camera.nonJitteredProjectionMatrix;
            var view = camera.worldToCameraMatrix;
            var viewProj = proj * view;

            material.SetMatrix("_ViewProjM", viewProj);

            if (m_ResetHistory)
                material.SetMatrix("_PrevViewProjM", viewProj);
            else
                material.SetMatrix("_PrevViewProjM", m_PrevViewProjM);

            material.SetFloat("_Intensity", m_MotionBlur.intensity.value);
            material.SetFloat("_Clamp", m_MotionBlur.clamp.value);
            cmd.Blit(source, destination, material, (int)m_MotionBlur.quality.value);

            m_PrevViewProjM = viewProj;
        }

        #endregion

        #region Panini Projection

        // Back-ported & adapted from the work of the Stockholm demo team - thanks Lasse!
        void DoPaniniProjection(Camera camera, CommandBuffer cmd, int source, int destination)
        {
            float distance = m_PaniniProjection.distance.value;
            var viewExtents = CalcViewExtents(camera);
            var cropExtents = CalcCropExtents(camera, distance);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = distance;
            float paniniS = Mathf.Lerp(1f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

            var material = m_Materials.paniniProjection;
            material.SetVector(ShaderConstants._Params, new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS));
            material.EnableKeyword(
                1f - Mathf.Abs(paniniD) > float.Epsilon
                ? ShaderKeywordStrings.PaniniGeneric : ShaderKeywordStrings.PaniniUnitDistance
            );

            cmd.Blit(source, destination, material);
        }

        Vector2 CalcViewExtents(Camera camera)
        {
            float fovY = camera.fieldOfView * Mathf.Deg2Rad;
            float aspect = m_Descriptor.width / (float)m_Descriptor.height;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        Vector2 CalcCropExtents(Camera camera, float d)
        {
            // given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // have X
            // want to find E

            float viewDist = 1f + d;

            var projPos = CalcViewExtents(camera);
            var projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1f);

            float cylDistMinusD = 1f / projHyp;
            float cylDist = cylDistMinusD + d;
            var cylPos = projPos * cylDistMinusD;

            return cylPos * (viewDist / cylDist);
        }

        #endregion

        #region Bloom

        void SetupBloom(CommandBuffer cmd, int source, Material uberMaterial)
        {
            // Start at half-res
            int tw = m_Descriptor.width >> 1;
            int th = m_Descriptor.height >> 1;

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, k_MaxPyramidSize);

            // Pre-filtering parameters
            float clamp = m_Bloom.clamp.value;
            float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

            // Material setup
            float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);
            var bloomMaterial = m_Materials.bloom;
            bloomMaterial.SetVector(ShaderConstants._Params, new Vector4(scatter, clamp, threshold, thresholdKnee));
            CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings.BloomHQ, m_Bloom.highQualityFiltering.value);
            CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings.UseRGBM, m_UseRGBM);

            // Prefilter
            var desc = GetStereoCompatibleDescriptor(tw, th, m_DefaultHDRFormat);
            cmd.GetTemporaryRT(ShaderConstants._BloomMipDown[0], desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(ShaderConstants._BloomMipUp[0], desc, FilterMode.Bilinear);
            cmd.Blit(source, ShaderConstants._BloomMipDown[0], bloomMaterial, 0);

            // Downsample - gaussian pyramid
            int lastDown = ShaderConstants._BloomMipDown[0];
            for (int i = 1; i < mipCount; i++)
            {
                tw = Mathf.Max(1, tw >> 1);
                th = Mathf.Max(1, th >> 1);
                int mipDown = ShaderConstants._BloomMipDown[i];
                int mipUp = ShaderConstants._BloomMipUp[i];

                desc.width = tw;
                desc.height = th;

                cmd.GetTemporaryRT(mipDown, desc, FilterMode.Bilinear);
                cmd.GetTemporaryRT(mipUp, desc, FilterMode.Bilinear);

                // Classic two pass gaussian blur - use mipUp as a temporary target
                //   First pass does 2x downsampling + 9-tap gaussian
                //   Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                cmd.Blit(lastDown, mipUp, bloomMaterial, 1);
                cmd.Blit(mipUp, mipDown, bloomMaterial, 2);
                lastDown = mipDown;
            }

            // Upsample (bilinear by default, HQ filtering does bicubic instead
            for (int i = mipCount - 2; i >= 0; i--)
            {
                int lowMip = (i == mipCount - 2) ? ShaderConstants._BloomMipDown[i + 1] : ShaderConstants._BloomMipUp[i + 1];
                int highMip = ShaderConstants._BloomMipDown[i];
                int dst = ShaderConstants._BloomMipUp[i];

                cmd.SetGlobalTexture(ShaderConstants._MainTexLowMip, lowMip);
                cmd.Blit(highMip, dst, bloomMaterial, 3);
            }

            // Cleanup
            for (int i = 0; i < mipCount; i++)
            {
                cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipDown[i]);
                if (i > 0) cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipUp[i]);
            }

            // Setup bloom on uber
            var tint = m_Bloom.tint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            var bloomParams = new Vector4(m_Bloom.intensity.value, tint.r, tint.g, tint.b);
            uberMaterial.SetVector(ShaderConstants._Bloom_Params, bloomParams);
            uberMaterial.SetFloat(ShaderConstants._Bloom_RGBM, m_UseRGBM ? 1f : 0f);

            cmd.SetGlobalTexture(ShaderConstants._Bloom_Texture, ShaderConstants._BloomMipUp[0]);

            // Setup lens dirtiness on uber
            // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
            // stretched or squashed
            var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
            float dirtRatio = dirtTexture.width / (float)dirtTexture.height;
            float screenRatio = m_Descriptor.width / (float)m_Descriptor.height;
            var dirtScaleOffset = new Vector4(1f, 1f, 0f, 0f);
            float dirtIntensity = m_Bloom.dirtIntensity.value;

            if (dirtRatio > screenRatio)
            {
                dirtScaleOffset.x = screenRatio / dirtRatio;
                dirtScaleOffset.z = (1f - dirtScaleOffset.x) * 0.5f;
            }
            else if (screenRatio > dirtRatio)
            {
                dirtScaleOffset.y = dirtRatio / screenRatio;
                dirtScaleOffset.w = (1f - dirtScaleOffset.y) * 0.5f;
            }

            uberMaterial.SetVector(ShaderConstants._LensDirt_Params, dirtScaleOffset);
            uberMaterial.SetFloat(ShaderConstants._LensDirt_Intensity, dirtIntensity);
            uberMaterial.SetTexture(ShaderConstants._LensDirt_Texture, dirtTexture);

            // Keyword setup - a bit convoluted as we're trying to save some variants in Uber...
            if (m_Bloom.highQualityFiltering.value)
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomHQDirt : ShaderKeywordStrings.BloomHQ);
            else
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomLQDirt : ShaderKeywordStrings.BloomLQ);
        }

        #endregion

        #region Lens Distortion

        void SetupLensDistortion(Material material, bool isSceneView)
        {
            float amount = 1.6f * Mathf.Max(Mathf.Abs(m_LensDistortion.intensity.value * 100f), 1f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            var center = m_LensDistortion.center.value * 2f - Vector2.one;
            var p1 = new Vector4(
                center.x,
                center.y,
                Mathf.Max(m_LensDistortion.xMultiplier.value, 1e-4f),
                Mathf.Max(m_LensDistortion.yMultiplier.value, 1e-4f)
            );
            var p2 = new Vector4(
                m_LensDistortion.intensity.value >= 0f ? theta : 1f / theta,
                sigma,
                1f / m_LensDistortion.scale.value,
                m_LensDistortion.intensity.value * 100f
            );

            material.SetVector(ShaderConstants._Distortion_Params1, p1);
            material.SetVector(ShaderConstants._Distortion_Params2, p2);

            if (m_LensDistortion.IsActive() && !isSceneView)
                material.EnableKeyword(ShaderKeywordStrings.Distortion);
        }

        #endregion

        #region Chromatic Aberration

        void SetupChromaticAberration(Material material)
        {
            material.SetFloat(ShaderConstants._Chroma_Params, m_ChromaticAberration.intensity.value * 0.05f);

            if (m_ChromaticAberration.IsActive())
                material.EnableKeyword(ShaderKeywordStrings.ChromaticAberration);
        }

        #endregion

        #region Vignette

        void SetupVignette(Material material)
        {
            var color = m_Vignette.color.value;
            var center = m_Vignette.center.value;

            var v1 = new Vector4(
                color.r, color.g, color.b,
                m_Vignette.rounded.value ? m_Descriptor.width / (float)m_Descriptor.height : 1f
            );
            var v2 = new Vector4(
                center.x, center.y,
                m_Vignette.intensity.value * 3f,
                m_Vignette.smoothness.value * 5f
            );

            material.SetVector(ShaderConstants._Vignette_Params1, v1);
            material.SetVector(ShaderConstants._Vignette_Params2, v2);
        }

        #endregion

        #region Color Grading

        void SetupColorGrading(CommandBuffer cmd, ref RenderingData renderingData, Material material)
        {
            ref var postProcessingData = ref renderingData.postProcessingData;
            bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;

            // Source material setup
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
            cmd.SetGlobalTexture(ShaderConstants._InternalLut, m_InternalLut.Identifier());
            material.SetVector(ShaderConstants._Lut_Params, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear));
            material.SetTexture(ShaderConstants._UserLut, m_ColorLookup.texture.value);
            material.SetVector(ShaderConstants._UserLut_Params, !m_ColorLookup.IsActive()
                ? Vector4.zero
                : new Vector4(1f / m_ColorLookup.texture.value.width,
                              1f / m_ColorLookup.texture.value.height,
                              m_ColorLookup.texture.value.height - 1f,
                              m_ColorLookup.contribution.value)
            );

            if (hdr)
            {
                material.EnableKeyword(ShaderKeywordStrings.HDRGrading);
            }
            else
            {
                switch (m_Tonemapping.mode.value)
                {
                    case TonemappingMode.Neutral: material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral); break;
                    case TonemappingMode.ACES: material.EnableKeyword(ShaderKeywordStrings.TonemapACES); break;
                    default: break; // None
                }
            }
        }

        #endregion

        #region Film Grain

        void SetupGrain(Camera camera, Material material)
        {
            if (m_Destination == RenderTargetHandle.CameraTarget && m_FilmGrain.IsActive())
            {
                material.EnableKeyword(ShaderKeywordStrings.FilmGrain);
                PostProcessUtils.ConfigureFilmGrain(
                    m_Data,
                    m_FilmGrain,
                    camera,
                    material
                );
            }
        }

        #endregion

        #region 8-bit Dithering

        void SetupDithering(ref CameraData cameraData, Material material)
        {
            if (m_Destination == RenderTargetHandle.CameraTarget && cameraData.isDitheringEnabled)
            {
                material.EnableKeyword(ShaderKeywordStrings.Dithering);
                m_DitheringTextureIndex = PostProcessUtils.ConfigureDithering(
                    m_Data,
                    m_DitheringTextureIndex,
                    cameraData.camera,
                    material
                );
            }
        }

        #endregion

        #region Final pass

        void RenderFinalPass(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var material = m_Materials.finalPass;
            material.shaderKeywords = null;

            // FXAA setup
            if (cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing)
                material.EnableKeyword(ShaderKeywordStrings.Fxaa);

            SetupGrain(cameraData.camera, material);
            SetupDithering(ref cameraData, material);

            cmd.SetGlobalTexture("_BlitTex", m_Source.Identifier());

            if (cameraData.isStereoEnabled)
            {
                Blit(cmd, m_Source.Identifier(), m_Destination.Identifier(), material);
            }
            else
            {
                cmd.SetRenderTarget(m_Destination.Identifier());
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetViewport(cameraData.camera.pixelRect);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material);
                cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);
            }
        }

        #endregion

        #region Internal utilities

        class MaterialLibrary
        {
            public readonly Material stopNaN;
            public readonly Material subpixelMorphologicalAntialiasing;
            public readonly Material gaussianDepthOfField;
            public readonly Material bokehDepthOfField;
            public readonly Material cameraMotionBlur;
            public readonly Material paniniProjection;
            public readonly Material bloom;
            public readonly Material uber;
            public readonly Material finalPass;

            public MaterialLibrary(PostProcessData data)
            {
                stopNaN = Load(data.shaders.stopNanPS);
                subpixelMorphologicalAntialiasing = Load(data.shaders.subpixelMorphologicalAntialiasingPS);
                gaussianDepthOfField = Load(data.shaders.gaussianDepthOfFieldPS);
                bokehDepthOfField = Load(data.shaders.bokehDepthOfFieldPS);
                cameraMotionBlur = Load(data.shaders.cameraMotionBlurPS);
                paniniProjection = Load(data.shaders.paniniProjectionPS);
                bloom = Load(data.shaders.bloomPS);
                uber = Load(data.shaders.uberPostPS);
                finalPass = Load(data.shaders.finalPostPassPS);
            }

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                    return null;
                }

                return CoreUtils.CreateEngineMaterial(shader);
            }
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _TempTarget         = Shader.PropertyToID("_TempTarget");

            public static readonly int _StencilRef         = Shader.PropertyToID("_StencilRef");
            public static readonly int _StencilMask        = Shader.PropertyToID("_StencilMask");

            public static readonly int _FullCoCTexture     = Shader.PropertyToID("_FullCoCTexture");
            public static readonly int _HalfCoCTexture     = Shader.PropertyToID("_HalfCoCTexture");
            public static readonly int _DofTexture         = Shader.PropertyToID("_DofTexture");
            public static readonly int _CoCParams          = Shader.PropertyToID("_CoCParams");
            public static readonly int _BokehKernel        = Shader.PropertyToID("_BokehKernel");
            public static readonly int _PongTexture        = Shader.PropertyToID("_PongTexture");
            public static readonly int _PingTexture        = Shader.PropertyToID("_PingTexture");

            public static readonly int _Metrics            = Shader.PropertyToID("_Metrics");
            public static readonly int _AreaTexture        = Shader.PropertyToID("_AreaTexture");
            public static readonly int _SearchTexture      = Shader.PropertyToID("_SearchTexture");
            public static readonly int _EdgeTexture        = Shader.PropertyToID("_EdgeTexture");
            public static readonly int _BlendTexture       = Shader.PropertyToID("_BlendTexture");

            public static readonly int _ColorTexture       = Shader.PropertyToID("_ColorTexture");
            public static readonly int _Params             = Shader.PropertyToID("_Params");
            public static readonly int _MainTexLowMip      = Shader.PropertyToID("_MainTexLowMip");
            public static readonly int _Bloom_Params       = Shader.PropertyToID("_Bloom_Params");
            public static readonly int _Bloom_RGBM         = Shader.PropertyToID("_Bloom_RGBM");
            public static readonly int _Bloom_Texture      = Shader.PropertyToID("_Bloom_Texture");
            public static readonly int _LensDirt_Texture   = Shader.PropertyToID("_LensDirt_Texture");
            public static readonly int _LensDirt_Params    = Shader.PropertyToID("_LensDirt_Params");
            public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");
            public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
            public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
            public static readonly int _Chroma_Params      = Shader.PropertyToID("_Chroma_Params");
            public static readonly int _Vignette_Params1   = Shader.PropertyToID("_Vignette_Params1");
            public static readonly int _Vignette_Params2   = Shader.PropertyToID("_Vignette_Params2");
            public static readonly int _Lut_Params         = Shader.PropertyToID("_Lut_Params");
            public static readonly int _UserLut_Params     = Shader.PropertyToID("_UserLut_Params");
            public static readonly int _InternalLut        = Shader.PropertyToID("_InternalLut");
            public static readonly int _UserLut            = Shader.PropertyToID("_UserLut");

            public static int[] _BloomMipUp;
            public static int[] _BloomMipDown;
        }

        #endregion
    }
}
