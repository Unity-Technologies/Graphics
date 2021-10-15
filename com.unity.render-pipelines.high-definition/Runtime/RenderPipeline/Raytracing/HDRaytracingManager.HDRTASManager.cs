using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    struct HDEffectsParameters
    {
        public bool shadows;
        public bool ambientOcclusion;
        public int aoLayerMask;
        public bool reflections;
        public int reflLayerMask;
        public bool globalIllumination;
        public int giLayerMask;
        public bool recursiveRendering;
        public int recursiveLayerMask;
        public bool subSurface;
        public bool pathTracing;
        public int ptLayerMask;

        // Flag that tracks if at least one effect is enabled
        public bool rayTracingRequired;
    };

    class HDRTASManager
    {
        public RayTracingAccelerationStructure rtas = null;
        public RayTracingInstanceCullingConfig cullingConfig = new RayTracingInstanceCullingConfig();
        public List<RayTracingInstanceCullingTest> instanceTestArray = new List<RayTracingInstanceCullingTest>();

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
        public bool transformDirty;
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

        public RayTracingInstanceCullingResults Cull(HDCamera hdCamera, in HDEffectsParameters parameters)
        {
            // The list of instanceTestArray needs to be cleared every frame as the list depends on the active effects and their parameters.
            instanceTestArray.Clear();

            // Set up the culling flags
            cullingConfig.flags = RayTracingInstanceCullingFlags.EnableLODCulling | RayTracingInstanceCullingFlags.IgnoreReflectionProbes;
            if (parameters.pathTracing)
                cullingConfig.flags |= RayTracingInstanceCullingFlags.ComputeMaterialsCRC;

            // Set up the LOD flags
            cullingConfig.lodParameters.fieldOfView = hdCamera.camera.fieldOfView;
            cullingConfig.lodParameters.cameraPosition = hdCamera.camera.transform.position;
            cullingConfig.lodParameters.cameraPixelHeight = hdCamera.camera.pixelHeight;

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

            cullingConfig.instanceTests = instanceTestArray.ToArray();

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
