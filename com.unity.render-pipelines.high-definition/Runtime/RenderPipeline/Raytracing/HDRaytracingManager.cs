using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingManager
    {
        // The list of raytracing environments that have been registered
        List<HDRaytracingEnvironment> m_Environments = null;
        RayCountManager m_RayCountManager = new RayCountManager();
        public RayCountManager rayCountManager
        {
            get
            {
                return m_RayCountManager;
            }
        }

        // Flag that defines if we should rebuild everything (when adding or removing an environment)
        bool m_DirtyEnvironment = false;

        public void RegisterEnvironment(HDRaytracingEnvironment targetEnvironment)
        {
            if(!m_Environments.Contains(targetEnvironment))
            {
                m_Environments.Add(targetEnvironment);
                m_DirtyEnvironment = true;
            }
        }

        public void UnregisterEnvironment(HDRaytracingEnvironment targetEnvironment)
        {
            if (m_Environments.Contains(targetEnvironment))
            {
                // Add this graph
                m_Environments.Remove(targetEnvironment);
                m_DirtyEnvironment = true;
            }
        }

        // This class holds everything regarding the state of the ray tracing structure of a sub scene filtered by a layermask
        public class HDRayTracingSubScene
        {
            // The mask that defines which part of the sub-scene is targeted by this
            public LayerMask mask = -1;

            // The native acceleration structure that matches this sub-scene
            public RaytracingAccelerationStructure accelerationStructure = null;

            // The list of renderers in the sub-scene
            public List<Renderer> targetRenderers = null;

            // The list of non-directional lights in the sub-scene
            public List<HDAdditionalLightData> hdLightArray = null;

            // The list of directional lights in the sub-scene
            public List<HDAdditionalLightData> hdDirectionalLightArray = null;

            // The list of ray-tracing graphs that reference this sub-scene
            public List<HDRayTracingFilter> referenceFilters = new List<HDRayTracingFilter>();

            // Flag that defines if this sub-scene should be persistent even if there isn't any explicit graph referencing it
            public bool persistent = false;

            // Flag that defines if this sub-scene should be re-evaluated
            public bool obsolete = false;

            // Flag that defines if this sub-scene is valid
            public bool valid = false;
        }

        // The list of graphs that have been referenced
        List<HDRayTracingFilter> m_Filters = null;

        // The list of sub-scenes that exist
        Dictionary<int, HDRayTracingSubScene> m_SubScenes = null;

        // The list of layer masks that exist
        List<int> m_LayerMasks = null;

        // The HDRPAsset data that needs to be 
        RenderPipelineResources m_Resources = null;
        RenderPipelineSettings m_Settings;

        // Noise texture manager
        BlueNoise m_BlueNoise = null;

        public void RegisterFilter(HDRayTracingFilter targetFilter)
        {
            if(!m_Filters.Contains(targetFilter))
            {
                // Add this graph
                m_Filters.Add(targetFilter);

                // Try to get the sub-scene
                HDRayTracingSubScene currentSubScene = null;
                if (!m_SubScenes.TryGetValue(targetFilter.layermask.value, out currentSubScene))
                {
                    // Create the ray-tracing sub-scene
                    currentSubScene = new HDRayTracingSubScene();
                    currentSubScene.mask = targetFilter.layermask.value;

                    // If this is a new graph, we need to build its data
                    BuildSubSceneStructure(ref currentSubScene);

                    // register this sub-scene and this layer mask
                    m_SubScenes.Add(targetFilter.layermask.value, currentSubScene);
                    m_LayerMasks.Add(targetFilter.layermask.value);
                }

                // Add this graph to the reference graphs
                currentSubScene.referenceFilters.Add(targetFilter);
            }
        }

        public void UnregisterFilter(HDRayTracingFilter targetFilter)
        {
            if (m_Filters.Contains(targetFilter))
            {
                // Add this graph
                m_Filters.Remove(targetFilter);

                // Match the sub-matching sub-scene
                HDRayTracingSubScene currentSubScene = null;
                if (m_SubScenes.TryGetValue(targetFilter.layermask.value, out currentSubScene))
                {
                    // Remove the reference to this graph
                    currentSubScene.referenceFilters.Remove(targetFilter);

                    // Is there is no one referencing this sub-scene and it is not persistent, then we need to delete its
                    if (currentSubScene.referenceFilters.Count == 0 && !currentSubScene.persistent)
                    {
                        // If this is a new graph, we need to build its data
                        DestroySubSceneStructure(ref currentSubScene);

                        // Remove it from the list of the sub-scenes
                        m_SubScenes.Remove(targetFilter.layermask.value);
                        m_LayerMasks.Remove(targetFilter.layermask.value);
                    }
                }
            }
        }

        public void Init(RenderPipelineSettings settings, RenderPipelineResources resources, BlueNoise blueNoise)
        {
            // Keep track of the resources
            m_Resources = resources;

            // Keep track of the settings
            m_Settings = settings;

            // Keep track of the blue noise manager
            m_BlueNoise = blueNoise;

            // Create the list of environments
            m_Environments = new List<HDRaytracingEnvironment>();

            // Grab all the ray-tracing graphs that have been created before (in case the order of initialization has not been respected, which happens when we open unity the first time)
            HDRaytracingEnvironment[] environmentArray = Object.FindObjectsOfType<HDRaytracingEnvironment>();
            for (int envIdx = 0; envIdx < environmentArray.Length; ++envIdx)
            {
                RegisterEnvironment(environmentArray[envIdx]);
            }

            // keep track of all the graphs that are to be supported
            m_Filters = new List<HDRayTracingFilter>();

            // Create the sub-scenes structure
            m_SubScenes = new Dictionary<int, HDRayTracingSubScene>();

            // The list of masks that are currently requested
            m_LayerMasks = new List<int>();

            // Let's start by building the "default" sub-scene (used by the scene camera)
            HDRayTracingSubScene defaultSubScene = new HDRayTracingSubScene();
            defaultSubScene.mask = m_Settings.editorRaytracingFilterLayerMask.value;
            defaultSubScene.persistent = true;
            BuildSubSceneStructure(ref defaultSubScene);
            m_SubScenes.Add(m_Settings.editorRaytracingFilterLayerMask.value, defaultSubScene);
            m_LayerMasks.Add(m_Settings.editorRaytracingFilterLayerMask.value);

            // Grab all the ray-tracing graphs that have been created before
            HDRayTracingFilter[] filterArray = Object.FindObjectsOfType<HDRayTracingFilter>();
            for(int filterIdx = 0; filterIdx < filterArray.Length; ++filterIdx)
            {
                RegisterFilter(filterArray[filterIdx]);
            }

            m_RayCountManager.Init(resources);

#if UNITY_EDITOR
            // We need to invalidate the acceleration structures in case the hierarchy changed
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
#endif
        }
#if UNITY_EDITOR

        static void OnHierarchyChanged()
        {
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                hdPipeline.m_RayTracingManager.SetDirty();
            }
        }
#endif

        public void SetDirty()
        {
            int numFilters = m_Filters.Count;
            for(int filterIdx = 0; filterIdx < numFilters; ++filterIdx)
            {
                // Grab the target graph component
                HDRayTracingFilter filterComponent = m_Filters[filterIdx];
                
                // If this camera had a graph component had an obsolete flag
                if(filterComponent != null)
                {
                    filterComponent.SetDirty();
                }
            }
        }

        public void Release()
        {
            for (var subSceneIndex = 0; subSceneIndex < m_LayerMasks.Count; subSceneIndex++)
            {
                HDRayTracingSubScene currentSubScene = m_SubScenes[m_LayerMasks[subSceneIndex]];
                DestroySubSceneStructure(ref currentSubScene);
            }
            m_RayCountManager.Release();
        }

        public void DestroySubSceneStructure(ref HDRayTracingSubScene subScene)
        {
            if (subScene.accelerationStructure != null)
            {
                subScene.accelerationStructure.Dispose();
                subScene.targetRenderers = null;
                subScene.accelerationStructure = null;
                subScene.hdLightArray = null;
            }
        }
        public void UpdateAccelerationStructures()
        {
            // Here there is two options, either the full things needs to be rebuilded or we should only rebuild the ones that have been flagged obsolete
            if(m_DirtyEnvironment)
            {
                // First of let's reset all the obsolescence flags
                int numFilters = m_Filters.Count;
                for(int filterIdx = 0; filterIdx < numFilters; ++filterIdx)
                {
                    // Grab the target graph component
                    HDRayTracingFilter filterComponent = m_Filters[filterIdx];
                    
                    // If this camera had a graph component had an obsolete flag
                    if(filterComponent != null)
                    {
                        filterComponent.ResetDirty();
                    }
                }

                // Also let's mark all the sub-scenes as obsolete
                for (var subSceneIndex = 0; subSceneIndex < m_LayerMasks.Count; subSceneIndex++)
                {
                    HDRayTracingSubScene currentSubScene = m_SubScenes[m_LayerMasks[subSceneIndex]];
                    currentSubScene.obsolete = true;
                }
                m_DirtyEnvironment = false;
            }
            else
            {
                // First of all propagate the obsolete flags to the sub scenes
                int numGraphs = m_Filters.Count;
                for(int filterIdx = 0; filterIdx < numGraphs; ++filterIdx)
                {
                    // Grab the target graph component
                    HDRayTracingFilter filterComponent = m_Filters[filterIdx];
                    
                    // If this camera had a graph component had an obsolete flag
                    if(filterComponent != null && filterComponent.IsDirty())
                    {
                        // Get the sub-scene  that matches
                        HDRayTracingSubScene currentSubScene = null;
                        if (m_SubScenes.TryGetValue(filterComponent.layermask, out currentSubScene))
                        {
                            currentSubScene.obsolete = true;
                        }
                        filterComponent.ResetDirty();
                    }
                }
            }
 

            // Rebuild all the obsolete scenes
            for (var subSceneIndex = 0; subSceneIndex < m_LayerMasks.Count; subSceneIndex++)
            {
                // Grab the current sub-scene
                HDRayTracingSubScene subScene = m_SubScenes[m_LayerMasks[subSceneIndex]];

                // Does this scene need rebuilding?
                if (subScene.obsolete)
                {
                    DestroySubSceneStructure(ref subScene);
                    BuildSubSceneStructure(ref subScene);
                    subScene.obsolete = false;
                }
            }

            // Update all the transforms
            for (var subSceneIndex = 0; subSceneIndex < m_LayerMasks.Count; subSceneIndex++)
            {
                HDRayTracingSubScene subScene = m_SubScenes[m_LayerMasks[subSceneIndex]];
                if (subScene.accelerationStructure != null)
                {
                    for (var i = 0; i < subScene.targetRenderers.Count; i++)
                    {
                        if (subScene.targetRenderers[i] != null)
                        {
                            subScene.accelerationStructure.UpdateInstanceTransform(subScene.targetRenderers[i]);
                        }
                    }
                    subScene.accelerationStructure.Update();
                }
            }
        }

        public void BuildSubSceneStructure(ref HDRayTracingSubScene subScene)
        {
            // If there is no render environments, then we should not generate acceleration structure
            if(m_Environments.Count > 0)
            {
                // This structure references all the renderers that are considered to be processed
                Dictionary<int, int> rendererReference = new Dictionary<int, int>();

                // Destroy the acceleration structure
                subScene.targetRenderers = new List<Renderer>();

                // Create the acceleration structure
                subScene.accelerationStructure = new RaytracingAccelerationStructure();

                // First of all let's process all the LOD groups
                LODGroup[] lodGroupArray = UnityEngine.GameObject.FindObjectsOfType<LODGroup>();
                for (var i = 0; i < lodGroupArray.Length; i++)
                {
                    // Grab the current LOD group
                    LODGroup lodGroup = lodGroupArray[i];

                    // Get the set of LODs
                    LOD[] lodArray = lodGroup.GetLODs();
                    for(int lodIdx = 0; lodIdx < lodArray.Length; ++lodIdx)
                    {
                        LOD currentLOD = lodArray[lodIdx];
                        // We only want to push to the acceleration structure the first fella
                        if (lodIdx == 0)
                        {
                            for(int rendererIdx = 0; rendererIdx < currentLOD.renderers.Length; ++rendererIdx)
                            {
                                // Convert the object's layer to an int
                                int objectLayerValue = 1 << currentLOD.renderers[rendererIdx].gameObject.layer;

                                // Is this object in one of the allowed layers ?
                                if ((objectLayerValue & subScene.mask.value) != 0)
                                {
                                    // Add this fella to the renderer list
                                    subScene.targetRenderers.Add(currentLOD.renderers[rendererIdx]);
                                }
                            }
                        }

                        // Add them to the processed set
                        for (int rendererIdx = 0; rendererIdx < currentLOD.renderers.Length; ++rendererIdx)
                        {
                            Renderer currentRenderer = currentLOD.renderers[rendererIdx];
                            // Add this fella to the renderer list
                            rendererReference.Add(currentRenderer.GetInstanceID(), 1);
                        }
                        
                    }
                }

                int maxNumSubMeshes = 1;

                // Grab all the renderers from the scene
                var rendererArray = UnityEngine.GameObject.FindObjectsOfType<Renderer>();
                for (var i = 0; i < rendererArray.Length; i++)
                {
                    // Fetch the current renderer
                    Renderer currentRenderer =  rendererArray[i];

                    // If it is not active skip it
                    if(currentRenderer.enabled ==  false) continue;

                    // Grab the current game object
                    GameObject gameObject = currentRenderer.gameObject;

                    // Has this object already been processed, jsut skip
                    if(rendererReference.ContainsKey(currentRenderer.GetInstanceID()))
                    {
                        continue;
                    }

                    // Does this object have a reflection probe component? if yes we do not want to see it in the raytracing environment
                    ReflectionProbe targetProbe = gameObject.GetComponent<ReflectionProbe>();
                    if (targetProbe != null) continue;

                    // Convert the object's layer to an int
                    int objectLayerValue = 1 << currentRenderer.gameObject.layer;

                    // Is this object in one of the allowed layers ?
                    if ((objectLayerValue & subScene.mask.value) != 0)
                    {
                        // Add this fella to the renderer list
                        subScene.targetRenderers.Add(currentRenderer);
                    }

                    // Also, we need to contribute to the maximal number of submeshes
                    MeshFilter currentFilter = currentRenderer.GetComponent<MeshFilter>();
                    maxNumSubMeshes = Mathf.Max(maxNumSubMeshes, currentFilter.sharedMesh.subMeshCount);
                }

                bool[] subMeshFlagArray = new bool[maxNumSubMeshes];
                bool[] subMeshCutoffArray = new bool[maxNumSubMeshes];

                // If any object build the acceleration structure
                if (subScene.targetRenderers.Count != 0)
                {
                    // For all the renderers that we need to push in the acceleration structure
                    for (var i = 0; i < subScene.targetRenderers.Count; i++)
                    {
                        // Grab the current renderer
                        Renderer currentRenderer = subScene.targetRenderers[i];
                        bool singleSided = false;
                        if (currentRenderer.sharedMaterials != null)
                        {
                            // For every sub-mesh/sub-material let's build the right flags
                            int numSubMeshes = currentRenderer.sharedMaterials.Length;

                            for(int meshIdx = 0; meshIdx < numSubMeshes; ++meshIdx)
                            {
                                Material currentMaterial = currentRenderer.sharedMaterials[meshIdx];
                                bool materialIsTransparent = currentMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
                                if(currentMaterial != null)
                                {
                                    subMeshFlagArray[meshIdx] = true; // !currentMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
                                    subMeshCutoffArray[meshIdx] = currentMaterial.IsKeywordEnabled("_ALPHATEST_ON");
                                    singleSided |= !currentMaterial.IsKeywordEnabled("_DOUBLESIDED_ON");
                                }
                                else
                                {
                                    singleSided = true;
                                    subMeshCutoffArray[meshIdx] = false;
                                    subMeshFlagArray[meshIdx] = false;
                                }
                            }

                            // Add it to the acceleration structure
                            subScene.accelerationStructure.AddInstance(currentRenderer, subMeshMask: subMeshFlagArray, subMeshTransparencyFlags: subMeshCutoffArray, enableTriangleCulling: singleSided);
                        }
                    }
                }

                // build the acceleration structure
                subScene.accelerationStructure.Build();

                // Allocate the array for the lights
                subScene.hdLightArray = new List<HDAdditionalLightData>();
                subScene.hdDirectionalLightArray = new List<HDAdditionalLightData>();

                // fetch all the hdrp lights
                HDAdditionalLightData[] hdLightArray = UnityEngine.GameObject.FindObjectsOfType<HDAdditionalLightData>();

                // Here an important thing is to make sure that the point lights are first in the list then line then area
                List<HDAdditionalLightData> pointLights = new List<HDAdditionalLightData>();
                List<HDAdditionalLightData> lineLights = new List<HDAdditionalLightData>();
                List<HDAdditionalLightData> rectLights = new List<HDAdditionalLightData>();

                for (int lightIdx = 0; lightIdx < hdLightArray.Length; ++lightIdx)
                {
                    HDAdditionalLightData hdLight = hdLightArray[lightIdx];
                    if (hdLight.enabled )
                    {
                        // Convert the object's layer to an int
                        int lightayerValue = 1 << hdLight.gameObject.layer;
                        if ((lightayerValue & subScene.mask.value) != 0)
                        {
                            if(hdLight.GetComponent<Light>().type == LightType.Directional)
                            {
                                subScene.hdDirectionalLightArray.Add(hdLight);
                            }
                            else
                            {
                                if(hdLight.lightTypeExtent == LightTypeExtent.Punctual)
                                {
                                    pointLights.Add(hdLight);
                                }
                                else if (hdLight.lightTypeExtent == LightTypeExtent.Tube)
                                {
                                    lineLights.Add(hdLight);
                                }
                                else
                                {
                                    rectLights.Add(hdLight);

                                }
                            }
                        }
                    }
                }

                subScene.hdLightArray.AddRange(pointLights);
                subScene.hdLightArray.AddRange(lineLights);
                subScene.hdLightArray.AddRange(rectLights);

                // Mark this sub-scene as valid
                subScene.valid = true;
            }
            else
            {
                subScene.valid = false;
            }
        }

        public RaytracingAccelerationStructure RequestAccelerationStructure(HDCamera hdCamera)
        {
            bool editorCamera = hdCamera.camera.cameraType == CameraType.SceneView || hdCamera.camera.cameraType == CameraType.Preview;
            if (editorCamera)
            {
                // For the scene view, we want to use the default acceleration structure
                return RequestAccelerationStructure(m_Settings.editorRaytracingFilterLayerMask);
            }
            else
            {
                HDRayTracingFilter raytracingFilter = hdCamera.camera.gameObject.GetComponent<HDRayTracingFilter>();
                return raytracingFilter ? RequestAccelerationStructure(raytracingFilter.layermask) : null;
            }
        }

        public List<HDAdditionalLightData> RequestHDLightList(HDCamera hdCamera)
        {
            bool editorCamera = hdCamera.camera.cameraType == CameraType.SceneView || hdCamera.camera.cameraType == CameraType.Preview;
            if (editorCamera)
            {
                return RequestHDLightList(m_Settings.editorRaytracingFilterLayerMask);
            }
            else
            {
                HDRayTracingFilter raytracingFilter = hdCamera.camera.gameObject.GetComponent<HDRayTracingFilter>();
                return raytracingFilter ? RequestHDLightList(raytracingFilter.layermask) : null;
            }
        }

        public RaytracingAccelerationStructure RequestAccelerationStructure(LayerMask layerMask)
        {
            HDRayTracingSubScene currentSubScene = null;
            if (m_SubScenes.TryGetValue(layerMask.value, out currentSubScene))
            {
                return currentSubScene.valid ? currentSubScene.accelerationStructure : null;
            }
            return null;
        }

        public List<HDAdditionalLightData> RequestHDLightList(LayerMask layerMask)
        {
            HDRayTracingSubScene currentSubScene = null;
            if (m_SubScenes.TryGetValue(layerMask.value, out currentSubScene))
            {
                return currentSubScene.valid ? currentSubScene.hdLightArray : null;
            }
            return null;
        }

        // Returns a ray-tracing environment if any
        public HDRaytracingEnvironment CurrentEnvironment()
        {
            return m_Environments.Count != 0 ? m_Environments[m_Environments.Count - 1] : null;
        }

        public BlueNoise GetBlueNoiseManager()
        {
            return m_BlueNoise;
        }
    }
#endif
}
