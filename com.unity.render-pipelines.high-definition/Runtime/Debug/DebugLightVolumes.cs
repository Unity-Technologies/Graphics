using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class DebugLightVolumes
    {
        // Render target that holds the light count in floating points
        RTHandle m_LightCountBuffer = null;
        // Render target that holds the color accumulated value
        RTHandle m_ColorAccumulationBuffer = null;
        // The output texture of the debug
        RTHandle m_DebugLightVolumesTexture = null;
        // Required depth texture given that we render multiple render targets
        RTHandle m_DepthBuffer = null;

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

        // Render target array for the prepass
        RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[2];

        MaterialPropertyBlock m_MaterialProperty = new MaterialPropertyBlock();

        public DebugLightVolumes()
        {
        }

        public void InitData(RenderPipelineResources renderPipelineResources)
        {
            m_DebugLightVolumeMaterial = CoreUtils.CreateEngineMaterial(renderPipelineResources.shaders.debugLightVolumePS);
            m_DebugLightVolumeCompute = renderPipelineResources.shaders.debugLightVolumeCS;
            m_DebugLightVolumeGradientKernel = m_DebugLightVolumeCompute.FindKernel("LightVolumeGradient");
            m_DebugLightVolumeColorsKernel = m_DebugLightVolumeCompute.FindKernel("LightVolumeColors");
            m_ColorGradientTexture = renderPipelineResources.textures.colorGradient;

            m_Blit = CoreUtils.CreateEngineMaterial(renderPipelineResources.shaders.blitPS);

            m_LightCountBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R32_SFloat, enableRandomWrite: false, useMipMap: false, name: "LightVolumeCount");
            m_ColorAccumulationBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: false, useMipMap: false, name: "LightVolumeColorAccumulation");
            m_DebugLightVolumesTexture = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useMipMap: false, name: "LightVolumeDebugLightVolumesTexture");
            m_DepthBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, depthBufferBits: DepthBits.None, colorFormat: GraphicsFormat.R8_UNorm, name: "LightVolumeDepth");
            // Fill the render target array
            m_RTIDs[0] = m_LightCountBuffer;
            m_RTIDs[1] = m_ColorAccumulationBuffer;
        }

        public void ReleaseData()
        {
            CoreUtils.Destroy(m_Blit);

            RTHandles.Release(m_DepthBuffer);
            RTHandles.Release(m_DebugLightVolumesTexture);
            RTHandles.Release(m_ColorAccumulationBuffer);
            RTHandles.Release(m_LightCountBuffer);

            CoreUtils.Destroy(m_DebugLightVolumeMaterial);
        }

        public struct RenderLightVolumesParameters
        {
            public HDCamera         hdCamera;
            public CullingResults   cullResults;
            public Material         debugLightVolumeMaterial;
            public ComputeShader    debugLightVolumeCS;
            public int              debugLightVolumeKernel;
            public int              maxDebugLightCount;
            public Texture2D        colorGradientTexture;
        }

        public RenderLightVolumesParameters PrepareLightVolumeParameters(HDCamera hdCamera, LightingDebugSettings lightDebugSettings, CullingResults cullResults)
        {
            var parameters = new RenderLightVolumesParameters();

            parameters.hdCamera = hdCamera;
            parameters.cullResults = cullResults;
            parameters.debugLightVolumeMaterial = m_DebugLightVolumeMaterial;
            parameters.debugLightVolumeCS = m_DebugLightVolumeCompute;
            parameters.debugLightVolumeKernel = lightDebugSettings.lightVolumeDebugByCategory == LightVolumeDebug.ColorAndEdge ? m_DebugLightVolumeColorsKernel : m_DebugLightVolumeGradientKernel;
            parameters.maxDebugLightCount = (int)lightDebugSettings.maxDebugLightCount;
            parameters.colorGradientTexture = m_ColorGradientTexture;

            return parameters;
        }

        public static void RenderLightVolumes(CommandBuffer cmd,
                                                in RenderLightVolumesParameters parameters,
                                                RenderTargetIdentifier[] accumulationMRT, // [0] = m_LightCountBuffer, [1] m_ColorAccumulationBuffer
                                                RTHandle lightCountBuffer,
                                                RTHandle colorAccumulationBuffer,
                                                RTHandle debugLightVolumesTexture,
                                                RTHandle depthBuffer,
                                                RTHandle destination,
                                                MaterialPropertyBlock mpb)
        {
            // Set the render target array
            CoreUtils.SetRenderTarget(cmd, accumulationMRT, depthBuffer);

            // First of all let's do the regions for the light sources (we only support Punctual and Area)
            int numLights = parameters.cullResults.visibleLights.Length;
            for (int lightIdx = 0; lightIdx < numLights; ++lightIdx)
            {
                // Let's build the light's bounding sphere matrix
                Light currentLegacyLight = parameters.cullResults.visibleLights[lightIdx].light;
                if (currentLegacyLight == null) continue;
                HDAdditionalLightData currentHDRLight = currentLegacyLight.GetComponent<HDAdditionalLightData>();
                if (currentHDRLight == null) continue;

                Matrix4x4 positionMat = Matrix4x4.Translate(currentLegacyLight.transform.position);

                switch(currentHDRLight.ComputeLightType(currentLegacyLight))
                {
                    case HDLightType.Point:
                        mpb.SetColor(_ColorShaderID, new Color(0.0f, 0.5f, 0.0f, 1.0f));
                        mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                        mpb.SetVector(_RangeShaderID, new Vector3(currentLegacyLight.range, currentLegacyLight.range, currentLegacyLight.range));
                        cmd.DrawMesh(DebugShapes.instance.RequestSphereMesh(), positionMat, parameters.debugLightVolumeMaterial, 0, 0, mpb);
                        break;
                    case HDLightType.Spot:
                        switch (currentHDRLight.spotLightShape)
                        {
                            case SpotLightShape.Cone:
                                float bottomRadius = Mathf.Tan(currentLegacyLight.spotAngle * Mathf.PI / 360.0f) * currentLegacyLight.range;
                                mpb.SetColor(_ColorShaderID, new Color(1.0f, 0.5f, 0.0f, 1.0f));
                                mpb.SetVector(_RangeShaderID, new Vector3(bottomRadius, bottomRadius, currentLegacyLight.range));
                                mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                                cmd.DrawMesh(DebugShapes.instance.RequestConeMesh(), currentLegacyLight.gameObject.transform.localToWorldMatrix, parameters.debugLightVolumeMaterial, 0, 0, mpb);
                                break;
                            case SpotLightShape.Box:
                                mpb.SetColor(_ColorShaderID, new Color(1.0f, 0.5f, 0.0f, 1.0f));
                                mpb.SetVector(_RangeShaderID, new Vector3(currentHDRLight.shapeWidth, currentHDRLight.shapeHeight, currentLegacyLight.range));
                                mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, currentLegacyLight.range / 2.0f));
                                cmd.DrawMesh(DebugShapes.instance.RequestBoxMesh(), currentLegacyLight.gameObject.transform.localToWorldMatrix, parameters.debugLightVolumeMaterial, 0, 0, mpb);
                                break;
                            case SpotLightShape.Pyramid:
                                float bottomWidth = Mathf.Tan(currentLegacyLight.spotAngle * Mathf.PI / 360.0f) * currentLegacyLight.range;
                                mpb.SetColor(_ColorShaderID, new Color(1.0f, 0.5f, 0.0f, 1.0f));
                                mpb.SetVector(_RangeShaderID, new Vector3(currentHDRLight.aspectRatio * bottomWidth * 2, bottomWidth * 2, currentLegacyLight.range));
                                mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                                cmd.DrawMesh(DebugShapes.instance.RequestPyramidMesh(), currentLegacyLight.gameObject.transform.localToWorldMatrix, parameters.debugLightVolumeMaterial, 0, 0, mpb);
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
                                cmd.DrawMesh(DebugShapes.instance.RequestSphereMesh(), positionMat, parameters.debugLightVolumeMaterial, 0, 0, mpb);
                                break;
                            case AreaLightShape.Tube:
                                mpb.SetColor(_ColorShaderID, new Color(1.0f, 0.0f, 0.5f, 1.0f));
                                mpb.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                                mpb.SetVector(_RangeShaderID, new Vector3(currentLegacyLight.range, currentLegacyLight.range, currentLegacyLight.range));
                                cmd.DrawMesh(DebugShapes.instance.RequestSphereMesh(), positionMat, parameters.debugLightVolumeMaterial, 0, 0, mpb);
                                break;
                            default:
                                break;
                        }
                        break;
                }
            }

            // Now let's do the same but for reflection probes
            int numProbes = parameters.cullResults.visibleReflectionProbes.Length;
            for (int probeIdx = 0; probeIdx < numProbes; ++probeIdx)
            {
                // Let's build the light's bounding sphere matrix
                ReflectionProbe currentLegacyProbe = parameters.cullResults.visibleReflectionProbes[probeIdx].reflectionProbe;
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
                cmd.DrawMesh(targetMesh, positionMat, parameters.debugLightVolumeMaterial, 0, 0, m_MaterialProperty);
            }

            // Set the input params for the compute
            cmd.SetComputeTextureParam(parameters.debugLightVolumeCS, parameters.debugLightVolumeKernel, _DebugLightCountBufferShaderID, lightCountBuffer);
            cmd.SetComputeTextureParam(parameters.debugLightVolumeCS, parameters.debugLightVolumeKernel, _DebugColorAccumulationBufferShaderID, colorAccumulationBuffer);
            cmd.SetComputeTextureParam(parameters.debugLightVolumeCS, parameters.debugLightVolumeKernel, _DebugLightVolumesTextureShaderID, debugLightVolumesTexture);
            cmd.SetComputeTextureParam(parameters.debugLightVolumeCS, parameters.debugLightVolumeKernel, _ColorGradientTextureShaderID, parameters.colorGradientTexture);
            cmd.SetComputeIntParam(parameters.debugLightVolumeCS, _MaxDebugLightCountShaderID, parameters.maxDebugLightCount);

            // Texture dimensions
            int texWidth = parameters.hdCamera.actualWidth; // m_ColorAccumulationBuffer.rt.width;
            int texHeight = parameters.hdCamera.actualHeight; // m_ColorAccumulationBuffer.rt.width;


            // Dispatch the compute
            int lightVolumesTileSize = 8;
            int numTilesX = (texWidth + (lightVolumesTileSize - 1)) / lightVolumesTileSize;
            int numTilesY = (texHeight + (lightVolumesTileSize - 1)) / lightVolumesTileSize;
            cmd.DispatchCompute(parameters.debugLightVolumeCS, parameters.debugLightVolumeKernel, numTilesX, numTilesY, parameters.hdCamera.viewCount);

            // Blit this into the camera target
            CoreUtils.SetRenderTarget(cmd, destination);
            mpb.SetTexture(HDShaderIDs._BlitTexture, debugLightVolumesTexture);
            cmd.DrawProcedural(Matrix4x4.identity, parameters.debugLightVolumeMaterial, 1, MeshTopology.Triangles, 3, 1, mpb);
        }

        public void RenderLightVolumes(CommandBuffer cmd, HDCamera hdCamera, CullingResults cullResults, LightingDebugSettings lightDebugSettings, RTHandle finalRT)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DisplayLightVolume)))
            {
                // Clear the buffers
                CoreUtils.SetRenderTarget(cmd, m_ColorAccumulationBuffer, ClearFlag.Color, Color.black);
                CoreUtils.SetRenderTarget(cmd, m_LightCountBuffer, ClearFlag.Color, Color.black);
                CoreUtils.SetRenderTarget(cmd, m_DebugLightVolumesTexture, ClearFlag.Color, Color.black);

                var parameters = PrepareLightVolumeParameters(hdCamera, lightDebugSettings, cullResults);

                RenderLightVolumes(cmd, parameters, m_RTIDs, m_LightCountBuffer, m_ColorAccumulationBuffer, m_DebugLightVolumesTexture, m_DepthBuffer, finalRT, m_MaterialProperty);
            }
        }
    }
}
