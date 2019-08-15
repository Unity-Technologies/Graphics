using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Unity.Jobs;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {

        public struct LightDatasJob : IJobParallelFor
        {
            public struct BakedShadowMaskdata
            {
                public bool m_IsNull;
                public LightmapBakeType m_LightmapBakeType;
                public MixedLightingMode m_MixedLightingMode;
                public int m_OcclusionMaskChannel;
                public LightShadowCasterMode m_LightShadowCasterMode;
                public void copyInto(Light light)
                {
                    if (light == null)
                    {
                        m_IsNull = true;
                    }
                    else
                    {
                        m_IsNull = false;
                        m_LightmapBakeType = light.bakingOutput.lightmapBakeType;
                        m_MixedLightingMode = light.bakingOutput.mixedLightingMode;
                        m_OcclusionMaskChannel = light.bakingOutput.occlusionMaskChannel; 
                        m_LightShadowCasterMode = light.lightShadowCasterMode;
                    }
                }
            }

            public struct AdditionalLightData
            {
                public uint m_LightLayers;
                public bool m_ApplyRangeAttenuation;
                public float m_ShapeWidth;
                public float m_ShapeHeight;
                public float m_AspectRatio;
                public float m_InnerSpotPercent01;
                public float m_ShapeRadius;
                public float m_LightDimmer;
                public bool m_AffectDiffuse;
                public bool m_AffectSpecular;
                public float m_VolumetricDimmer;
                public float m_ShadowDimmer;
                public float m_VolumetricShadowDimmer;
                public bool m_ContactShadows;
                public float m_ShadowFadeDistance;
                public float m_MaxSmoothness;
                public Color m_ShadowTint;
                public void copyInto(HDAdditionalLightData additionalLightData)
                {
                    m_LightLayers = additionalLightData.GetLightLayers();
                    m_ApplyRangeAttenuation = additionalLightData.applyRangeAttenuation;
                    m_ShapeWidth = additionalLightData.shapeWidth;
                    m_ShapeHeight = additionalLightData.shapeHeight;
                    m_AspectRatio = additionalLightData.aspectRatio;
                    m_InnerSpotPercent01 = additionalLightData.innerSpotPercent01;
                    m_ShapeRadius = additionalLightData.shapeRadius;
                    m_LightDimmer = additionalLightData.lightDimmer;
                    m_AffectDiffuse = additionalLightData.affectDiffuse;
                    m_AffectSpecular = additionalLightData.affectSpecular;
                    m_VolumetricDimmer = additionalLightData.volumetricDimmer;
                    m_ShadowDimmer = additionalLightData.shadowDimmer;
                    m_VolumetricShadowDimmer = additionalLightData.volumetricShadowDimmer;
                    m_ContactShadows = additionalLightData.contactShadows;
                    m_ShadowFadeDistance = additionalLightData.shadowFadeDistance;
                    m_MaxSmoothness = additionalLightData.maxSmoothness;
                    m_ShadowTint = additionalLightData.shadowTint;
                }
            }
            public NativeArray<LightData> m_LightData;
            public NativeArray<Vector3> m_LightDimensions;

            [ReadOnly]
            public NativeArray<float> m_DistanceToCamera;
            [ReadOnly]
            public NativeArray<float>  m_LightDistanceFade;
            [ReadOnly]
            public NativeArray<AdditionalLightData> m_AdditionalLightData;
            [ReadOnly]
            public NativeArray<VisibleLight> m_VisibleLights;
            [ReadOnly]
            public NativeArray<int> m_VisibleLightRemapping;
            [ReadOnly]
            public NativeArray<int> m_SortIndexRemapping;
            [ReadOnly]
            NativeArray<GPULightType> m_GpuLightType;
            [ReadOnly]
            NativeArray<int> m_ShadowIndices;
            [ReadOnly]
            NativeArray<BakedShadowMaskdata> m_BakedShadowMaskdatas;
            [ReadOnly]
            public float m_SpecularGlobalDimmer;
            [ReadOnly]
            public float m_MaxShadowDistance;

            bool IsBakedShadowMaskLight(BakedShadowMaskdata bakedShadowMaskdata)
            {
                // This can happen for particle lights.
                if (bakedShadowMaskdata.m_IsNull)
                    return false;

                return bakedShadowMaskdata.m_LightmapBakeType == LightmapBakeType.Mixed &&
                    bakedShadowMaskdata.m_MixedLightingMode == MixedLightingMode.Shadowmask &&
                    bakedShadowMaskdata.m_OcclusionMaskChannel != -1;     // We need to have an occlusion mask channel assign, else we have no shadow mask
            }

            public void Execute(int index)
            {
                float distanceToCamera = m_DistanceToCamera[index];
                float lightDistanceFade = m_LightDistanceFade[index];
                int sortIndex = m_SortIndexRemapping[index];
                AdditionalLightData additionalLightData = m_AdditionalLightData[index];
                BakedShadowMaskdata bakedShadowMaskdata = m_BakedShadowMaskdatas[index];

                LightData lightData = m_LightData[index];
                lightData.lightLayers = additionalLightData.m_LightLayers;

                lightData.lightType = m_GpuLightType[index];

                VisibleLight light = m_VisibleLights[m_VisibleLightRemapping[index]];
                lightData.positionRWS = light.GetPosition();

                bool applyRangeAttenuation = additionalLightData.m_ApplyRangeAttenuation && (m_GpuLightType[index] != GPULightType.ProjectorBox);

                lightData.range = light.range;

                if (applyRangeAttenuation)
                {
                    lightData.rangeAttenuationScale = 1.0f / (light.range * light.range);
                    lightData.rangeAttenuationBias  = 1.0f;

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
                    const float sqrtHuge  = 4096.0f;
                    lightData.rangeAttenuationScale = sqrtHuge / (light.range * light.range);
                    lightData.rangeAttenuationBias  = hugeValue;

                    if (lightData.lightType == GPULightType.Rectangle)
                    {
                        // Rect lights are currently a special case because they use the normalized
                        // [0, 1] attenuation range rather than the regular [0, r] one.
                        lightData.rangeAttenuationScale = sqrtHuge;
                    }
                }

                lightData.color = GetLightColor(light);

                lightData.forward = light.GetForward();
                lightData.up = light.GetUp();
                lightData.right = light.GetRight();

                Vector3 lightDimensions = m_LightDimensions[index];
                lightDimensions.x = additionalLightData.m_ShapeWidth;
                lightDimensions.y = additionalLightData.m_ShapeHeight;
                lightDimensions.z = light.range;
                m_LightDimensions[index] = lightDimensions;

                if (lightData.lightType == GPULightType.ProjectorBox)
                {
                    // Rescale for cookies and windowing.
                    lightData.right *= 2.0f / Mathf.Max(additionalLightData.m_ShapeWidth, 0.001f);
                    lightData.up    *= 2.0f / Mathf.Max(additionalLightData.m_ShapeHeight, 0.001f);
                }
                else if (lightData.lightType == GPULightType.ProjectorPyramid)
                {
                    // Get width and height for the current frustum
                    var spotAngle = light.spotAngle;

                    float frustumWidth, frustumHeight;

                    if (additionalLightData.m_AspectRatio >= 1.0f)
                    {
                        frustumHeight = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                        frustumWidth = frustumHeight * additionalLightData.m_AspectRatio;
                    }
                    else
                    {
                        frustumWidth = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                        frustumHeight = frustumWidth / additionalLightData.m_AspectRatio;
                    }

                    lightDimensions = m_LightDimensions[index];
                    // Adjust based on the new parametrization.
                    lightDimensions.x = frustumWidth;
                    lightDimensions.y = frustumHeight;
                    m_LightDimensions[index] = lightDimensions;

                    // Rescale for cookies and windowing.
                    lightData.right *= 2.0f / frustumWidth;
                    lightData.up *= 2.0f / frustumHeight;
                }

                if (lightData.lightType == GPULightType.Spot)
                {
                    var spotAngle = light.spotAngle;

                    var innerConePercent = additionalLightData.m_InnerSpotPercent01;
                    var cosSpotOuterHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                    var sinSpotOuterHalfAngle = Mathf.Sqrt(1.0f - cosSpotOuterHalfAngle * cosSpotOuterHalfAngle);
                    var cosSpotInnerHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * innerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f); // inner cone

                    var val = Mathf.Max(0.0001f, (cosSpotInnerHalfAngle - cosSpotOuterHalfAngle));
                    lightData.angleScale = 1.0f / val;
                    lightData.angleOffset = -cosSpotOuterHalfAngle * lightData.angleScale;

                    // Rescale for cookies and windowing.
                    float cotOuterHalfAngle = cosSpotOuterHalfAngle / sinSpotOuterHalfAngle;
                    lightData.up    *= cotOuterHalfAngle;
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
                    lightData.size = new Vector2(additionalLightData.m_ShapeRadius * additionalLightData.m_ShapeRadius, 0);
                }

                if (lightData.lightType == GPULightType.Rectangle || lightData.lightType == GPULightType.Tube)
                {
                    lightData.size = new Vector2(additionalLightData.m_ShapeWidth, additionalLightData.m_ShapeHeight);
                }

                lightData.lightDimmer           = lightDistanceFade * (additionalLightData.m_LightDimmer);
                lightData.diffuseDimmer         = lightDistanceFade * (additionalLightData.m_AffectDiffuse  ? additionalLightData.m_LightDimmer : 0);
                lightData.specularDimmer        = lightDistanceFade * (additionalLightData.m_AffectSpecular ? additionalLightData.m_LightDimmer * m_SpecularGlobalDimmer : 0);
                lightData.volumetricLightDimmer = lightDistanceFade * (additionalLightData.m_VolumetricDimmer);

                lightData.cookieIndex = -1;
                lightData.shadowIndex = -1;
                lightData.screenSpaceShadowIndex = -1;

                float shadowDistanceFade         = HDUtils.ComputeLinearDistanceFade(distanceToCamera, Mathf.Min(m_MaxShadowDistance, additionalLightData.m_ShadowFadeDistance));
                lightData.shadowDimmer           = shadowDistanceFade * additionalLightData.m_ShadowDimmer;
                lightData.volumetricShadowDimmer = shadowDistanceFade * additionalLightData.m_VolumetricShadowDimmer;
                lightData.shadowTint             = new Vector3(additionalLightData.m_ShadowTint.r, additionalLightData.m_ShadowTint.g, additionalLightData.m_ShadowTint.b);

    #if ENABLE_RAYTRACING
                // If there is still a free slot in the screen space shadow array and this needs to render a screen space shadow
                if(screenSpaceShadowIndex < m_Asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadows && additionalLightData.WillRenderScreenSpaceShadow())
                {
                    lightData.screenSpaceShadowIndex = screenSpaceShadowIndex;
                    additionalLightData.shadowIndex = -1;
                    m_CurrentRayTracedShadows[screenSpaceShadowIndex] = additionalLightData;
                    screenSpaceShadowIndex++;
                }
                else
                {
                    // fix up shadow information
                    lightData.shadowIndex = shadowIndex;
                    additionalLightData.shadowIndex = shadowIndex;
                }
    #else
                // fix up shadow information
                lightData.shadowIndex = m_ShadowIndices[sortIndex];
    #endif
                // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
                lightData.minRoughness = (1.0f - additionalLightData.m_MaxSmoothness) * (1.0f - additionalLightData.m_MaxSmoothness);

                lightData.shadowMaskSelector = Vector4.zero;

                if (IsBakedShadowMaskLight(bakedShadowMaskdata))
                {
                    lightData.shadowMaskSelector[bakedShadowMaskdata.m_OcclusionMaskChannel] = 1.0f;
                    lightData.nonLightMappedOnly = bakedShadowMaskdata.m_LightShadowCasterMode == LightShadowCasterMode.NonLightmappedOnly ? 1 : 0;
                }
                else
                {
                    // use -1 to say that we don't use shadow mask
                    lightData.shadowMaskSelector.x = -1.0f;
                    lightData.nonLightMappedOnly = 0;
                }

                m_LightData[index] = lightData;
            }

            public void SetData( NativeArray<LightData> lightData,
                NativeArray<Vector3> lightDimensions,
                NativeArray<float> distanceToCamera,
                NativeArray<float> lightDistanceFade,
                NativeArray<AdditionalLightData> additionalLightData,
                NativeArray<VisibleLight> visibleLights,
                NativeArray<int> visibleLightRemapping,
                NativeArray<int> sortIndexRemapping,
                NativeArray<GPULightType> gpuLightType,
                NativeArray<int> shadowIndices,
                NativeArray<BakedShadowMaskdata> bakedShadowMaskdatas,
                float specularGlobalDimmer,
                float maxShadowDistance)
            {
                m_LightData = lightData;
                m_LightDimensions = lightDimensions;
                m_DistanceToCamera = distanceToCamera;
                m_LightDistanceFade = lightDistanceFade;
                m_AdditionalLightData = additionalLightData;
                m_VisibleLights = visibleLights;
                m_VisibleLightRemapping = visibleLightRemapping;
                m_SortIndexRemapping = sortIndexRemapping;
                m_GpuLightType = gpuLightType;
                m_ShadowIndices = shadowIndices;
                m_BakedShadowMaskdatas = bakedShadowMaskdatas;
                m_SpecularGlobalDimmer = specularGlobalDimmer;
                m_MaxShadowDistance = maxShadowDistance;
            }
        }

        public struct LightVolumeBoundsJob : IJobParallelFor
        {
            public NativeArray<SFiniteLightBound> m_Bounds;
            public NativeArray<LightVolumeData> m_LightVolumeData;

            [ReadOnly]
            public NativeArray<LightCategory> m_LightCategory;
            [ReadOnly]
            public NativeArray<GPULightType> m_GpuLightType;
            [ReadOnly]
            public NativeArray<LightVolumeType> m_LightVolumeType;
            [ReadOnly]
            public NativeArray<VisibleLight> m_VisibleLights;
            [ReadOnly]
            public NativeArray<LightData> m_LightData;
            [ReadOnly]
            public NativeArray<Vector3> m_LightDimensions;
            [ReadOnly]
            public NativeArray<Matrix4x4> m_WorldToView;
            [ReadOnly]
            public NativeArray<int> m_VisibleLightRemapping;
            [ReadOnly]
            public int m_LightsPerView;
            [ReadOnly]
            public int m_DecalsPerView;
            [ReadOnly]
            public int m_NumViews;

            public void SetData( NativeArray<SFiniteLightBound> bounds,
                NativeArray<LightVolumeData> lightVolumeData,
                NativeArray<LightCategory> lightCategory,
                NativeArray<GPULightType> gpuLightType,
                NativeArray<LightVolumeType> lightVolumeType,
                NativeArray<VisibleLight> visibleLights,
                NativeArray<LightData> lightData,
                NativeArray<Vector3> lightDimensions,
                NativeArray<Matrix4x4> worldToView,
                NativeArray<int> visibleLightRemapping,
                int lightsPerView,
                int decalsPerView,
                int numViews)
            {
                m_Bounds = bounds;
                m_LightVolumeData = lightVolumeData;
                m_LightCategory = lightCategory;
                m_GpuLightType = gpuLightType;
                m_LightVolumeType = lightVolumeType;
                m_VisibleLights = visibleLights;
                m_LightData = lightData;
                m_LightDimensions = lightDimensions;
                m_WorldToView = worldToView;
                m_VisibleLightRemapping = visibleLightRemapping;
                m_LightsPerView = lightsPerView;
                m_DecalsPerView = decalsPerView;
                m_NumViews = numViews;                
            }
//            [BurstCompile(CompileSynchronously = true)]
            public void Execute(int index)
            {
                // Then Culling side
                int entriesPerView = (m_LightsPerView + m_DecalsPerView);
                for(int viewIndex = 0; viewIndex < m_NumViews; viewIndex++)
                {
                    int offsetPerView = viewIndex * entriesPerView;
                    index = offsetPerView + (index % m_LightsPerView); // calculate actual index in the array padded with decal datas
                    var range = m_LightDimensions[index].z;
                    var lightToWorld = m_VisibleLights[m_VisibleLightRemapping[index]].localToWorldMatrix;
                    Vector3 positionWS = m_LightData[index].positionRWS;
                    Vector3 positionVS = m_WorldToView[viewIndex].MultiplyPoint(positionWS);

                    Matrix4x4 lightToView = m_WorldToView[viewIndex] * lightToWorld;
                    Vector3 xAxisVS = lightToView.GetColumn(0);
                    Vector3 yAxisVS = lightToView.GetColumn(1);
                    Vector3 zAxisVS = lightToView.GetColumn(2);

                    LightVolumeData lvd = m_LightVolumeData[offsetPerView + index];
                    SFiniteLightBound sflb = m_Bounds[offsetPerView + index];
                
                    lvd.lightCategory = (uint)m_LightCategory[index];
                    lvd.lightVolume = (uint)m_LightVolumeType[index];

                    if (m_GpuLightType[index] == GPULightType.Spot || m_GpuLightType[index] == GPULightType.ProjectorPyramid)
                    {
                        Vector3 lightDir = lightToWorld.GetColumn(2);

                        // represents a left hand coordinate system in world space since det(worldToView)<0
                        Vector3 vx = xAxisVS;
                        Vector3 vy = yAxisVS;
                        Vector3 vz = zAxisVS;

                        const float pi = 3.1415926535897932384626433832795f;
                        const float degToRad = (float)(pi / 180.0);

                        var sa = m_VisibleLights[m_VisibleLightRemapping[index]].spotAngle;
                        var cs = Mathf.Cos(0.5f * sa * degToRad);
                        var si = Mathf.Sin(0.5f * sa * degToRad);

                        if (m_GpuLightType[index] == GPULightType.ProjectorPyramid)
                        {
                            Vector3 lightPosToProjWindowCorner = (0.5f * m_LightDimensions[index].x) * vx + (0.5f * m_LightDimensions[index].y) * vy + 1.0f * vz;
                            cs = Vector3.Dot(vz, Vector3.Normalize(lightPosToProjWindowCorner));
                            si = Mathf.Sqrt(1.0f - cs * cs);
                        }

                        const float FltMax = 3.402823466e+38F;
                        var ta = cs > 0.0f ? (si / cs) : FltMax;
                        var cota = si > 0.0f ? (cs / si) : FltMax;

                        //const float cotasa = l.GetCotanHalfSpotAngle();

                        // apply nonuniform scale to OBB of spot light
                        var squeeze = true;//sa < 0.7f * 90.0f;      // arb heuristic
                        var fS = squeeze ? ta : si;
                        sflb.center = m_WorldToView[viewIndex].MultiplyPoint(positionWS + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                        // scale axis to match box or base of pyramid
                        sflb.boxAxisX = (fS * range) * vx;
                        sflb.boxAxisY = (fS * range) * vy;
                        sflb.boxAxisZ = (0.5f * range) * vz;

                        // generate bounding sphere radius
                        var fAltDx = si;
                        var fAltDy = cs;
                        fAltDy = fAltDy - 0.5f;
                        //if(fAltDy<0) fAltDy=-fAltDy;

                        fAltDx *= range; fAltDy *= range;

                        // Handle case of pyramid with this select (currently unused)
                        var altDist = Mathf.Sqrt(fAltDy * fAltDy + (true ? 1.0f : 2.0f) * fAltDx * fAltDx);
                        sflb.radius = altDist > (0.5f * range) ? altDist : (0.5f * range);       // will always pick fAltDist
                        sflb.scaleXY = squeeze ? new Vector2(0.01f, 0.01f) : new Vector2(1.0f, 1.0f);

                        lvd.lightAxisX = vx;
                        lvd.lightAxisY = vy;
                        lvd.lightAxisZ = vz;
                        lvd.lightPos = positionVS;
                        lvd.radiusSq = range * range;
                        lvd.cotan = cota;
                        lvd.featureFlags = (uint)LightFeatureFlags.Punctual;
                    }
                    else if (m_GpuLightType[index] == GPULightType.Point)
                    {
                        Vector3 vx = xAxisVS;
                        Vector3 vy = yAxisVS;
                        Vector3 vz = zAxisVS;

                        sflb.center = positionVS;
                        sflb.boxAxisX = vx * range;
                        sflb.boxAxisY = vy * range;
                        sflb.boxAxisZ = vz * range;
                        sflb.scaleXY.Set(1.0f, 1.0f);
                        sflb.radius = range;

                        // fill up ldata
                        lvd.lightAxisX = vx;
                        lvd.lightAxisY = vy;
                        lvd.lightAxisZ = vz;
                        lvd.lightPos = sflb.center;
                        lvd.radiusSq = range * range;
                        lvd.featureFlags = (uint)LightFeatureFlags.Punctual;
                
                    }
                    else if (m_GpuLightType[index] == GPULightType.Tube)
                    {
                        Vector3 dimensions = new Vector3(m_LightDimensions[index].x + 2 * range, 2 * range, 2 * range); // Omni-directional
                        Vector3 extents = 0.5f * dimensions;

                        sflb.center = positionVS;
                        sflb.boxAxisX = extents.x * xAxisVS;
                        sflb.boxAxisY = extents.y * yAxisVS;
                        sflb.boxAxisZ = extents.z * zAxisVS;
                        sflb.scaleXY.Set(1.0f, 1.0f);
                        sflb.radius = extents.magnitude;

                        lvd.lightPos = positionVS;
                        lvd.lightAxisX = xAxisVS;
                        lvd.lightAxisY = yAxisVS;
                        lvd.lightAxisZ = zAxisVS;
                        lvd.boxInnerDist = new Vector3(m_LightDimensions[index].x, 0, 0);
                        lvd.boxInvRange.Set(1.0f / range, 1.0f / range, 1.0f / range);
                        lvd.featureFlags = (uint)LightFeatureFlags.Area;
                    }
                    else if (m_GpuLightType[index] == GPULightType.Rectangle)
                    {
                        Vector3 dimensions = new Vector3(m_LightDimensions[index].x + 2 * range, m_LightDimensions[index].y + 2 * range, range); // One-sided
                        Vector3 extents = 0.5f * dimensions;
                        Vector3 centerVS = positionVS + extents.z * zAxisVS;

                        sflb.center = centerVS;
                        sflb.boxAxisX = extents.x * xAxisVS;
                        sflb.boxAxisY = extents.y * yAxisVS;
                        sflb.boxAxisZ = extents.z * zAxisVS;
                        sflb.scaleXY.Set(1.0f, 1.0f);
                        sflb.radius = extents.magnitude;

                        lvd.lightPos = centerVS;
                        lvd.lightAxisX = xAxisVS;
                        lvd.lightAxisY = yAxisVS;
                        lvd.lightAxisZ = zAxisVS;
                        lvd.boxInnerDist = extents;
                        lvd.boxInvRange.Set(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                        lvd.featureFlags = (uint)LightFeatureFlags.Area;
                    }
                    else if (m_GpuLightType[index] == GPULightType.ProjectorBox)
                    {
                        Vector3 dimensions = new Vector3(m_LightDimensions[index].x, m_LightDimensions[index].y, range);  // One-sided
                        Vector3 extents = 0.5f * dimensions;
                        Vector3 centerVS = positionVS + extents.z * zAxisVS;

                        sflb.center = centerVS;
                        sflb.boxAxisX = extents.x * xAxisVS;
                        sflb.boxAxisY = extents.y * yAxisVS;
                        sflb.boxAxisZ = extents.z * zAxisVS;
                        sflb.radius = extents.magnitude;
                        sflb.scaleXY.Set(1.0f, 1.0f);

                        lvd.lightPos = centerVS;
                        lvd.lightAxisX = xAxisVS;
                        lvd.lightAxisY = yAxisVS;
                        lvd.lightAxisZ = zAxisVS;
                        lvd.boxInnerDist = extents;
                        lvd.boxInvRange.Set(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                        lvd.featureFlags = (uint)LightFeatureFlags.Punctual;
                    }
                    else
                    {
                        Debug.Assert(false, "TODO: encountered an unknown GPULightType.");
                    }
                    m_LightVolumeData[offsetPerView + index] = lvd;
                    m_Bounds[offsetPerView + index] = sflb;
                }
            }
        }

        void AllocateRemappingArray(int sortCount)
        {
            m_VisibleLightRemapping = new NativeArray<int>(sortCount, Allocator.TempJob);
            m_SortIndexRemapping = new NativeArray<int>(sortCount, Allocator.TempJob);
            m_LightPositions = new NativeArray<Vector3>(sortCount, Allocator.TempJob);
            m_LightTypes = new NativeArray<LightType>(sortCount, Allocator.TempJob);
        }

        void DisposeRemappingArray()
        {
            m_VisibleLightRemapping.Dispose();
            m_SortIndexRemapping.Dispose();
            m_LightPositions.Dispose();
            m_LightTypes.Dispose();
        }
        
        void AllocateVolumeBoundsJobArrays(int lightCount, int numViews)
        {
            m_LightCategory = new NativeArray<LightCategory>(lightCount, Allocator.TempJob);
            m_GpuLightType = new NativeArray<GPULightType>(lightCount, Allocator.TempJob);
            m_LightVolumeType = new NativeArray<LightVolumeType>(lightCount, Allocator.TempJob);
            m_LightDimensions = new NativeArray<Vector3>(lightCount, Allocator.TempJob);
            m_WorldToView = new NativeArray<Matrix4x4>(numViews, Allocator.TempJob);
        }

        void DisposeBoundsJobArrays()
        {
            m_LightCategory.Dispose(); 
            m_GpuLightType.Dispose(); 
            m_LightVolumeType.Dispose(); 
            m_LightDimensions.Dispose(); 
            m_WorldToView.Dispose();
        }

        void AllocateDistanceAndFadeArrays(int lightCount)
        {
            m_DistanceToCamera = new NativeArray<float>(lightCount, Allocator.TempJob);
            m_LightDistanceFade = new NativeArray<float>(lightCount, Allocator.TempJob);
            m_ShadowIndices = new NativeArray<int>(lightCount, Allocator.TempJob);
            m_AdditionalLightData = new NativeArray<LightDatasJob.AdditionalLightData>(lightCount, Allocator.TempJob);
            m_BakedShadowMaskdatas = new NativeArray<LightDatasJob.BakedShadowMaskdata>(lightCount, Allocator.TempJob);
        }

        void DisposeDistanceAndFadeArrays()
        {
            m_DistanceToCamera.Dispose(); 
            m_LightDistanceFade.Dispose();
            m_ShadowIndices.Dispose();
            m_AdditionalLightData.Dispose();
            m_BakedShadowMaskdatas.Dispose();
        }

        NativeArray<LightCategory> m_LightCategory;
        NativeArray<GPULightType> m_GpuLightType;
        NativeArray<LightVolumeType> m_LightVolumeType;
        NativeArray<Vector3> m_LightDimensions;
        NativeArray<Matrix4x4> m_WorldToView;

        NativeArray<int> m_VisibleLightRemapping;
        NativeArray<int> m_SortIndexRemapping;
        NativeArray<Vector3> m_LightPositions;
        NativeArray<LightType> m_LightTypes;

        NativeArray<float> m_DistanceToCamera;
        NativeArray<float> m_LightDistanceFade;
        NativeArray<LightDatasJob.AdditionalLightData> m_AdditionalLightData;
        NativeArray<int> m_ShadowIndices;
        NativeArray<LightDatasJob.BakedShadowMaskdata> m_BakedShadowMaskdatas;

        LightVolumeBoundsJob m_LightVolumeBoundsJob;
        LightDatasJob m_LightDatasJob;
    }
}

