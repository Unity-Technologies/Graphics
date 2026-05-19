using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor.U2D.Graphics.Profiler.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal.U2D.Profiler;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Graphics.Profiler
{
    class U2DGraphicsProfilerViewController : ProfilerModuleViewController
    {
        private U2DGraphicsProfilerView m_Root;

        public U2DGraphicsProfilerViewController(ProfilerWindow profilerWindow)
            : base(profilerWindow)
        {
            profilerWindow.SelectedFrameIndexChanged += OnProfilerFrameChange;
        }

        void OnProfilerFrameChange(long obj)
        {
#if ENABLE_PROFILER && PROFILER_INSTALLED
            if(Event.current != null && Event.current.type == EventType.Layout)
                return;

            if (obj < 0)
            {
                m_Root.SetHierarchyData(new[] { new List<U2DGraphicsHierarchyNodeData>(), new List<U2DGraphicsHierarchyNodeData>() });
                m_Root.SetStatistic(0,0,0,0,0,0,0,0,0,0,0);
                return;
            }
            CreateRootIfNotExist();

            if (UnityEngine.Profiling.Profiler.enabled && !m_Root.IsLiveUpdateEnabled())
                return;

            var selectedFrameIndexInt32 = Convert.ToInt32(obj);
            try
            {
                using (var frameData =
                       UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(selectedFrameIndexInt32, 0))
                {
                    // Extract shadow and light hierarchy data
                    List<U2DGraphicsHierarchyNodeData> shadowNodeList = new List<U2DGraphicsHierarchyNodeData>();
                    List<U2DGraphicsHierarchyNodeData> lightNodeList = new List<U2DGraphicsHierarchyNodeData>();

                    // Extract shadow data (tag 0)
                    var shadowData = frameData.GetFrameMetaData<MeshFrameData>(
                        ProfilerMarkers.k_2DGraphicProfilerProjectId,
                        (int)ProfilerMarkers.ProfilerFrameDataTag.ShadowFrameData);
                    Dictionary<EntityId, U2DGraphicsHierarchyNodeData> nodeDataDict =
                        new Dictionary<EntityId, U2DGraphicsHierarchyNodeData>();
                    if (shadowData.IsCreated)
                    {
                        for (int i = 0; i < shadowData.Length; i++)
                        {
                            var shadowCaster = shadowData[i];
                            if (shadowCaster.gameObjectEntityId != EntityId.None)
                            {
                                if (frameData.GetUnityObjectInfo(shadowCaster.gameObjectEntityId,
                                        out var unityObjectInfo))
                                {
                                    var node = new U2DGraphicsHierarchyNodeData(
                                        unityObjectInfo.name,
                                        shadowCaster.gameObjectEntityId,
                                        shadowCaster.triangleCount,
                                        shadowCaster.vertexCount,
                                        i,
                                        "") { drawCount = 0 };
                                    shadowNodeList.Add(node);
                                    nodeDataDict.Add(node.entityId, node);
                                }
                            }
                        }

                        shadowData.Dispose();
                    }

                    var shadowRenderCount = frameData.GetFrameMetaData<EntityId>(
                        ProfilerMarkers.k_2DGraphicProfilerProjectId,
                        (int)ProfilerMarkers.ProfilerFrameDataTag.ShadowRenderFrameData);
                    if (shadowRenderCount.IsCreated)
                    {
                        for (int i = 0; i < shadowRenderCount.Length; i++)
                        {
                            var entityId = shadowRenderCount[i];
                            if (entityId != EntityId.None)
                            {
                                if (nodeDataDict.TryGetValue(entityId, out var node))
                                {
                                    node.drawCount++;
                                }
                            }
                        }

                        shadowRenderCount.Dispose();
                    }

                    // Extract light data (tag 1)
                    var lightData = frameData.GetFrameMetaData<MeshFrameData>(
                        ProfilerMarkers.k_2DGraphicProfilerProjectId,
                        (int)ProfilerMarkers.ProfilerFrameDataTag.LightFrameData);
                    nodeDataDict.Clear();
                    if (lightData.IsCreated)
                    {
                        for (int i = 0; i < lightData.Length; i++)
                        {
                            var lightMesh = lightData[i];
                            if (lightMesh.gameObjectEntityId != EntityId.None)
                            {
                                if (frameData.GetUnityObjectInfo(lightMesh.gameObjectEntityId, out var unityObjectInfo))
                                {
                                    var node = new U2DGraphicsHierarchyNodeData(
                                        unityObjectInfo.name,
                                        lightMesh.gameObjectEntityId,
                                        lightMesh.triangleCount,
                                        lightMesh.vertexCount,
                                        i,
                                        "") { drawCount = 0 };
                                    lightNodeList.Add(node);
                                    nodeDataDict.Add(node.entityId, node);
                                }
                            }
                        }

                        lightData.Dispose();
                    }

                    var lightRenderCount = frameData.GetFrameMetaData<EntityId>(
                        ProfilerMarkers.k_2DGraphicProfilerProjectId,
                        (int)ProfilerMarkers.ProfilerFrameDataTag.LightRenderFrameData);
                    if (lightRenderCount.IsCreated)
                    {
                        for (int i = 0; i < lightRenderCount.Length; i++)
                        {
                            var entityId = lightRenderCount[i];
                            if (entityId != EntityId.None)
                            {
                                if (nodeDataDict.TryGetValue(entityId, out var node))
                                {
                                    node.drawCount++;
                                }
                            }
                        }

                        lightRenderCount.Dispose();
                    }

                    // Pass categorized data as array: [0] = shadow, [1] = light
                    m_Root.SetHierarchyData(new[] { shadowNodeList, lightNodeList });

                    // Extract counter values
                    var markerId = frameData.GetMarkerId(ProfilerMarkers.k_U2DNormalMapProfilerCounterName);
                    var normalTextures = frameData.GetCounterValueAsLong(markerId);

                    markerId = frameData.GetMarkerId(ProfilerMarkers.k_U2DLightProfilerCounterName);
                    var lightTextures = frameData.GetCounterValueAsLong(markerId);

                    markerId = frameData.GetMarkerId(ProfilerMarkers.k_U2DLightBatchCounterName);
                    var lightBatches = frameData.GetCounterValueAsLong(markerId);

                    markerId = frameData.GetMarkerId(ProfilerMarkers.k_U2DLightTriangleCounterName);
                    var lightTriangles = frameData.GetCounterValueAsLong(markerId);

                    markerId = frameData.GetMarkerId(ProfilerMarkers.k_U2DShadowProfilerCounterName);
                    var shadowTextures = frameData.GetCounterValueAsLong(markerId);

                    markerId = frameData.GetMarkerId(ProfilerMarkers.k_U2DShadowCasterCounterName);
                    var shadowCasters = frameData.GetCounterValueAsLong(markerId);

                    markerId = frameData.GetMarkerId(ProfilerMarkers.k_U2DShadowVerticesCounterName);
                    var shadowTriangles = frameData.GetCounterValueAsLong(markerId);

                    var renderPassMarkerID = frameData.GetMarkerId(ProfilerMarkers.s_RenderPass);
                    float renderPassTime = 0;
                    var drawShadowMarkerID = frameData.GetMarkerId(ProfilerMarkers.s_ShadowTexture);
                    float drawShadowTime = 0;
                    var normalPassMarkerID = frameData.GetMarkerId(ProfilerMarkers.s_MormalPass);
                    float normalPassTime = 0;
                    var shadowPasaMarkerID = frameData.GetMarkerId(ProfilerMarkers.s_ShadowPass);
                    float shadowPasaTime = 0;
                    int sampleCount = frameData.sampleCount;
                    for (int i = 0; i < sampleCount; ++i)
                    {
                        var sampleMarkerID = frameData.GetSampleMarkerId(i);
                        if (sampleMarkerID == renderPassMarkerID)
                        {
                            renderPassTime += frameData.GetSampleTimeMs(i);
                        }
                        else if (sampleMarkerID == drawShadowMarkerID)
                        {
                            drawShadowTime += frameData.GetSampleTimeMs(i);
                        }
                        else if (sampleMarkerID == normalPassMarkerID)
                        {
                            normalPassTime += frameData.GetSampleTimeMs(i);
                        }
                        else if (sampleMarkerID == shadowPasaMarkerID)
                        {
                            shadowPasaTime += frameData.GetSampleTimeMs(i);
                        }
                    }


                    m_Root.SetStatistic(normalTextures, lightTextures, lightBatches, lightTriangles,
                        shadowTextures, shadowCasters, shadowTriangles,
                        renderPassTime, drawShadowTime, normalPassTime, shadowPasaTime);
                }
            }
            catch (Exception)
            {
                //do nothing if any error happens during data extraction.
            }
#endif
        }

        U2DGraphicsProfilerView CreateRootIfNotExist()
        {
            if (m_Root == null)
            {
                m_Root = new U2DGraphicsProfilerView();
            }
            return m_Root;
        }

        protected override VisualElement CreateView()
        {
            CreateRootIfNotExist();
            OnProfilerFrameChange(ProfilerWindow.selectedFrameIndex);
            return m_Root;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && m_Root != null)
            {
                m_Root = null;
            }
            base.Dispose(disposing);
        }
    }
}
