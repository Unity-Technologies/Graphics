using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;
using static UnityEditor.Rendering.HighDefinition.HDProbeUI;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class WaterSurfaceEditor : Editor
    {
        // General
        static public readonly GUIContent k_SurfaceType = L10n.TextContent("Surface Type", "Specifies the nature of the water body that the water system needs to simulate.", null, null);
        static public readonly GUIContent k_GeometryType = L10n.TextContent("Geometry Type", "Specifies the type of geometry used to render the water surface. Quad stretches a unique grid over the size of the water surface. Custom allows you to specify a set of mesh renderers used to render the water surface for better culling. Instanced Quads creates a finite water surface with multiple instanced grids to keep a higher vertex density. Infinite generates a surface that extends to the far plane (or closer if the number of LODs is reached).", null, null);
        static public GUIContent[] k_GeometryTypeEnum = { new GUIContent(WaterGeometryType.Quad.ToString()), new GUIContent(WaterGeometryType.Custom.ToString()), new GUIContent(WaterGeometryType.InstancedQuads.ToString()) };
        static public readonly GUIContent k_MeshRenderers = L10n.TextContent("Mesh Renderers", "Sets the geometries to use when rendering in custom geometry type mode.", null, null);

        // CPU Simulation
        static public readonly GUIContent k_CPUFullResolution = L10n.TextContent("Full Resolution", "Specifies if the simulation is evaluated at full or half resolution in Low Latency mode. When in full resolution in Low Latency mode, the visual fidelity is higher but the cost of the simulation increases.", null, null);
        static public readonly GUIContent k_CPUEvaluateRipples = L10n.TextContent("Evaluate Ripples", "When enabled, ripples are included when evaluating the water simulation for script interactions. Including ripples allows a higher visual fidelity but the CPU cost increases.", null, null);

        // Simulation
        static public readonly GUIContent k_TimeMultiplier = L10n.TextContent("Time Multiplier", "Sets the speed of the water simulation. This allows to slow down the wave's speed or to accelerate it.", null, null);
        static public readonly GUIContent k_WaterMaskSwell = L10n.TextContent("Water Mask", "Sets the texture used to attenuate or suppress swell (red and green channels) and ripples (blue channel) water frequencies.", null, null);
        static public readonly GUIContent k_WaterMaskAgitation = L10n.TextContent("Water Mask", "Sets the texture used to attenuate or suppress agitation (red channel) and ripples (green channel) water frequencies.", null, null);
        static public readonly GUIContent k_WaterMaskRipples = L10n.TextContent("Water Mask", "Sets the texture used to attenuate or suppress ripples (red channel) water frequencies.", null, null);
        static public readonly GUIContent k_CurrentRegionExtent = L10n.TextContent("Extent", "Sets the extent of the current region in meters.", null, null);
        static public readonly GUIContent k_CurrentRegionOffset = L10n.TextContent("Offset", "Sets the offset of the current region in meters.", null, null);

        // Water Mask
        static public readonly GUIContent k_WaterMaskExtent = L10n.TextContent("Extent", "Sets the extent of the water mask in meters.", null, null);
        static public readonly GUIContent k_WaterMaskOffset = L10n.TextContent("Offset", "Sets the offset of the water mask in meters.", null, null);
        static public readonly GUIContent k_WaterMaskRemap = L10n.TextContent("Remapping", "Sets the remapped water mask value.", null, null);

        // Large current
        static public readonly GUIContent k_LargeCurrentSpeed = L10n.TextContent("Current Speed", "Sets the speed of the current for the swell in kilometers per hour. This current translates the swell at a constant speed.", null, null);
        static public readonly GUIContent k_LargeCurrentMap = L10n.TextContent("Current Map", "Sets the texture used to modify the local swell or agitation currents. The Red and Green channel of the texture contain the 2D direction of the current and Blue channel contains the influence of the current map.", null, null);
        static public readonly GUIContent k_LargeCurrentMapInfluence = L10n.TextContent("Influence", "Sets the influence of the current map over the global current. This value is used to interpolate between the global current and the current specified by the current map.", null, null);

        // Swell
        static public readonly GUIContent k_SwellRepetitionSize = L10n.TextContent("Repetition Size", "Controls the size of the water patch in meters. Larger values results in less visible repetition across the water surface. This parameter also affects the maximum amplitude of the swell frequency bands.", null, null);
        static public readonly GUIContent k_SwellOrientation = L10n.TextContent("Orientation", "Sets the orientation of distant wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the swell with a chaos value inferior to one. ", null, null);
        static public readonly GUIContent k_SwellWindSpeed = L10n.TextContent("Distant Wind Speed", "Controls the speed of the distant wind in kilometers per hour that blowed over the water surface for a long period of time. This indirectly controls the maximum amplitude and shape of the swell in a non-linear way.", null, null);
        static public readonly GUIContent k_SwellChaos = L10n.TextContent("Chaos", "Controls how directional the swell is. The lower the value, the more the swell waves are traveling in the Distant Wind Orientation.", null, null);
        static public readonly GUIContent k_SwellTotalAmplitude = L10n.TextContent("Total Amplitude", "Displays the current maximum amplitude of the swell. This is the sum of the first and second frequency bands.", null, null);

        // Swell Bands
        static public readonly GUIContent k_SwellBand0Mutliplier = L10n.TextContent("Amplitude Dimmer", "Controls the attenuation of amplitude on the first frequency band of the swell.", null, null);
        static public readonly GUIContent k_SwellBand1Mutliplier = L10n.TextContent("Amplitude Dimmer", "Controls the attenuation of amplitude on the second frequency band of the swell.", null, null);
        static public readonly GUIContent k_SwellFadeToggle = L10n.TextContent("Fade", "When enabled, HDRP starts fading the contribution of this frequency band over a given range. This helps reduce the aliasing artifacts at a distance.\nAutomatic mode computes the fading range based on the Repetition Size parameter.", null, null);
        static public readonly GUIContent k_SwellMaxAmplitude = L10n.TextContent("Max Amplitude", "Displays the current maximum amplitude of this frequency band.", null, null);

        // Agitation
        static public readonly GUIContent k_AgitationRepetitionSize = L10n.TextContent("Repetition Size", "Controls the size of the water patch in meters. Larger values results in less visible repetition across the water surface. This parameter also affects the maximum amplitude of the agitation frequency bands.", null, null);
        static public readonly GUIContent k_AgitationWindSpeed = L10n.TextContent("Distant Wind Speed", "Controls the speed of the distant wind in kilometers per hour that blowed over the water surface for a long period of time. This indirectly controls the maximum amplitude and shape of the agitation in a non-linear way.", null, null);
        static public readonly GUIContent k_AgitationOrientation = L10n.TextContent("Orientation", "Sets the orientation of distant wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the agitation with a chaos value inferior to one. ", null, null);
        static public readonly GUIContent k_AgitationChaos = L10n.TextContent("Chaos", "Controls how directional the agitation is. The lower the value, the more the agitation waves are traveling in the Distant Wind Orientation.", null, null);
        static public readonly GUIContent k_AgitationBandMutliplier = L10n.TextContent("Amplitude Dimmer", "Controls the attenuation of amplitude on the frequency band of the agitation.", null, null);
        static public readonly GUIContent k_AgitationTotalAmplitude = L10n.TextContent("Total Amplitude", "Displays the current maximum amplitude of the agitation.", null, null);

        // Fade parameters
        static public readonly GUIContent k_SwellRangeToggle = L10n.TextContent("Range", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.", null, null);
        static public readonly GUIContent k_SwellFadeStart = L10n.TextContent("Start", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.\nAutomatic mode computes the fading range based on the Repetition Size parameter.", null, null);
        static public readonly GUIContent k_SwellFadeDistance = L10n.TextContent("Distance", "Sets the length in meters during which HDRP fades out the contribution of this frequency band.", null, null);

        // Ripples
        static public readonly GUIContent k_RipplesEnable = L10n.TextContent("Ripples", "When enabled, the Water System allows you to simulate and render ripples.\nThe frequency range is not affected by the swell/agitation parameters.", null, null);
        static public readonly GUIContent k_RipplesWindSpeed = L10n.TextContent("Local Wind Speed", "Controls the speed of the local wind in kilometers per hour that is blowing over the water surface. This indirectly controls the maximum amplitude and shape of the ripples in a non-linear way.", null, null);
        static public readonly GUIContent k_RipplesChaos = L10n.TextContent("Chaos", "Controls how directional the ripples are. The lower the value, the more the ripples are traveling in the Local Wind Orientation.", null, null);
        static public readonly GUIContent k_RipplesMotionInherit = L10n.TextContent("Motion", "Specifies if the Local Wind's Orientation and Current properties are inherited from the Swell/Agitation or set independently.", null, null);
        static public readonly GUIContent k_RipplesOrientation = L10n.TextContent("Orientation", "Sets the orientation of local wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the ripples with a chaos value inferior to one.", null, null);

        // Ripples current
        static public readonly GUIContent k_RipplesCurrentSpeed = L10n.TextContent("Speed", "Sets the speed of the current for the ripples in kilometers per hour. This current translates the ripples at a constant speed in a given direction.", null, null);
        static public readonly GUIContent k_RipplesCurrentMap = L10n.TextContent("Current Map", "Sets the texture used to modify the local ripples currents. The Red and Green channel of the texture contain the 2D direction of the current and Blue channel contains the influence of the current map.", null, null);
        static public readonly GUIContent k_RipplesCurrentMapInfluence = L10n.TextContent("Influence", "Sets the influence of the current map over the global current. This value is used to interpolate between the global current and the current specified by the current map.", null, null);

        // Fade
        static public readonly GUIContent k_RipplesFadeToggle = L10n.TextContent("Fade", "When enabled, HDRP starts fading the contribution of this frequency band over a given range. This helps reduce the aliasing artifacts at a distance.", null, null);
        static public readonly GUIContent k_RipplesFadeRange = L10n.TextContent("Range", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.", null, null);
        static public readonly GUIContent k_RipplesFadeStart = L10n.TextContent("Start", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.\nAutomatic mode computes the fading range based on the Repetition Size parameter.", null, null);
        static public readonly GUIContent k_RipplesFadeDistance = L10n.TextContent("Distance", "Sets the length in meters during which HDRP fades out the contribution of this frequency band.", null, null);

        // Material
        static public readonly GUIContent k_CustomMaterial = L10n.TextContent("Custom Material", "Sets a custom material that will be used to render the water surface. If set to None a default material is used.", null, null);
        static public readonly GUIContent k_SmoothnessRange = L10n.TextContent("Value Range", "Controls the smoothness value range over the Fade region.", null, null);
        static public readonly GUIContent k_SmoothnessFadeRange = L10n.TextContent("Fade Range", "Specifies the range over which the smoothness is interpolated from close to distant.", null, null);
        static public readonly GUIContent k_SmoothnessFadeStart = L10n.TextContent("Start", "Sets the distance in meters at which HDRP starts interpolating the smoothness value for the water surface. ", null, null);
        static public readonly GUIContent k_SmoothnessFadeDistance = L10n.TextContent("Distance", "Sets the length in meters during which HDRP interpolates the smoothness value for the water surface. ", null, null);

        static public readonly GUIContent k_Amplitude = L10n.TextContent("Amplitude", "Sets the normalized (between 0.0 and 1.0) amplitude of each simulation band (from lower to higher frequencies).", null, null);

        // Refraction
        static public readonly GUIContent k_RefractionColor = L10n.TextContent("Color", "Sets the color that is used to simulate the under-water refraction.", null, null);
        static public readonly GUIContent k_MaxRefractionDistance = L10n.TextContent("Maximum Distance", "Controls the maximum distance in meters used to clamp the underwater refraction depth. Higher value increases the distortion amount.", null, null);
        static public readonly GUIContent k_AbsorptionDistance = L10n.TextContent("Absorption Distance", "Controls the approximative distance in meters that the camera can perceive through a water surface. This distance can vary widely depending on the intensity of the light the object receives.", null, null);

        // Scattering
        static public readonly GUIContent k_ScatteringColor = L10n.TextContent("Color", "Sets the color that is used to simulate the under-water refraction. The luminance of the color affects the intensity of the scattering terms.", null, null);
        static public readonly GUIContent k_AmbientScattering = L10n.TextContent("Ambient Term", "Controls the intensity of the ambient scattering term. This can be adjusted for artistic purposes.", null, null);
        static public readonly GUIContent k_HeightScattering = L10n.TextContent("Height Term", "Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.", null, null);
        static public readonly GUIContent k_DisplacementScattering = L10n.TextContent("Displacement Term", "Controls the intensity of the displacement based scattering. The bigger horizontal displacement, the more the water receives scattering. This can be adjusted for artistic purposes.", null, null);
        static public readonly GUIContent k_DirectLightTipScattering = L10n.TextContent("Direct Light Tip Term", "Controls the intensity of the direct light scattering on the tip of the waves. The effect is more perceivable at grazing angles.", null, null);
        static public readonly GUIContent k_DirectLightBodyScattering = L10n.TextContent("Direct Light Body Term", "Controls the intensity of the direct light scattering on the body of the waves. The effect is more perceivable at grazing angles.", null, null);

        static public readonly GUIContent k_Caustics = L10n.TextContent("Caustics", "When enabled, the water surface will render caustics.", null, null);
        static public readonly GUIContent k_CausticsInstensity = L10n.TextContent("Intensity", "Sets the intensity of the under-water caustics.", null, null);
        static public readonly GUIContent k_CausticsVirtualPlaneDistance = L10n.TextContent("Virtual Plane Distance", "Sets the distance at which the simulated caustics are projected. High values generate sharper caustics but can cause artefacts. The larger the waves are, the further the plane distance should be to obtain sharp caustics.", null, null);
        static public readonly GUIContent k_CausticsTilingFactor = L10n.TextContent("Tiling Factor", "Sets a tiling factor for the water caustics.", null, null);
        static public readonly GUIContent k_CausticsBandSwell = L10n.TextContent("Simulation Band", "Controls which simulation band is used for the caustics evaluation. The first (index 0) and second band (index 1) come from the swell simulation and the third (index 2) one from the ripples.", null, null);
        static public readonly GUIContent k_CausticsBandAgitation = L10n.TextContent("Simulation Band", "Controls which simulation band is used for the caustics evaluation. The first band (index 0) comes from the agitation simulation and the second one (index 1) from the ripples.", null, null);
        static public readonly GUIContent k_CausticsDirectionalShadow = L10n.TextContent("Directional Shadow", "When enabled, the water caustics will take into account the directional light's shadow.", null, null);
        static public readonly GUIContent k_CausticsDirectionalShadowDimmer = L10n.TextContent("Directional Shadow Dimmer", "Sets the water caustics dimmer value for the directional shadow.", null, null);

        static public readonly GUIContent k_FoamSmoothness = L10n.TextContent("Smoothness", "Controls the simulation foam smoothness.", null, null);
        static public readonly GUIContent k_FoamPersistenceMultiplier = L10n.TextContent("Persistence Multiplier", "Specifies the decal foam persistence multiplier. A higher value will lead to the foam remaining visible longer.\nThis option requires Foam to be enabled in the Water Decals section.", null, null);
        static public readonly GUIContent k_FoamCurrentInfluence = L10n.TextContent("Current Influence", "Specifies the influence of the swell current on foam. A value of zero means foam stays still, a value of one makes the foam match with current direction and speed. Ripples motion have no impact on foam.", null, null);
        static public readonly GUIContent k_FoamTextureTiling = L10n.TextContent("Texture Tiling", "Sets the per meter tiling for the foam texture.", null, null);
        static public readonly GUIContent k_FoamColor = L10n.TextContent("Color", "Sets the foam color.", null, null);
        static public readonly GUIContent k_SimulationFoam = L10n.TextContent("Simulation Foam", "When enabled, the water simulation will generate foam on the tip of the waves.", null, null);
        static public readonly GUIContent k_SimulationFoamAmount = L10n.TextContent("Amount", "Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the Distant Wind Speed.", null, null);
        static public readonly GUIContent k_SimulationFoamMask = L10n.TextContent("Mask", "Sets the texture used to attenuate or suppress the simulation foam. The red channel of the texture is used for the masking.", null, null);
        static public readonly GUIContent k_WindFoamCurve = L10n.TextContent("Wind Speed Dimmer", "Controls the foam intensity depending on the normalized Distant Wind Speed. The X axis refers to the normalized Distant Wind Speed, the Y axis refers to the dimmer value.", null, null);

        // Underwater
        public static readonly GUIContent k_UnderWater = L10n.TextContent("Underwater", "When enabled, HDRP will apply a fog and color shift to the final image when the camera is under the surface. This feature has a cost even when the camera is above the water surface.", null, null);
        public static readonly string k_AddColliderMessage = "The water surface does not support an underwater scenario if the box collider is not set.";
        public static readonly GUIContent k_AddBoxColliderPopup = L10n.TextContent("Add a Box Collider", null, null, null);
        public static readonly GUIContent k_UseBoxColliderPopup = L10n.TextContent("Use an existing Box Collider", null, null, null);
        public static readonly GUIContent k_UnderWaterRefraction = L10n.TextContent("Screen Space Refraction", "When enabled, the water surface will refract light when looking at objects from underwater. Distant and Local Wind will still have an effect on refraction.\nThis setting only affects screen space refraction when sampling the color pyramid from underwater.", null, null);

        // Foam
        static public readonly GUIContent k_FoamMaskExtent = L10n.TextContent("Extent", "Sets the extent of the foam mask in meters.", null, null);
        static public readonly GUIContent k_FoamMaskOffset = L10n.TextContent("Offset", "Sets the offset of the foam mask in meters.", null, null);

        // Misc
        static public readonly GUIContent k_DebugMode = L10n.TextContent("Debug Mode", "Sets the debug mode for the water surface.", null, null);
        static public readonly GUIContent k_WaterMaskDebugMode = L10n.TextContent("Water Mask Mode", "Sets the water mask debug mode for a given water surface.", null, null);
        static public readonly GUIContent k_WaterCurrentDebugMode = L10n.TextContent("Water Current Mode", "Sets the water current debug mode for a given water surface.", null, null);
        static public readonly GUIContent k_CurrentDebugMultiplier = L10n.TextContent("Arrow Density Multiplier", "Sets a multiplier for the arrow density in the current debug mode.", null, null);
        static public readonly GUIContent k_WaterFoamDebugMode = L10n.TextContent("Water Foam Mode", "Sets the water foam debug mode for a given water surface.", null, null);
    }
}
