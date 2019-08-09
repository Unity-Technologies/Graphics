using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Unity.Jobs;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {        
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
        }

        void DisposeRemappingArray()
        {
            m_VisibleLightRemapping.Dispose();
            m_SortIndexRemapping.Dispose();
        }
        
        void AllocateVolumeBoundsJobArrays(int lightCount, int decalCount, int numViews)
        {
            m_LightVolumeData = new NativeArray<LightVolumeData>((lightCount + decalCount) * numViews, Allocator.TempJob);
            m_Bounds = new NativeArray<SFiniteLightBound>((lightCount + decalCount) * numViews, Allocator.TempJob);
            m_LightCategory = new NativeArray<LightCategory>(lightCount, Allocator.TempJob);
            m_GpuLightType = new NativeArray<GPULightType>(lightCount, Allocator.TempJob);
            m_LightVolumeType = new NativeArray<LightVolumeType>(lightCount, Allocator.TempJob);
            m_LightData = new NativeArray<LightData>(lightCount, Allocator.TempJob);
            m_LightDimensions = new NativeArray<Vector3>(lightCount, Allocator.TempJob);
            m_WorldToView = new NativeArray<Matrix4x4>(numViews, Allocator.TempJob);
        }

        void DisposeBoundsJobArrays()
        {
            m_LightVolumeData.Dispose();
            m_Bounds.Dispose(); 
            m_LightCategory.Dispose(); 
            m_GpuLightType.Dispose(); 
            m_LightVolumeType.Dispose(); 
            m_LightData.Dispose();
            m_LightDimensions.Dispose(); 
            m_WorldToView.Dispose();
        }



        NativeArray<SFiniteLightBound> m_Bounds;
        NativeArray<LightVolumeData> m_LightVolumeData;
        NativeArray<LightCategory> m_LightCategory;
        NativeArray<GPULightType> m_GpuLightType;
        NativeArray<LightVolumeType> m_LightVolumeType;
        NativeArray<VisibleLight> m_VisibleLights;
        NativeArray<LightData> m_LightData;
        NativeArray<Vector3> m_LightDimensions;
        NativeArray<Matrix4x4> m_WorldToView;
        NativeArray<int> m_VisibleLightRemapping;
        NativeArray<int> m_SortIndexRemapping;

        LightVolumeBoundsJob m_LightVolumeBoundsJob;
    }
}

