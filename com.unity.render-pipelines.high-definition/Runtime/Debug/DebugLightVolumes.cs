using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class DebugLightVolumes
    {
        // Material used to blit the output texture into the camera render target
        Material m_Blit;
        // Material used to render the light volumes
        Material m_DebugLightVolumeMaterial;
        // Material to resolve the light volume textures
        ComputeShader m_DebugLightVolumeCompute;
        int m_DebugLightVolumeGradientKernel;
        int m_DebugLightVolumeColorsKernel;

        // Texture used to display the gradient
        Texture2D m_ColorGradientTexture = null;

        // Shader property ids
        public static readonly int _ColorShaderID = Shader.PropertyToID("_Color");
        public static readonly int _OffsetShaderID = Shader.PropertyToID("_Offset");
        public static readonly int _RangeShaderID = Shader.PropertyToID("_Range");
        public static readonly int _DebugLightCountBufferShaderID = Shader.PropertyToID("_DebugLightCountBuffer");
        public static readonly int _DebugColorAccumulationBufferShaderID = Shader.PropertyToID("_DebugColorAccumulationBuffer");
        public static readonly int _DebugLightVolumesTextureShaderID = Shader.PropertyToID("_DebugLightVolumesTexture");
        public static readonly int _ColorGradientTextureShaderID = Shader.PropertyToID("_ColorGradientTexture");
        public static readonly int _MaxDebugLightCountShaderID = Shader.PropertyToID("_MaxDebugLightCount");
        public static readonly int _BorderRadiusShaderID = Shader.PropertyToID("_BorderRadius");

        MaterialPropertyBlock m_MaterialProperty = new MaterialPropertyBlock();

        public DebugLightVolumes()
        {
        }

        public void InitData(HDRenderPipelineRuntimeResources renderPipelineResources)
        {
            m_DebugLightVolumeMaterial = CoreUtils.CreateEngineMaterial(renderPipelineResources.shaders.debugLightVolumePS);
            m_DebugLightVolumeCompute = renderPipelineResources.shaders.debugLightVolumeCS;
            m_DebugLightVolumeGradientKernel = m_DebugLightVolumeCompute.FindKernel("LightVolumeGradient");
            m_DebugLightVolumeColorsKernel = m_DebugLightVolumeCompute.FindKernel("LightVolumeColors");
            m_ColorGradientTexture = renderPipelineResources.textures.colorGradient;

            m_Blit = Blitter.GetBlitMaterial(TextureDimension.Tex2D);
        }

        public void ReleaseData()
        {
            CoreUtils.Destroy(m_DebugLightVolumeMaterial);
        }

        class RenderLightVolumesPassData
        {
            public HDCamera hdCamera;
            public CullingResults cullResults;
            public Material debugLightVolumeMaterial;
            public ComputeShader debugLightVolumeCS;
            public int debugLightVolumeKernel;
            public int maxDebugLightCount;
            public float borderRadius;
            public Texture2D colorGradientTexture;
            public bool lightOverlapEnabled;

            // Render target that holds the light count in floating points
            public TextureHandle lightCountBuffer;
            // Render target that holds the color accumulated value
            public TextureHandle colorAccumulationBuffer;
            // The output texture of the debug
            public TextureHandle debugLightVolumesTexture;
            // Required depth texture given that we render multiple render targets
            public TextureHandle depthBuffer;
            public TextureHandle destination;
        }

        public void RenderLightVolumes(RenderGraph renderGraph, LightingDebugSettings lightingDebugSettings, TextureHandle destination, TextureHandle depthBuffer, CullingResults cullResults, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<RenderLightVolumesPassData>("LightVolumes", out var passData))
            {
                bool lightOverlapEnabled = CoreUtils.IsLightOverlapDebugEnabled(hdCamera.camera);
                bool useColorAndEdge = lightingDebugSettings.lightVolumeDebugByCategory == LightVolumeDebug.ColorAndEdge || lightOverlapEnabled;

                passData.hdCamera = hdCamera;
                passData.cullResults = cullResults;
                passData.debugLightVolumeMaterial = m_DebugLightVolumeMaterial;
                passData.debugLightVolumeCS = m_DebugLightVolumeCompute;
                passData.debugLightVolumeKernel = useColorAndEdge ? m_DebugLightVolumeColorsKernel : m_DebugLightVolumeGradientKernel;
                passData.maxDebugLightCount = (int)lightingDebugSettings.maxDebugLightCount;
                passData.borderRadius = lightOverlapEnabled ? 0.5f : 1f;
                passData.colorGradientTexture = m_ColorGradientTexture;
                passData.lightOverlapEnabled = lightOverlapEnabled;
                passData.lightCountBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, clearBuffer = true, clearColor = Color.black, name = "LightVolumeCount" });
                passData.colorAccumulationBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.black, name = "LightVolumeColorAccumulation" });
                passData.debugLightVolumesTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.black, enableRandomWrite = true, name = "LightVolumeDebugLightVolumesTexture" });
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.destination = builder.WriteTexture(destination);

                builder.SetRenderFunc(
                    (RenderLightVolumesPassData data, RenderGraphContext ctx) =>
                    {
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        RenderTargetIdentifier[] mrt = ctx.renderGraphPool.GetTempArray<RenderTargetIdentifier>(2);
                        mrt[0] = data.lightCountBuffer;
                        mrt[1] = data.colorAccumulationBuffer;

                        if (data.lightOverlapEnabled)
                        {
                            // We only need the accumulation buffer, not the color (we only display the outline of the light shape in this mode).
                            CoreUtils.SetRenderTarget(ctx.cmd, mrt[0], depthBuffer);

                            // The cull result doesn't contains overlapping lights so we use a custom list
                            foreach (var overlappingHDLight in HDAdditionalLightData.s_overlappingHDLights)
                            {
                                RenderLightVolume(ctx.cmd, data.debugLightVolumeMaterial, overlappingHDLight, overlappingHDLight.legacyLight, mpb);
                            }
                        }
                        else
                        {
                            // Set the render target array
                            CoreUtils.SetRenderTarget(ctx.cmd, mrt, depthBuffer);

                            // First of all let's do the regions for the light sources (we only support Punctual and Area)
                            int numLights = data.cullResults.visibleLights.Length;
                            for (int lightIdx = 0; lightIdx < numLights; ++lightIdx)
                            {
                                // Let's build the light's bounding sphere matrix
                                Light currentLegacyLight = data.cullResults.visibleLights[lightIdx].light;
                                if (currentLegacyLight == null) continue;
                                HDAdditionalLightData currentHDRLight = currentLegacyLight.GetComponent<HDAdditionalLightData>();
                                if (currentHDRLight == null) continue;

                                RenderLightVolume(ctx.cmd, data.debugLightVolumeMaterial, currentHDRLight, currentLegacyLight, mpb);
                            }

                            // When we enable the light overlap mode we hide probes as they can't be baked in shadow masks
                            if (!data.lightOverlapEnabled)
                            {
                                // Now let's do the same but for reflection probes
                                int numProbes = data.cullResults.visibleReflectionProbes.Length;
                                for (int probeIdx = 0; probeIdx < numProbes; ++probeIdx)
                                {
                                    // Let's build the light's bounding sphere matrix
                                    ReflectionProbe currentLegacyProbe = data.cullResults.visibleReflectionProbes[probeIdx].reflectionProbe;
                                    HDAdditionalReflectionData currentHDProbe = currentLegacyProbe.GetComponent<HDAdditionalReflectionData>();

                                    if (!currentHDProbe)
                                        continue;

                                    MaterialPropertyBlock m_MaterialProperty = new MaterialPropertyBlock();
                                    Mesh targetMesh = null;
                                    if (currentHDProbe.influenceVolume.shape == InfluenceShape.Sphere)
                                    {
                                        m_MaterialProperty.SetVector(_RangeShaderID, new Vector3(currentHDProbe.influenceVolume.sphereRadius, currentHDProbe.influenceVolume.sphereRadius, currentHDProbe.influenceVolume.sphereRadius));
                                        targetMesh = DebugShapes.instance.RequestSphereMesh();
                                    }
                                    else
                                    {
                                        m_MaterialProperty.SetVector(_RangeShaderID, new Vector3(currentHDProbe.influenceVolume.boxSize.x, currentHDProbe.influenceVolume.boxSize.y, currentHDProbe.influenceVolume.boxSize.z));
                                        targetMesh = DebugShapes.instance.RequestBoxMesh();
                                    }

                                    m_MaterialProperty.SetColor(_ColorShaderID, new Color(1.0f, 1.0f, 0.0f, 1.0f));
                                    m_MaterialProperty.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                                    Matrix4x4 positionMat = Matrix4x4.Translate(currentLegacyProbe.transform.position);
                                    ctx.cmd.DrawMesh(targetMesh, positionMat, data.debugLightVolumeMaterial, 0, 0, m_MaterialProperty);
                                }
                            }
                        }

                        // Set the input params for the compute
                        ctx.cmd.SetComputeTextureParam(data.debugLightVolumeCS, data.debugLightVolumeKernel, _DebugLightCountBufferShaderID, data.lightCountBuffer);
                        ctx.cmd.SetComputeTextureParam(data.debugLightVolumeCS, data.debugLightVolumeKernel, _DebugColorAccumulationBufferShaderID, data.colorAccumulationBuffer);
                        ctx.cmd.SetComputeTextureParam(data.debugLightVolumeCS, data.debugLightVolumeKernel, _DebugLightVolumesTextureShaderID, data.debugLightVolumesTexture);
                        ctx.cmd.SetComputeTextureParam(data.debugLightVolumeCS, data.debugLightVolumeKernel, _ColorGradientTextureShaderID, data.colorGradientTexture);
                        ctx.cmd.SetComputeIntParam(data.debugLightVolumeCS, _MaxDebugLightCountShaderID, data.maxDebugLightCount);
                        ctx.cmd.SetComputeFloatParam(data.debugLightVolumeCS, _BorderRadiusShaderID, data.borderRadius);

                        // Texture dimensions
                        int texWidth = data.hdCamera.actualWidth;
                        int texHeight = data.hdCamera.actualHeight;

                        // Dispatch the compute
                        int lightVolumesTileSize = 8;
                        int numTilesX = (texWidth + (lightVolumesTileSize - 1)) / lightVolumesTileSize;
                        int numTilesY = (texHeight + (lightVolumesTileSize - 1)) / lightVolumesTileSize;
                        ctx.cmd.DispatchCompute(data.debugLightVolumeCS, data.debugLightVolumeKernel, numTilesX, numTilesY, data.hdCamera.viewCount);

                        // Blit this into the camera target
                        CoreUtils.SetRenderTarget(ctx.cmd, destination);
                        mpb.SetTexture(HDShaderIDs._BlitTexture, data.debugLightVolumesTexture);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugLightVolumeMaterial, 1, MeshTopology.Triangles, 3, 1, mpb);
                    });
            }
        }

        static void RenderLightVolume(
            CommandBuffer cmd,
            Material debugLightVolumeMaterial,
            HDAdditionalLightData currentHDRLight,
            Light currentLegacyLight,
            MaterialPropertyBlock mpb)
        {
            Matrix4x4 positionMat = Matrix4x4.Translate(currentLegacyLight.transform.position);

            switch (currentHDRLight.ComputeLightType(currentLegacyLight))
            {
                case HDLightType.Point:
                    mpb.SetColor(_ColorShaderID, new Color(0.0f, 0.5f, 0.0f, 1.0f));
                    mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                    mpb.SetVector(_RangeShaderID, new Vector3(currentLegacyLight.range, currentLegacyLight.range, currentLegacyLight.range));
                    cmd.DrawMesh(DebugShapes.instance.RequestSphereMesh(), positionMat, debugLightVolumeMaterial, 0, 0, mpb);
                    break;
                case HDLightType.Spot:
                    switch (currentHDRLight.spotLightShape)
                    {
                        case SpotLightShape.Cone:
                            float bottomRadius = Mathf.Tan(currentLegacyLight.spotAngle * Mathf.PI / 360.0f) * currentLegacyLight.range;
                            mpb.SetColor(_ColorShaderID, new Color(1.0f, 0.5f, 0.0f, 1.0f));
                            mpb.SetVector(_RangeShaderID, new Vector3(bottomRadius, bottomRadius, currentLegacyLight.range));
                            mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                            cmd.DrawMesh(DebugShapes.instance.RequestConeMesh(), currentLegacyLight.gameObject.transform.localToWorldMatrix, debugLightVolumeMaterial, 0, 0, mpb);
                            break;
                        case SpotLightShape.Box:
                            mpb.SetColor(_ColorShaderID, new Color(1.0f, 0.5f, 0.0f, 1.0f));
                            mpb.SetVector(_RangeShaderID, new Vector3(currentHDRLight.shapeWidth, currentHDRLight.shapeHeight, currentLegacyLight.range));
                            mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, currentLegacyLight.range / 2.0f));
                            cmd.DrawMesh(DebugShapes.instance.RequestBoxMesh(), currentLegacyLight.gameObject.transform.localToWorldMatrix, debugLightVolumeMaterial, 0, 0, mpb);
                            break;
                        case SpotLightShape.Pyramid:
                            float bottomWidth = Mathf.Tan(currentLegacyLight.spotAngle * Mathf.PI / 360.0f) * currentLegacyLight.range;
                            mpb.SetColor(_ColorShaderID, new Color(1.0f, 0.5f, 0.0f, 1.0f));
                            mpb.SetVector(_RangeShaderID, new Vector3(currentHDRLight.aspectRatio * bottomWidth * 2, bottomWidth * 2, currentLegacyLight.range));
                            mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                            cmd.DrawMesh(DebugShapes.instance.RequestPyramidMesh(), currentLegacyLight.gameObject.transform.localToWorldMatrix, debugLightVolumeMaterial, 0, 0, mpb);
                            break;
                    }
                    break;
                case HDLightType.Area:
                    switch (currentHDRLight.areaLightShape)
                    {
                        case AreaLightShape.Rectangle:
                            mpb.SetColor(_ColorShaderID, new Color(0.0f, 1.0f, 1.0f, 1.0f));
                            mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                            mpb.SetVector(_RangeShaderID, new Vector3(currentLegacyLight.range, currentLegacyLight.range, currentLegacyLight.range));
                            cmd.DrawMesh(DebugShapes.instance.RequestSphereMesh(), positionMat, debugLightVolumeMaterial, 0, 0, mpb);
                            break;
                        case AreaLightShape.Tube:
                            mpb.SetColor(_ColorShaderID, new Color(1.0f, 0.0f, 0.5f, 1.0f));
                            mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                            mpb.SetVector(_RangeShaderID, new Vector3(currentLegacyLight.range, currentLegacyLight.range, currentLegacyLight.range));
                            cmd.DrawMesh(DebugShapes.instance.RequestSphereMesh(), positionMat, debugLightVolumeMaterial, 0, 0, mpb);
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }
    }
}
