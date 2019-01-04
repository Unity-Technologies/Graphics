using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingManager
    {
        // The list of raytracing environments that have been registered
        List<HDRaytracingEnvironment> m_Environments = null;

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

        // The set of resources
        RenderPipelineResources m_Resources = null;

        // Noise texture used for screen space sampling
        public Texture2DArray m_RGNoiseTexture = null;

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

        public void CheckNoiseTexture()
        {
            if (m_Resources.textures.rgNoiseTex0 != null && m_Resources.textures.rgNoiseTex1 != null && m_Resources.textures.rgNoiseTex2 != null && m_Resources.textures.rgNoiseTex3 != null && 
                m_Resources.textures.rgNoiseTex4 != null && m_Resources.textures.rgNoiseTex5 != null && m_Resources.textures.rgNoiseTex6 != null && m_Resources.textures.rgNoiseTex7 != null)
            {
                int textureResolution = m_Resources.textures.rgNoiseTex0.width;
                // Texture
                m_RGNoiseTexture = new Texture2DArray(textureResolution, textureResolution, 8, m_Resources.textures.rgNoiseTex0.format, false, true);

                Graphics.CopyTexture(m_Resources.textures.rgNoiseTex0, 0, 0, m_RGNoiseTexture, 0, 0);
                Graphics.CopyTexture(m_Resources.textures.rgNoiseTex1, 0, 0, m_RGNoiseTexture, 1, 0);
                Graphics.CopyTexture(m_Resources.textures.rgNoiseTex2, 0, 0, m_RGNoiseTexture, 2, 0);
                Graphics.CopyTexture(m_Resources.textures.rgNoiseTex3, 0, 0, m_RGNoiseTexture, 3, 0);
                Graphics.CopyTexture(m_Resources.textures.rgNoiseTex4, 0, 0, m_RGNoiseTexture, 4, 0);
                Graphics.CopyTexture(m_Resources.textures.rgNoiseTex5, 0, 0, m_RGNoiseTexture, 5, 0);
                Graphics.CopyTexture(m_Resources.textures.rgNoiseTex6, 0, 0, m_RGNoiseTexture, 6, 0);
                Graphics.CopyTexture(m_Resources.textures.rgNoiseTex7, 0, 0, m_RGNoiseTexture, 7, 0);
            }
        }


        public void Init(RenderPipelineSettings settings, RenderPipelineResources resources)
        {
            // Keep track of the resources
            m_Resources = resources;

            // Try create the noise texture
            CheckNoiseTexture();

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
            defaultSubScene.mask = settings.editorRaytracingFilterLayerMask.value;
            defaultSubScene.persistent = true;
            BuildSubSceneStructure(ref defaultSubScene);
            m_SubScenes.Add(settings.editorRaytracingFilterLayerMask.value, defaultSubScene);
            m_LayerMasks.Add(settings.editorRaytracingFilterLayerMask.value);

            // Grab all the ray-tracing graphs that have been created before
            HDRayTracingFilter[] filterArray = Object.FindObjectsOfType<HDRayTracingFilter>();
            for(int filterIdx = 0; filterIdx < filterArray.Length; ++filterIdx)
            {
                RegisterFilter(filterArray[filterIdx]);
            }
        }

        public void Release()
        {
            for (var subSceneIndex = 0; subSceneIndex < m_LayerMasks.Count; subSceneIndex++)
            {
                HDRayTracingSubScene currentSubScene = m_SubScenes[m_LayerMasks[subSceneIndex]];
                DestroySubSceneStructure(ref currentSubScene);
            }
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
            if(m_RGNoiseTexture == null)
            {
                CheckNoiseTexture();
            }

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
                }

                // If any object build the acceleration structure
                if (subScene.targetRenderers.Count != 0)
                {
                    for (var i = 0; i < subScene.targetRenderers.Count; i++)
                    {
                        Renderer currentRenderer = subScene.targetRenderers[i];
                        if(currentRenderer.sharedMaterial != null && !currentRenderer.sharedMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT") )
                        {
                            // Add it to the acceleration structure
                            subScene.accelerationStructure.AddInstance(currentRenderer);
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
    }
#endif
}
