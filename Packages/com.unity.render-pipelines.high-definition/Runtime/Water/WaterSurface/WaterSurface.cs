using System;
using System.Collections.Generic;
using UnityEditor;
using Unity.Mathematics;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Enum that defines the type of a given water surface.
    /// </summary>
    public enum WaterSurfaceType
    {
        /// <summary>
        /// The water surface is either a Ocean, Sea, Lake or a large water body.
        /// </summary>
        [InspectorName("Ocean, Sea or Lake")]
        OceanSeaLake,

        /// <summary>
        /// The water surface is a river a stream.
        /// </summary>
        River,

        /// <summary>
        /// The water surface is a pool or a small water body.
        /// </summary>
        Pool,
    }

    /// <summary>
    /// Controls the type of geometry used to render the water surface.
    /// </summary>
    public enum WaterGeometryType
    {
        /// <summary>
        /// The water surface will be rendered as a procedural quad.
        /// </summary>
        Quad,

        /// <summary>
        /// The water will be rendered with one or multiple Mesh Renderers provided by the user.
        /// </summary>
        Custom,

        /// <summary>
        /// The water surface will be rendered as a set of quads that cover the area defined by the water surface.
        /// </summary>
        InstancedQuads,

        /// <summary>
        /// The water surface will be rendered as a infinite plane.
        /// </summary>
        Infinite
    }

    /// <summary>
    /// Controls how a property is defined for a water surface.
    /// </summary>
    public enum WaterPropertyOverrideMode
    {
        /// <summary>
        /// The property in inherited from an other similar property.
        /// </summary>
        Inherit,

        /// <summary>
        /// The property needs to be specified individually.
        /// </summary>
        Custom,
    }

    /// <summary>
    /// Water surface component.
    /// </summary>
    [HDRPHelpURL("water")]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public partial class WaterSurface : MonoBehaviour
    {
        #region Instance Management
        // Management to avoid memory allocations at fetch time
        // NOTE: instances tracks active instances, disabled instances can exist and are not included.
        internal static HashSet<WaterSurface> instances = new HashSet<WaterSurface>();
        internal static WaterSurface[] instancesAsArray = null;
        internal static int instanceCount = 0;

        internal static void RegisterInstance(WaterSurface surface)
        {
            instances.Add(surface);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new WaterSurface[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }

        internal static void UnregisterInstance(WaterSurface surface)
        {
            instances.Remove(surface);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new WaterSurface[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }
        #endregion

        #region Water General
        /// <summary>
        /// Specifies the nature of the water body that the water system needs to simulate.
        /// </summary>
        public WaterSurfaceType surfaceType = WaterSurfaceType.OceanSeaLake;

        /// <summary>
        /// Specifies the type of geometry used to render the water surface.
        /// </summary>
        public WaterGeometryType geometryType = WaterGeometryType.Infinite;

        /// <summary>
        /// Sets the geometry to use when rendering in quad and custom geometry type mode. The vertical position of the vertices will be overridden to keep the surface of water leveled.
        /// </summary>
        public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        /// <summary>
        /// Sets the speed of the water simulation. This allows to slow down the waves' speed or to accelerate it.
        /// </summary>
        [Range(0, 10)]
        public float timeMultiplier = 1.0f;
        #endregion

        #region Water CPU Simulation
        /// <summary>
        /// When enabled, the Water System allows you to make height requests from a C# script.
        /// </summary>
        [Tooltip("When enabled, the Water System allows you to make height requests from a C# script."), FormerlySerializedAs("cpuSimulation")]
        public bool scriptInteractions = false;

        /// <summary>
        /// Specifies if the CPU simulation should evaluate the ripples as part of the simulation. Including ripples will allow a higher visual fidelity but the cost of the simulation will increase.
        /// </summary>
        public bool cpuEvaluateRipples = false;
        #endregion

        #region Water Material
        /// <summary>
        /// Sets a custom material that will be used to render the water surface. If set to None a default material is used.
        /// </summary>
        public Material customMaterial = null;

        /// <summary>
        ///
        /// </summary>
        public float startSmoothness = 0.95f;

        /// <summary>
        ///
        /// </summary>
        public float endSmoothness = 0.85f;

        /// <summary>
        ///
        /// </summary>
        public float smoothnessFadeStart = 100.0f;

        /// <summary>
        ///
        /// </summary>
        public float smoothnessFadeDistance = 500.0f;

        /// <summary>
        /// Use hardware tessellation when rendering the water surface
        /// </summary>
        [Tooltip("When enabled, HDRP activates tessellation for this Water Surface.\nThis improves the visual quality but may have a significant performance cost depending on the platform.")]
        public bool tessellation = true;

        /// <summary>
        /// Sets the maximum tessellation factor for the water surface.
        /// </summary>
        [Range(0.0f, 10.0f), Tooltip("Sets the maximum tessellation factor for the water surface.")]
        public float maxTessellationFactor = 3.0f;

        /// <summary>
        /// Sets the distance at which the tessellation factor start to lower.
        /// </summary>
        [Min(0.0f), Tooltip(" Sets the distance at which the tessellation factor start to lower.")]
        public float tessellationFactorFadeStart = 150.0f;

        /// <summary>
        /// Sets the range at which the tessellation factor reaches zero.
        /// </summary>
        [Min(0.0f), Tooltip("Sets the range at which the tessellation factor reaches zero.")]
        public float tessellationFactorFadeRange = 1850.0f;

#if UNITY_EDITOR
        static internal bool IsWaterMaterial(Material material)
        {
            return material.shader.FindSubshaderTagValue(0, (ShaderTagId)"ShaderGraphTargetId").name == "WaterSubTarget";
        }
#endif
        #endregion

        #region Water Refraction
        /// <summary>
        /// Sets the color that is used to simulate the under-water refraction.
        /// </summary>
        [Tooltip("Sets the color that is used to simulate the under-water refraction.")]
        [ColorUsage(false)]
        public Color refractionColor = new Color(0.00f, 0.45f, 0.65f);

        /// <summary>
        /// Controls the maximum distance in meters used to clamp the underwater refraction depth. Higher value increases the distortion amount.
        /// </summary>
        [Range(0.0f, 3.5f), Tooltip("Controls the maximum distance in meters used to clamp the underwater refraction depth. Higher value increases the distortion amount.")]
        public float maxRefractionDistance = 1.0f;

        /// <summary>
        /// Controls the approximative distance in meters that the camera can perceive through a water surface. This distance can vary widely depending on the intensity of the light the object receives.
        /// </summary>
        [Range(0.001f, 100.0f), Tooltip("Controls the approximative distance in meters that the camera can perceive through a water surface. This distance can vary widely depending on the intensity of the light the object receives.")]
        public float absorptionDistance = 5.0f;

        internal Vector3 extinction => (-Mathf.Log(0.02f) / absorptionDistance) * new Vector3(Mathf.Max(1.0f - refractionColor.r, 0.01f), Mathf.Max(1.0f - refractionColor.g, 0.01f), Mathf.Max(1.0f - refractionColor.b, 0.01f));
        internal Vector3 underWaterExtinction => extinction / absorptionDistanceMultiplier;
        #endregion

        #region Water Scattering
        /// <summary>
        /// Sets the color that is used to simulate the water light scattering.
        /// </summary>
        [Tooltip("Sets the color that is used to simulate the water light scattering.")]
        [ColorUsage(false)]
        public Color scatteringColor = new Color(0.0f, 0.27f, 0.23f);

        /// <summary>
        /// Controls the intensity of the ambient scattering term. This can be adjusted for artistic purposes.
        /// </summary>
        [Range(0.0f, 1.0f), Tooltip("Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.")]
        public float ambientScattering = 0.1f;

        /// <summary>
        /// Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.
        /// </summary>
        [Range(0.0f, 1.0f), Tooltip("Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.")]
        public float heightScattering = 0.1f;

        /// <summary>
        /// Controls the intensity of the displacement based scattering. The bigger horizontal displacement, the more the water receives scattering. This can be adjusted for artistic purposes.
        /// </summary>
        [Range(0.0f, 1.0f), Tooltip("Controls the intensity of the displacement based scattering. The bigger horizontal displacement, the more the water receives scattering. This can be adjusted for artistic purposes.")]
        public float displacementScattering = 0.3f;

        /// <summary>
        /// Controls the intensity of the direct light scattering on the tip of the waves. The effect is more perceivable at grazing angles.
        /// </summary>
        [Range(0.0f, 1.0f), Tooltip("Controls the intensity of the direct light scattering on the tip of the waves. The effect is more perceivable at grazing angles.")]
        public float directLightTipScattering = 0.6f;

        /// <summary>
        /// Controls the intensity of the direct light scattering on the body of the waves. The effect is more perceivable at grazing angles.
        /// </summary>
        [Range(0.0f, 1.0f), Tooltip("Controls the intensity of the direct light scattering on the body of the waves. The effect is more perceivable at grazing angles.")]
        public float directLightBodyScattering = 0.4f;

        /// <summary>
        /// Specifies a maximum wave height that overrides the simulation to support scattering properly for deformers.
        /// </summary>
        [Min(0.0f), Tooltip("Specifies a maximum wave height that overrides the simulation to support scattering properly for deformers.")]
        public float maximumHeightOverride = 0.0f;
        #endregion

        #region Water Caustics General
        /// <summary>
        /// When enabled, the water surface will render caustics.
        /// </summary>
        [Tooltip("When enabled, the water surface will render caustics.")]
        public bool caustics = true;

        /// <summary>
        /// Sets the intensity of the under-water caustics.
        /// </summary>
        [Min(0.0f)]
        [Tooltip("Sets the intensity of the under-water caustics.")]
        public float causticsIntensity = 0.5f;

        /// <summary>
        /// Sets the vertical blending distance for the water caustics.
        /// </summary>
        [Min(0.0f)]
        [Tooltip("Sets the vertical blending distance for the water caustics.")]
        public float causticsPlaneBlendDistance = 1.0f;

        /// <summary>
        /// Defines the resolution a which caustics are rendered (simulation only).
        /// </summary>
        public enum WaterCausticsResolution
        {
            /// <summary>
            /// The water caustics are rendered at 256x256
            /// </summary>
            [InspectorName("Low 256")]
            Caustics256 = 256,
            /// <summary>
            /// The water caustics are rendered at 512x512
            /// </summary>
            [InspectorName("Medium 512")]
            Caustics512 = 512,
            /// <summary>
            /// The water caustics are rendered at 1024x1024
            /// </summary>
            [InspectorName("High 1024")]
            Caustics1024 = 1024,
        }

        /// <summary>
        /// Specifies the resolution at which the water caustics are rendered (simulation only).
        /// </summary>
        [Tooltip("Specifies the resolution at which the water caustics are rendered (simulation only).")]
        public WaterCausticsResolution causticsResolution = WaterCausticsResolution.Caustics256;

        /// <summary>
        /// Controls which band is used for the caustics evaluation.
        /// </summary>
        [Tooltip("Controls which band is used for the caustics evaluation.")]
        public int causticsBand = 1;

        /// <summary>
        /// Sets the distance at which the simulated caustics are projected. High values generate sharper caustics but can cause artifacts.
        /// </summary>
        [Min(0.001f)]
        public float virtualPlaneDistance = 5.0f;

        /// <summary>
        /// Sets a tiling factor for the water caustics.
        /// </summary>
        [Min(0.001f)]
        public float causticsTilingFactor = 1.0f;

        /// <summary>
        /// When enabled, the water caustics will take into account the directional light's shadow.
        /// </summary>
        public bool causticsDirectionalShadow = false;

        /// <summary>
        /// Sets the water caustics dimmer value for the directional shadow.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float causticsDirectionalShadowDimmer = 0.25f;
        #endregion

        #region Water Miscellaneous
        /// <summary>
        /// Specifies the rendering layers that affect the water surface.
        /// </summary>
        [Tooltip("Specifies the rendering layers that affect the water surface.")]
        public RenderingLayerMask renderingLayerMask = (RenderingLayerMask) (uint) UnityEngine.RenderingLayerMask.defaultRenderingLayerMask;

        /// <summary>
        /// Sets the debug mode for a given water surface.
        /// </summary>
        public WaterDebugMode debugMode = WaterDebugMode.None;

        /// <summary>
        /// Sets the water mask debug mode for a given water surface.
        /// </summary>
        public WaterMaskDebugMode waterMaskDebugMode = WaterMaskDebugMode.RedChannel;

        /// <summary>
        /// Sets the water current debug mode for a given water surface.
        /// </summary>
        public WaterCurrentDebugMode waterCurrentDebugMode = WaterCurrentDebugMode.Large;

        /// <summary>
        /// Sets a multiplier for the arrow density in the current debug mode.
        /// </summary>
        public float currentDebugMultiplier = 1.0f;

        /// <summary>
        /// Sets the water foam debug mode for a given water surface.
        /// </summary>
        public WaterFoamDebugMode waterFoamDebugMode = WaterFoamDebugMode.SurfaceFoam;
        #endregion

        #region Water Underwater
        /// <summary>
        /// When enabled, HDRP will apply a fog and color shift to the final image when the camera is under the surface. This feature has a cost even when the camera is above the water surface.
        /// </summary>
        public bool underWater = false;

        /// <summary>
        /// Sets a box collider that will be used to define the volume where the underwater effect is applied for non infinite surfaces.
        /// </summary>
        [Tooltip("Sets a box collider that will be used to define the volume where the underwater effect is applied for non infinite surfaces.")]
        public BoxCollider volumeBounds = null;

        /// <summary>
        /// Sets maximum depth at which the underwater effect is evaluated for infinite surfaces.
        /// </summary>
        [Min(0.0f), Tooltip("Sets maximum depth at which the underwater effect is evaluated for infinite surfaces.")]
        public float volumeDepth = 50.0f;

        /// <summary>
        /// Sets the maximum height at which the underwater effect is evaluated for infinite surfaces. This allows to cover the underwater scenario when deformers are higher than waves or ripples.
        /// </summary>
        [Min(0.0f), Tooltip("Sets the maximum height at which the underwater effect is evaluated for infinite surfaces. This allows to cover the underwater scenario when deformers are higher than waves or ripples.")]
        public float volumeHeight = 0.0f;

        /// <summary>
        /// Sets a priority value that is used to define which surface should be considered for underwater rendering in the case of multiple overlapping surfaces.
        /// </summary>
        [Min(0), Tooltip("Sets a priority value that is used to define which surface should be considered for underwater rendering in the case of multiple overlapping surfaces.")]
        public int volumePrority = 0;

        /// <summary>
        /// Sets the multiplier for the Absorption Distance when the camera is underwater. A value of 2.0 means you will see twice as far underwater.
        /// </summary>
        [Min(0.001f), Tooltip("Sets the multiplier for the  Absorption Distance when the camera is underwater. A value of 2.0 means you will see twice as far underwater.")]
        public float absorptionDistanceMultiplier = 1.0f;

        /// <summary>
        /// Sets the contribution of the ambient probe luminance when multiplied by the underwater scattering color.
        /// </summary>
        [Obsolete("Will be removed in the next version.")]
        public float underWaterAmbientProbeContribution = 1.0f;

        /// <summary>
        /// Controls how the scattering color is evaluated for the underwater scenario.
        /// </summary>
        [Obsolete("Will be removed in the next version.")]
        public enum UnderWaterScatteringColorMode
        {
            /// <summary>
            /// The scattering color is used as the underwater scattering color.
            /// </summary>
            ScatteringColor,

            /// <summary>
            /// The property needs to be specified manually.
            /// </summary>
            Custom,
        }

        /// <summary>
        /// Sets how the underwater scattering color is specified.
        /// </summary>
        [Obsolete("Will be removed in the next version.")]
        public UnderWaterScatteringColorMode underWaterScatteringColorMode = UnderWaterScatteringColorMode.ScatteringColor;

        /// <summary>
        /// Sets the color that is used to simulate the scattering when the camera is under-water.
        /// </summary>
        [ColorUsage(false)]
        [Obsolete("Will be removed in the next version.")]
        public Color underWaterScatteringColor = new Color(0.0f, 0.27f, 0.23f);

        /// <summary>
        /// Determines if water surface should refract light when looking at objects from underwater.
        /// This simulates the correct behavior of water but may introduce visual artifacts as it relies on screen space refraction.
        /// </summary>
        public bool underWaterRefraction = false;
        #endregion

        #region Constant Buffers
        internal MaterialPropertyBlock mpb;
        internal int surfaceIndex;

        internal void CreatePropertyBlock()
        {
            // Prepare the material property block for the rendering
            mpb = new MaterialPropertyBlock();
            mpb.SetTexture(HDShaderIDs._WaterDisplacementBuffer, simulation.gpuBuffers.displacementBuffer);
            mpb.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, simulation.gpuBuffers.additionalDataBuffer);
        }

        internal void FillMaterialPropertyBlock(WaterSystem system, bool supportDecals)
        {
            var constantBuffer = HDRenderPipeline.currentPipeline.waterSystem.m_ShaderVariablesWaterPerSurface[surfaceIndex];
            mpb.SetConstantBuffer(HDShaderIDs._ShaderVariablesWaterPerSurface, constantBuffer, 0, constantBuffer.stride);

            // Textures
            mpb.SetTexture(HDShaderIDs._SimulationFoamMask, GetSimulationFoamMaskBuffer(system, supportDecals, Texture2D.whiteTexture));
            mpb.SetTexture(HDShaderIDs._WaterMask, GetSimulationMaskBuffer(system, supportDecals, Texture2D.whiteTexture));
            mpb.SetTexture(HDShaderIDs._Group0CurrentMap, GetLargeCurrentBuffer(system, supportDecals, Texture2D.blackTexture));
            mpb.SetTexture(HDShaderIDs._Group1CurrentMap, GetRipplesCurrentBuffer(system, supportDecals, Texture2D.blackTexture));
            mpb.SetTexture(HDShaderIDs._WaterDeformationBuffer, GetDeformationBuffer(system, supportDecals, Texture2D.blackTexture));
            mpb.SetTexture(HDShaderIDs._WaterDeformationSGBuffer, GetDeformationNormalBuffer(system, supportDecals, Texture2D.blackTexture));
            mpb.SetTexture(HDShaderIDs._WaterFoamBuffer, GetFoamBuffer(system, supportDecals,Texture2D.blackTexture));

            Texture causticsData = caustics ? simulation.gpuBuffers.causticsBuffer : Texture2D.blackTexture;
            mpb.SetTexture(HDShaderIDs._WaterCausticsDataBuffer, causticsData);

        }

        /// <summary>
        ///  Function that globally binds the textures and constant buffer for use by external systems such as VFX Graph
        ///  As the binding is done globally, only one surface can be bound during a frame
        /// </summary>
        /// <returns>A boolean that indicates if the function was able to bind GlobalTextures.</returns>
        public bool SetGlobalTextures()
        {
            if (simulation == null)
                return false;

            var constantBuffer = HDRenderPipeline.currentPipeline.waterSystem.m_ShaderVariablesWaterPerSurface[surfaceIndex];
            Shader.SetGlobalTexture(HDShaderIDs._WaterDisplacementBuffer, simulation.gpuBuffers.displacementBuffer);
            Shader.SetGlobalTexture(HDShaderIDs._WaterAdditionalDataBuffer, simulation.gpuBuffers.additionalDataBuffer);
            Shader.SetGlobalConstantBuffer(HDShaderIDs._ShaderVariablesWaterPerSurface, constantBuffer, 0, constantBuffer.stride);

            var system = HDRenderPipeline.currentPipeline.waterSystem;
            Shader.SetGlobalTexture(HDShaderIDs._SimulationFoamMask, GetSimulationFoamMaskBuffer(system, true, Texture2D.whiteTexture));
            Shader.SetGlobalTexture(HDShaderIDs._WaterMask, GetSimulationMaskBuffer(system, true, Texture2D.whiteTexture));
            Shader.SetGlobalTexture(HDShaderIDs._Group0CurrentMap, GetLargeCurrentBuffer(system, true, Texture2D.blackTexture));
            Shader.SetGlobalTexture(HDShaderIDs._Group1CurrentMap, GetRipplesCurrentBuffer(system, true, Texture2D.blackTexture));
            Shader.SetGlobalTexture(HDShaderIDs._WaterDeformationBuffer, GetDeformationBuffer(system, true, Texture2D.blackTexture));
            Shader.SetGlobalTexture(HDShaderIDs._WaterDeformationSGBuffer, GetDeformationNormalBuffer(system, true, Texture2D.blackTexture));
            Shader.SetGlobalTexture(HDShaderIDs._WaterFoamBuffer, GetFoamBuffer(system, true, Texture2D.blackTexture));
            return true;
        }
        #endregion

        #region Water Decals
        /// <summary>
        /// Defines the resolution of the internal decal region textures.
        /// </summary>
        public enum WaterDecalRegionResolution
        {
            /// <summary>
            /// The water decals are rendered in a 256x256 texture.
            /// </summary>
            [InspectorName("Low 256")]
            Resolution256 = 256,
            /// <summary>
            /// The water decals are rendered in a 512x512 texture.
            /// </summary>
            [InspectorName("Medium 512")]
            Resolution512 = 512,
            /// <summary>
            /// The water decals are rendered in a 1024x1024 texture.
            /// </summary>
            [InspectorName("High 1024")]
            Resolution1024 = 1024,
            /// <summary>
            /// The water decals are rendered in a 2048x2048 texture.
            /// </summary>
            [InspectorName("Very High 2048")]
            Resolution2048 = 2048,
        }

        /// <summary>
        /// Specifies the size of the decal region in meters.
        /// </summary>
        public Vector2 decalRegionSize = new Vector2(200.0f, 200.0f);

        /// <summary>
        /// Specifies the center of the decal region. When null, the region will follow the main camera.
        /// </summary>
        public Transform decalRegionAnchor = null;

        // Compute the decal region bounds for this frame
        internal float2 frameRegionCenter, frameRegionSize;
        internal void UpdateDecalRegion(Transform anchor)
        {
            if (decalRegionAnchor != null)
                anchor = decalRegionAnchor;

            frameRegionSize = decalRegionSize;
            frameRegionCenter = anchor != null ? new float2(anchor.position.x, anchor.position.z) : 0.0f;
            if (anchor == null)
                return;


            if (IsProceduralGeometry() && !IsInfinite())
            {
                float3 position = transform.position;
                float size = length(new float2(abs(transform.lossyScale.x), abs(transform.lossyScale.z))) * 0.5f;

                float2 waterMin = position.xz - size, waterMax = position.xz + size;
                float2 regionMin = max(waterMin, frameRegionCenter - frameRegionSize * 0.5f);
                float2 regionMax = min(waterMax, frameRegionCenter + frameRegionSize * 0.5f);

                frameRegionCenter = (regionMin + regionMax) * 0.5f;
                frameRegionSize = regionMax - regionMin;
                //frameRegionSize = max(regionMax-frameRegionCenter, frameRegionCenter-regionMin) * 2.0f;
            }
            /*
            else if (IsCustomMesh())
            {
                Vector3 decalRegionCenter = new Vector3(anchor.position.x, 0.0f, anchor.position.z);
                if (!IsCustomMesh())
                    decalRegionCenter = transform.rotation * decalRegionCenter;
            }
            */

            // Move region in steps to avoid flickering under camera movement
            // We only use the foam resolution as it's the one that gets reprojected
            float step = max(frameRegionSize.x, frameRegionSize.y) / (float)foamResolution;
            frameRegionCenter = round(frameRegionCenter / step) * step;
        }

        /// <summary>
        /// Function that returns the decal region center and size.
        /// </summary>
        /// <param name="center">Region center, based on the anchor gameObject.</param>
        /// <param name="size">Region size.</param>
        public void GetDecalRegion(out float2 center, out float2 size)
        {
            center = frameRegionCenter;
            size = frameRegionSize;
        }
        #endregion

        /// <summary>
        /// Function that fills a WaterSimSearchData with the data of the current water surface.
        /// </summary>
        /// <param name="wsd">The water simulation search data to fill.</param>
        /// <returns>A boolean that defines if the function was able to fill the search data.</returns>
        public bool FillWaterSearchData(ref WaterSimSearchData wsd)
        {
            var hdrp = HDRenderPipeline.currentPipeline;
            if (hdrp == null || !scriptInteractions)
                return false;

            if (simulation != null && simulation.ValidResources((int)hdrp.waterSystem.simationRes, numActiveBands, HasSimulationFoam()))
            {
                // General
                wsd.simulationTime = simulation.simulationTime;

                // Simulation
                wsd.activeBandCount = WaterSystem.EvaluateCPUBandCount(surfaceType, ripples, cpuEvaluateRipples);
                wsd.cpuSimulation = hdrp.waterSystem.replicateSimulationOnCPU;
                wsd.spectrum = simulation.spectrum;
                wsd.rendering = simulation.rendering;

                wsd.decalWorkflow = HDRenderPipeline.currentPipeline.waterSystem.m_EnableDecalWorkflow;

                GetDecalRegion(out var center, out var size);
                wsd.decalRegionCenter = center;
                wsd.decalRegionScale = 1.0f / size;

                if (wsd.cpuSimulation)
                {
                    if (simulation.cpuBuffers == null)
                        return false;

                    wsd.simulationRes = (int)hdrp.waterSystem.cpuSimationRes;
                    wsd.displacementDataCPU = simulation.cpuBuffers.displacementBufferCPU;
                    wsd.displacementDataGPU = wsd.displacementDataCPU.Reinterpret<half4>(4 * sizeof(float));
                }
                else
                {
                    if (!displacementBufferSynchronizer.TryGetBuffer(out wsd.displacementDataGPU))
                        return false;

                    wsd.simulationRes = simulation.simulationResolution;
                    wsd.displacementDataCPU = wsd.displacementDataGPU.Reinterpret<float4>(2 * sizeof(float));

                    if (wsd.displacementDataGPU.Length == 0 || displacementBufferSynchronizer.CurrentSlices() < wsd.activeBandCount)
                        return false;
                }

                // Mask data
                FillWaterMaskData(ref wsd);

                // Deformation data
                FillWaterDeformationData(ref wsd);

                // Current map data
                FillCurrentMapData(ref wsd);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Function that attempts to evaluate the projection of a given world space position onto the water surface. This requires the HDRP asset and the water surface to have script interactions enabled.
        /// </summary>
        /// <param name="wsp">The water simulation search parameters that defines the location we are targeting and additional parameters.</param>
        /// <param name="wsr">The water simulation search result that contains the result position, error, etc.</param>
        /// <returns>A boolean that defines if the function was able to execute the evaluation.</returns>
        public bool ProjectPointOnWaterSurface(WaterSearchParameters wsp, out WaterSearchResult wsr)
        {
            // Invalidate the search result in case the simulation data is not available
            wsr.error = float.MaxValue;
            wsr.projectedPositionWS = float3(0, 0, 0);
            wsr.normalWS = UpVector();
            wsr.candidateLocationWS = float3(0, 0, 0);
            wsr.currentDirectionWS = float3(1, 0, 0);
            wsr.numIterations = wsp.maxIterations;

            // Try to to fill the search data and run the evaluation.
            WaterSimSearchData wsd = new WaterSimSearchData();
            if (FillWaterSearchData(ref wsd))
                return WaterSystem.ProjectPointOnWaterSurface(wsd, wsp, ref wsr);
            return false;
        }

        private void Start()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void Awake()
        {
            k_Migration.Migrate(this);

            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void OnEnable()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);

#if UNITY_EDITOR
            // Handle scene visibility
            PrefabStage.prefabStageOpened += RegisterWaterSurfaceVisibilityUpdatePrefabStage;
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) // In case the prefab stage is already opened when enabling the decal
                RegisterWaterSurfaceVisibilityUpdatePrefabStage();
#endif
        }

        private void OnDisable()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);

#if UNITY_EDITOR
            UnregisterWaterSurfaceVisibilityUpdatePrefabStage();
            PrefabStage.prefabStageOpened -= RegisterWaterSurfaceVisibilityUpdatePrefabStage;
#endif
        }

#if UNITY_EDITOR
        void RegisterWaterSurfaceVisibilityUpdatePrefabStage(PrefabStage stage = null)
        {
            SceneView.duringSceneGui -= UpdateWaterSurfaceVisibilityPrefabStage;
            SceneView.duringSceneGui += UpdateWaterSurfaceVisibilityPrefabStage;
        }

        void UnregisterWaterSurfaceVisibilityUpdatePrefabStage()
            => SceneView.duringSceneGui -= UpdateWaterSurfaceVisibilityPrefabStage;

        bool m_LastPrefabStageVisibility = true;
        void UpdateWaterSurfaceVisibilityPrefabStage(SceneView sv)
        {
            bool showWaterSurface = true;

            // If prefab context is not hidden, then we should render the WaterSurface
            if (!CoreUtils.IsSceneViewPrefabStageContextHidden())
                showWaterSurface = true;

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                bool isWaterSurfaceInPrefabStage = gameObject.scene == stage.scene;

                if (!isWaterSurfaceInPrefabStage && stage.mode == PrefabStage.Mode.InIsolation)
                    showWaterSurface = false;
                if (!isWaterSurfaceInPrefabStage && CoreUtils.IsSceneViewPrefabStageContextHidden())
                    showWaterSurface = false;
            }

            // Update decal visibility based on showDecal
            if (!m_LastPrefabStageVisibility && showWaterSurface)
            {
                RegisterInstance(this);
            }
            else if (m_LastPrefabStageVisibility && !showWaterSurface)
            {
                UnregisterInstance(this);
            }
            m_LastPrefabStageVisibility = showWaterSurface;
        }
#endif

        internal bool IsInstancedQuads()
        {
            return IsInfinite() || geometryType == WaterGeometryType.InstancedQuads;
        }

        internal bool IsInfinite()
        {
            return (surfaceType == WaterSurfaceType.OceanSeaLake) ? geometryType == WaterGeometryType.Infinite : false;
        }

        internal bool IsProceduralGeometry()
        {
            return !IsCustomMesh();
        }

        internal bool IsCustomMesh()
        {
            return geometryType == WaterGeometryType.Custom && meshRenderers.Count != 0;

        }

        internal bool IsQuad()
        {
            return geometryType == WaterGeometryType.Quad || (geometryType == WaterGeometryType.Custom && meshRenderers.Count == 0);
        }

        internal float3 UpVector()
        {
            return transform.up;
        }

        internal void ReleaseResources()
        {
            ReleaseSimulationResources();
            ReleaseCurrentMapResources();
            ReleaseDeformationResources();
            ReleaseWaterMaskResources();
            ReleaseFoamResources();
        }

        void OnDestroy()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);

            // Release all CPU and GPU buffers
            ReleaseResources();
        }

        /// <summary>
        /// Function that returns the caustics buffer for the water surface. If the feature is disabled or the resource is not available the function returns null.
        /// </summary>
        /// <param name="regionSize">Output parameter that returns the size of the caustics region.</param>
        /// <returns>A monochrome texture that holds the caustics simulation of the water surface.</returns>
        public Texture GetCausticsBuffer(out float regionSize)
        {
            if (caustics && simulation?.gpuBuffers?.causticsBuffer != null)
            {
                int causticsBandIndex = WaterSystem.SanitizeCausticsBand(causticsBand, simulation.numActiveBands);
                regionSize = simulation.spectrum.patchSizes[causticsBandIndex];
                return simulation.gpuBuffers.causticsBuffer;
            }
            regionSize = 0.0f;
            return null;
        }
    }
}
