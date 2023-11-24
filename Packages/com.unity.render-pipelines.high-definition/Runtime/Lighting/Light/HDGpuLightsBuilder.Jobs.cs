using System;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst.CompilerServices;
using System.Threading;

namespace UnityEngine.Rendering.HighDefinition
{
    internal partial class HDGpuLightsBuilder
    {
        JobHandle m_CreateGpuLightDataJobHandle;

        internal struct CreateGpuLightDataJobGlobalConfig
        {
            public bool lightLayersEnabled;
            public float specularGlobalDimmer;
            public int invalidScreenSpaceShadowIndex;
            public float maxShadowFadeDistance;

            public static CreateGpuLightDataJobGlobalConfig Create(
                HDCamera hdCamera,
                HDShadowSettings hdShadowSettings)
            {
                return new CreateGpuLightDataJobGlobalConfig()
                {
                    lightLayersEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers),
                    specularGlobalDimmer = hdCamera.frameSettings.specularGlobalDimmer,
                    maxShadowFadeDistance = hdShadowSettings.maxShadowDistance.value,
                    invalidScreenSpaceShadowIndex = (int)LightDefinitions.s_InvalidScreenSpaceShadow
                };
            }
        }

#if ENABLE_BURST_1_5_0_OR_NEWER
        [Unity.Burst.BurstCompile]
#endif
        internal struct CreateGpuLightDataJob : IJobParallelFor
        {
            #region Parameters
            [ReadOnly]
            public int totalLightCounts;
            [ReadOnly]
            public int outputLightCounts;
            [ReadOnly]
            public int outputDirectionalLightCounts;
            [ReadOnly]
            public int outputLightBoundsCount;
            [ReadOnly]
            public CreateGpuLightDataJobGlobalConfig globalConfig;
            [ReadOnly]
            public Vector3 cameraPos;
            [ReadOnly]
            public int directionalSortedLightCounts;
            [ReadOnly]
            public bool isPbrSkyActive;
            [ReadOnly]
            public int defaultDataIndex;
            [ReadOnly]
            public int viewCounts;
            [ReadOnly]
            public bool useCameraRelativePosition;
            [ReadOnly]
            public float maxShadowDistance;


            #endregion

            #region input light entity data
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<HDLightRenderData> lightRenderDataArray;
            #endregion

            #region input visible lights processed
            [ReadOnly]
            public NativeArray<uint> sortKeys;
            [ReadOnly]
            public NativeArray<HDProcessedVisibleLight> processedEntities;
            [ReadOnly]
            public NativeArray<VisibleLight> visibleLights;
            [ReadOnly]
            public NativeArray<LightBakingOutput> visibleLightBakingOutput;
            [ReadOnly]
            public NativeArray<LightShadowCasterMode> visibleLightShadowCasterMode;
            #endregion

            #region output processed lights
            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<LightData> lights;
            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<DirectionalLightData> directionalLights;
            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<LightsPerView> lightsPerView;
            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<SFiniteLightBound> lightBounds;
            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<LightVolumeData> lightVolumes;
            [WriteOnly]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<int> gpuLightCounters;
            #endregion

#if DEBUG
            [IgnoreWarning(1370)] //Ignore throwing exception warning.
#endif
            private ref HDLightRenderData GetLightData(int dataIndex)
            {
#if DEBUG
                if (dataIndex < 0 || dataIndex >= totalLightCounts)
                    throw new Exception("Trying to access a light from the DB out of bounds. The index requested is: " + dataIndex + " and the length is " + totalLightCounts);
#endif
                unsafe
                {
                    HDLightRenderData* data = (HDLightRenderData*)lightRenderDataArray.GetUnsafePtr<HDLightRenderData>() + dataIndex;
                    return ref UnsafeUtility.AsRef<HDLightRenderData>(data);
                }
            }

            private static uint GetLightLayer(bool lightLayersEnabled, in HDLightRenderData lightRenderData)
            {
                int lightLayerMaskValue = (int)lightRenderData.renderingLayerMask;
                uint lightLayerValue = lightLayerMaskValue < 0 ? (uint)RenderingLayerMask.Everything : lightRenderData.renderingLayerMask;
                return lightLayersEnabled ? lightLayerValue : uint.MaxValue;
            }

            private static Vector3 GetLightColor(in VisibleLight light) => new Vector3(light.finalColor.r, light.finalColor.g, light.finalColor.b);

            private void IncrementCounter(HDGpuLightsBuilder.GPULightTypeCountSlots counterSlot)
            {
                unsafe
                {
                    int* ptr = (int*)gpuLightCounters.GetUnsafePtr<int>() + (int)counterSlot;
                    Interlocked.Increment(ref UnsafeUtility.AsRef<int>(ptr));
                }
            }

            public static void ConvertLightToGPUFormat(
                LightCategory lightCategory, GPULightType gpuLightType,
                in CreateGpuLightDataJobGlobalConfig globalConfig,
                LightShadowCasterMode visibleLightShadowCasterMode,
                in LightBakingOutput visibleLightBakingOutput,
                in VisibleLight light,
                in HDProcessedVisibleLight processedEntity,
                in HDLightRenderData lightRenderData,
                out Vector3 lightDimensions,
                ref LightData lightData)
            {
                int dataIndex = processedEntity.dataIndex;
                lightData.lightLayers = GetLightLayer(globalConfig.lightLayersEnabled, lightRenderData);
                lightData.lightType = gpuLightType;

                var visibleLightAxisAndPosition = light.GetAxisAndPosition();
                lightData.positionRWS = visibleLightAxisAndPosition.Position;
                lightData.range = light.range;

                if (lightRenderData.applyRangeAttenuation)
                {
                    lightData.rangeAttenuationScale = 1.0f / (light.range * light.range);
                    lightData.rangeAttenuationBias = 1.0f;
                }
                else
                {
                    // Solve f(x) = b - (a * x)^2 where x = (d/r)^2.
                    // f(0) = huge -> b = huge.
                    // f(1) = 0    -> huge - a^2 = 0 -> a = sqrt(huge).
                    const float hugeValue = 16777216.0f;
                    const float sqrtHuge = 4096.0f;
                    lightData.rangeAttenuationScale = sqrtHuge / (light.range * light.range);
                    lightData.rangeAttenuationBias = hugeValue;
                }

                float shapeWidthVal = lightRenderData.shapeWidth;
                float shapeHeightVal = lightRenderData.shapeHeight;

                if (lightData.lightType == GPULightType.Tube) shapeHeightVal = 0;

                lightData.color = GetLightColor(light);
                lightData.forward = visibleLightAxisAndPosition.Forward;
                lightData.up = visibleLightAxisAndPosition.Up;
                lightData.right = visibleLightAxisAndPosition.Right;

                lightDimensions.x = shapeWidthVal;
                lightDimensions.y = shapeHeightVal;
                lightDimensions.z = light.range;

                lightData.boxLightSafeExtent = 1.0f;

                if (lightData.lightType == GPULightType.ProjectorBox)
                {
                    // Rescale for cookies and windowing.
                    lightData.right *= 2.0f / Mathf.Max(shapeWidthVal, 0.001f);
                    lightData.up *= 2.0f / Mathf.Max(shapeHeightVal, 0.001f);
                }
                else if (lightData.lightType == GPULightType.ProjectorPyramid)
                {
                    // Get width and height for the current frustum
                    var spotAngle = light.spotAngle;
                    float aspectRatioValue = lightRenderData.aspectRatio;

                    float frustumWidth, frustumHeight;

                    if (aspectRatioValue >= 1.0f)
                    {
                        frustumHeight = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                        frustumWidth = frustumHeight * aspectRatioValue;
                    }
                    else
                    {
                        frustumWidth = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                        frustumHeight = frustumWidth / aspectRatioValue;
                    }

                    // Adjust based on the new parametrization.
                    lightDimensions.x = frustumWidth;
                    lightDimensions.y = frustumHeight;

                    //// Rescale for cookies and windowing.
                    lightData.right *= 2.0f / frustumWidth;
                    lightData.up *= 2.0f / frustumHeight;
                }

                if (lightData.lightType == GPULightType.Spot)
                {
                    var spotAngle = light.spotAngle;

                    var innerConePercent = lightRenderData.innerSpotPercent / 100.0f;
                    var cosSpotOuterHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                    var sinSpotOuterHalfAngle = Mathf.Sqrt(1.0f - cosSpotOuterHalfAngle * cosSpotOuterHalfAngle);
                    var cosSpotInnerHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * innerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f); // inner cone

                    var val = Mathf.Max(0.0001f, (cosSpotInnerHalfAngle - cosSpotOuterHalfAngle));
                    lightData.angleScale = 1.0f / val;
                    lightData.angleOffset = -cosSpotOuterHalfAngle * lightData.angleScale;
                    lightData.iesCut = lightRenderData.spotIESCutoffPercent / 100.0f;

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
                    lightData.iesCut = 1.0f;
                }

                float shapeRadiusVal = lightRenderData.shapeRadius;
                if (lightData.lightType != GPULightType.Directional && lightData.lightType != GPULightType.ProjectorBox)
                {
                    // Store the squared radius of the light to simulate a fill light.
                    lightData.size = new Vector4(shapeRadiusVal * shapeRadiusVal, 0, 0, 0);
                }

                if (lightData.lightType == GPULightType.Rectangle || lightData.lightType == GPULightType.Tube || lightData.lightType == GPULightType.Disc)
                {
                    lightData.size = new Vector4(shapeWidthVal, shapeHeightVal, Mathf.Cos(lightRenderData.barnDoorAngle * Mathf.PI / 180.0f), lightRenderData.barnDoorLength);
                }

                var lightDimmerVal = lightRenderData.lightDimmer;
                lightData.lightDimmer = processedEntity.lightDistanceFade * lightDimmerVal;
                lightData.diffuseDimmer = processedEntity.lightDistanceFade * (lightRenderData.affectDiffuse ? lightDimmerVal : 0);
                lightData.specularDimmer = processedEntity.lightDistanceFade * (lightRenderData.affectSpecular ? lightDimmerVal * globalConfig.specularGlobalDimmer : 0);
                lightData.volumetricLightDimmer = Mathf.Min(processedEntity.lightVolumetricDistanceFade, processedEntity.lightDistanceFade) * (lightRenderData.affectVolumetric ? lightRenderData.volumetricDimmer : 0.0f);

                lightData.cookieMode = CookieMode.None;
                lightData.shadowIndex = -1;
                lightData.screenSpaceShadowIndex = globalConfig.invalidScreenSpaceShadowIndex;
                lightData.isRayTracedContactShadow = 0.0f;

                var distanceToCamera = processedEntity.distanceToCamera;
                var lightsShadowFadeDistance = lightRenderData.shadowFadeDistance;
                var shadowDimmerVal = lightRenderData.shadowDimmer;
                var volumetricShadowDimmerVal = lightRenderData.affectVolumetric ? lightRenderData.volumetricShadowDimmer : 0.0f;
                float shadowDistanceFade = HDUtils.ComputeLinearDistanceFade(distanceToCamera, Mathf.Min(globalConfig.maxShadowFadeDistance, lightsShadowFadeDistance));
                lightData.shadowDimmer = shadowDistanceFade * shadowDimmerVal;
                lightData.volumetricShadowDimmer = shadowDistanceFade * volumetricShadowDimmerVal;

                // We want to have a colored penumbra if the flag is on and the color is not gray
                var shadowTintVal = lightRenderData.shadowTint;
                bool penumbraTintVal = lightRenderData.penumbraTint && ((shadowTintVal.r != shadowTintVal.g) || (shadowTintVal.g != shadowTintVal.b));
                lightData.penumbraTint = penumbraTintVal ? 1.0f : 0.0f;
                if (penumbraTintVal)
                    lightData.shadowTint = new Vector3(Mathf.Pow(shadowTintVal.r, 2.2f), Mathf.Pow(shadowTintVal.g, 2.2f), Mathf.Pow(shadowTintVal.b, 2.2f));
                else
                    lightData.shadowTint = new Vector3(shadowTintVal.r, shadowTintVal.g, shadowTintVal.b);

                //Value of max smoothness is derived from Radius. Formula results from eyeballing. Radius of 0 results in 1 and radius of 2.5 results in 0.
                float maxSmoothness = Mathf.Clamp01(1.1725f / (1.01f + Mathf.Pow(1.0f * (shapeRadiusVal + 0.1f), 2f)) - 0.15f);
                // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
                lightData.minRoughness = (1.0f - maxSmoothness) * (1.0f - maxSmoothness);
                lightData.shadowMaskSelector = Vector4.zero;

                if (processedEntity.isBakedShadowMask)
                {
                    lightData.shadowMaskSelector[visibleLightBakingOutput.occlusionMaskChannel] = 1.0f;
                    lightData.nonLightMappedOnly = visibleLightShadowCasterMode == LightShadowCasterMode.NonLightmappedOnly ? 1 : 0;
                }
                else
                {
                    // use -1 to say that we don't use shadow mask
                    lightData.shadowMaskSelector.x = -1.0f;
                    lightData.nonLightMappedOnly = 0;
                }
            }

#if DEBUG
            [IgnoreWarning(1370)] //Ignore throwing exception warning.
#endif
            private void StoreAndConvertLightToGPUFormat(
                int outputIndex, int lightIndex,
                LightCategory lightCategory, GPULightType gpuLightType, LightVolumeType lightVolumeType, bool offscreen)
            {
                var light = visibleLights[lightIndex];
                var processedEntity = processedEntities[lightIndex];
                var lightData = new LightData();
                ref HDLightRenderData lightRenderData = ref GetLightData(processedEntity.dataIndex);

                ConvertLightToGPUFormat(
                    lightCategory, gpuLightType, globalConfig,
                    visibleLightShadowCasterMode[lightIndex],
                    visibleLightBakingOutput[lightIndex],
                    light,
                    processedEntity,
                    lightRenderData,
                    out var lightDimensions,
                    ref lightData);

                for (int viewId = 0; viewId < viewCounts; ++viewId)
                {
                    var lightsPerViewContainer = lightsPerView[viewId];
                    ComputeLightVolumeDataAndBound(
                        lightCategory, gpuLightType, lightVolumeType,
                        light, lightData, lightDimensions, lightsPerViewContainer.worldToView, outputIndex + lightsPerViewContainer.boundsOffset);
                }

                // Light volumes has been calculated, now we can update positionRWS to be relative camera
                if (useCameraRelativePosition)
                    lightData.positionRWS -= cameraPos;

                switch (lightCategory)
                {
                    case LightCategory.Punctual:
                        IncrementCounter(HDGpuLightsBuilder.GPULightTypeCountSlots.Punctual);
                        break;
                    case LightCategory.Area:
                        IncrementCounter(HDGpuLightsBuilder.GPULightTypeCountSlots.Area);
                        break;
                    default:
                        Debug.Assert(false, "TODO: encountered an unknown LightCategory.");
                        break;
                }

#if DEBUG
                if (outputIndex < 0 || outputIndex >= outputLightCounts)
                    throw new Exception("Trying to access an output index out of bounds. Output index is " + outputIndex + "and max length is " + outputLightCounts);
#endif
                lights[outputIndex] = lightData;
            }

#if DEBUG
            [IgnoreWarning(1370)] //Ignore throwing exception warning.
#endif
            private void ComputeLightVolumeDataAndBound(
                LightCategory lightCategory, GPULightType gpuLightType, LightVolumeType lightVolumeType,
                in VisibleLight light, in LightData lightData, in Vector3 lightDimensions, in Matrix4x4 worldToView, int outputIndex)
            {
                // Then Culling side
                var range = lightDimensions.z;
                var lightToWorld = light.localToWorldMatrix;
                Vector3 positionWS = lightData.positionRWS; // Currently not including camera relative transform
                Vector3 positionVS = worldToView.MultiplyPoint(positionWS);

                Vector3 xAxisVS = worldToView.MultiplyVector(lightToWorld.GetColumn(0));
                Vector3 yAxisVS = worldToView.MultiplyVector(lightToWorld.GetColumn(1));
                Vector3 zAxisVS = worldToView.MultiplyVector(lightToWorld.GetColumn(2));

                // Fill bounds
                var bound = new SFiniteLightBound();
                var lightVolumeData = new LightVolumeData();

                lightVolumeData.lightCategory = (uint)lightCategory;
                lightVolumeData.lightVolume = (uint)lightVolumeType;
                lightVolumeData.affectVolumetric = lightData.volumetricLightDimmer > 0.0f ? 1 : 0;

                if (gpuLightType == GPULightType.Spot || gpuLightType == GPULightType.ProjectorPyramid)
                {
                    Vector3 lightDir = lightToWorld.GetColumn(2);

                    // represents a left hand coordinate system in world space since det(worldToView)<0
                    Vector3 vx = xAxisVS;
                    Vector3 vy = yAxisVS;
                    Vector3 vz = zAxisVS;

                    var sa = light.spotAngle;
                    var cs = Mathf.Cos(0.5f * sa * Mathf.Deg2Rad);
                    var si = Mathf.Sin(0.5f * sa * Mathf.Deg2Rad);

                    if (gpuLightType == GPULightType.ProjectorPyramid)
                    {
                        Vector3 lightPosToProjWindowCorner = (0.5f * lightDimensions.x) * vx + (0.5f * lightDimensions.y) * vy + 1.0f * vz;
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
                    bound.center = worldToView.MultiplyPoint(positionWS + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                    // scale axis to match box or base of pyramid
                    bound.boxAxisX = (fS * range) * vx;
                    bound.boxAxisY = (fS * range) * vy;
                    bound.boxAxisZ = (0.5f * range) * vz;

                    // generate bounding sphere radius
                    var fAltDx = si;
                    var fAltDy = cs;
                    fAltDy = fAltDy - 0.5f;
                    //if(fAltDy<0) fAltDy=-fAltDy;

                    fAltDx *= range; fAltDy *= range;

                    // Handle case of pyramid with this select (currently unused)
                    var altDist = Mathf.Sqrt(fAltDy * fAltDy + (true ? 1.0f : 2.0f) * fAltDx * fAltDx);
                    bound.radius = altDist > (0.5f * range) ? altDist : (0.5f * range);       // will always pick fAltDist
                    bound.scaleXY = squeeze ? 0.01f : 1.0f;

                    lightVolumeData.lightAxisX = vx;
                    lightVolumeData.lightAxisY = vy;
                    lightVolumeData.lightAxisZ = vz;
                    lightVolumeData.lightPos = positionVS;
                    lightVolumeData.radiusSq = range * range;
                    lightVolumeData.cotan = cota;
                    lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
                }
                else if (gpuLightType == GPULightType.Point)
                {
                    // Construct a view-space axis-aligned bounding cube around the bounding sphere.
                    // This allows us to utilize the same polygon clipping technique for all lights.
                    // Non-axis-aligned vectors may result in a larger screen-space AABB.
                    Vector3 vx = new Vector3(1, 0, 0);
                    Vector3 vy = new Vector3(0, 1, 0);
                    Vector3 vz = new Vector3(0, 0, 1);

                    bound.center = positionVS;
                    bound.boxAxisX = vx * range;
                    bound.boxAxisY = vy * range;
                    bound.boxAxisZ = vz * range;
                    bound.scaleXY = 1.0f;
                    bound.radius = range;

                    // fill up ldata
                    lightVolumeData.lightAxisX = vx;
                    lightVolumeData.lightAxisY = vy;
                    lightVolumeData.lightAxisZ = vz;
                    lightVolumeData.lightPos = bound.center;
                    lightVolumeData.radiusSq = range * range;
                    lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
                }
                else if (gpuLightType == GPULightType.Tube)
                {
                    Vector3 dimensions = new Vector3(lightDimensions.x + 2 * range, 2 * range, 2 * range); // Omni-directional
                    Vector3 extents = 0.5f * dimensions;
                    Vector3 centerVS = positionVS;

                    bound.center = centerVS;
                    bound.boxAxisX = extents.x * xAxisVS;
                    bound.boxAxisY = extents.y * yAxisVS;
                    bound.boxAxisZ = extents.z * zAxisVS;
                    bound.radius = extents.x;
                    bound.scaleXY = 1.0f;

                    lightVolumeData.lightPos = centerVS;
                    lightVolumeData.lightAxisX = xAxisVS;
                    lightVolumeData.lightAxisY = yAxisVS;
                    lightVolumeData.lightAxisZ = zAxisVS;
                    lightVolumeData.boxInvRange.Set(1.0f / extents.x, 1.0f / extents.y, 1.0f / extents.z);
                    lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
                }
                else if (gpuLightType == GPULightType.Rectangle)
                {
                    Vector3 dimensions = new Vector3(lightDimensions.x + 2 * range, lightDimensions.y + 2 * range, range); // One-sided
                    Vector3 extents = 0.5f * dimensions;
                    Vector3 centerVS = positionVS + extents.z * zAxisVS;

                    float d = range + 0.5f * Mathf.Sqrt(lightDimensions.x * lightDimensions.x + lightDimensions.y * lightDimensions.y);

                    bound.center = centerVS;
                    bound.boxAxisX = extents.x * xAxisVS;
                    bound.boxAxisY = extents.y * yAxisVS;
                    bound.boxAxisZ = extents.z * zAxisVS;
                    bound.radius = Mathf.Sqrt(d * d + (0.5f * range) * (0.5f * range));
                    bound.scaleXY = 1.0f;

                    lightVolumeData.lightPos = centerVS;
                    lightVolumeData.lightAxisX = xAxisVS;
                    lightVolumeData.lightAxisY = yAxisVS;
                    lightVolumeData.lightAxisZ = zAxisVS;
                    lightVolumeData.boxInvRange.Set(1.0f / extents.x, 1.0f / extents.y, 1.0f / extents.z);
                    lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
                }
                else if (gpuLightType == GPULightType.ProjectorBox)
                {
                    Vector3 dimensions = new Vector3(lightDimensions.x, lightDimensions.y, range);  // One-sided
                    Vector3 extents = 0.5f * dimensions;
                    Vector3 centerVS = positionVS + extents.z * zAxisVS;

                    bound.center = centerVS;
                    bound.boxAxisX = extents.x * xAxisVS;
                    bound.boxAxisY = extents.y * yAxisVS;
                    bound.boxAxisZ = extents.z * zAxisVS;
                    bound.radius = extents.magnitude;
                    bound.scaleXY = 1.0f;

                    lightVolumeData.lightPos = centerVS;
                    lightVolumeData.lightAxisX = xAxisVS;
                    lightVolumeData.lightAxisY = yAxisVS;
                    lightVolumeData.lightAxisZ = zAxisVS;
                    lightVolumeData.boxInvRange.Set(1.0f / extents.x, 1.0f / extents.y, 1.0f / extents.z);
                    lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
                }
                else if (gpuLightType == GPULightType.Disc)
                {
                    //not supported at real time at the moment
                }
                else
                {
                    Debug.Assert(false, "TODO: encountered an unknown GPULightType.");
                }

#if DEBUG
                if (outputIndex < 0 || outputIndex >= outputLightBoundsCount)
                    throw new Exception("Trying to access an output index out of bounds. Output index is " + outputIndex + "and max length is " + outputLightBoundsCount);
#endif
                lightBounds[outputIndex] = bound;
                lightVolumes[outputIndex] = lightVolumeData;
            }


#if DEBUG
            [IgnoreWarning(1370)] //Ignore throwing exception warning.
#endif
            private void ConvertDirectionalLightToGPUFormat(
                int outputIndex, int lightIndex, LightCategory lightCategory, GPULightType gpuLightType, LightVolumeType lightVolumeType)
            {
                var light = visibleLights[lightIndex];
                var processedEntity = processedEntities[lightIndex];
                int dataIndex = processedEntity.dataIndex;
                var lightData = new DirectionalLightData();

                ref HDLightRenderData lightRenderData = ref GetLightData(dataIndex);
                lightData.lightLayers = GetLightLayer(globalConfig.lightLayersEnabled, lightRenderData);
                // Light direction for directional is opposite to the forward direction
                lightData.forward = light.GetForward();
                lightData.color = GetLightColor(light);

                // Caution: This is bad but if additionalData == HDUtils.s_DefaultHDAdditionalLightData it mean we are trying to promote legacy lights, which is the case for the preview for example, so we need to multiply by PI as legacy Unity do implicit divide by PI for direct intensity.
                // So we expect that all light with additionalData == HDUtils.s_DefaultHDAdditionalLightData are currently the one from the preview, light in scene MUST have additionalData
                lightData.color *= (defaultDataIndex == dataIndex) ? Mathf.PI : 1.0f;

                lightData.lightDimmer = lightRenderData.lightDimmer;
                lightData.diffuseDimmer = lightRenderData.affectDiffuse ? lightData.lightDimmer : 0;
                lightData.specularDimmer = lightRenderData.affectSpecular ? lightData.lightDimmer * globalConfig.specularGlobalDimmer : 0;
                lightData.volumetricLightDimmer = (lightRenderData.affectVolumetric ? lightRenderData.volumetricDimmer : 0.0f);

                lightData.shadowIndex = -1;
                lightData.screenSpaceShadowIndex = globalConfig.invalidScreenSpaceShadowIndex;
                lightData.isRayTracedContactShadow = 0.0f;

                // Rescale for cookies and windowing.
                lightData.right = light.GetRight() * 2 / Mathf.Max(lightRenderData.shapeWidth, 0.001f);
                lightData.up = light.GetUp() * 2 / Mathf.Max(lightRenderData.shapeHeight, 0.001f);
                lightData.positionRWS = light.GetPosition();
                lightData.shadowDimmer = lightRenderData.shadowDimmer;

                var volumetricShadowDimmerVal = lightRenderData.affectVolumetric ? lightRenderData.volumetricShadowDimmer : 0.0f;
                lightData.volumetricShadowDimmer = volumetricShadowDimmerVal;

                // We want to have a colored penumbra if the flag is on and the color is not gray
                var shadowTintValue = lightRenderData.shadowTint;
                bool penumbraTintValue = lightRenderData.penumbraTint && ((shadowTintValue.r != shadowTintValue.g) || (shadowTintValue.g != shadowTintValue.b));
                lightData.penumbraTint = penumbraTintValue ? 1.0f : 0.0f;
                if (penumbraTintValue)
                    lightData.shadowTint = new Vector3(shadowTintValue.r * shadowTintValue.r, shadowTintValue.g * shadowTintValue.g, shadowTintValue.b * shadowTintValue.b);
                else
                    lightData.shadowTint = new Vector3(shadowTintValue.r, shadowTintValue.g, shadowTintValue.b);

                //Value of max smoothness is derived from AngularDiameter. Formula results from eyeballing. Angular diameter of 0 results in 1 and angular diameter of 80 results in 0.
                float maxSmoothness = Mathf.Clamp01(1.35f / (1.0f + Mathf.Pow(1.15f * (0.0315f * lightRenderData.angularDiameter + 0.4f), 2f)) - 0.11f);
                // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
                lightData.minRoughness = (1.0f - maxSmoothness) * (1.0f - maxSmoothness);

                lightData.shadowMaskSelector = Vector4.zero;

                if (processedEntity.isBakedShadowMask)
                {
                    var bakingOutput = visibleLightBakingOutput[lightIndex];
                    lightData.shadowMaskSelector[bakingOutput.occlusionMaskChannel] = 1.0f;
                    lightData.nonLightMappedOnly = visibleLightShadowCasterMode[lightIndex] == LightShadowCasterMode.NonLightmappedOnly ? 1 : 0;
                }
                else
                {
                    // use -1 to say that we don't use shadow mask
                    lightData.shadowMaskSelector.x = -1.0f;
                    lightData.nonLightMappedOnly = 0;
                }

                lightData.angularDiameter = lightRenderData.angularDiameter * Mathf.Deg2Rad;
                lightData.distanceFromCamera = (isPbrSkyActive && lightRenderData.interactsWithSky) ? lightRenderData.distance : -1;

                if (useCameraRelativePosition)
                    lightData.positionRWS -= cameraPos;

                IncrementCounter(HDGpuLightsBuilder.GPULightTypeCountSlots.Directional);

#if DEBUG
                if (outputIndex < 0 || outputIndex >= outputDirectionalLightCounts)
                    throw new Exception("Trying to access an output index out of bounds. Output index is " + outputIndex + "and max length is " + outputLightCounts);
#endif

                directionalLights[outputIndex] = lightData;
            }

            public void Execute(int index)
            {
                var sortKey = sortKeys[index];
                HDGpuLightsBuilder.UnpackLightSortKey(sortKey, out var lightCategory, out var gpuLightType, out var lightVolumeType, out var lightIndex, out var offscreen);

                if (gpuLightType == GPULightType.Directional)
                {
                    int outputIndex = index;
                    ConvertDirectionalLightToGPUFormat(outputIndex, lightIndex, lightCategory, gpuLightType, lightVolumeType);
                }
                else
                {
                    int outputIndex = index - directionalSortedLightCounts;
                    StoreAndConvertLightToGPUFormat(outputIndex, lightIndex, lightCategory, gpuLightType, lightVolumeType, offscreen);
                }
            }
        }

        public void StartCreateGpuLightDataJob(
            HDCamera hdCamera,
            in CullingResults cullingResult,
            HDShadowSettings hdShadowSettings,
            HDProcessedVisibleLightsBuilder visibleLights,
            HDLightRenderDatabase lightEntities)
        {
            var visualEnvironment = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
            var shadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();
            Debug.Assert(visualEnvironment != null);
            bool isPbrSkyActive = visualEnvironment.skyType.value == (int)SkyType.PhysicallyBased;

            var createGpuLightDataJob = new CreateGpuLightDataJob()
            {
                //Parameters
                totalLightCounts = lightEntities.lightCount,
                outputLightCounts = m_LightCount,
                outputDirectionalLightCounts = m_DirectionalLightCount,
                outputLightBoundsCount = m_LightBoundsCount,
                globalConfig = CreateGpuLightDataJobGlobalConfig.Create(hdCamera, hdShadowSettings),
                cameraPos = hdCamera.mainViewConstants.worldSpaceCameraPos,
                directionalSortedLightCounts = visibleLights.sortedDirectionalLightCounts,
                isPbrSkyActive = isPbrSkyActive,
                defaultDataIndex = lightEntities.GetEntityDataIndex(lightEntities.GetDefaultLightEntity()),
                viewCounts = hdCamera.viewCount,
                useCameraRelativePosition = ShaderConfig.s_CameraRelativeRendering != 0,
                maxShadowDistance = shadowSettings.maxShadowDistance.value,

                // light entity data
                lightRenderDataArray = lightEntities.lightData,

                //visible lights processed
                sortKeys = visibleLights.sortKeys,
                processedEntities = visibleLights.processedEntities,
                visibleLights = cullingResult.visibleLights,
                visibleLightBakingOutput = visibleLights.visibleLightBakingOutput,
                visibleLightShadowCasterMode = visibleLights.visibleLightShadowCasterMode,

                //outputs
                gpuLightCounters = m_LightTypeCounters,
                lights = m_Lights,
                directionalLights = m_DirectionalLights,
                lightsPerView = m_LightsPerView,
                lightBounds = m_LightBounds,
                lightVolumes = m_LightVolumes
            };

            m_CreateGpuLightDataJobHandle = createGpuLightDataJob.Schedule(visibleLights.sortedLightCounts, 32);
        }

        public void CompleteGpuLightDataJob()
        {
            m_CreateGpuLightDataJobHandle.Complete();
        }
    }
}
