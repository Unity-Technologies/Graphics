using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(PackingRules.Exact, false)]
    struct LightVolume
    {
        public int active;
        public int shape;
        public Vector3 position;
        public Vector3 range;
        public uint lightType;
        public uint lightIndex;
    }

    class HDRaytracingLightCluster
    {
        // External data
        RenderPipelineResources m_RenderPipelineResources = null;
        HDRenderPipelineRayTracingResources m_RenderPipelineRayTracingResources = null;
        HDRenderPipeline m_RenderPipeline = null;

        // Light Culling data
        LightVolume[] m_LightVolumesCPUArray = null;
        ComputeBuffer m_LightVolumeGPUArray = null;

        // Culling result
        ComputeBuffer m_LightCullResult = null;

        // Output cluster data
        ComputeBuffer m_LightCluster = null;

        // Light runtime data
        List<LightData> m_LightDataCPUArray = new List<LightData>();
        ComputeBuffer m_LightDataGPUArray = null;

        // Env Light data
        List<EnvLightData> m_EnvLightDataCPUArray = new List<EnvLightData>();
        ComputeBuffer m_EnvLightDataGPUArray = null;

        // Light cluster debug material
        Material m_DebugMaterial = null;

        // String values
        const string m_LightClusterKernelName = "RaytracingLightCluster";
        const string m_LightCullKernelName = "RaytracingLightCull";

        public static readonly int _ClusterCellSize = Shader.PropertyToID("_ClusterCellSize");
        public static readonly int _LightVolumes = Shader.PropertyToID("_LightVolumes");
        public static readonly int _LightVolumeCount = Shader.PropertyToID("_LightVolumeCount");
        public static readonly int _DebugColorGradientTexture = Shader.PropertyToID("_DebugColorGradientTexture");
        public static readonly int _DebutLightClusterTexture = Shader.PropertyToID("_DebutLightClusterTexture");
        public static readonly int _RaytracingLightCullResult = Shader.PropertyToID("_RaytracingLightCullResult");
        public static readonly int _ClusterCenterPosition = Shader.PropertyToID("_ClusterCenterPosition");
        public static readonly int _ClusterDimension = Shader.PropertyToID("_ClusterDimension");

        // Temporary variables
        // This value is now fixed for every HDRP asset
        int m_NumLightsPerCell = 0;

        // These values are overriden for every light cluster that is built
        Vector3 minClusterPos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 maxClusterPos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 clusterCellSize = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 clusterCenter = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 clusterDimension = new Vector3(0.0f, 0.0f, 0.0f);
        int punctualLightCount = 0;
        int areaLightCount = 0;
        int envLightCount = 0;
        int totalLightCount = 0;
        Bounds bounds = new Bounds();
        Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxBounds = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
        Matrix4x4 localToWorldMatrix = new Matrix4x4();
        VisibleLight visibleLight = new VisibleLight();
        Light lightComponent;

        public void Initialize(HDRenderPipeline renderPipeline)
        {
            // Keep track of the external buffers
            m_RenderPipelineResources = renderPipeline.asset.renderPipelineResources;
            m_RenderPipelineRayTracingResources = renderPipeline.asset.renderPipelineRayTracingResources;

            // Keep track of the render pipeline
            m_RenderPipeline = renderPipeline;

            // Pre allocate the cluster with a dummy size
            m_LightDataGPUArray = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
            m_EnvLightDataGPUArray = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));

            // Allocate the light cluster buffer at the right size
            m_NumLightsPerCell = renderPipeline.asset.currentPlatformRenderPipelineSettings.lightLoopSettings.maxLightsPerClusterCell;
            int bufferSize = 64 * 64 * 32 * (renderPipeline.asset.currentPlatformRenderPipelineSettings.lightLoopSettings.maxLightsPerClusterCell + 4);
            ResizeClusterBuffer(bufferSize);

            // Create the material required for debug
            m_DebugMaterial = CoreUtils.CreateEngineMaterial(m_RenderPipelineRayTracingResources.lightClusterDebugS);
        }

        public void ReleaseResources()
        {
            CoreUtils.SafeRelease(m_LightVolumeGPUArray);
            m_LightVolumeGPUArray = null;

            CoreUtils.SafeRelease(m_LightCluster);
            m_LightCluster = null;

            CoreUtils.SafeRelease(m_LightCullResult);
            m_LightCullResult = null;

            CoreUtils.SafeRelease(m_LightDataGPUArray);
            m_LightDataGPUArray = null;

            CoreUtils.SafeRelease(m_EnvLightDataGPUArray);
            m_EnvLightDataGPUArray = null;

            CoreUtils.Destroy(m_DebugMaterial);
            m_DebugMaterial = null;
        }

        void ResizeClusterBuffer(int bufferSize)
        {
            // Release the previous buffer
            if (m_LightCluster != null)
            {
                // If it is not null and it has already the right size, we are pretty much done
                if (m_LightCluster.count == bufferSize)
                    return;

                CoreUtils.SafeRelease(m_LightCluster);
                m_LightCluster = null;
            }

            // Allocate the next buffer buffer
            if (bufferSize > 0)
            {
                m_LightCluster = new ComputeBuffer(bufferSize, sizeof(uint));
            }
        }

        void ResizeCullResultBuffer(int numLights)
        {
            // Release the previous buffer
            if (m_LightCullResult != null)
            {
                // If it is not null and it has already the right size, we are pretty much done
                if (m_LightCullResult.count == numLights)
                    return;

                CoreUtils.SafeRelease(m_LightCullResult);
                m_LightCullResult = null;
            }

            // Allocate the next buffer buffer
            if (numLights > 0)
            {
                m_LightCullResult = new ComputeBuffer(numLights, sizeof(uint));
            }
        }

        void ResizeVolumeBuffer(int numLights)
        {
            // Release the previous buffer
            if (m_LightVolumeGPUArray != null)
            {
                // If it is not null and it has already the right size, we are pretty much done
                if (m_LightVolumeGPUArray.count == numLights)
                    return;

                CoreUtils.SafeRelease(m_LightVolumeGPUArray);
                m_LightVolumeGPUArray = null;
            }

            // Allocate the next buffer buffer
            if (numLights > 0)
            {
                m_LightVolumesCPUArray = new LightVolume[numLights];
                m_LightVolumeGPUArray = new ComputeBuffer(numLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightVolume)));
            }
        }

        void ResizeLightDataBuffer(int numLights)
        {
            // Release the previous buffer
            if (m_LightDataGPUArray != null)
            {
                // If it is not null and it has already the right size, we are pretty much done
                if (m_LightDataGPUArray.count == numLights)
                    return;

                // It is not the right size, free it to be reallocated
                CoreUtils.SafeRelease(m_LightDataGPUArray);
                m_LightDataGPUArray = null;
            }

            // Allocate the next buffer buffer
            if (numLights > 0)
            {
                m_LightDataGPUArray = new ComputeBuffer(numLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
            }
        }

        void ResizeEnvLightDataBuffer(int numEnvLights)
        {
            // Release the previous buffer
            if (m_EnvLightDataGPUArray != null)
            {
                // If it is not null and it has already the right size, we are pretty much done
                if (m_EnvLightDataGPUArray.count == numEnvLights)
                    return;

                CoreUtils.SafeRelease(m_EnvLightDataGPUArray);
                m_EnvLightDataGPUArray = null;
            }

            // Allocate the next buffer buffer
            if (numEnvLights > 0)
            {
                m_EnvLightDataGPUArray = new ComputeBuffer(numEnvLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
            }
        }

        void OOBBToAABBBounds(Vector3 centerWS, Vector3 extents, Vector3 up, Vector3 right, Vector3 forward, ref Bounds outBounds)
        {
            // Reset the bounds of the AABB
            bounds.min = minBounds;
            bounds.max = maxBounds;

            // Push the 8 corners of the oobb into the AABB
            bounds.Encapsulate(centerWS + right * extents.x + up * extents.y + forward * extents.z);
            bounds.Encapsulate(centerWS + right * extents.x + up * extents.y - forward * extents.z);
            bounds.Encapsulate(centerWS + right * extents.x - up * extents.y + forward * extents.z);
            bounds.Encapsulate(centerWS + right * extents.x - up * extents.y - forward * extents.z);
            bounds.Encapsulate(centerWS - right * extents.x + up * extents.y + forward * extents.z);
            bounds.Encapsulate(centerWS - right * extents.x + up * extents.y - forward * extents.z);
            bounds.Encapsulate(centerWS - right * extents.x - up * extents.y + forward * extents.z);
            bounds.Encapsulate(centerWS - right * extents.x - up * extents.y - forward * extents.z);
        }

        void BuildGPULightVolumes(HDCamera hdCamera, HDRayTracingLights rayTracingLights)
        {
            int totalNumLights = rayTracingLights.lightCount;

            // Make sure the light volume buffer has the right size
            if (m_LightVolumesCPUArray == null || totalNumLights != m_LightVolumesCPUArray.Length)
            {
                ResizeVolumeBuffer(totalNumLights);
            }

            // Set Light volume data to the CPU buffer
            punctualLightCount = 0;
            areaLightCount = 0;
            envLightCount = 0;
            totalLightCount = 0;

            int realIndex = 0;
            for (int lightIdx = 0; lightIdx < rayTracingLights.hdLightArray.Count; ++lightIdx)
            {
                HDAdditionalLightData currentLight = rayTracingLights.hdLightArray[lightIdx];

                // When the user deletes a light source in the editor, there is a single frame where the light is null before the collection of light in the scene is triggered
                // the workaround for this is simply to not add it if it is null for that invalid frame
                if (currentLight != null)
                {
                    Light light = currentLight.gameObject.GetComponent<Light>();
                    if (light == null || !light.enabled)
                        continue;

                    // If the light is flagged as baked and has been effectively been baked, we need to skip it and not add it to the light cluster
                    if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Baked && light.bakingOutput.isBaked)
                        continue;

                    // If this light should not be included when ray tracing is active on the camera, skip it
                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && !currentLight.includeForRayTracing)
                        continue;

                    // Reserve space in the cookie atlas
                    m_RenderPipeline.ReserveCookieAtlasTexture(currentLight, light, currentLight.type);

                    // Compute the camera relative position
                    Vector3 lightPositionRWS = currentLight.gameObject.transform.position;
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                    {
                        lightPositionRWS -= hdCamera.camera.transform.position;
                    }

                    // Grab the light range
                    float lightRange = light.range;

                    if (currentLight.type != HDLightType.Area)
                    {
                        m_LightVolumesCPUArray[realIndex].range = new Vector3(lightRange, lightRange, lightRange);
                        m_LightVolumesCPUArray[realIndex].position = lightPositionRWS;
                        m_LightVolumesCPUArray[realIndex].active = (currentLight.gameObject.activeInHierarchy ? 1 : 0);
                        m_LightVolumesCPUArray[realIndex].lightIndex = (uint)lightIdx;
                        m_LightVolumesCPUArray[realIndex].shape = 0;
                        m_LightVolumesCPUArray[realIndex].lightType = 0;
                        punctualLightCount++;
                    }
                    else
                    {
                        // let's compute the oobb of the light influence volume first
                        Vector3 oobbDimensions = new Vector3(currentLight.shapeWidth + 2 * lightRange, currentLight.shapeHeight + 2 * lightRange, lightRange); // One-sided
                        Vector3 extents    = 0.5f * oobbDimensions;
                        Vector3 oobbCenter = lightPositionRWS + extents.z * currentLight.gameObject.transform.forward;

                        // Let's now compute an AABB that matches the previously defined OOBB
                        OOBBToAABBBounds(oobbCenter, extents, currentLight.gameObject.transform.up, currentLight.gameObject.transform.right, currentLight.gameObject.transform.forward, ref bounds);

                        // Fill the volume data
                        m_LightVolumesCPUArray[realIndex].range = bounds.extents;
                        m_LightVolumesCPUArray[realIndex].position = bounds.center;
                        m_LightVolumesCPUArray[realIndex].active = (currentLight.gameObject.activeInHierarchy ? 1 : 0);
                        m_LightVolumesCPUArray[realIndex].lightIndex = (uint)lightIdx;
                        m_LightVolumesCPUArray[realIndex].shape = 1;
                        m_LightVolumesCPUArray[realIndex].lightType = 1;
                        areaLightCount++;
                    }
                    realIndex++;
                }
            }

            int indexOffset = realIndex;

            // Set Env Light volume data to the CPU buffer
            for (int lightIdx = 0; lightIdx < rayTracingLights.reflectionProbeArray.Count; ++lightIdx)
            {
                HDProbe currentEnvLight = rayTracingLights.reflectionProbeArray[lightIdx];


                if (currentEnvLight != null)
                {
                    // If the reflection probe is disabled, we should not be adding it
                    if (!currentEnvLight.enabled) continue;

                    // Compute the camera relative position
                    Vector3 probePositionRWS = currentEnvLight.influenceToWorld.GetColumn(3);
                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                    {
                        probePositionRWS -= hdCamera.camera.transform.position;
                    }

                    if (currentEnvLight.influenceVolume.shape == InfluenceShape.Sphere)
                    {
                        m_LightVolumesCPUArray[lightIdx + indexOffset].shape = 0;
                        m_LightVolumesCPUArray[lightIdx + indexOffset].range = new Vector3(currentEnvLight.influenceVolume.sphereRadius, currentEnvLight.influenceVolume.sphereRadius, currentEnvLight.influenceVolume.sphereRadius);
                        m_LightVolumesCPUArray[lightIdx + indexOffset].position = probePositionRWS;
                    }
                    else
                    {
                        m_LightVolumesCPUArray[lightIdx + indexOffset].shape = 1;
                        m_LightVolumesCPUArray[lightIdx + indexOffset].range = new Vector3(currentEnvLight.influenceVolume.boxSize.x / 2.0f, currentEnvLight.influenceVolume.boxSize.y / 2.0f, currentEnvLight.influenceVolume.boxSize.z / 2.0f);
                        m_LightVolumesCPUArray[lightIdx + indexOffset].position = probePositionRWS;
                    }
                    m_LightVolumesCPUArray[lightIdx + indexOffset].active = (currentEnvLight.gameObject.activeInHierarchy ? 1 : 0);
                    m_LightVolumesCPUArray[lightIdx + indexOffset].lightIndex = (uint)lightIdx;
                    m_LightVolumesCPUArray[lightIdx + indexOffset].lightType = 2;
                    envLightCount++;
                }
            }

            totalLightCount = punctualLightCount + areaLightCount + envLightCount;

            // Push the light volumes to the GPU
            m_LightVolumeGPUArray.SetData(m_LightVolumesCPUArray);
        }

        void EvaluateClusterVolume(HDCamera hdCamera)
        {
            var settings = hdCamera.volumeStack.GetComponent<LightCluster>();

            if (ShaderConfig.s_CameraRelativeRendering != 0)
                clusterCenter.Set(0, 0, 0);
            else
                clusterCenter = hdCamera.camera.gameObject.transform.position;

            minClusterPos.Set(float.MaxValue, float.MaxValue, float.MaxValue);
            maxClusterPos.Set(-float.MaxValue, -float.MaxValue, -float.MaxValue);

            for (int lightIdx = 0; lightIdx < totalLightCount; ++lightIdx)
            {
                minClusterPos.x = Mathf.Min(m_LightVolumesCPUArray[lightIdx].position.x - m_LightVolumesCPUArray[lightIdx].range.x, minClusterPos.x);
                minClusterPos.y = Mathf.Min(m_LightVolumesCPUArray[lightIdx].position.y - m_LightVolumesCPUArray[lightIdx].range.y, minClusterPos.y);
                minClusterPos.z = Mathf.Min(m_LightVolumesCPUArray[lightIdx].position.z - m_LightVolumesCPUArray[lightIdx].range.z, minClusterPos.z);

                maxClusterPos.x = Mathf.Max(m_LightVolumesCPUArray[lightIdx].position.x + m_LightVolumesCPUArray[lightIdx].range.x, maxClusterPos.x);
                maxClusterPos.y = Mathf.Max(m_LightVolumesCPUArray[lightIdx].position.y + m_LightVolumesCPUArray[lightIdx].range.y, maxClusterPos.y);
                maxClusterPos.z = Mathf.Max(m_LightVolumesCPUArray[lightIdx].position.z + m_LightVolumesCPUArray[lightIdx].range.z, maxClusterPos.z);
            }

            minClusterPos.x = minClusterPos.x < clusterCenter.x - settings.cameraClusterRange.value ? clusterCenter.x - settings.cameraClusterRange.value : minClusterPos.x;
            minClusterPos.y = minClusterPos.y < clusterCenter.y - settings.cameraClusterRange.value ? clusterCenter.y - settings.cameraClusterRange.value : minClusterPos.y;
            minClusterPos.z = minClusterPos.z < clusterCenter.z - settings.cameraClusterRange.value ? clusterCenter.z - settings.cameraClusterRange.value : minClusterPos.z;

            maxClusterPos.x = maxClusterPos.x > clusterCenter.x + settings.cameraClusterRange.value ? clusterCenter.x + settings.cameraClusterRange.value : maxClusterPos.x;
            maxClusterPos.y = maxClusterPos.y > clusterCenter.y + settings.cameraClusterRange.value ? clusterCenter.y + settings.cameraClusterRange.value : maxClusterPos.y;
            maxClusterPos.z = maxClusterPos.z > clusterCenter.z + settings.cameraClusterRange.value ? clusterCenter.z + settings.cameraClusterRange.value : maxClusterPos.z;

            // Compute the cell size per dimension
            clusterCellSize = (maxClusterPos - minClusterPos);
            clusterCellSize.x /= 64.0f;
            clusterCellSize.y /= 64.0f;
            clusterCellSize.z /= 32.0f;

            // Compute the bounds of the cluster volume3
            clusterCenter = (maxClusterPos + minClusterPos) / 2.0f;
            clusterDimension = (maxClusterPos - minClusterPos);
        }

        void CullLights(CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingCullLights)))
            {
                // Make sure the culling buffer has the right size
                if (m_LightCullResult == null || m_LightCullResult.count != totalLightCount)
                {
                    ResizeCullResultBuffer(totalLightCount);
                }

                ComputeShader lightClusterCS = m_RenderPipelineRayTracingResources.lightClusterBuildCS;

                // Grab the kernel
                int lightClusterCullKernel = lightClusterCS.FindKernel(m_LightCullKernelName);

                // Inject all the parameters
                cmd.SetComputeVectorParam(lightClusterCS, _ClusterCenterPosition, clusterCenter);
                cmd.SetComputeVectorParam(lightClusterCS, _ClusterDimension, clusterDimension);
                cmd.SetComputeFloatParam(lightClusterCS, _LightVolumeCount, HDShadowUtils.Asfloat(totalLightCount));

                cmd.SetComputeBufferParam(lightClusterCS, lightClusterCullKernel, _LightVolumes, m_LightVolumeGPUArray);
                cmd.SetComputeBufferParam(lightClusterCS, lightClusterCullKernel, _RaytracingLightCullResult, m_LightCullResult);

                // Dispatch a compute
                int numLightGroups = (totalLightCount / 16 + 1);
                cmd.DispatchCompute(lightClusterCS, lightClusterCullKernel, numLightGroups, 1, 1);
            }
        }

        void BuildLightCluster(HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingBuildCluster)))
            {
                // Grab the kernel
                ComputeShader lightClusterCS = m_RenderPipelineRayTracingResources.lightClusterBuildCS;
                int lightClusterKernel = lightClusterCS.FindKernel(m_LightClusterKernelName);

                // Inject all the parameters
                cmd.SetComputeBufferParam(lightClusterCS, lightClusterKernel, HDShaderIDs._RaytracingLightClusterRW, m_LightCluster);
                cmd.SetComputeVectorParam(lightClusterCS, _ClusterCellSize, clusterCellSize);

                cmd.SetComputeBufferParam(lightClusterCS, lightClusterKernel, _LightVolumes, m_LightVolumeGPUArray);
                cmd.SetComputeFloatParam(lightClusterCS, _LightVolumeCount, HDShadowUtils.Asfloat(totalLightCount));
                cmd.SetComputeBufferParam(lightClusterCS, lightClusterKernel, _RaytracingLightCullResult, m_LightCullResult);

                // Dispatch a compute
                int numGroupsX = 8;
                int numGroupsY = 8;
                int numGroupsZ = 4;
                cmd.DispatchCompute(lightClusterCS, lightClusterKernel, numGroupsX, numGroupsY, numGroupsZ);
            }
        }

        void BuildLightData(CommandBuffer cmd, HDCamera hdCamera, HDRayTracingLights rayTracingLights, DebugDisplaySettings debugDisplaySettings)
        {
            // If no lights, exit
            if (rayTracingLights.lightCount == 0)
            {
                ResizeLightDataBuffer(1);
                return;
            }

            // Also we need to build the light list data
            if (m_LightDataGPUArray == null || m_LightDataGPUArray.count != rayTracingLights.lightCount)
            {
                ResizeLightDataBuffer(rayTracingLights.lightCount);
            }

            m_LightDataCPUArray.Clear();

            // Grab the shadow settings
            var hdShadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();
            BoolScalableSetting contactShadowScalableSetting = HDAdditionalLightData.ScalableSettings.UseContactShadow(m_RenderPipeline.asset);

            // Build the data for every light
            for (int lightIdx = 0; lightIdx < rayTracingLights.hdLightArray.Count; ++lightIdx)
            {
                // Grab the additinal light data to process
                HDAdditionalLightData additionalLightData = rayTracingLights.hdLightArray[lightIdx];

                LightData lightData = new LightData();
                // When the user deletes a light source in the editor, there is a single frame where the light is null before the collection of light in the scene is triggered
                // the workaround for this is simply to add an invalid light for that frame
                if (additionalLightData == null)
                {
                    m_LightDataCPUArray.Add(lightData);
                    continue;
                }

                // Evaluate all the light type data that we need
                LightCategory lightCategory = LightCategory.Count;
                GPULightType gpuLightType = GPULightType.Point;
                LightVolumeType lightVolumeType = LightVolumeType.Count;
                HDLightType lightType = additionalLightData.type;
                HDRenderPipeline.EvaluateGPULightType(lightType, additionalLightData.spotLightShape, additionalLightData.areaLightShape, ref lightCategory, ref gpuLightType, ref lightVolumeType);

                // Fetch the light component for this light
                additionalLightData.gameObject.TryGetComponent(out lightComponent);

                // Build the processed light data  that we need
                ProcessedLightData processedData = new ProcessedLightData();
                processedData.additionalLightData = additionalLightData;
                processedData.lightType = additionalLightData.type;
                processedData.lightCategory = lightCategory;
                processedData.gpuLightType = gpuLightType;
                processedData.lightVolumeType = lightVolumeType;
                // Both of these positions are non-camera-relative.
                processedData.distanceToCamera = (additionalLightData.gameObject.transform.position - hdCamera.camera.transform.position).magnitude;
                processedData.lightDistanceFade = HDUtils.ComputeLinearDistanceFade(processedData.distanceToCamera, additionalLightData.fadeDistance);
                processedData.volumetricDistanceFade = HDUtils.ComputeLinearDistanceFade(processedData.distanceToCamera, additionalLightData.volumetricFadeDistance);
                processedData.isBakedShadowMask = HDRenderPipeline.IsBakedShadowMaskLight(lightComponent);

                // Build a visible light
                Color finalColor = lightComponent.color.linear * lightComponent.intensity;
                if (additionalLightData.useColorTemperature)
                    finalColor *= Mathf.CorrelatedColorTemperatureToRGB(lightComponent.colorTemperature);
                visibleLight.finalColor = finalColor;
                visibleLight.range = lightComponent.range;
                // This should be done explicitely, localtoworld matrix doesn't work here
                localToWorldMatrix.SetColumn(3, lightComponent.gameObject.transform.position);
                localToWorldMatrix.SetColumn(2, lightComponent.transform.forward);
                localToWorldMatrix.SetColumn(1, lightComponent.transform.up);
                localToWorldMatrix.SetColumn(0, lightComponent.transform.right);
                visibleLight.localToWorldMatrix = localToWorldMatrix;
                visibleLight.spotAngle = lightComponent.spotAngle;

                int shadowIndex = additionalLightData.shadowIndex;
                int screenSpaceShadowIndex = -1;
                int screenSpaceChannelSlot = -1;
                Vector3 lightDimensions =  new Vector3(0.0f, 0.0f, 0.0f);

                // Use the shared code to build the light data
                m_RenderPipeline.GetLightData(cmd, hdCamera, hdShadowSettings, visibleLight, lightComponent, in processedData,
                    shadowIndex, contactShadowScalableSetting, isRasterization: false, ref lightDimensions, ref screenSpaceShadowIndex, ref screenSpaceChannelSlot, ref lightData);

                // We make the light position camera-relative as late as possible in order
                // to allow the preceding code to work with the absolute world space coordinates.
                Vector3 camPosWS = hdCamera.mainViewConstants.worldSpaceCameraPos;
                HDRenderPipeline.UpdateLightCameraRelativetData(ref lightData, camPosWS);

                // Set the data for this light
                m_LightDataCPUArray.Add(lightData);
            }

            // Push the data to the GPU
            m_LightDataGPUArray.SetData(m_LightDataCPUArray);
        }

        void BuildEnvLightData(CommandBuffer cmd, HDCamera hdCamera, HDRayTracingLights lights)
        {
            int totalReflectionProbes = lights.reflectionProbeArray.Count;
            if (totalReflectionProbes == 0)
            {
                ResizeEnvLightDataBuffer(1);
                return;
            }

            // Also we need to build the light list data
            if (m_EnvLightDataCPUArray == null || m_EnvLightDataGPUArray == null || m_EnvLightDataGPUArray.count != totalReflectionProbes)
            {
                ResizeEnvLightDataBuffer(totalReflectionProbes);
            }

            // Make sure the Cpu list is empty
            m_EnvLightDataCPUArray.Clear();
            ProcessedProbeData processedProbe = new ProcessedProbeData();

            // Build the data for every light
            for (int lightIdx = 0; lightIdx < lights.reflectionProbeArray.Count; ++lightIdx)
            {
                HDProbe probeData = lights.reflectionProbeArray[lightIdx];

                // Skip the probe if the probe has never rendered (in realtime cases) or if texture is null
                if (!probeData.HasValidRenderedData()) continue;

                HDRenderPipeline.PreprocessProbeData(ref processedProbe, probeData, hdCamera);

                var envLightData = new EnvLightData();
                m_RenderPipeline.GetEnvLightData(cmd, hdCamera, processedProbe, ref envLightData);

                // We make the light position camera-relative as late as possible in order
                // to allow the preceding code to work with the absolute world space coordinates.
                Vector3 camPosWS = hdCamera.mainViewConstants.worldSpaceCameraPos;
                HDRenderPipeline.UpdateEnvLighCameraRelativetData(ref envLightData, camPosWS);

                m_EnvLightDataCPUArray.Add(envLightData);
            }

            // Push the data to the GPU
            m_EnvLightDataGPUArray.SetData(m_EnvLightDataCPUArray);
        }

        public struct LightClusterDebugParameters
        {
            public int texWidth;
            public int texHeight;
            public int lightClusterDebugKernel;
            public Vector3 clusterCellSize;
            public Material debugMaterial;
            public ComputeBuffer lightCluster;
            public ComputeShader lightClusterDebugCS;
        }

        LightClusterDebugParameters PrepareLightClusterDebugParameters(HDCamera hdCamera)
        {
            LightClusterDebugParameters parameters = new LightClusterDebugParameters();
            parameters.texWidth = hdCamera.actualWidth;
            parameters.texHeight = hdCamera.actualHeight;
            parameters.clusterCellSize = clusterCellSize;
            parameters.lightCluster = m_LightCluster;
            parameters.lightClusterDebugCS = m_RenderPipelineRayTracingResources.lightClusterDebugCS;
            parameters.lightClusterDebugKernel = parameters.lightClusterDebugCS.FindKernel("DebugLightCluster");
            parameters.debugMaterial = m_DebugMaterial;
            return parameters;
        }

        public struct LightClusterDebugResources
        {
            public RTHandle depthStencilBuffer;
            public RTHandle depthTexture;
            public RTHandle debugLightClusterTexture;
        }

        static public void ExecuteLightClusterDebug(CommandBuffer cmd, LightClusterDebugParameters parameters, LightClusterDebugResources resources, MaterialPropertyBlock debugMaterialProperties)
        {
            // Bind the output texture
            CoreUtils.SetRenderTarget(cmd, resources.debugLightClusterTexture, resources.depthStencilBuffer, clearFlag: ClearFlag.Color, clearColor: Color.black);

            // Inject all the parameters to the debug compute
            cmd.SetComputeBufferParam(parameters.lightClusterDebugCS, parameters.lightClusterDebugKernel, HDShaderIDs._RaytracingLightCluster, parameters.lightCluster);
            cmd.SetComputeVectorParam(parameters.lightClusterDebugCS, _ClusterCellSize, parameters.clusterCellSize);
            cmd.SetComputeTextureParam(parameters.lightClusterDebugCS, parameters.lightClusterDebugKernel, HDShaderIDs._CameraDepthTexture, resources.depthStencilBuffer);

            // Target output texture
            cmd.SetComputeTextureParam(parameters.lightClusterDebugCS, parameters.lightClusterDebugKernel, _DebutLightClusterTexture, resources.debugLightClusterTexture);

            // Dispatch the compute
            int lightVolumesTileSize = 8;
            int numTilesX = (parameters.texWidth + (lightVolumesTileSize - 1)) / lightVolumesTileSize;
            int numTilesY = (parameters.texHeight + (lightVolumesTileSize - 1)) / lightVolumesTileSize;

            cmd.DispatchCompute(parameters.lightClusterDebugCS, parameters.lightClusterDebugKernel, numTilesX, numTilesY, 1);

            // Bind the parameters
            debugMaterialProperties.SetBuffer(HDShaderIDs._RaytracingLightCluster, parameters.lightCluster);
            debugMaterialProperties.SetVector(_ClusterCellSize, parameters.clusterCellSize);
            debugMaterialProperties.SetTexture(HDShaderIDs._CameraDepthTexture, resources.depthTexture);

            // Draw the faces
            cmd.DrawProcedural(Matrix4x4.identity, parameters.debugMaterial, 1, MeshTopology.Lines, 48, 64 * 64 * 32, debugMaterialProperties);
            cmd.DrawProcedural(Matrix4x4.identity, parameters.debugMaterial, 0, MeshTopology.Triangles, 36, 64 * 64 * 32, debugMaterialProperties);
        }

        class LightClusterDebugPassData
        {
            public LightClusterDebugParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle outputBuffer;
        }

        public void EvaluateClusterDebugView(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, TextureHandle depthPyramid)
        {
            TextureHandle debugTexture;
            using (var builder = renderGraph.AddRenderPass<LightClusterDebugPassData>("Debug Texture for the Light Cluster", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDebugCluster)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = PrepareLightClusterDebugParameters(hdCamera);
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.Read);
                passData.depthPyramid = builder.ReadTexture(depthStencilBuffer);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Light Cluster Debug Texture" }));

                builder.SetRenderFunc(
                    (LightClusterDebugPassData data, RenderGraphContext ctx) =>
                    {
                        // We need to fill the structure that holds the various resources
                        LightClusterDebugResources resources = new LightClusterDebugResources();
                        resources.depthStencilBuffer = data.depthStencilBuffer;
                        resources.depthTexture = data.depthPyramid;
                        resources.debugLightClusterTexture = data.outputBuffer;
                        ExecuteLightClusterDebug(ctx.cmd, data.parameters, resources, ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                    });

                debugTexture = passData.outputBuffer;
            }

            m_RenderPipeline.PushFullScreenDebugTexture(renderGraph, debugTexture, FullScreenDebugMode.LightCluster);
        }

        public ComputeBuffer GetCluster()
        {
            return m_LightCluster;
        }

        public ComputeBuffer GetLightDatas()
        {
            return m_LightDataGPUArray;
        }

        public ComputeBuffer GetEnvLightDatas()
        {
            return m_EnvLightDataGPUArray;
        }

        public Vector3 GetMinClusterPos()
        {
            return minClusterPos;
        }

        public Vector3 GetMaxClusterPos()
        {
            return maxClusterPos;
        }

        public Vector3 GetClusterCellSize()
        {
            return clusterCellSize;
        }

        public int GetPunctualLightCount()
        {
            return punctualLightCount;
        }

        public int GetAreaLightCount()
        {
            return areaLightCount;
        }

        public int GetEnvLightCount()
        {
            return envLightCount;
        }

        public int GetLightPerCellCount()
        {
            return m_NumLightsPerCell;
        }

        void InvalidateCluster()
        {
            // Invalidate the cluster's bounds so that we never access the buffer (the buffer's access in hlsl is surrounded by position testing)
            minClusterPos.Set(float.MaxValue, float.MaxValue, float.MaxValue);
            maxClusterPos.Set(-float.MaxValue, -float.MaxValue, -float.MaxValue);
            punctualLightCount = 0;
            areaLightCount = 0;
        }

        public void CullForRayTracing(HDCamera hdCamera, HDRayTracingLights rayTracingLights)
        {
            // If there is no lights to process or no environment not the shader is missing
            if (rayTracingLights.lightCount == 0 || !m_RenderPipeline.GetRayTracingState())
            {
                InvalidateCluster();
                return;
            }

            // Build the Light volumes
            BuildGPULightVolumes(hdCamera, rayTracingLights);

            // If no valid light were found, invalidate the cluster and leave
            if (totalLightCount == 0)
            {
                InvalidateCluster();
                return;
            }

            // Evaluate the volume of the cluster
            EvaluateClusterVolume(hdCamera);
        }

        public void BuildLightClusterBuffer(CommandBuffer cmd, HDCamera hdCamera, HDRayTracingLights rayTracingLights)
        {
            // If there is no lights to process or no environment not the shader is missing
            if (totalLightCount == 0 || rayTracingLights.lightCount == 0 || !m_RenderPipeline.GetRayTracingState())
                return;

            // Cull the lights within the evaluated cluster range
            CullLights(cmd);

            // Build the light Cluster
            BuildLightCluster(hdCamera, cmd);
        }

        public void ReserveCookieAtlasSlots(HDRayTracingLights rayTracingLights)
        {
            for (int lightIdx = 0; lightIdx < rayTracingLights.hdLightArray.Count; ++lightIdx)
            {
                // Grab the additional light data to process
                HDAdditionalLightData additionalLightData = rayTracingLights.hdLightArray[lightIdx];

                // Fetch the light component for this light
                additionalLightData.gameObject.TryGetComponent(out lightComponent);

                // Reserve the cookie resolution in the 2D atlas
                m_RenderPipeline.ReserveCookieAtlasTexture(additionalLightData, lightComponent, additionalLightData.type);
            }
        }

        public void BuildRayTracingLightData(CommandBuffer cmd, HDCamera hdCamera, HDRayTracingLights rayTracingLights, DebugDisplaySettings debugDisplaySettings)
        {
            // Build the light data
            BuildLightData(cmd, hdCamera, rayTracingLights, debugDisplaySettings);

            // Build the light data
            BuildEnvLightData(cmd, hdCamera, rayTracingLights);
        }

        public void BindLightClusterData(CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, GetCluster());
            cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, GetLightDatas());
            cmd.SetGlobalBuffer(HDShaderIDs._EnvLightDatasRT, GetEnvLightDatas());
        }
    }
}
