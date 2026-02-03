#if ENABLE_PATH_TRACING_SRP
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering.Denoising;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering.LiveGI
{
    using InstanceHandle = Handle<World.InstanceKey>;
    using LightHandle = Handle<World.LightDescriptor>;
    using MaterialHandle = Handle<MaterialPool.MaterialDescriptor>;

    [Serializable]
    internal class PathTracingSettings
    {
        [Header("Light Transport")]
        [Range(0, 10)]
        public int bounceCount = 2;

        [Range(1, 16)]
        public int sampleCount = 1;

        [Range(1, 8)]
        public int lightEvaluations = 1;

        [Min(0)]
        public int maxIntensity;

        [HideInInspector]
        public float exposureScale = 1.0f;

        // Keep emissive mesh sampling disabled for now, as we need a more reliable sceneMaterials.IsEmissive.
        // If you change that, you need to modify the EMISSIVE_SAMPLING define in the shader
        [HideInInspector]
        public bool enableEmissiveSampling;

        // For now, some SRPs pre-multiply the light intensity with PI (like URP), while other don't
        // https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
        [HideInInspector]
        public bool multiplyPunctualLightIntensityByPI;

        // This automatically adjust the light range based on the light intensity and range to better fit the falloff LUT (applies to punctual lights with inverse squared falloff only).
        [HideInInspector]
        public bool autoEstimateLUTRange = true;

        public LightPickingMethod lightPickingMethod = LightPickingMethod.Uniform;

        [HideInInspector]
        public PathTermination pathTermination = PathTermination.RusianRoulette;

        [Header("Post Processing")]
        public DenoiserType denoising = DenoiserType.None;

        [Header("Intersections")]
        public RayTracingBackend raytracingBackend = RayTracingBackend.Hardware;

        // Use "only static" when previewing lightmaps. Use "All" to path trace the full frame
        public RenderedGameObjectsFilter renderedGameObjects = RenderedGameObjectsFilter.OnlyStatic;

        [Header("Artistic Controls")]
        public bool respectLightLayers = true;

        [Range(1, 10)]
        public float albedoBoost = 1.0f;

        [Range(0, 10)]
        public float indirectIntensity = 1.0f;

        [Range(0, 10)]
        public float environmentIntensityMultiplier = 1.0f;

        [HideInInspector]
        public Vector3Int reservoirGridSize = new Vector3Int(64, 16, 64);

        [HideInInspector]
        public int reservoirsPerVoxel = 16;

        [Header("Debug")]
        public bool showRayHeatmap = false;
    }

    internal enum PathTracingOutput { FullPathTracer, GIPreview };

    internal enum RayTracingBackend { Hardware = 0, Compute = 1 };

    internal enum PathTermination { RusianRoulette = 0, NewUnbiased = 1, NewBiased = 2 };

    internal class PathTracingContext : IDisposable
    {
        // Public API
        #region Public API

        public PathTracingContext(PathTracingOutput pathTracingOutput)
        {
#if UNITY_EDITOR
            // Note: this code is not ready to run in the player, we just disable some parts to make sure the player builds successfully
            _defaultMaterial = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
#endif
            _sceneUpdatesTracker = new SceneUpdatesTracker();

            _pathTracingOutput = pathTracingOutput;

            _samplingResources = new SamplingResources();
#if UNITY_EDITOR
            _samplingResources.Load((uint)SamplingResources.ResourceType.All);
#endif
            _emptyExposureTexture = RTHandles.Alloc(1, 1,
                enableRandomWrite: true, name: "Empty EV100 Exposure");
        }

        public void Dispose()
        {
            _world?.Dispose();

            _rayTracingContext?.Dispose();
            _rayTracingContext = null;

            _sceneUpdatesTracker?.Dispose();

            _emptyExposureTexture?.Release();

            _buildScratchBuffer?.Dispose();

            _samplingResources?.Dispose();

            // Delete temporary textures for the default material
            CoreUtils.Destroy(_defaultMaterialDescriptor.Albedo);
            CoreUtils.Destroy(_defaultMaterialDescriptor.Emission);
            CoreUtils.Destroy(_defaultMaterialDescriptor.Transmission);
        }

        internal RayTracingBackend SelectRayTracingBackend(RayTracingBackend requestedBackend, RayTracingResources resources)
        {
            if (!RayTracingContext.IsBackendSupported((UnifiedRayTracing.RayTracingBackend)requestedBackend))
            {
                Debug.LogWarning("Hardware RayTracing not available. Falling back to Compute Shader based implementation.");
                requestedBackend = RayTracingBackend.Compute;
            }

            // Early exit if the backend has not changed and the RT resources are already loaded
            if (_world != null && _world.GetAccelerationStructure() != null && _rayTracingShader != null && _currentRayTracingBackend == requestedBackend)
                return requestedBackend;

            _rayTracingContext?.Dispose();

            _rayTracingContext = new RayTracingContext((UnifiedRayTracing.RayTracingBackend)requestedBackend, resources);
            _currentRayTracingBackend = requestedBackend;
#if UNITY_EDITOR
            _rayTracingShader = _rayTracingContext.LoadRayTracingShader("Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Shaders/LiveGI.urtshader");
#endif
            _world?.Dispose();
            _world = new World();
            var worldResources = new WorldResourceSet();
#if UNITY_EDITOR
            worldResources.LoadFromAssetDatabase();
#endif
            _world.Init(_rayTracingContext, worldResources);
            _defaultMaterialDescriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(_defaultMaterial);
            var defaultHandle = _world.AddMaterial(in _defaultMaterialDescriptor, UVChannel.UV0);
            _instanceIDToWorldMaterialHandles.Add(_defaultMaterial.GetEntityId(), defaultHandle);
            _instanceIDToWorldMaterialDescriptors.Add(_defaultMaterial.GetEntityId(), _defaultMaterialDescriptor);

            return requestedBackend;
        }

        // Handles scene updates. Should be called once per frame (and not per camera)
        public void Update(CommandBuffer cmd, PathTracingSettings settings)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("LiveGI Update")))
            {
                // TODO: enable this only for debugging
                Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;

                var changes = _sceneUpdatesTracker.GetChanges(_pathTracingOutput == PathTracingOutput.GIPreview);
                _sceneChanged = changes.HasChanges();

                UpdateMaterials(_world, _instanceIDToWorldMaterialHandles, _instanceIDToWorldMaterialDescriptors, changes.addedMaterials, changes.removedMaterials, changes.changedMaterials);
                UpdateInstances(_world, _instanceIDToWorldInstanceHandles, _instanceIDToWorldMaterialHandles, changes.addedInstances, changes.changedInstances, changes.removedInstances, settings.renderedGameObjects, settings.enableEmissiveSampling, _defaultMaterial);
                UpdateLights(_world, _instanceIDToWorldLightHandles, changes.addedLights, changes.removedLights, changes.changedLights, settings);

                // Calculate scene bounds.
                Bounds sceneBounds = new Bounds();
                if (settings.lightPickingMethod == LightPickingMethod.Regir ||
                    settings.lightPickingMethod == LightPickingMethod.LightGrid)
                {
                    var sceneRenderers = Object.FindObjectsByType<Renderer>();
                    foreach (Renderer r in sceneRenderers)
                        sceneBounds.Encapsulate(r.bounds);
                }

                _world.SetEnvironmentMaterial(RenderSettings.skybox);
                //_world.EnableEmissiveSampling = settings.enableEmissiveSampling;
                _world.Build(sceneBounds, cmd, ref _buildScratchBuffer, _samplingResources, settings.enableEmissiveSampling);

                int newLightListHashCode = _world.LightListHashCode;
                _sceneChanged |= (newLightListHashCode != _previousLightsHashCode);
                _previousLightsHashCode = newLightListHashCode;
            }
        }

        public void Render(CommandBuffer cmd, Vector2Int scaledSize, Vector4 viewFustum, Matrix4x4 cameraToWorldMatrix, Matrix4x4 worldToCameraMatrix, Matrix4x4 projectionMatrix, Matrix4x4 previousViewProjection, PathTracingSettings settings, RTHandle output, RTHandle normals, RTHandle motionVectors, RTHandle debugOutput, int frameIndex, bool preExpose = false)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("LiveGI Render")))
            {
                Debug.Assert(_world.GetAccelerationStructure() != null, "Acceleration structure does not exist. Did you call Update()?");

                // Path-tracer Input
                Util.BindWorld(cmd, _rayTracingShader, _world, _pathTracingOutput == PathTracingOutput.GIPreview ? 32 : 1024);
                _rayTracingShader.SetVectorParam(cmd, Shader.PropertyToID("g_CameraFrustum"), viewFustum);
                _rayTracingShader.SetMatrixParam(cmd, Shader.PropertyToID("g_CameraToWorldMatrix"), cameraToWorldMatrix);
                _rayTracingShader.SetMatrixParam(cmd, Shader.PropertyToID("g_PreviousViewProjection"), previousViewProjection);

                var viewProjection = projectionMatrix * worldToCameraMatrix;
                _rayTracingShader.SetMatrixParam(cmd, Shader.PropertyToID("g_CameraViewProjection"), viewProjection);

                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_FrameIndex"), frameIndex);

                // For now the history rejection in the denoising is not compatible with stochastic jitter
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_EnableSubPixelJittering"), 0);
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_LightPickingMethod"), (int)settings.lightPickingMethod);
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_BounceCount"), settings.bounceCount);
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_SampleCount"), settings.sampleCount);
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_LightEvaluations"), settings.lightEvaluations);
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_PathtracerAsGiPreviewMode"), (_pathTracingOutput == PathTracingOutput.GIPreview) ? 1 : 0);
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_CountNEERayAsPathSegment"), 1);
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_RenderedInstances"), (int)settings.renderedGameObjects);
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_PreExpose"), preExpose ? 1 : 0);
                _rayTracingShader.SetIntParam(cmd, Shader.PropertyToID("g_MaxIntensity"), settings.maxIntensity > 0 ? settings.maxIntensity : int.MaxValue);
                _rayTracingShader.SetFloatParam(cmd, Shader.PropertyToID("g_ExposureScale"), settings.exposureScale);
                _rayTracingShader.SetFloatParam(cmd, Shader.PropertyToID("g_AlbedoBoost"), settings.albedoBoost);
                _rayTracingShader.SetFloatParam(cmd, Shader.PropertyToID("g_IndirectScale"), settings.indirectIntensity);
                _rayTracingShader.SetFloatParam(cmd, Shader.PropertyToID("g_EnvIntensityMultiplier"), settings.environmentIntensityMultiplier);
                _rayTracingShader.SetFloatParam(cmd, Shader.PropertyToID("g_EnableDebug"), settings.showRayHeatmap ? 1 : 0);
                _rayTracingShader.SetFloatParam(cmd, Shader.PropertyToID("g_PathTermination"), (int)settings.pathTermination);

                // To avoid shader permutations, we always need to set an exposure texture, even if we don't read it
                if (!preExpose)
                {
                    _rayTracingShader.SetTextureParam(cmd, Shader.PropertyToID("_ExposureTexture"), _emptyExposureTexture);
                }

                SamplingResources.Bind(cmd, _samplingResources);

                // Path-tracer Output
                _rayTracingShader.SetTextureParam(cmd, Shader.PropertyToID("g_Radiance"), output);
                _rayTracingShader.SetTextureParam(cmd, Shader.PropertyToID("g_MotionVectors"), motionVectors);
                _rayTracingShader.SetTextureParam(cmd, Shader.PropertyToID("g_NormalsDepth"), normals);
                _rayTracingShader.SetTextureParam(cmd, Shader.PropertyToID("g_DebugOutput"), debugOutput);

                // Path-tracing
                RayTracingHelper.ResizeScratchBufferForTrace(_rayTracingShader, (uint)scaledSize.x, (uint)scaledSize.y, 1, ref _traceScratchBuffer);
                _rayTracingShader.Dispatch(cmd, _traceScratchBuffer, (uint)scaledSize.x, (uint)scaledSize.y, 1);

                _world.NextFrame();
            }
        }

        public void Denoise(CommandBuffer cmd, CommandBufferDenoiser denoiser, float nearClipPlane, float farClipPlane, Matrix4x4 viewProjection, PathTracingSettings settings, RTHandle color, RTHandle normals, RTHandle motionVectors)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("LiveGI Denoise")))
            {
                // Denoising
                if (settings.denoising != DenoiserType.None && denoiser != null)
                {
                    denoiser.DenoiseRequest(cmd, "color", color);
                    denoiser.DenoiseRequest(cmd, "flow", motionVectors);
                    denoiser.DenoiseRequest(cmd, "normals", normals);
                    //if (denoiser is RealTimeDenoiser)
                    //    ((RealTimeDenoiser)denoiser).SetCameraMatrix(cmd, viewProjection, nearClipPlane, farClipPlane, _sceneChanged);
                    denoiser.GetResults(cmd, color);
                }
            }
        }

        #endregion

        // Private Implementation
        #region Private Implementation

        private readonly SamplingResources _samplingResources;
        private RayTracingBackend _currentRayTracingBackend;
        private RayTracingContext _rayTracingContext;
        private IRayTracingShader _rayTracingShader;
        private GraphicsBuffer _traceScratchBuffer;
        private GraphicsBuffer _buildScratchBuffer;

        // TODO(Yvain) We should use the same buffer for  m_TraceScratchBuffer and m_BuildScratchBuffer but that's impractical when we need to
        // resize the buffer for both the build and the trace.
        // (we can't call Dispose() on a GraphicsBuffer before submitting the CommandBuffer using it)

        private readonly SceneUpdatesTracker _sceneUpdatesTracker;
        private bool _sceneChanged = true;
        private int _previousLightsHashCode;

        private readonly Material _defaultMaterial;
        private MaterialPool.MaterialDescriptor _defaultMaterialDescriptor;

        private readonly PathTracingOutput _pathTracingOutput;

        private readonly RTHandle _emptyExposureTexture;

        private World _world;

        // This dictionary maps from Unity InstanceID for MeshRenderer or Terrain, to corresponding InstanceHandle for accessing World.
        private readonly Dictionary<EntityId, InstanceHandle> _instanceIDToWorldInstanceHandles = new();

        // Same as above but for Lights
        private readonly Dictionary<EntityId, LightHandle> _instanceIDToWorldLightHandles = new();

        // Same as above but for Materials
        private Dictionary<EntityId, MaterialHandle> _instanceIDToWorldMaterialHandles = new();

        // We also keep track of associated material descriptors, so we can free temporary temporary textures when a material is removed
        private Dictionary<EntityId, MaterialPool.MaterialDescriptor> _instanceIDToWorldMaterialDescriptors = new();

        public static Vector4 GetCameraFrustum(Camera camera)
        {
            Vector3[] frustumCorners = new Vector3[4];
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), 1.0f, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
            float left = frustumCorners[0].x;
            float right = frustumCorners[2].x;
            float bottom = frustumCorners[0].y;
            float top = frustumCorners[2].y;

            return new Vector4(left, right, bottom, top);
        }

        internal static void UpdateMaterials(World world, Dictionary<EntityId, MaterialHandle> instanceIDToHandle, Dictionary<EntityId, MaterialPool.MaterialDescriptor> instanceIDToDescriptor, List<Material> addedMaterials, List<EntityId> removedMaterials, List<Material> changedMaterials)
        {
            static void DeleteTemporaryTextures(ref MaterialPool.MaterialDescriptor desc)
            {
                CoreUtils.Destroy(desc.Albedo);
                CoreUtils.Destroy(desc.Emission);
                CoreUtils.Destroy(desc.Transmission);
            }

            foreach (var materialInstanceID in removedMaterials)
            {
                // Clean up temporary textures in the descriptor
                Debug.Assert(instanceIDToDescriptor.ContainsKey(materialInstanceID));
                var descriptor = instanceIDToDescriptor[materialInstanceID];
                DeleteTemporaryTextures(ref descriptor);
                instanceIDToDescriptor.Remove(materialInstanceID);

                // Remove the material from the world
                Debug.Assert(instanceIDToHandle.ContainsKey(materialInstanceID));
                world.RemoveMaterial(instanceIDToHandle[materialInstanceID]);
                instanceIDToHandle.Remove(materialInstanceID);
            }

            foreach (var material in addedMaterials)
            {
                // Add material to the world
                var descriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material);
                var handle = world.AddMaterial(in descriptor, UVChannel.UV0);
                instanceIDToHandle.Add(material.GetEntityId(), handle);

                // Keep track of the descriptor
                instanceIDToDescriptor.Add(material.GetEntityId(), descriptor);
            }

            foreach (var material in changedMaterials)
            {
                // Clean up temporary textures in the old descriptor
                Debug.Assert(instanceIDToDescriptor.ContainsKey(material.GetEntityId()));
                var oldDescriptor = instanceIDToDescriptor[material.GetEntityId()];
                DeleteTemporaryTextures(ref oldDescriptor);

                // Update the material in the world using the new descriptor
                Debug.Assert(instanceIDToHandle.ContainsKey(material.GetEntityId()));
                var newDescriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material);
                world.UpdateMaterial(instanceIDToHandle[material.GetEntityId()], in newDescriptor, UVChannel.UV0);
                instanceIDToDescriptor[material.GetEntityId()] = newDescriptor;
            }
        }

        private static void UpdateLights(World world, Dictionary<EntityId, LightHandle> instanceIDToHandle, List<Light> addedLights, List<EntityId> removedLights,
            List<Light> changedLights, PathTracingSettings settings, MixedLightingMode mixedLightingMode = MixedLightingMode.IndirectOnly)
        {
            world.lightPickingMethod = settings.lightPickingMethod;

            // Remove deleted lights
            LightHandle[] handlesToRemove = new LightHandle[removedLights.Count];
            for (int i = 0; i < removedLights.Count; i++)
            {
                var lightInstanceID = removedLights[i];
                handlesToRemove[i] = instanceIDToHandle[lightInstanceID];
                instanceIDToHandle.Remove(lightInstanceID);
            }
            world.RemoveLights(handlesToRemove);

            // Add new lights
            LightHandle[] addedHandles = world.AddLights(Util.ConvertUnityLightsToLightDescriptors(addedLights.ToArray(), settings.multiplyPunctualLightIntensityByPI), settings.respectLightLayers, settings.autoEstimateLUTRange, mixedLightingMode);
            for (int i = 0; i < addedLights.Count; ++i)
                instanceIDToHandle.Add(addedLights[i].GetEntityId(), addedHandles[i]);

            // Update changed lights
            LightHandle[] handlesToUpdate = new LightHandle[changedLights.Count];
            for (int i = 0; i < changedLights.Count; i++)
                handlesToUpdate[i] = instanceIDToHandle[changedLights[i].GetEntityId()];
            world.UpdateLights(handlesToUpdate, Util.ConvertUnityLightsToLightDescriptors(changedLights.ToArray(), settings.multiplyPunctualLightIntensityByPI), settings.respectLightLayers, settings.autoEstimateLUTRange, mixedLightingMode);
        }

        internal static void UpdateInstances(
            World world,
            Dictionary<EntityId, InstanceHandle> instanceIDToInstanceHandle,
            Dictionary<EntityId, MaterialHandle> instanceIDToMaterialHandle,
            List<MeshRenderer> addedInstances,
            List<InstanceChanges> changedInstances,
            List<EntityId> removedInstances,
            RenderedGameObjectsFilter renderedGameObjects,
            bool enableEmissiveSampling,
            Material fallbackMaterial)
        {
            foreach (var meshRendererInstanceID in removedInstances)
            {
                if (instanceIDToInstanceHandle.TryGetValue(meshRendererInstanceID, out InstanceHandle instance))
                {
                    world.RemoveInstance(instance);
                    instanceIDToInstanceHandle.Remove(meshRendererInstanceID);
                }
                else
                {
                    Debug.LogError($"Failed to remove an instance with InstanceID {meshRendererInstanceID}");
                }
            }

            foreach (var meshRenderer in addedInstances)
            {
                if (meshRenderer.isPartOfStaticBatch)
                {
                    Debug.LogError("Static batching should be disabled when using the real time path tracer in play mode. You can disable it from the project settings.");
                    continue;
                }

                var mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
                var localToWorldMatrix = meshRenderer.transform.localToWorldMatrix;

                var materials = Util.GetMaterials(meshRenderer);
                var materialHandles = new MaterialHandle[materials.Length];
                bool[] visibility = new bool[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        materialHandles[i] = instanceIDToMaterialHandle[fallbackMaterial.GetEntityId()];
                        visibility[i] = false;
                    }
                    else
                    {
                        materialHandles[i] = instanceIDToMaterialHandle[materials[i].GetEntityId()];
                        visibility[i] = true;
                    }
                }
                uint[] masks = new uint[materials.Length];
                for (int i = 0; i < masks.Length; i++)
                {
#if UNITY_EDITOR
                    bool hasLightmaps = (meshRenderer.receiveGI == ReceiveGI.Lightmaps);
#else
                    bool hasLightmaps = true;
#endif
                    var mask = World.GetInstanceMask(meshRenderer.shadowCastingMode, Util.IsStatic(meshRenderer.gameObject), renderedGameObjects, hasLightmaps);
                    masks[i] = visibility[i] ? mask : 0u;
                }


                InstanceHandle instance = world.AddInstance(
                    mesh,
                    materialHandles,
                    masks,
                    1u << meshRenderer.gameObject.layer,
                    in localToWorldMatrix,
                    meshRenderer.bounds,
                    Util.IsStatic(meshRenderer.gameObject),
                    renderedGameObjects,
                    enableEmissiveSampling);
                var instanceID = meshRenderer.GetEntityId();
                Debug.Assert(!instanceIDToInstanceHandle.ContainsKey(instanceID));
                instanceIDToInstanceHandle.Add(instanceID, instance);
            }

            foreach (var instanceUpdate in changedInstances)
            {
                try
                {
                    var renderer = instanceUpdate.meshRenderer;
                    var gameObject = renderer.gameObject;

                    if (!instanceIDToInstanceHandle.TryGetValue(renderer.GetEntityId(), out InstanceHandle instance))
                    {
                        Debug.LogError($"Failed to update an instance with InstanceID {instanceUpdate.meshRenderer.GetEntityId()}");
                        continue;
                    }

                    if ((instanceUpdate.changes & ModifiedProperties.Transform) != 0)
                    {
                        world.UpdateInstanceTransform(instance, gameObject.transform.localToWorldMatrix);
                    }

                    bool materialChanged = (instanceUpdate.changes & ModifiedProperties.Material) != 0;
                    bool maskPropertiesChanged = (instanceUpdate.changes & ModifiedProperties.IsStatic) != 0 || (instanceUpdate.changes & ModifiedProperties.ShadowCasting) != 0 || (instanceUpdate.changes & ModifiedProperties.Layer) != 0;
                    if (materialChanged || enableEmissiveSampling || maskPropertiesChanged)
                    {
                        var materials = Util.GetMaterials(renderer);
                        var materialHandles = new MaterialHandle[materials.Length];
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (materials[i] == null)
                            {
                                materialHandles[i] = instanceIDToMaterialHandle[fallbackMaterial.GetEntityId()];
                            }
                            else
                            {
                                materialHandles[i] = instanceIDToMaterialHandle[materials[i].GetEntityId()];
                            }
                        }

                        if (materialChanged)
                            world.UpdateInstanceMaterials(instance, materialHandles);
                        if (enableEmissiveSampling)
                            world.UpdateInstanceEmission(instance, instanceUpdate.meshRenderer.GetComponent<MeshFilter>().sharedMesh, instanceUpdate.meshRenderer.bounds, materialHandles, Util.IsStatic(gameObject), renderedGameObjects);
                        if (maskPropertiesChanged || materialChanged)
                        {
                            bool[] visibility = new bool[materials.Length];
                            for (int i = 0; i < materials.Length; i++)
                                visibility[i] = materials[i] != null;
                            uint[] masks = new uint[materials.Length];
                            for (int i = 0; i < masks.Length; i++)
                            {
#if UNITY_EDITOR
                                bool hasLightmaps = (renderer.receiveGI == ReceiveGI.Lightmaps);
#else
                                bool hasLightmaps = true;
#endif
                                var mask = World.GetInstanceMask(renderer.shadowCastingMode, Util.IsStatic(renderer.gameObject), renderedGameObjects, hasLightmaps);
                                masks[i] = visibility[i] ? mask : 0u;
                            }
                            world.UpdateInstanceMask(instance, masks);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to modify instance {instanceUpdate.meshRenderer.name}: {e}");
                }
            }
        }
#endregion
    }
}
#endif
