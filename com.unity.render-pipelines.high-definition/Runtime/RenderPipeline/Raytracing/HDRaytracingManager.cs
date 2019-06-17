using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingManager
    {
        // The list of ray-tracing environments that have been registered
        List<HDRaytracingEnvironment> m_Environments = new List<HDRaytracingEnvironment>();

        // Flag that defines if we should rebuild everything (when adding or removing an environment)
        bool m_DirtyEnvironment = false;

        public void RegisterEnvironment(HDRaytracingEnvironment targetEnvironment)
        {
            if (!m_Environments.Contains(targetEnvironment))
            {
                // Add this env to the list of environments
                m_Environments.Add(targetEnvironment);
                m_DirtyEnvironment = true;

                // Now that a new environment has been set, we need to update
                UpdateEnvironmentSubScenes();
            }
        }

        public void UnregisterEnvironment(HDRaytracingEnvironment targetEnvironment)
        {
            if (m_Environments.Contains(targetEnvironment))
            {
                // Add this graph
                m_Environments.Remove(targetEnvironment);
                m_DirtyEnvironment = true;

                // Now that a new environment has been removed, we need to update
                UpdateEnvironmentSubScenes();
            }
        }

        // This class holds everything regarding the state of the ray tracing structure of a sub scene filtered by a layer mask
        public class HDRayTracingSubScene
        {
            // The mask that defines which part of the sub-scene is targeted by this
            public LayerMask mask = -1;

            // The native acceleration structure that matches this sub-scene
            public RayTracingAccelerationStructure accelerationStructure = null;

            // Flag that tracks if the acceleration needs to be updated
            public bool needUpdate = false;

            // The list of renderers in the sub-scene
            public List<Renderer> targetRenderers = null;

            // The list of non-directional lights in the sub-scene
            public List<HDAdditionalLightData> hdLightArray = null;

            // The list of directional lights in the sub-scene
            public List<HDAdditionalLightData> hdDirectionalLightArray = null;

            // Flag that defines if this sub-scene should be re-evaluated
            public bool obsolete = false;

            // Flag that defines if this sub-scene is valid
            public bool valid = false;

            // Light cluster used for some effects
            public HDRaytracingLightCluster lightCluster = null;

            // Integer that tracks the number of passes that reference this sub-scene
            public int references = 0;
        }

        // The current set of sub-scenes that we have
        Dictionary<int, HDRayTracingSubScene> m_SubScenes = new Dictionary<int, HDRayTracingSubScene>();

        // This list tracks for each effect of the current layer mask index that is assigned to them
        int[] m_EffectsMaks = new int[HDRaytracingEnvironment.numRaytracingPasses];

        // The HDRPAsset data that needs to be
        RenderPipelineResources m_Resources = null;
        HDRenderPipelineRayTracingResources m_RTResources = null;
        RenderPipelineSettings m_Settings;
        HDRenderPipeline m_RenderPipeline = null;
        SharedRTManager m_SharedRTManager = null;
        BlueNoise m_BlueNoise = null;

        // Denoisers
        HDSimpleDenoiser m_SimpleDenoiser = new HDSimpleDenoiser();

        // Ray-count manager data
        RayCountManager m_RayCountManager = new RayCountManager();
        public RayCountManager rayCountManager { get { return m_RayCountManager; } }

        public void Init(RenderPipelineSettings settings, RenderPipelineResources rpResources, HDRenderPipelineRayTracingResources rayTracingResources, BlueNoise blueNoise, HDRenderPipeline renderPipeline, SharedRTManager sharedRTManager, DebugDisplaySettings currentDebugDisplaySettings)
        {
            // Keep track of the resources
            m_Resources = rpResources;
            m_RTResources = rayTracingResources;

            // Keep track of the settings
            m_Settings = settings;

            // Keep track of the render pipeline
            m_RenderPipeline = renderPipeline;

            // Keep track of the shared RT manager
            m_SharedRTManager = sharedRTManager;

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

            // Init the simple denoiser
            m_SimpleDenoiser.Init(rayTracingResources, m_SharedRTManager);

            // Init the ray count manager
            m_RayCountManager.Init(rayTracingResources, currentDebugDisplaySettings);

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
            m_DirtyEnvironment = true;
        }

        public void Release()
        {
            // Destroy all the sub-scenes
            foreach (var subScene in m_SubScenes)
            {
                HDRayTracingSubScene currentSubScene = subScene.Value;
                DestroySubSceneStructure(ref currentSubScene);
            }

            // Clear the sub-scenes list
            m_SubScenes.Clear();

            m_SimpleDenoiser.Release();

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
                subScene.lightCluster.ReleaseResources();
                subScene.lightCluster = null;
            }
        }

        // This function is in charge of rebuilding the Subscenes in case the environment is flagged as obsolete
        public void CheckSubScenes()
        {
            // If the environment is dirty we needs to destroy and rebuild all the sub-scenes
            if (m_DirtyEnvironment)
            {
                // Let's lag all the sub-scenes as obsolete
                foreach (var subScene in m_SubScenes)
                {
                    subScene.Value.obsolete = true;
                }
                m_DirtyEnvironment = false;
            }

            // Rebuild all the obsolete scenes
            foreach (var subScenePair in m_SubScenes)
            {
                // Grab the current sub-scene
                HDRayTracingSubScene subScene = subScenePair.Value;

                // Does this scene need rebuilding?
                if (subScene.obsolete)
                {
                    DestroySubSceneStructure(ref subScene);
                    BuildSubSceneStructure(ref subScene);
                    subScene.obsolete = false;
                }
            }
        }

        void UpdateEffectSubScene(int layerMaskIndex, int effectIndex)
        {
            // Grab the previous sub-scene (try)
            HDRayTracingSubScene previousScene = null;
            bool previousSceneFound = m_SubScenes.TryGetValue(m_EffectsMaks[effectIndex], out previousScene);

            // Did the layer mask change for this effect ?
            if (layerMaskIndex != m_EffectsMaks[effectIndex])
            {
                // If the previous scene was allocated
                if(previousSceneFound)
                {
                    // Decrease the number of references for the old sub-scene
                    previousScene.references -= 1;
                }

                // Does the new sub-scene assigned to this effect already exist?
                HDRayTracingSubScene currentSubScene = null;
                if (!m_SubScenes.TryGetValue(layerMaskIndex, out currentSubScene))
                {
                    // Create the ray-tracing sub-scene
                    currentSubScene = new HDRayTracingSubScene();
                    currentSubScene.mask = layerMaskIndex;

                    // If this is a new graph, we need to build its data
                    BuildSubSceneStructure(ref currentSubScene);

                    // Push it to the list of sub-scenes
                    m_SubScenes.Add(layerMaskIndex, currentSubScene);
                }

                // Ok this sub-scene is already existing, let's simply track it and increase the number of references to it
                m_EffectsMaks[effectIndex] = layerMaskIndex;

                // Increase the number of references to this sub-scene
                currentSubScene.references += 1;
            }
            else
            {
                // Ok, it is the same value as it was before, but does this sub scene exist?
                if(!previousSceneFound)
                {
                    // Create the ray-tracing sub-scene
                    previousScene = new HDRayTracingSubScene();
                    previousScene.mask = layerMaskIndex;

                    // If this is a new graph, we need to build its data
                    BuildSubSceneStructure(ref previousScene);

                    // Push it to the list of sub-scenes
                    m_SubScenes.Add(layerMaskIndex, previousScene);

                    // Increase the number of references to this sub-scene
                    previousScene.references += 1;
                }
            }
        }

        // This function is to be called when the layers used for an effect have changed. It is called either through the inspector or using the scripting API
        public void UpdateEnvironmentSubScenes()
        {
            // Grab the current environment
            HDRaytracingEnvironment rtEnv = CurrentEnvironment();

            // We do not have any current environment, we need to clear all the subscenes we have and leave.
            if(rtEnv == null)
            {
                foreach (var subScene in m_SubScenes)
                {
                    // Destroy the sub-scene to remove
                    HDRayTracingSubScene currentSubscene = subScene.Value;
                    DestroySubSceneStructure(ref currentSubscene);
                }
                m_SubScenes.Clear();
                return;
            }

            // Update the references for the sub-scenes
            UpdateEffectSubScene(rtEnv.aoLayerMask.value, 0);
            UpdateEffectSubScene(rtEnv.reflLayerMask.value, 1);
            UpdateEffectSubScene(rtEnv.shadowLayerMask.value, 2);
            UpdateEffectSubScene(rtEnv.raytracedLayerMask.value, 3);
            UpdateEffectSubScene(rtEnv.indirectDiffuseLayerMask.value, 4);

            // Let's now go through all the sub-scenes and delete the ones that are not referenced by anyone
            var nonReferencedSubScenes = m_SubScenes.Where(x => x.Value.references == 0).ToArray();
            foreach(var subScene in nonReferencedSubScenes)
            {
                // Destroy the sub-scene to remove
                HDRayTracingSubScene currentSubscene = subScene.Value;
                DestroySubSceneStructure(ref currentSubscene);

                // Remove it from the array
                m_SubScenes.Remove(subScene.Key);
            }
        }

        // This function defines which acceleration structures are going to be used during the following frame
        // and updates their RAS
        public void UpdateFrameData()
        {
            // Set all the acceleration structures that are currently allocated to not updated
            foreach (var subScene in m_SubScenes)
            {
                subScene.Value.needUpdate = false;
            }

            // Grab the current environment
            HDRaytracingEnvironment rtEnv = CurrentEnvironment();
            if (rtEnv == null) return;

            // If AO is on flag its RAS needUpdate
            // if (rtEnv.raytracedAO)
            {
                HDRayTracingSubScene currentSubScene = RequestSubScene(rtEnv.aoLayerMask);
                currentSubScene.needUpdate = true;
            }

            // If Reflection is on flag its RAS needUpdate
            // if (rtEnv.raytracedReflections)
            {
                HDRayTracingSubScene currentSubScene = RequestSubScene(rtEnv.reflLayerMask);
                currentSubScene.needUpdate = true;
            }

            // If Area Shadow is on flag its RAS needUpdate
            //if (rtEnv.raytracedShadows)
            {
                HDRayTracingSubScene currentSubScene = RequestSubScene(rtEnv.shadowLayerMask);
                currentSubScene.needUpdate = true;
            }

            // If Primary Visibility is on flag its RAS needUpdate
            // if (rtEnv.raytracedObjects)
            {
                HDRayTracingSubScene currentSubScene = RequestSubScene(rtEnv.raytracedLayerMask);
                currentSubScene.needUpdate = true;
            }

            // If indirect diffuse is on flag its RAS needUpdate
            // if (rtEnv.raytracedIndirectDiffuse)
            {
                HDRayTracingSubScene currentSubScene = RequestSubScene(rtEnv.indirectDiffuseLayerMask);
                currentSubScene.needUpdate = true;
            }

            // Let's go through all the sub-scenes that are flagged needUpdate and update their RAS
            foreach (var subScene in m_SubScenes)
            {
                HDRayTracingSubScene currentSubScene = subScene.Value;
                if (currentSubScene.accelerationStructure != null && currentSubScene.needUpdate)
                {
                    for (var i = 0; i < currentSubScene.targetRenderers.Count; i++)
                    {
                        if (currentSubScene.targetRenderers[i] != null)
                        {
                            currentSubScene.accelerationStructure.UpdateInstanceTransform(currentSubScene.targetRenderers[i]);
                        }
                    }
                    currentSubScene.accelerationStructure.Update();

                    // It doesn't need RAS update anymore
                    currentSubScene.needUpdate = false;
                }
            }
        }

        // This function finds which subscenes are going to be used for the camera and computes their light clusters
        public void UpdateCameraData(CommandBuffer cmd, HDCamera hdCamera)
        {
            // Set all the acceleration structures that are currently allocated to not updated
            foreach (var subScene in m_SubScenes)
            {
                subScene.Value.needUpdate = false;
            }

            // Grab the current environment
            HDRaytracingEnvironment rtEnv = CurrentEnvironment();
            if (rtEnv == null) return;

            // If Reflection is on flag its light cluster
            // if (rtEnv.raytracedReflections)
            {
                HDRayTracingSubScene currentSubScene = RequestSubScene(rtEnv.reflLayerMask);
                currentSubScene.needUpdate = true;
            }

            // If Primary Visibility is on flag its light cluster
            // if (rtEnv.raytracedObjects)
            {
                HDRayTracingSubScene currentSubScene = RequestSubScene(rtEnv.raytracedLayerMask);
                currentSubScene.needUpdate = true;
            }

            // If indirect diffuse is on flag its light cluster
            // if (rtEnv.raytracedIndirectDiffuse)
            {
                HDRayTracingSubScene currentSubScene = RequestSubScene(rtEnv.indirectDiffuseLayerMask);
                currentSubScene.needUpdate = true;
            }

            // Let's go through all the sub-scenes that are flagged needUpdate and update their light clusters
            foreach (var subScene in m_SubScenes)
            {
                HDRayTracingSubScene currentSubScene = subScene.Value;

                // If it need update, go through it
                if (currentSubScene.needUpdate)
                {
                    // Evaluate the light cluster
                    currentSubScene.lightCluster.EvaluateLightClusters(cmd, hdCamera, currentSubScene.hdLightArray);

                    // It doesn't need RAS update anymore
                    currentSubScene.needUpdate = false;
                }
            }
        }

        public void BuildSubSceneStructure(ref HDRayTracingSubScene subScene)
        {
            // If there is no render environments, then we should not generate acceleration structure
            if (m_Environments.Count > 0)
            {
                // This structure references all the renderers that are considered to be processed
                Dictionary<int, int> rendererReference = new Dictionary<int, int>();

                // Destroy the acceleration structure
                subScene.targetRenderers = new List<Renderer>();

                // Create the acceleration structure
                subScene.accelerationStructure = new RayTracingAccelerationStructure();

                // First of all let's process all the LOD groups
                LODGroup[] lodGroupArray = UnityEngine.GameObject.FindObjectsOfType<LODGroup>();
                for (var i = 0; i < lodGroupArray.Length; i++)
                {
                    // Grab the current LOD group
                    LODGroup lodGroup = lodGroupArray[i];

                    // Get the set of LODs
                    LOD[] lodArray = lodGroup.GetLODs();
                    for (int lodIdx = 0; lodIdx < lodArray.Length; ++lodIdx)
                    {
                        LOD currentLOD = lodArray[lodIdx];
                        // We only want to push to the acceleration structure the first fella
                        if (lodIdx == 0)
                        {
                            for (int rendererIdx = 0; rendererIdx < currentLOD.renderers.Length; ++rendererIdx)
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
                    Renderer currentRenderer = rendererArray[i];

                    // If it is not active skip it
                    if (currentRenderer.enabled == false) continue;

                    // Grab the current game object
                    GameObject gameObject = currentRenderer.gameObject;

                    // Has this object already been processed, jsut skip
                    if (rendererReference.ContainsKey(currentRenderer.GetInstanceID()))
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
                    if (currentFilter != null && currentFilter.sharedMesh != null)
                    {
                        maxNumSubMeshes = Mathf.Max(maxNumSubMeshes, currentFilter.sharedMesh.subMeshCount);
                    }
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

                            uint instanceFlag = 0xff;
                            for (int meshIdx = 0; meshIdx < numSubMeshes; ++meshIdx)
                            {
                                Material currentMaterial = currentRenderer.sharedMaterials[meshIdx];
                                // The material is transparent if either it has the requested keyword or is in the transparent queue range
                                if (currentMaterial != null)
                                {
                                    subMeshFlagArray[meshIdx] = true;

                                    // Is the material transparent?
                                    bool materialIsTransparent = currentMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                                    || (HDRenderQueue.k_RenderQueue_Transparent.lowerBound <= currentMaterial.renderQueue
                                    && HDRenderQueue.k_RenderQueue_Transparent.upperBound >= currentMaterial.renderQueue)
                                    || (HDRenderQueue.k_RenderQueue_AllTransparentRaytracing.lowerBound <= currentMaterial.renderQueue
                                    && HDRenderQueue.k_RenderQueue_AllTransparentRaytracing.upperBound >= currentMaterial.renderQueue);

                                    // Propagate the right mask
                                    instanceFlag = materialIsTransparent ? (uint)0xf0 : (uint)0x0f;

                                    // Is the material alpha tested?
                                    subMeshCutoffArray[meshIdx] = currentMaterial.IsKeywordEnabled("_ALPHATEST_ON")
                                    || (HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.lowerBound <= currentMaterial.renderQueue
                                    && HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.upperBound >= currentMaterial.renderQueue);

                                    // Force it to be non single sided if it has the keyword if there is a reason
                                    bool doubleSided = currentMaterial.doubleSidedGI || currentMaterial.IsKeywordEnabled("_DOUBLESIDED_ON");
                                    singleSided |= !doubleSided;
                                }
                                else
                                {
                                    subMeshFlagArray[meshIdx] = false;
                                    subMeshCutoffArray[meshIdx] = false;
                                    singleSided = true;
                                }
                            }

                            // Add it to the acceleration structure
                            subScene.accelerationStructure.AddInstance(currentRenderer, subMeshMask: subMeshFlagArray, subMeshTransparencyFlags: subMeshCutoffArray, enableTriangleCulling: singleSided, mask: instanceFlag);
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
                    if (hdLight.enabled)
                    {
                        // Convert the object's layer to an int
                        int lightayerValue = 1 << hdLight.gameObject.layer;
                        if ((lightayerValue & subScene.mask.value) != 0)
                        {
                            if (hdLight.GetComponent<Light>().type == LightType.Directional)
                            {
                                subScene.hdDirectionalLightArray.Add(hdLight);
                            }
                            else
                            {
                                if (hdLight.lightTypeExtent == LightTypeExtent.Punctual)
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

                // Build the light cluster
                subScene.lightCluster = new HDRaytracingLightCluster();
                subScene.lightCluster.Initialize(m_Resources, m_RTResources, this, m_SharedRTManager, m_RenderPipeline);

                // Mark this sub-scene as valid
                subScene.valid = true;
            }
            else
            {
                subScene.valid = false;
            }
        }

        public RayTracingAccelerationStructure RequestAccelerationStructure(LayerMask layerMask)
        {
            HDRayTracingSubScene currentSubScene = null;
            if (m_SubScenes.TryGetValue(layerMask.value, out currentSubScene))
            {
                return currentSubScene.valid ? currentSubScene.accelerationStructure : null;
            }
            return null;
        }

        public HDRaytracingLightCluster RequestLightCluster(LayerMask layerMask)
        {
            HDRayTracingSubScene currentSubScene = null;
            if (m_SubScenes.TryGetValue(layerMask.value, out currentSubScene))
            {
                return currentSubScene.valid ? currentSubScene.lightCluster : null;
            }
            return null;
        }

        public HDRayTracingSubScene RequestSubScene(LayerMask layerMask)
        {
            HDRayTracingSubScene currentSubScene = null;
            if (m_SubScenes.TryGetValue(layerMask.value, out currentSubScene))
            {
                return currentSubScene.valid ? currentSubScene : null;
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

        public HDSimpleDenoiser GetSimpleDenoiser()
        {
            return m_SimpleDenoiser;
        }

        public HDRenderPipeline GetRenderPipeline()
        {
            return m_RenderPipeline;
        }
    }
#endif
}
