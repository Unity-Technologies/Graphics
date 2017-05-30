using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LowendMobile
{
    [Serializable]
    public class ShadowSettings
    {
        public bool     enabled;
        public int      shadowAtlasWidth;
        public int      shadowAtlasHeight;

        public float    maxShadowDistance;
        public int      directionalLightCascadeCount;
        public Vector3  directionalLightCascades;
        public float    directionalLightNearPlaneOffset;

        static ShadowSettings defaultShadowSettings = null;

        public static ShadowSettings Default
        {
            get
            {
                if (defaultShadowSettings == null)
                {
                    defaultShadowSettings = new ShadowSettings();
                    defaultShadowSettings.enabled = true;
                    defaultShadowSettings.shadowAtlasHeight = defaultShadowSettings.shadowAtlasWidth = 4096;
                    defaultShadowSettings.directionalLightCascadeCount = 1;
                    defaultShadowSettings.directionalLightCascades = new Vector3(0.05F, 0.2F, 0.3F);
                    defaultShadowSettings.directionalLightCascadeCount = 4;
                    defaultShadowSettings.directionalLightNearPlaneOffset = 5;
                    defaultShadowSettings.maxShadowDistance = 1000.0F;
                }
                return defaultShadowSettings;
            }
        }
    }

    public struct ShadowSliceData
    {
        public Matrix4x4    shadowTransform;
        public int          atlasX;
        public int          atlasY;
        public int          shadowResolution;
    }

    public class LowEndMobilePipeline : RenderPipeline, IComparer<VisibleLight>
    {
        private readonly LowEndMobilePipelineAsset m_Asset;

        private static readonly int kMaxCascades = 4;
        private static readonly int kMaxLights = 8;
        private static readonly int kMaxVertexLights = 4;
        private int m_ShadowLightIndex = -1;
        private int m_ShadowCasterCascadesCount = kMaxCascades;
        private readonly int[] m_LightTypePriority = new int[4] {2, 1, 2, 0}; // Spot and Point lights have max priority
        private int m_ShadowMapProperty;
        private RenderTargetIdentifier m_ShadowMapRTID;
        private int m_DepthBufferBits = 24;
        private Vector4[] m_DirectionalShadowSplitDistances = new Vector4[kMaxCascades];

        private static readonly ShaderPassName m_ForwardBasePassName = new ShaderPassName("LowEndMobileForward");

        private Vector4[] m_LightPositions = new Vector4[kMaxLights];
        private Vector4[] m_LightColors = new Vector4[kMaxLights];
        private Vector4[] m_LightAttenuations = new Vector4[kMaxLights];
        private Vector4[] m_LightSpotDirections = new Vector4[kMaxLights];

        private ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        private ShadowSliceData[] m_ShadowSlices = new ShadowSliceData[kMaxCascades];

        public LowEndMobilePipeline(LowEndMobilePipelineAsset asset)
        {
            m_Asset = asset;

            BuildShadowSettings();
            m_ShadowMapProperty = Shader.PropertyToID("_ShadowMap");
            m_ShadowMapRTID = new RenderTargetIdentifier(m_ShadowMapProperty);
        }

        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            var prevPipe = Shader.globalRenderPipeline;
            Shader.globalRenderPipeline = "LowEndMobilePipeline";
            base.Render(context, cameras);

            foreach (Camera camera in cameras)
            {
                CullingParameters cullingParameters;
                if (!CullResults.GetCullingParameters(camera, out cullingParameters))
                    continue;

                cullingParameters.shadowDistance = m_ShadowSettings.maxShadowDistance;
                CullResults cull = CullResults.Cull(ref cullingParameters, context);

                VisibleLight[] visibleLights = cull.visibleLights;

                int pixelLightsCount, vertexLightsCount;
                GetMaxSupportedLights(visibleLights.Length, out pixelLightsCount, out vertexLightsCount);

                SortLights(ref visibleLights, pixelLightsCount);

                // TODO: Add remaining lights to SH

                // Render Shadow Map
                bool shadowsRendered = false;
                if (m_ShadowLightIndex > -1)
                    shadowsRendered = RenderShadows(cull, visibleLights[m_ShadowLightIndex], context);

                // Setup camera matrices and RT
                context.SetupCameraProperties(camera);

                // Clear RenderTarget to avoid tile initialization on mobile GPUs
                // https://community.arm.com/graphics/b/blog/posts/mali-performance-2-how-to-correctly-handle-framebuffers
                var cmd = new CommandBuffer() { name = "Clear" };
                cmd.ClearRenderTarget(true, true, camera.backgroundColor);
                context.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                // Setup light and shadow shader constants
                SetupLightShaderVariables(visibleLights, pixelLightsCount, vertexLightsCount, context);
                if (shadowsRendered)
                    SetupShadowShaderVariables(context, m_ShadowCasterCascadesCount);

                // Render Opaques
                var settings = new DrawRendererSettings(cull, camera, m_ForwardBasePassName);
                settings.sorting.flags = SortFlags.CommonOpaque;
                settings.inputFilter.SetQueuesOpaque();

                settings.rendererConfiguration = RendererConfiguration.PerObjectReflectionProbes;
                if (m_Asset.EnableLightmap)
                    settings.rendererConfiguration |= RendererConfiguration.PerObjectLightmaps;

                if (m_Asset.EnableAmbientProbe)
                    settings.rendererConfiguration |= RendererConfiguration.PerObjectLightProbe;

                context.DrawRenderers(ref settings);

                // Release temporary RT
                var discardRT = new CommandBuffer();
                discardRT.ReleaseTemporaryRT(m_ShadowMapProperty);
                context.ExecuteCommandBuffer(discardRT);
                discardRT.Dispose();

                // TODO: Check skybox shader
                context.DrawSkybox(camera);

                // Render Alpha blended
                settings.sorting.flags = SortFlags.CommonTransparent;
                settings.inputFilter.SetQueuesTransparent();
                context.DrawRenderers(ref settings);
            }

            context.Submit();
        }

        private void BuildShadowSettings()
        {
            m_ShadowSettings = ShadowSettings.Default;
            m_ShadowSettings.directionalLightCascadeCount = m_Asset.CascadeCount;

            m_ShadowSettings.shadowAtlasWidth = m_Asset.ShadowAtlasResolution;
            m_ShadowSettings.shadowAtlasHeight = m_Asset.ShadowAtlasResolution;
            m_ShadowSettings.maxShadowDistance = m_Asset.ShadowDistance;

            switch (m_ShadowSettings.directionalLightCascadeCount)
            {
                case 1:
                    m_ShadowSettings.directionalLightCascades = new Vector3(1.0f, 0.0f, 0.0f);
                    break;

                case 2:
                    m_ShadowSettings.directionalLightCascades = new Vector3(m_Asset.Cascade2Split, 1.0f, 0.0f);
                    break;

                default:
                    m_ShadowSettings.directionalLightCascades = m_Asset.Cascade4Split;
                    break;
            }
        }

        private void GetMaxSupportedLights(int lightsCount, out int pixelLightsCount, out int vertexLightsCount)
        {
            pixelLightsCount = Mathf.Min(lightsCount, m_Asset.MaxSupportedPixelLights);
            vertexLightsCount = (m_Asset.SupportsVertexLight) ? Mathf.Min(lightsCount - pixelLightsCount, kMaxVertexLights) : 0;
        }

        private void InitializeLightData()
        {
            for (int i = 0; i < kMaxLights; ++i)
            {
                m_LightPositions[i] = Vector4.zero;
                m_LightColors[i] = Vector4.zero;
                m_LightAttenuations[i] = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
                m_LightSpotDirections[i] = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
            }
        }

        private void SetupLightShaderVariables(VisibleLight[] lights, int pixelLightCount, int vertexLightCount, ScriptableRenderContext context)
        {
            int totalLightCount = pixelLightCount + vertexLightCount;
            InitializeLightData();

            for (int i = 0; i < totalLightCount; ++i)
            {
                VisibleLight currLight = lights[i];
                if (currLight.lightType == LightType.Directional)
                {
                    Vector4 dir = -currLight.localToWorld.GetColumn(2);
                    m_LightPositions[i] = new Vector4(dir.x, dir.y, dir.z, 0.0f);
                }
                else
                {
                    Vector4 pos = currLight.localToWorld.GetColumn(3);
                    m_LightPositions[i] = new Vector4(pos.x, pos.y, pos.z, 1.0f);
                }

                m_LightColors[i] = currLight.finalColor;

                float rangeSq = currLight.range * currLight.range;
                float quadAtten = (currLight.lightType == LightType.Directional) ? 0.0f : 25.0f / rangeSq;

                if (currLight.lightType == LightType.Spot)
                {
                    Vector4 dir = currLight.localToWorld.GetColumn(2);
                    m_LightSpotDirections[i] = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                    float spotAngle = Mathf.Deg2Rad * currLight.spotAngle;
                    float cosOuterAngle = Mathf.Cos(spotAngle * 0.5f);
                    float cosInneAngle = Mathf.Cos(spotAngle * 0.25f);
                    float angleRange = cosInneAngle - cosOuterAngle;
                    m_LightAttenuations[i] = new Vector4(cosOuterAngle,
                            Mathf.Approximately(angleRange, 0.0f) ? 1.0f : angleRange, quadAtten, rangeSq);
                }
                else
                {
                    m_LightSpotDirections[i] = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
                    m_LightAttenuations[i] = new Vector4(-1.0f, 1.0f, quadAtten, rangeSq);
                }
            }

            CommandBuffer cmd = new CommandBuffer() {name = "SetupShadowShaderConstants"};
            cmd.SetGlobalVectorArray("globalLightPos", m_LightPositions);
            cmd.SetGlobalVectorArray("globalLightColor", m_LightColors);
            cmd.SetGlobalVectorArray("globalLightAtten", m_LightAttenuations);
            cmd.SetGlobalVectorArray("globalLightSpotDir", m_LightSpotDirections);
            float shadowMinNormalBias = m_Asset.ShadowMinNormalBias;
            float shadowNormalBias = m_Asset.ShadowNormalBias;
            cmd.SetGlobalVector("globalLightData", new Vector4(pixelLightCount, totalLightCount, shadowMinNormalBias, shadowNormalBias));
            SetShaderKeywords(cmd, vertexLightCount > 0);
            context.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        private bool RenderShadows(CullResults cullResults, VisibleLight shadowLight, ScriptableRenderContext context)
        {
            m_ShadowCasterCascadesCount = m_ShadowSettings.directionalLightCascadeCount;

            if (shadowLight.lightType == LightType.Spot)
                m_ShadowCasterCascadesCount = 1;

            int shadowResolution = GetMaxTileResolutionInAtlas(m_ShadowSettings.shadowAtlasWidth, m_ShadowSettings.shadowAtlasHeight, m_ShadowCasterCascadesCount);

            Bounds bounds;
            if (!cullResults.GetShadowCasterBounds(m_ShadowLightIndex, out bounds))
                return false;

            var setRenderTargetCommandBuffer = new CommandBuffer();
            setRenderTargetCommandBuffer.name = "Render packed shadows";
            setRenderTargetCommandBuffer.GetTemporaryRT(m_ShadowMapProperty, m_ShadowSettings.shadowAtlasWidth,
                m_ShadowSettings.shadowAtlasHeight, m_DepthBufferBits, FilterMode.Bilinear, RenderTextureFormat.Depth,
                RenderTextureReadWrite.Linear);
            setRenderTargetCommandBuffer.SetRenderTarget(m_ShadowMapRTID);
            setRenderTargetCommandBuffer.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(setRenderTargetCommandBuffer);
            setRenderTargetCommandBuffer.Dispose();

            float shadowNearPlane = m_Asset.ShadowNearOffset;
            Vector3 splitRatio = m_ShadowSettings.directionalLightCascades;
            Vector3 lightDir = Vector3.Normalize(shadowLight.light.transform.forward);

            Matrix4x4 view, proj;
            var settings = new DrawShadowsSettings(cullResults, m_ShadowLightIndex);
            bool needRendering = false;

            if (shadowLight.lightType == LightType.Spot)
            {
                needRendering = cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(m_ShadowLightIndex, out view, out proj,
                        out settings.splitData);

                if (!needRendering)
                    return false;

                SetupShadowSliceTransform(0, shadowResolution, proj, view);
                RenderShadowSlice(ref context, lightDir, 0, proj, view, settings);
            }
            else if (shadowLight.lightType == LightType.Directional)
            {
                for (int cascadeIdx = 0; cascadeIdx < m_ShadowCasterCascadesCount; ++cascadeIdx)
                {
                    needRendering = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(m_ShadowLightIndex,
                            cascadeIdx, m_ShadowCasterCascadesCount, splitRatio, shadowResolution, shadowNearPlane, out view, out proj,
                            out settings.splitData);

                    m_DirectionalShadowSplitDistances[cascadeIdx] = settings.splitData.cullingSphere;
                    m_DirectionalShadowSplitDistances[cascadeIdx].w *= settings.splitData.cullingSphere.w;

                    if (!needRendering)
                        return false;

                    SetupShadowSliceTransform(cascadeIdx, shadowResolution, proj, view);
                    RenderShadowSlice(ref context, lightDir, cascadeIdx, proj, view, settings);
                }
            }
            else
            {
                Debug.LogWarning("Only spot and directional shadow casters are supported in lowend mobile pipeline");
                return false;
            }

            return true;
        }

        private void SetupShadowSliceTransform(int cascadeIndex, int shadowResolution, Matrix4x4 proj, Matrix4x4 view)
        {
            // Assumes MAX_CASCADES = 4
            m_ShadowSlices[cascadeIndex].atlasX = (cascadeIndex % 2) * shadowResolution;
            m_ShadowSlices[cascadeIndex].atlasY = (cascadeIndex / 2) * shadowResolution;
            m_ShadowSlices[cascadeIndex].shadowResolution = shadowResolution;
            m_ShadowSlices[cascadeIndex].shadowTransform = Matrix4x4.identity;

            var matScaleBias = Matrix4x4.identity;
            matScaleBias.m00 = 0.5f;
            matScaleBias.m11 = 0.5f;
            matScaleBias.m22 = 0.5f;
            matScaleBias.m03 = 0.5f;
            matScaleBias.m23 = 0.5f;
            matScaleBias.m13 = 0.5f;

            // Later down the pipeline the proj matrix will be scaled to reverse-z in case of DX.
            // We need account for that scale in the shadowTransform.
            if (SystemInfo.usesReversedZBuffer)
                matScaleBias.m22 = -0.5f;

            var matTile = Matrix4x4.identity;
            matTile.m00 = (float)m_ShadowSlices[cascadeIndex].shadowResolution /
                (float)m_ShadowSettings.shadowAtlasWidth;
            matTile.m11 = (float)m_ShadowSlices[cascadeIndex].shadowResolution /
                (float)m_ShadowSettings.shadowAtlasHeight;
            matTile.m03 = (float)m_ShadowSlices[cascadeIndex].atlasX / (float)m_ShadowSettings.shadowAtlasWidth;
            matTile.m13 = (float)m_ShadowSlices[cascadeIndex].atlasY / (float)m_ShadowSettings.shadowAtlasHeight;

            m_ShadowSlices[cascadeIndex].shadowTransform = matTile * matScaleBias * proj * view;
        }

        private void RenderShadowSlice(ref ScriptableRenderContext context, Vector3 lightDir, int cascadeIndex,
            Matrix4x4 proj, Matrix4x4 view, DrawShadowsSettings settings)
        {
            var buffer = new CommandBuffer() {name = "Prepare Shadowmap Slice"};
            buffer.SetViewport(new Rect(m_ShadowSlices[cascadeIndex].atlasX, m_ShadowSlices[cascadeIndex].atlasY,
                    m_ShadowSlices[cascadeIndex].shadowResolution, m_ShadowSlices[cascadeIndex].shadowResolution));
            buffer.SetViewProjectionMatrices(view, proj);
            context.ExecuteCommandBuffer(buffer);
            buffer.Dispose();

            context.DrawShadows(ref settings);
        }

        private int GetMaxTileResolutionInAtlas(int atlasWidth, int atlasHeight, int tileCount)
        {
            int resolution = Mathf.Min(atlasWidth, atlasHeight);
            if (tileCount > Mathf.Log(resolution))
            {
                Debug.LogError(
                    String.Format(
                        "Cannot fit {0} tiles into current shadowmap atlas of size ({1}, {2}). ShadowMap Resolution set to zero.",
                        tileCount, atlasWidth, atlasHeight));
                return 0;
            }

            int currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
            while (currentTileCount < tileCount)
            {
                resolution = resolution >> 1;
                currentTileCount = atlasWidth / resolution * atlasHeight / resolution;
            }
            return resolution;
        }

        void SetupShadowShaderVariables(ScriptableRenderContext context, int cascadeCount)
        {
            float shadowResolution = m_ShadowSlices[0].shadowResolution;

            const int maxShadowCascades = 4;
            Matrix4x4[] shadowMatrices = new Matrix4x4[maxShadowCascades];
            for (int i = 0; i < cascadeCount; ++i)
                shadowMatrices[i] = (cascadeCount >= i) ? m_ShadowSlices[i].shadowTransform : Matrix4x4.identity;

            // TODO: shadow resolution per cascade in case cascades endup being supported.
            float invShadowResolution = 1.0f / shadowResolution;
            float[] pcfKernel =
            {
                -0.5f * invShadowResolution, 0.5f * invShadowResolution,
                0.5f * invShadowResolution, 0.5f * invShadowResolution,
                -0.5f * invShadowResolution, -0.5f * invShadowResolution,
                0.5f * invShadowResolution, -0.5f * invShadowResolution
            };

            var setupShadow = new CommandBuffer() {name = "SetupShadowShaderConstants"};
            setupShadow.SetGlobalMatrixArray("_WorldToShadow", shadowMatrices);
            setupShadow.SetGlobalVectorArray("_DirShadowSplitSpheres", m_DirectionalShadowSplitDistances);
            setupShadow.SetGlobalFloatArray("_PCFKernel", pcfKernel);
            context.ExecuteCommandBuffer(setupShadow);
            setupShadow.Dispose();
        }

        void SetShaderKeywords(CommandBuffer cmd, bool vertexLightSupport)
        {
            if (vertexLightSupport)
                cmd.EnableShaderKeyword("_VERTEX_LIGHTS");
            else
                cmd.DisableShaderKeyword("_VERTEX_LIGHTS");

            string[] shadowKeywords = new string[] { "_HARD_SHADOWS", "_SOFT_SHADOWS", "_HARD_SHADOWS_CASCADES", "_SOFT_SHADOWS_CASCADES" };
            for (int i = 0; i < shadowKeywords.Length; ++i)
                cmd.DisableShaderKeyword(shadowKeywords[i]);

            if (m_ShadowLightIndex != -1 && m_Asset.CurrShadowType != ShadowType.NO_SHADOW)
            {
                int keywordIndex = (int)m_Asset.CurrShadowType - 1;
                if (m_Asset.CascadeCount > 1)
                    keywordIndex += 2;
                cmd.EnableShaderKeyword(shadowKeywords[keywordIndex]);
            }

            if (m_Asset.EnableAmbientProbe)
                cmd.EnableShaderKeyword("_LIGHT_PROBES_ON");
            else
                cmd.DisableShaderKeyword("_LIGHT_PROBES_ON");
        }

        // Finds main light and main shadow casters and places them in the beginning of array.
        // Sort the remaining array based on custom IComparer criteria.
        private void SortLights(ref VisibleLight[] lights, int pixelLightsCount)
        {
            m_ShadowLightIndex = -1;
            if (lights.Length == 0)
                return;

            bool shadowsSupported = m_Asset.CurrShadowType != ShadowType.NO_SHADOW && pixelLightsCount > 0;
            int mainLightIndex = -1;

            for (int i = 0; i < lights.Length; ++i)
            {
                VisibleLight currLight = lights[i];
                if (currLight.lightType == LightType.Directional)
                    if (mainLightIndex == -1 || currLight.light.intensity > lights[mainLightIndex].light.intensity)
                        mainLightIndex = i;

                if (shadowsSupported && (currLight.light.shadows != LightShadows.None) && IsSupportedShadowType(currLight.lightType))
                    // Prefer directional shadows, if not sort by intensity
                    if (m_ShadowLightIndex == -1 || currLight.lightType > lights[m_ShadowLightIndex].lightType)
                        m_ShadowLightIndex = i;
            }

            // If supports a single directional light only, main light is main shadow light.
            if (pixelLightsCount == 1 && m_ShadowLightIndex > -1)
                mainLightIndex = m_ShadowLightIndex;

            int startIndex = 0;
            if (mainLightIndex > -1)
            {
                SwapLights(ref lights, 0, mainLightIndex);
                startIndex++;
            }

            if (mainLightIndex != m_ShadowLightIndex && m_ShadowLightIndex > 0)
            {
                SwapLights(ref lights, 1, m_ShadowLightIndex);
                m_ShadowLightIndex = 1;
                startIndex++;
            }

            Array.Sort(lights, startIndex, lights.Length - startIndex, this);
        }

        private bool IsSupportedShadowType(LightType type)
        {
            return (type == LightType.Directional || type == LightType.Spot);
        }

        private void SwapLights(ref VisibleLight[] lights, int lhsIndex, int rhsIndex)
        {
            if (lhsIndex == rhsIndex)
                return;

            VisibleLight temp = lights[lhsIndex];
            lights[lhsIndex] = lights[rhsIndex];
            lights[rhsIndex] = temp;
        }

        // Prioritizes Spot and Point lights by intensity. If any directional light, it will be the main
        // light and will not be considered in the computation.
        // TODO: Move to a better sorting solution, e.g, prioritize lights per object.
        public int Compare(VisibleLight lhs, VisibleLight rhs)
        {
            int lhsLightTypePriority = m_LightTypePriority[(int)lhs.lightType];
            int rhsLightTypePriority = m_LightTypePriority[(int)rhs.lightType];
            if (lhsLightTypePriority != rhsLightTypePriority)
                return rhsLightTypePriority - lhsLightTypePriority;

            return (int)(rhs.light.intensity - lhs.light.intensity);
        }
    }
}
