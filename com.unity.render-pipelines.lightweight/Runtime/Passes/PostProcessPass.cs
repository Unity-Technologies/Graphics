using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.LWRP
{
    // TODO: xmldoc
    public interface IPostProcessComponent
    {
        bool IsActive();
        bool IsTileCompatible();
    }

    // TODO: FXAA, TAA
    // TODO: Motion blur
    // TODO: Depth of Field
    // TODO: Final pass dithering
    internal class PostProcessPass : ScriptableRenderPass
    {
        RenderTextureDescriptor m_Descriptor;
        RenderTargetHandle m_Source;
        RenderTargetHandle m_Destination;
        RenderTargetHandle m_Depth;
        RenderTargetHandle m_InternalLut;

        const string k_RenderPostProcessingTag = "Render PostProcessing Effects";

        MaterialLibrary m_Materials;
        PostProcessData m_Data;

        // Builtin effects settings
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
        readonly GraphicsFormat m_BloomFormat;
        Matrix4x4 m_PrevViewProjM = Matrix4x4.identity;
        bool m_ResetHistory;

        public PostProcessPass(RenderPassEvent evt, PostProcessData data)
        {
            renderPassEvent = evt;
            m_Data = data;
            m_Materials = new MaterialLibrary(data);

            // Texture format pre-lookup
            var asset = LightweightRenderPipeline.asset;
            var hdr = asset != null && asset.supportsHDR;

            if (hdr && SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
                m_BloomFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            else
                m_BloomFormat = GraphicsFormat.R8G8B8A8_UNorm;

            // Bloom pyramid shader ids - can't use a simple stackalloc in the bloom function as we
            // unfortunately need to allocate strings
            ShaderConstants._BloomMipUp = new int[k_MaxPyramidSize];
            ShaderConstants._BloomMipDown = new int[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                ShaderConstants._BloomMipUp[i] = Shader.PropertyToID("_BloomMipUp" + i);
                ShaderConstants._BloomMipDown[i] = Shader.PropertyToID("_BloomMipDown" + i);
            }

            m_ResetHistory = true;
        }

        public void Setup(in RenderTextureDescriptor baseDescriptor, in RenderTargetHandle sourceHandle, in RenderTargetHandle destinationHandle, in RenderTargetHandle depth, in RenderTargetHandle internalLut)
        {
            m_Descriptor = baseDescriptor;
            m_Source = sourceHandle;
            m_Destination = destinationHandle;
            m_Depth = depth;
            m_InternalLut = internalLut;
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

            if (CanRunOnTile())
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

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;

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
                    cmd.GetTemporaryRT(
                        ShaderConstants._TempTarget, m_Descriptor.width, m_Descriptor.height,
                        0, FilterMode.Bilinear, m_Descriptor.graphicsFormat
                    );

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
            if (cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
            {
                using (new ProfilingSample(cmd, "Sub-pixel Morphological Anti-aliasing"))
                {
                    DoSubpixelMorphologicalAntialiasing(ref cameraData, cmd, GetSource(), GetDestination());
                    Swap();
                }
            }

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
                        SetupBloom(cameraData.camera, cmd, GetSource(), m_Materials.uber);
                }

                // Setup other effects constants
                SetupLensDistortion(m_Materials.uber, cameraData.isSceneViewCamera);
                SetupChromaticAberration(m_Materials.uber);
                SetupVignette(cameraData.camera, m_Materials.uber);
                SetupColorGrading(cmd, ref renderingData, m_Materials.uber);
                SetupGrain(cameraData.camera, m_Materials.uber, false);

                // Done with Uber, blit it
                Blit(cmd, GetSource(), m_Destination.Identifier(), m_Materials.uber);

                // Cleanup
                if (bloomActive)
                    cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipUp[0]);

                if (destination != -1)
                    cmd.ReleaseTemporaryRT(destination);
            }
        }

        #region Sub-pixel Morphological Anti-aliasing

        void DoSubpixelMorphologicalAntialiasing(ref CameraData cameraData, CommandBuffer cmd, int source, int destination)
        {
            var camera = cameraData.camera;
            var material = m_Materials.subpixelMorphologicalAntialiasing;
            const int kStencilBit = 64;

            // Globals
            material.SetVector(ShaderConstants._Metrics, new Vector4(1f / camera.pixelWidth, 1f / camera.pixelHeight, camera.pixelWidth, camera.pixelHeight));
            material.SetTexture(ShaderConstants._AreaTexture, m_Data.textures.smaaAreaTex);
            material.SetTexture(ShaderConstants._SearchTexture, m_Data.textures.smaaSearchTex);
            material.SetInt(ShaderConstants._StencilRef, kStencilBit);
            material.SetInt(ShaderConstants._StencilMask, kStencilBit);

            // Quality presets
            material.shaderKeywords = null;

            switch (cameraData.antialiasingQuality)
            {
                case AntialiasingQuality.Low: material.EnableKeyword("SMAA_PRESET_LOW");
                    break;
                case AntialiasingQuality.Medium: material.EnableKeyword("SMAA_PRESET_MEDIUM");
                    break;
                case AntialiasingQuality.High: material.EnableKeyword("SMAA_PRESET_HIGH");
                    break;
            }

            // Intermediate targets
            cmd.GetTemporaryRT(ShaderConstants._EdgeTexture, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, GraphicsFormat.R8G8B8A8_UNorm);
            cmd.GetTemporaryRT(ShaderConstants._BlendTexture, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, GraphicsFormat.R8G8B8A8_UNorm);

            // Prepare for manual blit
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetViewport(camera.pixelRect);

            // Pass 1: Edge detection
            cmd.SetRenderTarget(ShaderConstants._EdgeTexture, m_Depth.Identifier());
            cmd.ClearRenderTarget(true, true, Color.clear); // TODO: Explicitly clearing depth/stencil here but we shouldn't have to, FIXME /!\
            cmd.SetGlobalTexture(ShaderConstants._InputTexture, source);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 0);

            // Pass 2: Blend weights
            cmd.SetRenderTarget(ShaderConstants._BlendTexture, m_Depth.Identifier());
            cmd.ClearRenderTarget(false, true, Color.clear);
            cmd.SetGlobalTexture(ShaderConstants._InputTexture, ShaderConstants._EdgeTexture);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 1);

            // Pass 3: Neighborhood blending
            cmd.SetRenderTarget(destination);
            cmd.SetGlobalTexture(ShaderConstants._InputTexture, source);
            cmd.SetGlobalTexture(ShaderConstants._BlendTexture, ShaderConstants._BlendTexture);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, 2);

            // Cleanup
            cmd.ReleaseTemporaryRT(ShaderConstants._EdgeTexture);
            cmd.ReleaseTemporaryRT(ShaderConstants._BlendTexture);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        #endregion

        #region Motion Blur

        void DoMotionBlur(Camera camera, CommandBuffer cmd, int source, int destination)
        {
            var material = m_Materials.cameraMotionBlur;

            // This is needed because Blit will reset viewproj matrices to identity and LW currently
            // relies on SetupCameraProperties instead of handling its own matrices.
            // TODO: We need get rid of SetupCameraProperties and setup camera matrices in LWRP
            var proj = GL.GetGPUProjectionMatrix(camera.nonJitteredProjectionMatrix, true);
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
                ? "GENERIC" : "UNIT_DISTANCE"
            );

            cmd.Blit(source, destination, material);
        }

        static Vector2 CalcViewExtents(Camera camera)
        {
            float fovY = camera.fieldOfView * Mathf.Deg2Rad;
            float aspect = camera.pixelWidth / (float)camera.pixelHeight;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        static Vector2 CalcCropExtents(Camera camera, float d)
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

        // TODO: RGBM support when not HDR as right now it's pretty much useless in LDR
        void SetupBloom(Camera camera, CommandBuffer cmd, int source, Material uberMaterial)
        {
            // Start at half-res
            int tw = camera.pixelWidth >> 1;
            int th = camera.pixelHeight >> 1;

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
            CoreUtils.SetKeyword(bloomMaterial, "FILTERING_HQ", m_Bloom.highQualityFiltering.value);

            // Prefilter
            cmd.GetTemporaryRT(ShaderConstants._BloomMipDown[0], tw, th, 0, FilterMode.Bilinear, m_BloomFormat);
            cmd.GetTemporaryRT(ShaderConstants._BloomMipUp[0], tw, th, 0, FilterMode.Bilinear, m_BloomFormat);
            cmd.Blit(source, ShaderConstants._BloomMipDown[0], bloomMaterial, 0);

            // Downsample - gaussian pyramid
            int lastDown = ShaderConstants._BloomMipDown[0];
            for (int i = 1; i < mipCount; i++)
            {
                tw = Mathf.Max(1, tw >> 1);
                th = Mathf.Max(1, th >> 1);
                int mipDown = ShaderConstants._BloomMipDown[i];
                int mipUp = ShaderConstants._BloomMipUp[i];

                cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear, m_BloomFormat);
                cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear, m_BloomFormat);

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

            cmd.SetGlobalTexture(ShaderConstants._Bloom_Texture, ShaderConstants._BloomMipUp[0]);

            // Setup lens dirtiness on uber
            // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
            // stretched or squashed
            var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
            float dirtRatio = dirtTexture.width / (float)dirtTexture.height;
            float screenRatio = camera.pixelWidth / (float)camera.pixelHeight;
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
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? "BLOOM_HQ_DIRT" : "BLOOM_HQ");
            else
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? "BLOOM_LQ_DIRT" : "BLOOM_LQ");
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
                material.EnableKeyword("DISTORTION");
        }

        #endregion

        #region Chromatic Aberration

        void SetupChromaticAberration(Material material)
        {
            material.SetFloat(ShaderConstants._Chroma_Params, m_ChromaticAberration.intensity.value * 0.05f);

            if (m_ChromaticAberration.IsActive())
                material.EnableKeyword("CHROMATIC_ABERRATION");
        }

        #endregion

        #region Vignette

        void SetupVignette(Camera camera, Material material)
        {
            var color = m_Vignette.color.value;
            var center = m_Vignette.center.value;

            var v1 = new Vector4(
                color.r, color.g, color.b,
                m_Vignette.rounded.value ? camera.pixelWidth / (float)camera.pixelHeight : 1f
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
                material.EnableKeyword("HDR_GRADING");
            }
            else
            {
                switch (m_Tonemapping.mode.value)
                {
                    case TonemappingMode.Neutral: material.EnableKeyword("TONEMAP_NEUTRAL"); break;
                    case TonemappingMode.ACES: material.EnableKeyword("TONEMAP_ACES"); break;
                    default: break; // None
                }
            }
        }

        #endregion

        #region Film Grain

        void SetupGrain(Camera camera, Material material, bool onTile)
        {
            var texture = m_FilmGrain.texture.value;

            if (m_FilmGrain.type.value != FilmGrainLookup.Custom)
                texture = m_Data.textures.filmGrainTex[(int)m_FilmGrain.type.value];

            #if LWRP_DEBUG_STATIC_POSTFX
            float offsetX = 0f;
            float offsetY = 0f;
            #else
            float offsetX = Random.value;
            float offsetY = Random.value;
            #endif

            var tilingParams = texture == null
                ? Vector4.zero
                : new Vector4(camera.pixelWidth / (float)texture.width, camera.pixelHeight / (float)texture.height, offsetX, offsetY);

            material.SetTexture(ShaderConstants._Grain_Texture, texture);
            material.SetVector(ShaderConstants._Grain_Params, new Vector2(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value));
            material.SetVector(ShaderConstants._Grain_TilingParams, tilingParams);

            if (!onTile && m_FilmGrain.IsActive())
                material.EnableKeyword("GRAIN");
        }

        #endregion

        #region Internal utilities

        class MaterialLibrary
        {
            public readonly Material stopNaN;
            public readonly Material subpixelMorphologicalAntialiasing;
            public readonly Material cameraMotionBlur;
            public readonly Material paniniProjection;
            public readonly Material bloom;
            public readonly Material uber;

            public MaterialLibrary(PostProcessData data)
            {
                stopNaN = Load(data.shaders.stopNanPS);
                subpixelMorphologicalAntialiasing = Load(data.shaders.subpixelMorphologicalAntialiasingPS);
                cameraMotionBlur = Load(data.shaders.cameraMotionBlurPS);
                paniniProjection = Load(data.shaders.paniniProjectionPS);
                bloom = Load(data.shaders.bloomPS);
                uber = Load(data.shaders.uberPostPS);
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

            public static readonly int _Metrics            = Shader.PropertyToID("_Metrics");
            public static readonly int _AreaTexture        = Shader.PropertyToID("_AreaTexture");
            public static readonly int _SearchTexture      = Shader.PropertyToID("_SearchTexture");
            public static readonly int _EdgeTexture        = Shader.PropertyToID("_EdgeTexture");
            public static readonly int _BlendTexture       = Shader.PropertyToID("_BlendTexture");

            public static readonly int _InputTexture       = Shader.PropertyToID("_InputTexture");
            public static readonly int _Params             = Shader.PropertyToID("_Params");
            public static readonly int _MainTexLowMip      = Shader.PropertyToID("_MainTexLowMip");
            public static readonly int _Bloom_Params       = Shader.PropertyToID("_Bloom_Params");
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
            public static readonly int _Grain_Texture      = Shader.PropertyToID("_Grain_Texture");
            public static readonly int _Grain_Params       = Shader.PropertyToID("_Grain_Params");
            public static readonly int _Grain_TilingParams = Shader.PropertyToID("_Grain_TilingParams");

            public static int[] _BloomMipUp;
            public static int[] _BloomMipDown;
        }

        #endregion
    }
}
