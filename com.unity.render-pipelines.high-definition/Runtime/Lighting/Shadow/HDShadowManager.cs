using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum ShadowMapType
    {
        CascadedDirectional,
        PunctualAtlas,
        AreaLightAtlas
    }

    [GenerateHLSL]
    public struct HDShadowData
    {
        public Vector3      rot0;
        public Vector3      rot1;
        public Vector3      rot2;
        public Vector3      pos;
        public Vector4      proj;

        public Vector2      atlasOffset;
        public float        edgeTolerance;
        public int          flags;

        public Vector4      zBufferParam;
        public Vector4      shadowMapSize;

        public Vector4      viewBias;
        public Vector3      normalBias;
        public float        _padding;

        public Vector4      shadowFilterParams0;

        public Matrix4x4    shadowToWorld;
    }

    // We use a different structure for directional light because these is a lot of data there
    // and it will add too much useless stuff for other lights
    // Note: In order to support HLSL array generation, we need to use fixed arrays and so a unsafe context for this struct
    [GenerateHLSL]
    public unsafe struct HDDirectionalShadowData
    {
        // We can't use Vector4 here because the vector4[] makes this struct non blittable
        [HLSLArray(4, typeof(Vector4))]
        public fixed float      sphereCascades[4 * 4];

        public Vector4          cascadeDirection;

        [HLSLArray(4, typeof(float))]
        public fixed float      cascadeBorders[4];
    }

    [GenerateHLSL]
    public enum HDShadowFlag
    {
        SampleBiasScale     = (1 << 0),
        EdgeLeakFixup       = (1 << 1),
        EdgeToleranceNormal = (1 << 2),
    }

    public class HDShadowRequest
    {
        public Matrix4x4            view;
        // Use the y flipped device projection matrix as light projection matrix
        public Matrix4x4            deviceProjectionYFlip;
        public Matrix4x4            deviceProjection;
        public Matrix4x4            shadowToWorld;
        public Vector3              position;
        public Vector4              zBufferParam;
        // Warning: this field is updated by ProcessShadowRequests and is invalid before
        public Rect                 atlasViewport;
        public bool                 zClip;
        public Vector4[]            frustumPlanes;

        // Store the final shadow indice in the shadow data array
        // Warning: the index is computed during ProcessShadowRequest and so is invalid before calling this function
        public int                  shadowIndex;
        public int                  lightType;

        // Determine in which atlas the shadow will be rendered
        public ShadowMapType        shadowMapType = ShadowMapType.PunctualAtlas;

        // TODO: Remove these field once scriptable culling is here (currently required by ScriptableRenderContext.DrawShadows)
        public int                  lightIndex;
        public ShadowSplitData      splitData;
        // end

        public Vector4              viewBias;
        public Vector3              normalBias;
        public float                edgeTolerance;
        public int                  flags;

        // PCSS parameters
        public float                shadowSoftness;
        public int                  blockerSampleCount;
        public int                  filterSampleCount;
        public float                minFilterSize;

        // IMS parameters
        public float                kernelSize;
        public float                lightAngle;
        public float                maxDepthBias;

        public Vector4              evsmParams;
    }

    public enum HDShadowQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
        VeryHigh = 3,
    }

    public enum DirectionalShadowAlgorithm
    {
        PCF5x5,
        PCF7x7,
        PCSS,
        IMS
    }

    [Serializable]
    public struct HDShadowInitParameters
    {
        [Serializable]
        public struct HDShadowAtlasInitParams
        {
            public int shadowAtlasResolution;
            public DepthBits shadowAtlasDepthBits;
            public bool useDynamicViewportRescale;

            public static HDShadowAtlasInitParams GetDefault()
            {
                return new HDShadowAtlasInitParams()
                {
                    shadowAtlasResolution = k_DefaultShadowAtlasResolution,
                    shadowAtlasDepthBits = k_DefaultShadowMapDepthBits,
                    useDynamicViewportRescale = true
                };
            }
        }

        /// <summary>Default HDShadowInitParameters</summary>
        public static readonly HDShadowInitParameters @default = new HDShadowInitParameters()
        {
            maxShadowRequests           = k_DefaultMaxShadowRequests,
            directionalShadowsDepthBits = k_DefaultShadowMapDepthBits,
            punctualLightShadowAtlas    = HDShadowAtlasInitParams.GetDefault(),
            areaLightShadowAtlas        = HDShadowAtlasInitParams.GetDefault(),
			shadowQuality               = HDShadowQuality.Low,
            supportScreenSpaceShadows   = false,
            maxScreenSpaceShadows       = 2,
        };

        public const int k_DefaultShadowAtlasResolution = 4096;
        public const int k_DefaultMaxShadowRequests = 128;
        public const DepthBits k_DefaultShadowMapDepthBits = DepthBits.Depth32;

        public int maxShadowRequests;
        public DepthBits directionalShadowsDepthBits;

        public HDShadowQuality shadowQuality;

        public HDShadowAtlasInitParams punctualLightShadowAtlas;
        public HDShadowAtlasInitParams areaLightShadowAtlas;

        // Screen space shadow data
        public bool supportScreenSpaceShadows;
        public int maxScreenSpaceShadows;
    }

    public class HDShadowResolutionRequest
    {
        public Rect             atlasViewport;
        public Vector2          resolution;
        public ShadowMapType    shadowMapType;
    }

    public partial class HDShadowManager : IDisposable
    {
        public const int            k_DirectionalShadowCascadeCount = 4;
        public const int            k_MinShadowMapResolution = 16;

        List<HDShadowData>          m_ShadowDatas = new List<HDShadowData>();
        HDShadowRequest[]           m_ShadowRequests;
        List<HDShadowResolutionRequest> m_ShadowResolutionRequests = new List<HDShadowResolutionRequest>();

        HDDirectionalShadowData     m_DirectionalShadowData;

        // Structured buffer of shadow datas
        ComputeBuffer               m_ShadowDataBuffer;
        ComputeBuffer               m_DirectionalShadowDataBuffer;

        // The two shadowmaps atlases we uses, one for directional cascade (without resize) and the second for the rest of the shadows
        HDShadowAtlas               m_CascadeAtlas;
        HDShadowAtlas               m_Atlas;
        HDShadowAtlas               m_AreaLightShadowAtlas;

        int                         m_MaxShadowRequests;
        int                         m_ShadowRequestCount;
        int                         m_CascadeCount;

        public HDShadowManager(RenderPipelineResources renderPipelineResources, DepthBits directionalShadowDepthBits,
                            HDShadowInitParameters.HDShadowAtlasInitParams punctualLightAtlasInfo, HDShadowInitParameters.HDShadowAtlasInitParams areaLightAtlasInfo, int maxShadowRequests, Shader clearShader)
        {
            Material clearMaterial = CoreUtils.CreateEngineMaterial(clearShader);

            // Prevent the list from resizing their internal container when we add shadow requests
            m_ShadowDatas.Capacity = maxShadowRequests;
            m_ShadowResolutionRequests.Capacity = maxShadowRequests;
            m_ShadowRequests = new HDShadowRequest[maxShadowRequests];

            // The cascade atlas will be allocated only if there is a directional light
            m_Atlas = new HDShadowAtlas(renderPipelineResources, punctualLightAtlasInfo.shadowAtlasResolution, punctualLightAtlasInfo.shadowAtlasResolution, HDShaderIDs._ShadowmapAtlas, HDShaderIDs._ShadowAtlasSize, clearMaterial, depthBufferBits: punctualLightAtlasInfo.shadowAtlasDepthBits, name: "Shadow Map Atlas");
            // Cascade atlas render texture will only be allocated if there is a shadow casting directional light
            HDShadowAtlas.BlurAlgorithm cascadeBlur = GetDirectionalShadowAlgorithm() == DirectionalShadowAlgorithm.IMS ? HDShadowAtlas.BlurAlgorithm.IM : HDShadowAtlas.BlurAlgorithm.None;
            m_CascadeAtlas = new HDShadowAtlas(renderPipelineResources, 1, 1, HDShaderIDs._ShadowmapCascadeAtlas, HDShaderIDs._CascadeShadowAtlasSize, clearMaterial, cascadeBlur, depthBufferBits: directionalShadowDepthBits, name: "Cascade Shadow Map Atlas");

            m_AreaLightShadowAtlas = new HDShadowAtlas(renderPipelineResources, areaLightAtlasInfo.shadowAtlasResolution, areaLightAtlasInfo.shadowAtlasResolution, HDShaderIDs._AreaLightShadowmapAtlas, HDShaderIDs._AreaShadowAtlasSize, clearMaterial, HDShadowAtlas.BlurAlgorithm.EVSM, depthBufferBits: areaLightAtlasInfo.shadowAtlasDepthBits, name: "Area Light Shadow Map Atlas", momentAtlasShaderID: HDShaderIDs._AreaShadowmapMomentAtlas);

            m_ShadowDataBuffer = new ComputeBuffer(maxShadowRequests, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDShadowData)));
            m_DirectionalShadowDataBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDDirectionalShadowData)));

            m_MaxShadowRequests = maxShadowRequests;
        }

        public static DirectionalShadowAlgorithm GetDirectionalShadowAlgorithm()
        {
            var hdAsset = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            switch (hdAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowQuality)
            {
                case HDShadowQuality.Low:
                {
                    return DirectionalShadowAlgorithm.PCF5x5;
                }
                case HDShadowQuality.Medium:
                {
                    return DirectionalShadowAlgorithm.PCF7x7;
                }
                case HDShadowQuality.High:
                {
                    return DirectionalShadowAlgorithm.PCSS;
                }
                case HDShadowQuality.VeryHigh:
                {
                    return DirectionalShadowAlgorithm.IMS;
                }
            };
            return DirectionalShadowAlgorithm.PCF5x5;
        }

        public void UpdateDirectionalShadowResolution(int resolution, int cascadeCount)
        {
            Vector2Int atlasResolution = new Vector2Int(resolution, resolution);

            if (cascadeCount > 1)
                atlasResolution.x *= 2;
            if (cascadeCount > 2)
                atlasResolution.y *= 2;

            m_CascadeAtlas.UpdateSize(atlasResolution);
        }

        public int ReserveShadowResolutions(Vector2 resolution, ShadowMapType shadowMapType)
        {
            if (m_ShadowRequestCount >= m_MaxShadowRequests)
            {
                Debug.LogWarning("Max shadow requests count reached, dropping all exceeding requests. You can increase this limit by changing the max requests in the HDRP asset");
                return -1;
            }

            HDShadowResolutionRequest   resolutionRequest = new HDShadowResolutionRequest{
                resolution = resolution,
                shadowMapType = shadowMapType,
            };

            switch (shadowMapType)
            {
                case ShadowMapType.PunctualAtlas:
                    m_Atlas.ReserveResolution(resolutionRequest);
                    break;
                case ShadowMapType.AreaLightAtlas:
                    m_AreaLightShadowAtlas.ReserveResolution(resolutionRequest);
                    break;
                case ShadowMapType.CascadedDirectional:
                    m_CascadeAtlas.ReserveResolution(resolutionRequest);
                    break;
            }

            m_ShadowResolutionRequests.Add(resolutionRequest);
            m_ShadowRequestCount = m_ShadowResolutionRequests.Count;

            return m_ShadowResolutionRequests.Count - 1;
        }

        public Vector2 GetReservedResolution(int index)
        {
            if (index < 0 || index >= m_ShadowRequestCount)
                return Vector2.zero;

            return m_ShadowResolutionRequests[index].resolution;
        }

        public void UpdateShadowRequest(int index, HDShadowRequest shadowRequest)
        {
            if (index >= m_ShadowRequestCount)
                return;

            shadowRequest.atlasViewport = m_ShadowResolutionRequests[index].atlasViewport;
            m_ShadowRequests[index] = shadowRequest;

            switch (shadowRequest.shadowMapType)
            {
                case ShadowMapType.PunctualAtlas:
                {
                    m_Atlas.AddShadowRequest(shadowRequest);
                    break;
                }
                case ShadowMapType.CascadedDirectional:
                {
                    m_CascadeAtlas.AddShadowRequest(shadowRequest);
                    break;
                }
                case ShadowMapType.AreaLightAtlas:
                {
                    m_AreaLightShadowAtlas.AddShadowRequest(shadowRequest);
                    break;
                }
            };
        }

        public void UpdateCascade(int cascadeIndex, Vector4 cullingSphere, float border)
        {
            if (cullingSphere.w != float.NegativeInfinity)
            {
                cullingSphere.w *= cullingSphere.w;
            }

            m_CascadeCount = Mathf.Max(m_CascadeCount, cascadeIndex);

            unsafe
            {
                fixed (float * sphereCascadesBuffer = m_DirectionalShadowData.sphereCascades)
                    ((Vector4 *)sphereCascadesBuffer)[cascadeIndex] = cullingSphere;
                fixed (float * cascadeBorders = m_DirectionalShadowData.cascadeBorders)
                    cascadeBorders[cascadeIndex] = border;
            }
        }

        HDShadowData CreateShadowData(HDShadowRequest shadowRequest, HDShadowAtlas atlas)
        {
            HDShadowData data = new HDShadowData();

            var devProj = shadowRequest.deviceProjection;
            var view = shadowRequest.view;
            data.proj = new Vector4(devProj.m00, devProj.m11, devProj.m22, devProj.m23);
            data.pos = shadowRequest.position;
            data.rot0 = new Vector3(view.m00, view.m01, view.m02);
            data.rot1 = new Vector3(view.m10, view.m11, view.m12);
            data.rot2 = new Vector3(view.m20, view.m21, view.m22);
            data.shadowToWorld = shadowRequest.shadowToWorld;

            // Compute the scale and offset (between 0 and 1) for the atlas coordinates
            float rWidth = 1.0f / atlas.width;
            float rHeight = 1.0f / atlas.height;
            data.atlasOffset = Vector2.Scale(new Vector2(rWidth, rHeight), new Vector2(shadowRequest.atlasViewport.x, shadowRequest.atlasViewport.y));

            data.shadowMapSize = new Vector4(shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height, 1.0f / shadowRequest.atlasViewport.width, 1.0f / shadowRequest.atlasViewport.height);

            data.viewBias = shadowRequest.viewBias;
            data.normalBias = shadowRequest.normalBias;
            data.flags = shadowRequest.flags;
            data.edgeTolerance = shadowRequest.edgeTolerance;

            data.shadowFilterParams0.x = shadowRequest.shadowSoftness;
            data.shadowFilterParams0.y = HDShadowUtils.Asfloat(shadowRequest.blockerSampleCount);
            data.shadowFilterParams0.z = HDShadowUtils.Asfloat(shadowRequest.filterSampleCount);
            data.shadowFilterParams0.w = shadowRequest.minFilterSize;

            var hdAsset = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            if (hdAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowQuality == HDShadowQuality.VeryHigh && shadowRequest.lightType == (int)LightType.Directional)
            {
                data.shadowFilterParams0.x = shadowRequest.kernelSize;
                data.shadowFilterParams0.y = shadowRequest.lightAngle;
                data.shadowFilterParams0.z = shadowRequest.maxDepthBias;
            }

            if (atlas.HasBlurredEVSM())
            {
                data.shadowFilterParams0 = shadowRequest.evsmParams;
            }

            return data;
        }

        unsafe Vector4 GetCascadeSphereAtIndex(int index)
        {
            fixed (float * sphereCascadesBuffer = m_DirectionalShadowData.sphereCascades)
            {
                return ((Vector4 *)sphereCascadesBuffer)[index];
            }
        }

        public void UpdateCullingParameters(ref ScriptableCullingParameters cullingParams)
        {
            cullingParams.shadowDistance = Mathf.Min(VolumeManager.instance.stack.GetComponent<HDShadowSettings>().maxShadowDistance.value, cullingParams.shadowDistance);
        }

        public void LayoutShadowMaps(LightingDebugSettings lightingDebugSettings)
        {
            m_Atlas.UpdateDebugSettings(lightingDebugSettings);

            if (m_CascadeAtlas != null)
                m_CascadeAtlas.UpdateDebugSettings(lightingDebugSettings);

            m_AreaLightShadowAtlas.UpdateDebugSettings(lightingDebugSettings);

            if (lightingDebugSettings.shadowResolutionScaleFactor != 1.0f)
            {
                foreach (var shadowResolutionRequest in m_ShadowResolutionRequests)
                {
                    // We don't rescale the directional shadows with the global shadow scale factor
                    // because there is no dynamic atlas rescale when it overflow.
                    if (shadowResolutionRequest.shadowMapType != ShadowMapType.CascadedDirectional)
                        shadowResolutionRequest.resolution *= lightingDebugSettings.shadowResolutionScaleFactor;
                }
            }

            // Assign a position to all the shadows in the atlas, and scale shadows if needed
            if (m_CascadeAtlas != null && !m_CascadeAtlas.Layout(false))
                Debug.LogError("Cascade Shadow atlasing has failed, only one directional light can cast shadows at a time");
            m_Atlas.Layout();
            m_AreaLightShadowAtlas.Layout();
        }

        unsafe public void PrepareGPUShadowDatas(CullingResults cullResults, Camera camera)
        {
            int shadowIndex = 0;

            m_ShadowDatas.Clear();

            // Create all HDShadowDatas and update them with shadow request datas
            for (int i = 0; i < m_ShadowRequestCount; i++)
            {
                HDShadowAtlas atlas = m_Atlas;
                if (m_ShadowRequests[i].shadowMapType == ShadowMapType.CascadedDirectional)
                {
                    atlas = m_CascadeAtlas;
                }
                else if (m_ShadowRequests[i].shadowMapType == ShadowMapType.AreaLightAtlas)
                {
                    atlas = m_AreaLightShadowAtlas;
                }
                m_ShadowDatas.Add(CreateShadowData(m_ShadowRequests[i], atlas));
                m_ShadowRequests[i].shadowIndex = shadowIndex++;
            }

            int first = k_DirectionalShadowCascadeCount, second = k_DirectionalShadowCascadeCount;

            fixed (float *sphereBuffer = m_DirectionalShadowData.sphereCascades)
            {
                Vector4 * sphere = (Vector4 *)sphereBuffer;
                for (int i = 0; i < k_DirectionalShadowCascadeCount; i++)
                {
                    first  = (first  == k_DirectionalShadowCascadeCount                       && sphere[i].w > 0.0f) ? i : first;
                    second = ((second == k_DirectionalShadowCascadeCount || second == first)  && sphere[i].w > 0.0f) ? i : second;
                }
            }

            // Update directional datas:
            if (second != k_DirectionalShadowCascadeCount)
                m_DirectionalShadowData.cascadeDirection = (GetCascadeSphereAtIndex(second) - GetCascadeSphereAtIndex(first)).normalized;
            else
                m_DirectionalShadowData.cascadeDirection = Vector4.zero;

            m_DirectionalShadowData.cascadeDirection.w = VolumeManager.instance.stack.GetComponent<HDShadowSettings>().cascadeShadowSplitCount.value;
        }

        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, CullingResults cullResults, HDCamera hdCamera)
        {
            // Avoid to do any commands if there is no shadow to draw
            if (m_ShadowRequestCount == 0)
                return ;

            // Clear atlas render targets and draw shadows
            using (new ProfilingSample(cmd, "Punctual Lights Shadows rendering", CustomSamplerId.RenderShadowMaps.GetSampler()))
            {
                m_Atlas.RenderShadows(cullResults, hdCamera.frameSettings, renderContext, cmd);
            }

            using (new ProfilingSample(cmd, "Directional Light Shadows rendering", CustomSamplerId.RenderShadowMaps.GetSampler()))
            {
                m_CascadeAtlas.RenderShadows(cullResults, hdCamera.frameSettings, renderContext, cmd);
            }

            using (new ProfilingSample(cmd, "Area Light Shadows rendering", CustomSamplerId.RenderShadowMaps.GetSampler()))
            {
                m_AreaLightShadowAtlas.RenderShadows(cullResults, hdCamera.frameSettings, renderContext, cmd);
            }
        }

        public void SyncData()
        {
            // Avoid to upload datas which will not be used
            if (m_ShadowRequestCount == 0)
                return;

            // Upload the shadow buffers to GPU
            m_ShadowDataBuffer.SetData(m_ShadowDatas);
            m_DirectionalShadowDataBuffer.SetData(new HDDirectionalShadowData[]{ m_DirectionalShadowData });
        }

        public void BindResources(CommandBuffer cmd)
        {
            // This code must be in sync with HDShadowContext.hlsl
            cmd.SetGlobalBuffer(HDShaderIDs._HDShadowDatas, m_ShadowDataBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._HDDirectionalShadowData, m_DirectionalShadowDataBuffer);

            m_Atlas.BindResources(cmd);
            m_CascadeAtlas.BindResources(cmd);
            m_AreaLightShadowAtlas.BindResources(cmd);

            cmd.SetGlobalInt(HDShaderIDs._CascadeShadowCount, m_CascadeCount + 1);
        }

        public int GetShadowRequestCount()
        {
            return m_ShadowRequestCount;
        }

        public void Clear()
        {
            // Clear the shadows atlas infos and requests
            m_Atlas.Clear();
            m_CascadeAtlas.Clear();
            m_AreaLightShadowAtlas.Clear();
            m_ShadowResolutionRequests.Clear();

            m_ShadowRequestCount = 0;
            m_CascadeCount = 0;
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue)
        {
            m_Atlas.DisplayAtlas(cmd, debugMaterial, new Rect(0, 0, m_Atlas.width, m_Atlas.height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowCascadeAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue)
        {
            m_CascadeAtlas.DisplayAtlas(cmd, debugMaterial, new Rect(0, 0, m_CascadeAtlas.width, m_CascadeAtlas.height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayAreaLightShadowAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue)
        {
            m_AreaLightShadowAtlas.DisplayAtlas(cmd, debugMaterial, new Rect(0, 0, m_AreaLightShadowAtlas.width, m_AreaLightShadowAtlas.height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowMap(int shadowIndex, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue)
        {
            if (shadowIndex >= m_ShadowRequestCount)
                return;

            HDShadowRequest   shadowRequest = m_ShadowRequests[shadowIndex];

            switch (shadowRequest.shadowMapType)
            {
                case ShadowMapType.PunctualAtlas:
                {
                    m_Atlas.DisplayAtlas(cmd, debugMaterial, shadowRequest.atlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue);
                    break;
                }
                case ShadowMapType.CascadedDirectional:
                {
                    m_CascadeAtlas.DisplayAtlas(cmd, debugMaterial, shadowRequest.atlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue);
                    break;
                }
                case ShadowMapType.AreaLightAtlas:
                {
                    m_AreaLightShadowAtlas.DisplayAtlas(cmd, debugMaterial, shadowRequest.atlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue);
                    break;
                }
            };
        }

        public void Dispose()
        {
            m_ShadowDataBuffer.Dispose();
            m_DirectionalShadowDataBuffer.Dispose();
            m_Atlas.Release();
            m_AreaLightShadowAtlas.Release();
            m_CascadeAtlas.Release();
        }
    }
}
