using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LowendMobile
{
    public class LowEndMobilePipeline : RenderPipeline
    {
        private readonly LowEndMobilePipelineAsset m_Asset;

        private static readonly int kMaxCascades = 4;
        private static readonly int kMaxLights = 8;
        private int m_ShadowCasterCascadesCount = kMaxCascades;
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

                // Render Shadow Map
                bool shadowsRendered = RenderShadows(cull, context);

                // Draw Opaques with support to one directional shadow cascade
                // Setup camera matrices
                context.SetupCameraProperties(camera);

                var cmd = new CommandBuffer() { name = "Clear" };
                cmd.ClearRenderTarget(true, true, Color.black);
                context.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                // Setup light and shadow shader constants
                SetupLightShaderVariables(cull.visibleLights, context);
                if (shadowsRendered)
                    SetupShadowShaderVariables(context, m_ShadowCasterCascadesCount);

                // Render Opaques
                var settings = new DrawRendererSettings(cull, camera, m_ForwardBasePassName);
                settings.sorting.flags = SortFlags.CommonOpaque;
                settings.inputFilter.SetQueuesOpaque();

                if (m_Asset.EnableLightmap)
                    settings.rendererConfiguration |= RendererConfiguration.PerObjectLightmaps;

                if (m_Asset.EnableAmbientProbe)
                    settings.rendererConfiguration |= RendererConfiguration.PerObjectLightProbe;

                context.DrawRenderers(ref settings);

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
            Shader.globalRenderPipeline = prevPipe;
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

        private void SetupLightShaderVariables(VisibleLight[] lights, ScriptableRenderContext context)
        {
            if (lights.Length <= 0)
                return;

            int pixelLightCount = Mathf.Min(lights.Length, m_Asset.MaxSupportedPixelLights);
            int vertexLightCount = (m_Asset.SupportsVertexLight)
                ? Mathf.Min(lights.Length - pixelLightCount, kMaxLights)
                : 0;
            int totalLightCount = Mathf.Min(pixelLightCount + vertexLightCount, kMaxLights);

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

                float rangeSq = currLight.range*currLight.range;
                float quadAtten = (currLight.lightType == LightType.Directional) ? 0.0f : 25.0f/rangeSq;

                if (currLight.lightType == LightType.Spot)
                {
                    Vector4 dir = currLight.localToWorld.GetColumn(2);
                    m_LightSpotDirections[i] = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                    float spotAngle = Mathf.Deg2Rad*currLight.spotAngle;
                    float cosOuterAngle = Mathf.Cos(spotAngle*0.5f);
                    float cosInneAngle = Mathf.Cos(spotAngle*0.25f);
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
            cmd.SetGlobalVector("globalLightCount", new Vector4(pixelLightCount, totalLightCount, 0.0f, 0.0f));
            SetShaderKeywords(cmd);
            context.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        private bool RenderShadows(CullResults cullResults, ScriptableRenderContext context)
        {
            m_ShadowCasterCascadesCount = m_ShadowSettings.directionalLightCascadeCount;

            VisibleLight[] lights = cullResults.visibleLights;
            int lightCount = lights.Length;

            int shadowResolution = 0;
            int lightIndex = -1;
            for (int i = 0; i < lightCount; ++i)
            {
                LightType type = lights[i].lightType;
                if (lights[i].light.shadows != LightShadows.None && (type == LightType.Directional || type == LightType.Spot))
                {
                    lightIndex = i;
                    if (lights[i].lightType == LightType.Spot)
                        m_ShadowCasterCascadesCount = 1;

                    shadowResolution = GetMaxTileResolutionInAtlas(m_ShadowSettings.shadowAtlasWidth,
                        m_ShadowSettings.shadowAtlasHeight, m_ShadowCasterCascadesCount);
                    break;
                }
            }

            if (lightIndex < 0)
                return false;

            Bounds bounds;
            if (!cullResults.GetShadowCasterBounds(lightIndex, out bounds))
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
            Vector3 lightDir = Vector3.Normalize(lights[lightIndex].light.transform.forward);

            Matrix4x4 view, proj;
            var settings = new DrawShadowsSettings(cullResults, lightIndex);
            bool needRendering = false;

            if (lights[lightIndex].lightType == LightType.Spot)
            {
                needRendering = cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(lightIndex, out view, out proj,
                        out settings.splitData);

                if (needRendering)
                {
                    SetupShadowSliceTransform(0, shadowResolution, proj, view);
                    RenderShadowSlice(ref context, lightDir, 0, proj, view, settings);
                }
            }
            else if (lights[lightIndex].lightType == LightType.Directional)
            {
                for (int cascadeIdx = 0; cascadeIdx < m_ShadowCasterCascadesCount; ++cascadeIdx)
                {
                    needRendering = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex,
                    cascadeIdx, m_ShadowCasterCascadesCount, splitRatio, shadowResolution, shadowNearPlane, out view, out proj,
                        out settings.splitData);

                    m_DirectionalShadowSplitDistances[cascadeIdx] = settings.splitData.cullingSphere;
                    m_DirectionalShadowSplitDistances[cascadeIdx].w *= settings.splitData.cullingSphere.w;

                    if (needRendering)
                    {
                        SetupShadowSliceTransform(cascadeIdx, shadowResolution, proj, view);
                        RenderShadowSlice(ref context, lightDir, cascadeIdx, proj, view, settings);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Only spot and directional shadow casters are supported in lowend mobile pipeline");
            }

            return true;
        }

        private void SetupShadowSliceTransform(int cascadeIndex, int shadowResolution, Matrix4x4 proj, Matrix4x4 view)
        {
            // Assumes MAX_CASCADES = 4
            m_ShadowSlices[cascadeIndex].atlasX = (cascadeIndex%2)*shadowResolution;
            m_ShadowSlices[cascadeIndex].atlasY = (cascadeIndex/2)*shadowResolution;
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
            matTile.m00 = (float) m_ShadowSlices[cascadeIndex].shadowResolution/
                          (float) m_ShadowSettings.shadowAtlasWidth;
            matTile.m11 = (float) m_ShadowSlices[cascadeIndex].shadowResolution/
                          (float) m_ShadowSettings.shadowAtlasHeight;
            matTile.m03 = (float) m_ShadowSlices[cascadeIndex].atlasX/(float) m_ShadowSettings.shadowAtlasWidth;
            matTile.m13 = (float) m_ShadowSlices[cascadeIndex].atlasY/(float) m_ShadowSettings.shadowAtlasHeight;

            m_ShadowSlices[cascadeIndex].shadowTransform = matTile*matScaleBias*proj*view;
        }

        private void RenderShadowSlice(ref ScriptableRenderContext context, Vector3 lightDir, int cascadeIndex,
            Matrix4x4 proj, Matrix4x4 view, DrawShadowsSettings settings)
        {
            var buffer = new CommandBuffer() {name = "Prepare Shadowmap Slice"};
            buffer.SetViewport(new Rect(m_ShadowSlices[cascadeIndex].atlasX, m_ShadowSlices[cascadeIndex].atlasY,
                m_ShadowSlices[cascadeIndex].shadowResolution, m_ShadowSlices[cascadeIndex].shadowResolution));
            buffer.SetViewProjectionMatrices(view, proj);
            buffer.SetGlobalVector("_WorldLightDirAndBias",
                new Vector4(-lightDir.x, -lightDir.y, -lightDir.z, m_Asset.ShadowBias));
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

            int currentTileCount = atlasWidth/resolution*atlasHeight/resolution;
            while (currentTileCount < tileCount)
            {
                resolution = resolution >> 1;
                currentTileCount = atlasWidth/resolution*atlasHeight/resolution;
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
            float invShadowResolution = 1.0f/shadowResolution;
            float[] pcfKernel =
            {
                -0.5f*invShadowResolution, 0.5f*invShadowResolution,
                0.5f*invShadowResolution, 0.5f*invShadowResolution,
                -0.5f*invShadowResolution, -0.5f*invShadowResolution,
                0.5f*invShadowResolution, -0.5f*invShadowResolution
            };

            var setupShadow = new CommandBuffer() {name = "SetupShadowShaderConstants"};
            setupShadow.SetGlobalMatrixArray("_WorldToShadow", shadowMatrices);
            setupShadow.SetGlobalVectorArray("_DirShadowSplitSpheres", m_DirectionalShadowSplitDistances);
            setupShadow.SetGlobalFloatArray("_PCFKernel", pcfKernel);
            context.ExecuteCommandBuffer(setupShadow);
            setupShadow.Dispose();
        }

        void SetShaderKeywords(CommandBuffer cmd)
        {
            if (m_Asset.SupportsVertexLight)
                cmd.EnableShaderKeyword("_VERTEX_LIGHTS");
            else
                cmd.DisableShaderKeyword("_VERTEX_LIGHTS");

            if (m_ShadowCasterCascadesCount == 1)
                cmd.DisableShaderKeyword("_SHADOW_CASCADES");
           	else
                cmd.EnableShaderKeyword("_SHADOW_CASCADES");

            switch (m_Asset.CurrShadowType)
            {
                case ShadowType.NO_SHADOW:
                    cmd.DisableShaderKeyword("HARD_SHADOWS");
                    cmd.DisableShaderKeyword("SOFT_SHADOWS");
                    break;

                case ShadowType.HARD_SHADOWS:
                    cmd.EnableShaderKeyword("HARD_SHADOWS");
                    cmd.DisableShaderKeyword("SOFT_SHADOWS");
                    break;

                case ShadowType.SOFT_SHADOWS:
                    cmd.DisableShaderKeyword("HARD_SHADOWS");
                    cmd.EnableShaderKeyword("SOFT_SHADOWS");
                    break;
            }
        }
    }
}
