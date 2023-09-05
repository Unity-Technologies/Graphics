using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Structure that keeps track of the ray tracing and path tracing effects that are enabled for a given camera.
    /// </summary>
    public struct HDEffectsParameters
    {
        /// <summary>
        /// Specified if ray traced shadows are active.
        /// </summary>
        public bool shadows;
        /// <summary>
        /// Specified if ray traced ambient occlusion is active.
        /// </summary>
        public bool ambientOcclusion;
        /// <summary>
        /// Specified the layer mask that will be used to evaluate ray traced ambient occlusion.
        /// </summary>
        public int aoLayerMask;
        /// <summary>
        /// Specified if ray traced reflections are active.
        /// </summary>
        public bool reflections;
        /// <summary>
        /// Specified the layer mask that will be used to evaluate ray traced reflections.
        /// </summary>
        public int reflLayerMask;
        /// <summary>
        /// Specified if ray traced global illumination is active.
        /// </summary>
        public bool globalIllumination;
        /// <summary>
        /// Specified the layer mask that will be used to evaluate ray traced global illumination.
        /// </summary>
        public int giLayerMask;
        /// <summary>
        /// Specified if recursive rendering is active.
        /// </summary>
        public bool recursiveRendering;
        /// <summary>
        /// Specified the layer mask that will be used to evaluate recursive rendering.
        /// </summary>
        public int recursiveLayerMask;
        /// <summary>
        /// Specified if ray traced sub-surface scattering is active.
        /// </summary>
        public bool subSurface;
        /// <summary>
        /// Specified if path tracing is active.
        /// </summary>
        public bool pathTracing;
        /// <summary>
        /// Specified the layer mask that will be used to evaluate path tracing.
        /// </summary>
        public int ptLayerMask;
        /// <summary>
        /// Specified if at least one ray tracing effect is enabled.
        /// </summary>
        public bool rayTracingRequired;
    };

    class HDRTASManager
    {
        public RayTracingAccelerationStructure rtas = null;
        public RayTracingInstanceCullingConfig cullingConfig = new RayTracingInstanceCullingConfig();
        public List<RayTracingInstanceCullingTest> instanceTestArray = new List<RayTracingInstanceCullingTest>();
        internal Plane[] rtCullingPlaneArray = new Plane[6];

        // Culling tests
        RayTracingInstanceCullingTest ShT_CT = new RayTracingInstanceCullingTest();
        RayTracingInstanceCullingTest ShO_CT = new RayTracingInstanceCullingTest();
        RayTracingInstanceCullingTest AO_CT = new RayTracingInstanceCullingTest();
        RayTracingInstanceCullingTest Refl_CT = new RayTracingInstanceCullingTest();
        RayTracingInstanceCullingTest GI_CT = new RayTracingInstanceCullingTest();
        RayTracingInstanceCullingTest RR_CT = new RayTracingInstanceCullingTest();
        RayTracingInstanceCullingTest SSS_CT = new RayTracingInstanceCullingTest();
        RayTracingInstanceCullingTest PT_CT = new RayTracingInstanceCullingTest();

        // Path tracing dirtiness parameters
        public bool transformsDirty;
        public bool materialsDirty;

        public void Initialize()
        {
            // We only support perspective projection in HDRP, so we flag the lod parameters as always non orthographic.
            cullingConfig.lodParameters.orthoSize = 0;
            cullingConfig.lodParameters.isOrthographic = false;

            // Opaque sub meshes need to be included and do not need to have their any hit enabled
            cullingConfig.subMeshFlagsConfig.opaqueMaterials = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;

            // Transparent sub meshes need to be included and we need the guarantee that they will trigger their any hit only once
            cullingConfig.subMeshFlagsConfig.transparentMaterials = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.UniqueAnyHitCalls;

            // Alpha tested sub meshes need to be included. (Note, not sure how it combines with transparency)
            cullingConfig.subMeshFlagsConfig.alphaTestedMaterials = RayTracingSubMeshFlags.Enabled;

            // Controls for the double sidedness
            cullingConfig.triangleCullingConfig.checkDoubleSidedGIMaterial = true;
            cullingConfig.triangleCullingConfig.frontTriangleCounterClockwise = false;
            cullingConfig.triangleCullingConfig.optionalDoubleSidedShaderKeywords = new string[1];
            cullingConfig.triangleCullingConfig.optionalDoubleSidedShaderKeywords[0] = "_DOUBLESIDED_ON";

            // Flags for the alpha testing
            cullingConfig.alphaTestedMaterialConfig.renderQueueLowerBound = HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.lowerBound;
            cullingConfig.alphaTestedMaterialConfig.renderQueueUpperBound = HDRenderQueue.k_RenderQueue_OpaqueAlphaTest.upperBound;
            cullingConfig.alphaTestedMaterialConfig.optionalShaderKeywords = new string[1];
            cullingConfig.alphaTestedMaterialConfig.optionalShaderKeywords[0] = "_ALPHATEST_ON";

            // Flags for the transparency
            cullingConfig.transparentMaterialConfig.renderQueueLowerBound = HDRenderQueue.k_RenderQueue_Transparent.lowerBound;
            cullingConfig.transparentMaterialConfig.renderQueueUpperBound = HDRenderQueue.k_RenderQueue_Transparent.upperBound;
            cullingConfig.transparentMaterialConfig.optionalShaderKeywords = new string[1];
            cullingConfig.transparentMaterialConfig.optionalShaderKeywords[0] = "_SURFACE_TYPE_TRANSPARENT";

            // Flags that define which shaders to include (HDRP shaders only)
            cullingConfig.materialTest.requiredShaderTags = new RayTracingInstanceCullingShaderTagConfig[1];
            cullingConfig.materialTest.requiredShaderTags[0].tagId = new ShaderTagId("RenderPipeline");
            cullingConfig.materialTest.requiredShaderTags[0].tagValueId = new ShaderTagId("HDRenderPipeline");
            cullingConfig.materialTest.deniedShaderPasses = DecalSystem.s_MaterialDecalPassNames;
            cullingConfig.instanceTests = new RayTracingInstanceCullingTest[9];

            // Setup the culling data for transparent shadows
            ShT_CT.allowOpaqueMaterials = true;
            ShT_CT.allowAlphaTestedMaterials = true;
            ShT_CT.allowTransparentMaterials = true;
            ShT_CT.layerMask = -1;
            ShT_CT.shadowCastingModeMask = (1 << (int)ShadowCastingMode.On) | (1 << (int)ShadowCastingMode.TwoSided) | (1 << (int)ShadowCastingMode.ShadowsOnly);
            ShT_CT.instanceMask = (uint)RayTracingRendererFlag.CastShadowTransparent;

            // Setup the culling data for opaque shadows
            ShO_CT.allowOpaqueMaterials = true;
            ShO_CT.allowAlphaTestedMaterials = true;
            ShO_CT.allowTransparentMaterials = false;
            ShO_CT.layerMask = -1;
            ShO_CT.shadowCastingModeMask = (1 << (int)ShadowCastingMode.On) | (1 << (int)ShadowCastingMode.TwoSided) | (1 << (int)ShadowCastingMode.ShadowsOnly);
            ShO_CT.instanceMask = (uint)RayTracingRendererFlag.CastShadowOpaque;

            // Setup the culling data for the ambient occlusion
            AO_CT.allowOpaqueMaterials = true;
            AO_CT.allowAlphaTestedMaterials = true;
            AO_CT.allowTransparentMaterials = false;
            AO_CT.layerMask = -1;
            AO_CT.shadowCastingModeMask = (1 << (int)ShadowCastingMode.Off) | (1 << (int)ShadowCastingMode.On) | (1 << (int)ShadowCastingMode.TwoSided);
            AO_CT.instanceMask = (uint)RayTracingRendererFlag.AmbientOcclusion;

            // Setup the culling data for the reflections
            Refl_CT.allowOpaqueMaterials = true;
            Refl_CT.allowAlphaTestedMaterials = true;
            Refl_CT.allowTransparentMaterials = false;
            Refl_CT.layerMask = -1;
            Refl_CT.shadowCastingModeMask = (1 << (int)ShadowCastingMode.Off) | (1 << (int)ShadowCastingMode.On) | (1 << (int)ShadowCastingMode.TwoSided);
            Refl_CT.instanceMask = (uint)RayTracingRendererFlag.Reflection;

            // Setup the culling data for the global illumination
            GI_CT.allowOpaqueMaterials = true;
            GI_CT.allowAlphaTestedMaterials = true;
            GI_CT.allowTransparentMaterials = false;
            GI_CT.layerMask = -1;
            GI_CT.shadowCastingModeMask = (1 << (int)ShadowCastingMode.Off) | (1 << (int)ShadowCastingMode.On) | (1 << (int)ShadowCastingMode.TwoSided);
            GI_CT.instanceMask = (uint)RayTracingRendererFlag.GlobalIllumination;

            // Setup the culling data for the recursive rendering
            RR_CT.allowOpaqueMaterials = true;
            RR_CT.allowAlphaTestedMaterials = true;
            RR_CT.allowTransparentMaterials = true;
            RR_CT.layerMask = -1;
            RR_CT.shadowCastingModeMask = (1 << (int)ShadowCastingMode.Off) | (1 << (int)ShadowCastingMode.On) | (1 << (int)ShadowCastingMode.TwoSided);
            RR_CT.instanceMask = (uint)RayTracingRendererFlag.RecursiveRendering;

            // Setup the culling data for the recursive rendering
            RR_CT.allowOpaqueMaterials = true;
            RR_CT.allowAlphaTestedMaterials = true;
            RR_CT.allowTransparentMaterials = true;
            RR_CT.layerMask = -1;
            RR_CT.shadowCastingModeMask = (1 << (int)ShadowCastingMode.Off) | (1 << (int)ShadowCastingMode.On) | (1 << (int)ShadowCastingMode.TwoSided);
            RR_CT.instanceMask = (uint)RayTracingRendererFlag.RecursiveRendering;

            // Setup the culling data for the SSS
            SSS_CT.allowOpaqueMaterials = true;
            SSS_CT.allowAlphaTestedMaterials = true;
            SSS_CT.allowTransparentMaterials = false;
            SSS_CT.layerMask = -1;
            SSS_CT.shadowCastingModeMask = -1;
            SSS_CT.instanceMask = (uint)RayTracingRendererFlag.Opaque;

            // Setup the culling data for the recursive rendering
            PT_CT.allowOpaqueMaterials = true;
            PT_CT.allowAlphaTestedMaterials = true;
            PT_CT.allowTransparentMaterials = true;
            PT_CT.layerMask = -1;
            PT_CT.shadowCastingModeMask = (1 << (int)ShadowCastingMode.Off) | (1 << (int)ShadowCastingMode.On) | (1 << (int)ShadowCastingMode.TwoSided);
            PT_CT.instanceMask = (uint)RayTracingRendererFlag.PathTracing;
        }

        void SetupCullingData(HDCamera hdCamera, bool pathTracingEnabled)
        {
            // Grab the ray tracing settings parameter
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
            switch (rtSettings.cullingMode.value)
            {
                case RTASCullingMode.ExtendedFrustum:
                {
                    // We'll be using an extension
                    cullingConfig.flags = RayTracingInstanceCullingFlags.EnablePlaneCulling;

                    // Build the culling plane data
                    Vector3 camerPosWS = hdCamera.camera.transform.position;
                    Vector3 forward = hdCamera.camera.transform.forward;
                    Vector3 right = hdCamera.camera.transform.right;
                    Vector3 up = hdCamera.camera.transform.up;

                    float far, height, width;
                    far = hdCamera.camera.farClipPlane;
                    height = Mathf.Tan(Mathf.Deg2Rad * hdCamera.camera.fieldOfView * 0.5f) * far;
                    float horizontalFov = Camera.VerticalToHorizontalFieldOfView(hdCamera.camera.fieldOfView, hdCamera.camera.aspect);
                    width = Mathf.Tan(Mathf.Deg2Rad * horizontalFov * 0.5f) * far;

                    // Front plane
                    rtCullingPlaneArray[0].normal = -forward;
                    rtCullingPlaneArray[0].distance = -Vector3.Dot(camerPosWS + forward * far, -forward);

                    // Back plane
                    rtCullingPlaneArray[1].normal = forward;
                    rtCullingPlaneArray[1].distance = -Vector3.Dot(camerPosWS - forward * far, forward);

                    // Right plane
                    rtCullingPlaneArray[2].normal = -right;
                    rtCullingPlaneArray[2].distance = -Vector3.Dot(camerPosWS + right * width, -right);

                    // Left plane
                    rtCullingPlaneArray[3].normal = right;
                    rtCullingPlaneArray[3].distance = -Vector3.Dot(camerPosWS - right * width, right);

                    // Top plane
                    rtCullingPlaneArray[4].normal = -up;
                    rtCullingPlaneArray[4].distance = -Vector3.Dot(camerPosWS + up * height, -up);

                    // Bottom plane
                    rtCullingPlaneArray[5].normal = up;
                    rtCullingPlaneArray[5].distance = -Vector3.Dot(camerPosWS - up * height, up);

                    // Set the planes
                    cullingConfig.planes = rtCullingPlaneArray;
                }
                break;
                case RTASCullingMode.Sphere:
                {
                    // We use a sphere
                    cullingConfig.flags = RayTracingInstanceCullingFlags.EnableSphereCulling;
                    cullingConfig.sphereRadius = rtSettings.cullingDistance.value;
                    cullingConfig.sphereCenter = hdCamera.camera.transform.position;
                }
                break;
                default:
                {
                    // We explicitly want no culling.
                    cullingConfig.flags = RayTracingInstanceCullingFlags.None;
                }
                break;
            }

            // We want the LODs to match the rasterization and we want to exclude reflection probes
            cullingConfig.flags |= RayTracingInstanceCullingFlags.EnableLODCulling | RayTracingInstanceCullingFlags.IgnoreReflectionProbes;

            // Dirtiness need to be kept track of for the path tracing (when enabled)
            if (pathTracingEnabled)
                cullingConfig.flags |= RayTracingInstanceCullingFlags.ComputeMaterialsCRC;
        }

        public RayTracingInstanceCullingResults Cull(HDCamera hdCamera, in HDEffectsParameters parameters)
        {
            // The list of instanceTestArray needs to be cleared every frame as the list depends on the active effects and their parameters.
            instanceTestArray.Clear();

            // Set up the culling data
            SetupCullingData(hdCamera, parameters.pathTracing);

            // Set up the LOD flags
            cullingConfig.lodParameters.fieldOfView = hdCamera.camera.fieldOfView;
            cullingConfig.lodParameters.cameraPosition = hdCamera.camera.transform.position;
            cullingConfig.lodParameters.cameraPixelHeight = hdCamera.camera.pixelHeight;

            // If we have path tracing, the shadow inclusion constraints must be aggregated with the layer masks of the path tracing.
            if (parameters.pathTracing)
            {
                ShO_CT.layerMask = parameters.ptLayerMask;
                ShT_CT.layerMask = parameters.ptLayerMask;
            }

            if (parameters.shadows || parameters.pathTracing)
            {
                instanceTestArray.Add(ShO_CT);
                instanceTestArray.Add(ShT_CT);
            }

            if (parameters.ambientOcclusion)
            {
                AO_CT.layerMask = parameters.aoLayerMask;
                instanceTestArray.Add(AO_CT);
            }

            if (parameters.reflections)
            {
                Refl_CT.layerMask = parameters.reflLayerMask;
                instanceTestArray.Add(Refl_CT);
            }

            if (parameters.globalIllumination)
            {
                GI_CT.layerMask = parameters.giLayerMask;
                instanceTestArray.Add(GI_CT);
            }

            if (parameters.recursiveRendering)
            {
                RR_CT.layerMask = parameters.recursiveLayerMask;
                instanceTestArray.Add(RR_CT);
            }

            if (parameters.subSurface)
                instanceTestArray.Add(SSS_CT);

            if (parameters.pathTracing)
            {
                PT_CT.layerMask = parameters.ptLayerMask;
                instanceTestArray.Add(PT_CT);
            }

            // avoid reallocation uf previous instanceTests array is the same size as the current one
            if (cullingConfig.instanceTests.Length != instanceTestArray.Count)
                cullingConfig.instanceTests = instanceTestArray.ToArray();
            else
                instanceTestArray.CopyTo(0, cullingConfig.instanceTests, 0, instanceTestArray.Count);

            return rtas.CullInstances(ref cullingConfig);
        }

        public void Build(HDCamera hdCamera)
        {
            if (ShaderConfig.s_CameraRelativeRendering != 0)
                rtas.Build(hdCamera.mainViewConstants.worldSpaceCameraPos);
            else
                rtas.Build();
        }

        public void Reset()
        {
            // Clear all the per frame-data or allocate the rtas if it is the first time)
            if (rtas != null)
                rtas.ClearInstances();
            else
                rtas = new RayTracingAccelerationStructure();
        }

        public void ReleaseResources()
        {
            if (rtas != null)
                rtas.Dispose();
        }
    }

    class HDRayTracingLights
    {
        // The list of non-directional lights in the sub-scene
        public List<HDLightRenderEntity> hdPointLightArray = new List<HDLightRenderEntity>();
        public List<HDLightRenderEntity> hdLineLightArray = new List<HDLightRenderEntity>();
        public List<HDLightRenderEntity> hdRectLightArray = new List<HDLightRenderEntity>();
        public List<HDLightRenderEntity> hdLightEntityArray = new List<HDLightRenderEntity>();

        // The list of directional lights in the sub-scene
        public List<HDAdditionalLightData> hdDirectionalLightArray = new List<HDAdditionalLightData>();

        // The list of reflection probes
        public List<HDProbe> reflectionProbeArray = new List<HDProbe>();

        // Counter of the current number of lights
        public int lightCount;

        internal void Reset()
        {
            hdDirectionalLightArray.Clear();
            hdPointLightArray.Clear();
            hdLineLightArray.Clear();
            hdRectLightArray.Clear();
            hdLightEntityArray.Clear();
            reflectionProbeArray.Clear();
            lightCount = 0;
        }
    }
}
