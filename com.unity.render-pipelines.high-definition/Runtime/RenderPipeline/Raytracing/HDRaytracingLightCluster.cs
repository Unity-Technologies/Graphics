using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

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

    internal class HDRaytracingLightCluster
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

        RTHandle m_DebugLightClusterTexture = null;

        // Light cluster debug material
        Material m_DebugMaterial = null;
        MaterialPropertyBlock m_DebugMaterialProperties = new MaterialPropertyBlock();

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
        Vector3 minClusterPos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 maxClusterPos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 clusterCellSize = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 clusterCenter = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 clusterDimension = new Vector3(0.0f, 0.0f, 0.0f);
        int punctualLightCount = 0;
        int areaLightCount = 0;
        int envLightCount = 0;
        int totalLightCount = 0;
        int numLightsPerCell = 0;
        Bounds bounds = new Bounds();
        Vector3 minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxBounds = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

        public HDRaytracingLightCluster()
        {

        }

        public void Initialize(HDRenderPipeline renderPipeline)
        {
            // Keep track of the external buffers
            m_RenderPipelineResources = renderPipeline.asset.renderPipelineResources;
            m_RenderPipelineRayTracingResources = renderPipeline.asset.renderPipelineRayTracingResources;

            // Keep track of the render pipeline
            m_RenderPipeline = renderPipeline;

            // Texture used to output debug information
            m_DebugLightClusterTexture = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "DebugLightClusterTexture");

            // Pre allocate the cluster with a dummy size
            m_LightCluster = new ComputeBuffer(1, sizeof(uint));
            m_LightDataGPUArray = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
            m_EnvLightDataGPUArray = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));

            // Create the material required for debug
            m_DebugMaterial = CoreUtils.CreateEngineMaterial(m_RenderPipelineRayTracingResources.lightClusterDebugS);
        }

        public void ReleaseResources()
        {
            m_DebugLightClusterTexture.Release();

            if (m_LightVolumeGPUArray != null)
            {
                CoreUtils.SafeRelease(m_LightVolumeGPUArray);
                m_LightVolumeGPUArray = null;
            }

            if (m_LightCluster != null)
            {
                CoreUtils.SafeRelease(m_LightCluster);
                m_LightCluster = null;
            }

            if (m_LightCullResult != null)
            {
                CoreUtils.SafeRelease(m_LightCullResult);
                m_LightCullResult = null;
            }

            if (m_LightDataGPUArray != null)
            {
                CoreUtils.SafeRelease(m_LightDataGPUArray);
                m_LightDataGPUArray = null;
            }

            if (m_EnvLightDataGPUArray != null)
            {
                CoreUtils.SafeRelease(m_EnvLightDataGPUArray);
                m_EnvLightDataGPUArray = null;
            }

            if (m_DebugMaterial != null)
            {
                CoreUtils.Destroy(m_DebugMaterial);
                m_DebugMaterial = null;
            }
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

        void BuildGPULightVolumes(HDRayTracingLights rayTracingLights)
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
                    if (light == null || !light.enabled) continue;

                    // Reserve space in the cookie atlas
                    m_RenderPipeline.ReserveCookieAtlasTexture(currentLight, light);

               
                    // Grab the light range
                    float lightRange = light.range;
                    
                    if (currentLight.type != HDLightType.Area)
                    {
                        m_LightVolumesCPUArray[realIndex].range = new Vector3(lightRange, lightRange, lightRange);
                        m_LightVolumesCPUArray[realIndex].position = currentLight.gameObject.transform.position;
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
                        Vector3 oobbCenter = currentLight.gameObject.transform.position + extents.z * currentLight.gameObject.transform.forward;

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
                    if(currentEnvLight.influenceVolume.shape == InfluenceShape.Sphere)
                    {
                        m_LightVolumesCPUArray[lightIdx + indexOffset].shape = 0;
                        m_LightVolumesCPUArray[lightIdx + indexOffset].range = new Vector3(currentEnvLight.influenceVolume.sphereRadius, currentEnvLight.influenceVolume.sphereRadius, currentEnvLight.influenceVolume.sphereRadius);
                        m_LightVolumesCPUArray[lightIdx + indexOffset].position = currentEnvLight.influenceToWorld.GetColumn(3);
                    }
                    else
                    {
                        m_LightVolumesCPUArray[lightIdx + indexOffset].shape = 1;
                        m_LightVolumesCPUArray[lightIdx + indexOffset].range = new Vector3(currentEnvLight.influenceVolume.boxSize.x / 2.0f, currentEnvLight.influenceVolume.boxSize.y / 2.0f, currentEnvLight.influenceVolume.boxSize.z / 2.0f);
                        m_LightVolumesCPUArray[lightIdx + indexOffset].position = currentEnvLight.influenceToWorld.GetColumn(3);
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
                var lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();
                numLightsPerCell = lightClusterSettings.maxNumLightsPercell.value;

                // Make sure the Cluster buffer has the right size
                int bufferSize = 64 * 64 * 32 * (numLightsPerCell + 4);
                if (m_LightCluster.count != bufferSize)
                {
                    ResizeClusterBuffer(bufferSize);
                }

                // Grab the kernel
                ComputeShader lightClusterCS = m_RenderPipelineRayTracingResources.lightClusterBuildCS;
                int lightClusterKernel = lightClusterCS.FindKernel(m_LightClusterKernelName);

                // Inject all the parameters
                cmd.SetComputeBufferParam(lightClusterCS, lightClusterKernel, HDShaderIDs._RaytracingLightCluster, m_LightCluster);
                cmd.SetComputeVectorParam(lightClusterCS, HDShaderIDs._MinClusterPos, minClusterPos);
                cmd.SetComputeVectorParam(lightClusterCS, HDShaderIDs._MaxClusterPos, maxClusterPos);
                cmd.SetComputeVectorParam(lightClusterCS, _ClusterCellSize, clusterCellSize);
                cmd.SetComputeFloatParam(lightClusterCS, HDShaderIDs._LightPerCellCount, HDShadowUtils.Asfloat(numLightsPerCell));

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

        void BuildLightData(CommandBuffer cmd, HDCamera hdCamera, HDRayTracingLights rayTracingLights)
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

            // Build the data for every light
            for (int lightIdx = 0; lightIdx < rayTracingLights.hdLightArray.Count; ++lightIdx)
            {
                var lightData = new LightData();

                HDAdditionalLightData additionalLightData = rayTracingLights.hdLightArray[lightIdx];
                // When the user deletes a light source in the editor, there is a single frame where the light is null before the collection of light in the scene is triggered
                // the workaround for this is simply to add an invalid light for that frame
                if(additionalLightData == null)
                {
                    m_LightDataCPUArray.Add(lightData);
                    continue;
                }
                Light light = additionalLightData.gameObject.GetComponent<Light>();

                // Both of these positions are non-camera-relative.
                float distanceToCamera = (light.gameObject.transform.position - hdCamera.camera.transform.position).magnitude;
                float lightDistanceFade = HDUtils.ComputeLinearDistanceFade(distanceToCamera, additionalLightData.fadeDistance);

                bool contributesToLighting = ((additionalLightData.lightDimmer > 0) && (additionalLightData.affectDiffuse || additionalLightData.affectSpecular)) || (additionalLightData.volumetricDimmer > 0);
                contributesToLighting = contributesToLighting && (lightDistanceFade > 0);

                if (!contributesToLighting)
                    continue;

                lightData.lightLayers = additionalLightData.GetLightLayers();
                LightCategory lightCategory = LightCategory.Count;
                GPULightType gpuLightType = GPULightType.Point;
                LightVolumeType lightVolumeType = LightVolumeType.Count;
                HDLightType lightType = additionalLightData.type;
                HDRenderPipeline.EvaluateGPULightType(lightType, additionalLightData.spotLightShape, additionalLightData.areaLightShape, ref lightCategory, ref gpuLightType, ref lightVolumeType);

                lightData.lightType = gpuLightType;

                lightData.positionRWS = light.gameObject.transform.position;

                bool applyRangeAttenuation = additionalLightData.applyRangeAttenuation && (gpuLightType != GPULightType.ProjectorBox);

                lightData.range = light.range;

                if (applyRangeAttenuation)
                {
                    lightData.rangeAttenuationScale = 1.0f / (light.range * light.range);
                    lightData.rangeAttenuationBias = 1.0f;

                    if (lightData.lightType == GPULightType.Rectangle)
                    {
                        // Rect lights are currently a special case because they use the normalized
                        // [0, 1] attenuation range rather than the regular [0, r] one.
                        lightData.rangeAttenuationScale = 1.0f;
                    }
                }
                else // Don't apply any attenuation but do a 'step' at range
                {
                    // Solve f(x) = b - (a * x)^2 where x = (d/r)^2.
                    // f(0) = huge -> b = huge.
                    // f(1) = 0    -> huge - a^2 = 0 -> a = sqrt(huge).
                    const float hugeValue = 16777216.0f;
                    const float sqrtHuge = 4096.0f;
                    lightData.rangeAttenuationScale = sqrtHuge / (light.range * light.range);
                    lightData.rangeAttenuationBias = hugeValue;

                    if (lightData.lightType == GPULightType.Rectangle)
                    {
                        // Rect lights are currently a special case because they use the normalized
                        // [0, 1] attenuation range rather than the regular [0, r] one.
                        lightData.rangeAttenuationScale = sqrtHuge;
                    }
                }

                Color value = light.color.linear * light.intensity;
                if (additionalLightData.useColorTemperature)
                    value *= Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature);
                lightData.color = new Vector3(value.r, value.g, value.b);

                lightData.forward = light.transform.forward;
                lightData.up = light.transform.up;
                lightData.right = light.transform.right;

                lightData.boxLightSafeExtent = 1.0f;
                if (lightData.lightType == GPULightType.ProjectorBox)
                {
                    // Rescale for cookies and windowing.
                    lightData.right *= 2.0f / Mathf.Max(additionalLightData.shapeWidth, 0.001f);
                    lightData.up *= 2.0f / Mathf.Max(additionalLightData.shapeHeight, 0.001f);
                }
                else if (lightData.lightType == GPULightType.ProjectorPyramid)
                {
                    // Get width and height for the current frustum
                    var spotAngle = light.spotAngle;

                    float frustumWidth, frustumHeight;

                    if (additionalLightData.aspectRatio >= 1.0f)
                    {
                        frustumHeight = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                        frustumWidth = frustumHeight * additionalLightData.aspectRatio;
                    }
                    else
                    {
                        frustumWidth = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                        frustumHeight = frustumWidth / additionalLightData.aspectRatio;
                    }

                    // Rescale for cookies and windowing.
                    lightData.right *= 2.0f / frustumWidth;
                    lightData.up *= 2.0f / frustumHeight;
                }

                if (lightData.lightType == GPULightType.Spot)
                {
                    var spotAngle = light.spotAngle;

                    var innerConePercent = additionalLightData.innerSpotPercent01;
                    var cosSpotOuterHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                    var sinSpotOuterHalfAngle = Mathf.Sqrt(1.0f - cosSpotOuterHalfAngle * cosSpotOuterHalfAngle);
                    var cosSpotInnerHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * innerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f); // inner cone

                    var val = Mathf.Max(0.0001f, (cosSpotInnerHalfAngle - cosSpotOuterHalfAngle));
                    lightData.angleScale = 1.0f / val;
                    lightData.angleOffset = -cosSpotOuterHalfAngle * lightData.angleScale;

                    // Rescale for cookies and windowing.
                    float cotOuterHalfAngle = cosSpotOuterHalfAngle / sinSpotOuterHalfAngle;
                    lightData.up *= cotOuterHalfAngle;
                    lightData.right *= cotOuterHalfAngle;
                }
                else
                {
                    // These are the neutral values allowing GetAngleAnttenuation in shader code to return 1.0
                    lightData.angleScale = 0.0f;
                    lightData.angleOffset = 1.0f;
                }

                if (lightData.lightType != GPULightType.Directional && lightData.lightType != GPULightType.ProjectorBox)
                {
                    // Store the squared radius of the light to simulate a fill light.
                    lightData.size = new Vector2(additionalLightData.shapeRadius * additionalLightData.shapeRadius, 0);
                }

                if (lightData.lightType == GPULightType.Rectangle || lightData.lightType == GPULightType.Tube)
                {
                    lightData.size = new Vector2(additionalLightData.shapeWidth, additionalLightData.shapeHeight);
                }

                lightData.lightDimmer = lightDistanceFade * (additionalLightData.lightDimmer);
                lightData.diffuseDimmer = lightDistanceFade * (additionalLightData.affectDiffuse ? additionalLightData.lightDimmer : 0);
                lightData.specularDimmer = lightDistanceFade * (additionalLightData.affectSpecular ? additionalLightData.lightDimmer * hdCamera.frameSettings.specularGlobalDimmer : 0);
                lightData.volumetricLightDimmer = lightDistanceFade * (additionalLightData.volumetricDimmer);

                lightData.cookieMode = CookieMode.None;
                lightData.contactShadowMask = 0;
                lightData.cookieIndex = -1;
                lightData.shadowIndex = -1;
                lightData.screenSpaceShadowIndex = -1;


                if (light != null && light.cookie != null)
                {
                    // TODO: add texture atlas support for cookie textures.
                    switch (lightType)
                    {
                        case HDLightType.Spot:
                            lightData.cookieMode = (additionalLightData.legacyLight.cookie.wrapMode == TextureWrapMode.Repeat) ? CookieMode.Repeat : CookieMode.Clamp;
                            lightData.cookieScaleOffset = m_RenderPipeline.m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, light.cookie);
                            break;
                        case HDLightType.Point:
                            lightData.cookieMode = CookieMode.Clamp;
                            lightData.cookieIndex = m_RenderPipeline.m_TextureCaches.lightCookieManager.FetchCubeCookie(cmd, light.cookie);
                            break;
                    }
                }
                else if (lightType == HDLightType.Spot && additionalLightData.spotLightShape != SpotLightShape.Cone)
                {
                    // Projectors lights must always have a cookie texture.
                    // As long as the cache is a texture array and not an atlas, the 4x4 white texture will be rescaled to 128
                    lightData.cookieMode = CookieMode.Clamp;
                    lightData.cookieScaleOffset = m_RenderPipeline.m_TextureCaches.lightCookieManager.Fetch2DCookie(cmd, Texture2D.whiteTexture);
                }
                else if (lightData.lightType == GPULightType.Rectangle && additionalLightData.areaLightCookie != null)
                {
                    lightData.cookieMode = CookieMode.Clamp;
                    lightData.cookieScaleOffset = m_RenderPipeline.m_TextureCaches.lightCookieManager.FetchAreaCookie(cmd, additionalLightData.areaLightCookie);
                }

                {
                    lightData.shadowDimmer = 1.0f;
                    lightData.volumetricShadowDimmer = 1.0f;
                }

                {
                    // fix up shadow information
                    lightData.shadowIndex = additionalLightData.shadowIndex;
                }

                // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
                lightData.minRoughness = (1.0f - additionalLightData.maxSmoothness) * (1.0f - additionalLightData.maxSmoothness);

                // No usage for the shadow masks
                lightData.shadowMaskSelector = Vector4.zero;
                {
                    // use -1 to say that we don't use shadow mask
                    lightData.shadowMaskSelector.x = -1.0f;
                    lightData.nonLightMappedOnly = 0;
                }

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    // Caution: 'LightData.positionWS' is camera-relative after this point.
                    Vector3 camPosWS = hdCamera.mainViewConstants.worldSpaceCameraPos;
                    lightData.positionRWS -= camPosWS;
                }

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
                m_RenderPipeline.UpdateEnvLighCameraRelativetData(ref envLightData, camPosWS);

                m_EnvLightDataCPUArray.Add(envLightData);
            }

            // Push the data to the GPU
            m_EnvLightDataGPUArray.SetData(m_EnvLightDataCPUArray);
        }

        public void EvaluateClusterDebugView(CommandBuffer cmd, HDCamera hdCamera)
        {
            ComputeShader lightClusterDebugCS = m_RenderPipelineRayTracingResources.lightClusterDebugCS;
            if (lightClusterDebugCS == null) return;

            // Bind the output texture
            CoreUtils.SetRenderTarget(cmd, m_DebugLightClusterTexture, m_RenderPipeline.sharedRTManager.GetDepthStencilBuffer(), clearFlag: ClearFlag.Color, clearColor: Color.black);

            // Grab the kernel
            int m_LightClusterDebugKernel = lightClusterDebugCS.FindKernel("DebugLightCluster");

            // Inject all the parameters to the debug compute
            cmd.SetComputeBufferParam(lightClusterDebugCS, m_LightClusterDebugKernel, HDShaderIDs._RaytracingLightCluster, m_LightCluster);
            cmd.SetComputeVectorParam(lightClusterDebugCS, HDShaderIDs._MinClusterPos, minClusterPos);
            cmd.SetComputeVectorParam(lightClusterDebugCS, HDShaderIDs._MaxClusterPos, maxClusterPos);
            cmd.SetComputeVectorParam(lightClusterDebugCS, _ClusterCellSize, clusterCellSize);
            cmd.SetComputeIntParam(lightClusterDebugCS, HDShaderIDs._LightPerCellCount, numLightsPerCell);
            cmd.SetComputeTextureParam(lightClusterDebugCS, m_LightClusterDebugKernel, HDShaderIDs._CameraDepthTexture, m_RenderPipeline.sharedRTManager.GetDepthStencilBuffer());

            // Target output texture
            cmd.SetComputeTextureParam(lightClusterDebugCS, m_LightClusterDebugKernel, _DebutLightClusterTexture, m_DebugLightClusterTexture);

            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Dispatch the compute
            int lightVolumesTileSize = 8;
            int numTilesX = (texWidth + (lightVolumesTileSize - 1)) / lightVolumesTileSize;
            int numTilesY = (texHeight + (lightVolumesTileSize - 1)) / lightVolumesTileSize;

            cmd.DispatchCompute(lightClusterDebugCS, m_LightClusterDebugKernel, numTilesX, numTilesY, 1);

            // Bind the parameters
            m_DebugMaterialProperties.SetBuffer(HDShaderIDs._RaytracingLightCluster, m_LightCluster);
            m_DebugMaterialProperties.SetVector(HDShaderIDs._MinClusterPos, minClusterPos);
            m_DebugMaterialProperties.SetVector(HDShaderIDs._MaxClusterPos, maxClusterPos);
            m_DebugMaterialProperties.SetVector(_ClusterCellSize, clusterCellSize);
            m_DebugMaterialProperties.SetInt(HDShaderIDs._LightPerCellCount, numLightsPerCell);
            m_DebugMaterialProperties.SetTexture(HDShaderIDs._CameraDepthTexture, m_RenderPipeline.sharedRTManager.GetDepthTexture());

            // Draw the faces
            cmd.DrawProcedural(Matrix4x4.identity, m_DebugMaterial, 1, MeshTopology.Lines, 48, 64 * 64 * 32, m_DebugMaterialProperties);
            cmd.DrawProcedural(Matrix4x4.identity, m_DebugMaterial, 0, MeshTopology.Triangles, 36, 64 * 64 * 32, m_DebugMaterialProperties);

            // Bind the result
            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_DebugLightClusterTexture, FullScreenDebugMode.LightCluster);
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

        void InvalidateCluster()
        {
            // Invalidate the cluster's bounds so that we never access the buffer
            minClusterPos.Set(float.MaxValue, float.MaxValue, float.MaxValue);
            maxClusterPos.Set(-float.MaxValue, -float.MaxValue, -float.MaxValue);
            punctualLightCount = 0;
            areaLightCount = 0;

            // Make sure the buffer is at least of size 1
            if (m_LightCluster.count != 1)
            {
                ResizeClusterBuffer(1);
            }
            return;
        }

        public void EvaluateLightClusters(CommandBuffer cmd, HDCamera hdCamera, HDRayTracingLights rayTracingLights)
        {
            // If there is no lights to process or no environment not the shader is missing
            if (rayTracingLights.lightCount == 0 || !m_RenderPipeline.GetRayTracingState())
            {
                InvalidateCluster();
                return;
            }

            // Build the Light volumes
            BuildGPULightVolumes(rayTracingLights);

            // If no valid light were found, invalidate the cluster and leave
            if (totalLightCount == 0)
            {
                InvalidateCluster();
                return;
            }

            // Evaluate the volume of the cluster
            EvaluateClusterVolume(hdCamera);

            // Cull the lights within the evaluated cluster range
            CullLights(cmd);

            // Build the light Cluster
            BuildLightCluster(hdCamera, cmd);

            // Build the light data
            BuildLightData(cmd, hdCamera, rayTracingLights);

            // Build the light data
            BuildEnvLightData(cmd, hdCamera, rayTracingLights);
        }

        public void BindLightClusterData(CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, GetCluster());
            cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, GetLightDatas());
            cmd.SetGlobalBuffer(HDShaderIDs._EnvLightDatasRT, GetEnvLightDatas());
            cmd.SetGlobalVector(HDShaderIDs._MinClusterPos, GetMinClusterPos());
            cmd.SetGlobalVector(HDShaderIDs._MaxClusterPos, GetMaxClusterPos());
            cmd.SetGlobalInt(HDShaderIDs._LightPerCellCount, numLightsPerCell);
            cmd.SetGlobalInt(HDShaderIDs._PunctualLightCountRT, GetPunctualLightCount());
            cmd.SetGlobalInt(HDShaderIDs._AreaLightCountRT, GetAreaLightCount());
            cmd.SetGlobalInt(HDShaderIDs._EnvLightCountRT, GetEnvLightCount());
        }
    }
}
