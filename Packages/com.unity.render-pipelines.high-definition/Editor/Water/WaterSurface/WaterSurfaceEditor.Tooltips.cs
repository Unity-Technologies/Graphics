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
        static public readonly GUIContent k_SurfaceType = EditorGUIUtility.TrTextContent("Surface Type", "Specifies the nature of the water body that the water system needs to simulate.");
        static public readonly GUIContent k_GeometryType = EditorGUIUtility.TrTextContent("Geometry Type", "Specifies the type of geometry used to render the water surface. Quad stretches a unique grid over the size of the water surface. Custom allows you to specify a set of mesh renderers used to render the water surface for better culling. Instanced Quads creates a finite water surface with multiple instanced grids to keep a higher vertex density. Infinite generates a surface that extends to the far plane (or closer if the number of LODs is reached).");
        static public GUIContent[] k_GeometryTypeEnum = { new GUIContent(WaterGeometryType.Quad.ToString()), new GUIContent(WaterGeometryType.Custom.ToString()), new GUIContent(WaterGeometryType.InstancedQuads.ToString()) };
        static public readonly GUIContent k_MeshRenderers = EditorGUIUtility.TrTextContent("Mesh Renderers", "Sets the geometries to use when rendering in custom geometry type mode.");

        // CPU Simulation
        static public readonly GUIContent k_CPUFullResolution = EditorGUIUtility.TrTextContent("Full Resolution", "Specifies if the simulation is evaluated at full or half resolution in Low Latency mode. When in full resolution in Low Latency mode, the visual fidelity is higher but the cost of the simulation increases.");
        static public readonly GUIContent k_CPUEvaluateRipples = EditorGUIUtility.TrTextContent("Evaluate Ripples", "When enabled, ripples are included when evaluating the water simulation for script interactions. Including ripples allows a higher visual fidelity but the CPU cost increases.");

        // Simulation
        static public readonly GUIContent k_TimeMultiplier = EditorGUIUtility.TrTextContent("Time Multiplier", "Sets the speed of the water simulation. This allows to slow down the wave's speed or to accelerate it.");
        static public readonly GUIContent k_WaterMaskSwell = EditorGUIUtility.TrTextContent("Water Mask", "Sets the texture used to attenuate or suppress swell (red and green channels) and ripples (blue channel) water frequencies.");
        static public readonly GUIContent k_WaterMaskAgitation = EditorGUIUtility.TrTextContent("Water Mask", "Sets the texture used to attenuate or suppress agitation (red channel) and ripples (green channel) water frequencies.");
        static public readonly GUIContent k_WaterMaskRipples = EditorGUIUtility.TrTextContent("Water Mask", "Sets the texture used to attenuate or suppress ripples (red channel) water frequencies.");
        static public readonly GUIContent k_CurrentRegionExtent = EditorGUIUtility.TrTextContent("Extent", "Sets the extent of the current region in meters.");
        static public readonly GUIContent k_CurrentRegionOffset = EditorGUIUtility.TrTextContent("Offset", "Sets the offset of the current region in meters.");

        // Water Mask
        static public readonly GUIContent k_WaterMaskExtent = EditorGUIUtility.TrTextContent("Extent", "Sets the extent of the water mask in meters.");
        static public readonly GUIContent k_WaterMaskOffset = EditorGUIUtility.TrTextContent("Offset", "Sets the offset of the water mask in meters.");
        static public readonly GUIContent k_WaterMaskRemap = EditorGUIUtility.TrTextContent("Remapping", "Sets the remapped water mask value.");

        // Large current
        static public readonly GUIContent k_LargeCurrentSpeed = EditorGUIUtility.TrTextContent("Current Speed", "Sets the speed of the current for the swell in kilometers per hour. This current translates the swell at a constant speed.");
        static public readonly GUIContent k_LargeCurrentMap = EditorGUIUtility.TrTextContent("Current Map", "Sets the texture used to modify the local swell or agitation currents. The Red and Green channel of the texture contain the 2D direction of the current and Blue channel contains the influence of the current map.");
        static public readonly GUIContent k_LargeCurrentMapInfluence = EditorGUIUtility.TrTextContent("Influence", "Sets the influence of the current map over the global current. This value is used to interpolate between the global current and the current specified by the current map.");

        // Swell
        static public readonly GUIContent k_SwellRepetitionSize = EditorGUIUtility.TrTextContent("Repetition Size", "Controls the size of the water patch in meters. Larger values results in less visible repetition across the water surface. This parameter also affects the maximum amplitude of the swell frequency bands.");
        static public readonly GUIContent k_SwellOrientation = EditorGUIUtility.TrTextContent("Orientation", "Sets the orientation of distant wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the swell with a chaos value inferior to one. ");
        static public readonly GUIContent k_SwellWindSpeed = EditorGUIUtility.TrTextContent("Distant Wind Speed", "Controls the speed of the distant wind in kilometers per hour that blowed over the water surface for a long period of time. This indirectly controls the maximum amplitude and shape of the swell in a non-linear way.");
        static public readonly GUIContent k_SwellChaos = EditorGUIUtility.TrTextContent("Chaos", "Controls how directional the swell is. The lower the value, the more the swell waves are traveling in the Distant Wind Orientation.");
        static public readonly GUIContent k_SwellTotalAmplitude = EditorGUIUtility.TrTextContent("Total Amplitude", "Displays the current maximum amplitude of the swell. This is the sum of the first and second frequency bands.");

        // Swell Bands
        static public readonly GUIContent k_SwellBand0Mutliplier = EditorGUIUtility.TrTextContent("Amplitude Dimmer", "Controls the attenuation of amplitude on the first frequency band of the swell.");
        static public readonly GUIContent k_SwellBand1Mutliplier = EditorGUIUtility.TrTextContent("Amplitude Dimmer", "Controls the attenuation of amplitude on the second frequency band of the swell.");
        static public readonly GUIContent k_SwellFadeToggle = EditorGUIUtility.TrTextContent("Fade", "When enabled, HDRP starts fading the contribution of this frequency band over a given range. This helps reduce the aliasing artifacts at a distance.\nAutomatic mode computes the fading range based on the Repetition Size parameter.");
        static public readonly GUIContent k_SwellMaxAmplitude = EditorGUIUtility.TrTextContent("Max Amplitude", "Displays the current maximum amplitude of this frequency band.");

        // Agitation
        static public readonly GUIContent k_AgitationRepetitionSize = EditorGUIUtility.TrTextContent("Repetition Size", "Controls the size of the water patch in meters. Larger values results in less visible repetition across the water surface. This parameter also affects the maximum amplitude of the agitation frequency bands.");
        static public readonly GUIContent k_AgitationWindSpeed = EditorGUIUtility.TrTextContent("Distant Wind Speed", "Controls the speed of the distant wind in kilometers per hour that blowed over the water surface for a long period of time. This indirectly controls the maximum amplitude and shape of the agitation in a non-linear way.");
        static public readonly GUIContent k_AgitationOrientation = EditorGUIUtility.TrTextContent("Orientation", "Sets the orientation of distant wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the agitation with a chaos value inferior to one. ");
        static public readonly GUIContent k_AgitationChaos = EditorGUIUtility.TrTextContent("Chaos", "Controls how directional the agitation is. The lower the value, the more the agitation waves are traveling in the Distant Wind Orientation.");
        static public readonly GUIContent k_AgitationBandMutliplier = EditorGUIUtility.TrTextContent("Amplitude Dimmer", "Controls the attenuation of amplitude on the frequency band of the agitation.");
        static public readonly GUIContent k_AgitationTotalAmplitude = EditorGUIUtility.TrTextContent("Total Amplitude", "Displays the current maximum amplitude of the agitation.");

        // Fade parameters
        static public readonly GUIContent k_SwellRangeToggle = EditorGUIUtility.TrTextContent("Range", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_SwellFadeStart = EditorGUIUtility.TrTextContent("Start", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.\nAutomatic mode computes the fading range based on the Repetition Size parameter.");
        static public readonly GUIContent k_SwellFadeDistance = EditorGUIUtility.TrTextContent("Distance", "Sets the length in meters during which HDRP fades out the contribution of this frequency band.");

        // Ripples
        static public readonly GUIContent k_RipplesEnable = EditorGUIUtility.TrTextContent("Ripples", "When enabled, the Water System allows you to simulate and render ripples.\nThe frequency range is not affected by the swell/agitation parameters.");
        static public readonly GUIContent k_RipplesWindSpeed = EditorGUIUtility.TrTextContent("Local Wind Speed", "Controls the speed of the local wind in kilometers per hour that is blowing over the water surface. This indirectly controls the maximum amplitude and shape of the ripples in a non-linear way.");
        static public readonly GUIContent k_RipplesChaos = EditorGUIUtility.TrTextContent("Chaos", "Controls how directional the ripples are. The lower the value, the more the ripples are traveling in the Local Wind Orientation.");
        static public readonly GUIContent k_RipplesMotionInherit = EditorGUIUtility.TrTextContent("Motion", "Specifies if the Local Wind's Orientation and Current properties are inherited from the Swell/Agitation or set independently.");
        static public readonly GUIContent k_RipplesOrientation = EditorGUIUtility.TrTextContent("Orientation", "Sets the orientation of local wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the ripples with a chaos value inferior to one.");

        // Ripples current
        static public readonly GUIContent k_RipplesCurrentSpeed = EditorGUIUtility.TrTextContent("Speed", "Sets the speed of the current for the ripples in kilometers per hour. This current translates the ripples at a constant speed in a given direction.");
        static public readonly GUIContent k_RipplesCurrentMap = EditorGUIUtility.TrTextContent("Current Map", "Sets the texture used to modify the local ripples currents. The Red and Green channel of the texture contain the 2D direction of the current and Blue channel contains the influence of the current map.");
        static public readonly GUIContent k_RipplesCurrentMapInfluence = EditorGUIUtility.TrTextContent("Influence", "Sets the influence of the current map over the global current. This value is used to interpolate between the global current and the current specified by the current map.");

        // Fade
        static public readonly GUIContent k_RipplesFadeToggle = EditorGUIUtility.TrTextContent("Fade", "When enabled, HDRP starts fading the contribution of this frequency band over a given range. This helps reduce the aliasing artifacts at a distance.");
        static public readonly GUIContent k_RipplesFadeRange = EditorGUIUtility.TrTextContent("Range", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_RipplesFadeStart = EditorGUIUtility.TrTextContent("Start", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.\nAutomatic mode computes the fading range based on the Repetition Size parameter.");
        static public readonly GUIContent k_RipplesFadeDistance = EditorGUIUtility.TrTextContent("Distance", "Sets the length in meters during which HDRP fades out the contribution of this frequency band.");

        // Material
        static public readonly GUIContent k_CustomMaterial = EditorGUIUtility.TrTextContent("Custom Material", "Sets a custom material that will be used to render the water surface. If set to None a default material is used.");
        static public readonly GUIContent k_SmoothnessRange = EditorGUIUtility.TrTextContent("Value Range", "Controls the smoothness value range over the Fade region.");
        static public readonly GUIContent k_SmoothnessFadeRange = EditorGUIUtility.TrTextContent("Fade Range", "Specifies the range over which the smoothness is interpolated from close to distant.");
        static public readonly GUIContent k_SmoothnessFadeStart = EditorGUIUtility.TrTextContent("Start", "Sets the distance in meters at which HDRP starts interpolating the smoothness value for the water surface. ");
        static public readonly GUIContent k_SmoothnessFadeDistance = EditorGUIUtility.TrTextContent("Distance", "Sets the length in meters during which HDRP interpolates the smoothness value for the water surface. ");

        static public readonly GUIContent k_Amplitude = EditorGUIUtility.TrTextContent("Amplitude", "Sets the normalized (between 0.0 and 1.0) amplitude of each simulation band (from lower to higher frequencies).");

        // Refraction
        static public readonly GUIContent k_RefractionColor = EditorGUIUtility.TrTextContent("Color", "Sets the color that is used to simulate the under-water refraction.");
        static public readonly GUIContent k_MaxRefractionDistance = EditorGUIUtility.TrTextContent("Maximum Distance", "Controls the maximum distance in meters used to clamp the underwater refraction depth. Higher value increases the distortion amount.");
        static public readonly GUIContent k_AbsorptionDistance = EditorGUIUtility.TrTextContent("Absorption Distance", "Controls the approximative distance in meters that the camera can perceive through a water surface. This distance can vary widely depending on the intensity of the light the object receives.");

        // Scattering
        static public readonly GUIContent k_ScatteringColor = EditorGUIUtility.TrTextContent("Color", "Sets the color that is used to simulate the under-water refraction. The luminance of the color affects the intensity of the scattering terms.");
        static public readonly GUIContent k_AmbientScattering = EditorGUIUtility.TrTextContent("Ambient Term", "Controls the intensity of the ambient scattering term. This can be adjusted for artistic purposes.");
        static public readonly GUIContent k_HeightScattering = EditorGUIUtility.TrTextContent("Height Term", "Controls the intensity of the height based scattering. The higher the vertical displacement, the more the water receives scattering. This can be adjusted for artistic purposes.");
        static public readonly GUIContent k_DisplacementScattering = EditorGUIUtility.TrTextContent("Displacement Term", "Controls the intensity of the displacement based scattering. The bigger horizontal displacement, the more the water receives scattering. This can be adjusted for artistic purposes.");
        static public readonly GUIContent k_DirectLightTipScattering = EditorGUIUtility.TrTextContent("Direct Light Tip Term", "Controls the intensity of the direct light scattering on the tip of the waves. The effect is more perceivable at grazing angles.");
        static public readonly GUIContent k_DirectLightBodyScattering = EditorGUIUtility.TrTextContent("Direct Light Body Term", "Controls the intensity of the direct light scattering on the body of the waves. The effect is more perceivable at grazing angles.");

        static public readonly GUIContent k_Caustics = EditorGUIUtility.TrTextContent("Caustics", "When enabled, the water surface will render caustics.");
        static public readonly GUIContent k_CausticsInstensity = EditorGUIUtility.TrTextContent("Intensity", "Sets the intensity of the under-water caustics.");
        static public readonly GUIContent k_CausticsVirtualPlaneDistance = EditorGUIUtility.TrTextContent("Virtual Plane Distance", "Sets the distance at which the simulated caustics are projected. High values generate sharper caustics but can cause artefacts. The larger the waves are, the further the plane distance should be to obtain sharp caustics.");
        static public readonly GUIContent k_CausticsTilingFactor = EditorGUIUtility.TrTextContent("Tiling Factor", "Sets a tiling factor for the water caustics.");
        static public readonly GUIContent k_CausticsBandSwell = EditorGUIUtility.TrTextContent("Simulation Band", "Controls which simulation band is used for the caustics evaluation. The first (index 0) and second band (index 1) come from the swell simulation and the third (index 2) one from the ripples.");
        static public readonly GUIContent k_CausticsBandAgitation = EditorGUIUtility.TrTextContent("Simulation Band", "Controls which simulation band is used for the caustics evaluation. The first band (index 0) comes from the agitation simulation and the second one (index 1) from the ripples.");
        static public readonly GUIContent k_CausticsDirectionalShadow = EditorGUIUtility.TrTextContent("Directional Shadow", "When enabled, the water caustics will take into account the directional light's shadow.");
        static public readonly GUIContent k_CausticsDirectionalShadowDimmer = EditorGUIUtility.TrTextContent("Directional Shadow Dimmer", "Sets the water caustics dimmer value for the directional shadow.");

        static public readonly GUIContent k_FoamSmoothness = EditorGUIUtility.TrTextContent("Smoothness", "Controls the simulation foam smoothness.");
        static public readonly GUIContent k_FoamPersistenceMultiplier = EditorGUIUtility.TrTextContent("Persistence Multiplier", "Specifies the decal foam persistence multiplier. A higher value will lead to the foam remaining visible longer.\nThis option requires Foam to be enabled in the Water Decals section.");
        static public readonly GUIContent k_FoamCurrentInfluence = EditorGUIUtility.TrTextContent("Current Influence", "Specifies the influence of the swell current on foam. A value of zero means foam stays still, a value of one makes the foam match with current direction and speed. Ripples motion have no impact on foam.");
        static public readonly GUIContent k_FoamTextureTiling = EditorGUIUtility.TrTextContent("Texture Tiling", "Sets the per meter tiling for the foam texture.");
        static public readonly GUIContent k_FoamColor = EditorGUIUtility.TrTextContent("Color", "Sets the foam color.");
        static public readonly GUIContent k_SimulationFoam = EditorGUIUtility.TrTextContent("Simulation Foam", "When enabled, the water simulation will generate foam on the tip of the waves.");
        static public readonly GUIContent k_SimulationFoamAmount = EditorGUIUtility.TrTextContent("Amount", "Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the Distant Wind Speed.");
        static public readonly GUIContent k_SimulationFoamMask = EditorGUIUtility.TrTextContent("Mask", "Sets the texture used to attenuate or suppress the simulation foam. The red channel of the texture is used for the masking.");
        static public readonly GUIContent k_WindFoamCurve = EditorGUIUtility.TrTextContent("Wind Speed Dimmer", "Controls the foam intensity depending on the normalized Distant Wind Speed. The X axis refers to the normalized Distant Wind Speed, the Y axis refers to the dimmer value.");

        // Underwater
        public static readonly GUIContent k_UnderWater = EditorGUIUtility.TrTextContent("Underwater", "When enabled, HDRP will apply a fog and color shift to the final image when the camera is under the surface. This feature has a cost even when the camera is above the water surface.");
        public static readonly string k_AddColliderMessage = "The water surface does not support an underwater scenario if the box collider is not set.";
        public static readonly GUIContent k_AddBoxColliderPopup = EditorGUIUtility.TrTextContent("Add a Box Collider");
        public static readonly GUIContent k_UseBoxColliderPopup = EditorGUIUtility.TrTextContent("Use an existing Box Collider");
        public static readonly GUIContent k_UnderWaterRefraction = EditorGUIUtility.TrTextContent("Screen Space Refraction", "When enabled, the water surface will refract light when looking at objects from underwater. Distant and Local Wind will still have an effect on refraction.\nThis setting only affects screen space refraction when sampling the color pyramid from underwater.");

        // Foam
        static public readonly GUIContent k_FoamMaskExtent = EditorGUIUtility.TrTextContent("Extent", "Sets the extent of the foam mask in meters.");
        static public readonly GUIContent k_FoamMaskOffset = EditorGUIUtility.TrTextContent("Offset", "Sets the offset of the foam mask in meters.");

        // Misc
        static public readonly GUIContent k_DebugMode = EditorGUIUtility.TrTextContent("Debug Mode", "Sets the debug mode for the water surface.");
        static public readonly GUIContent k_WaterMaskDebugMode = EditorGUIUtility.TrTextContent("Water Mask Mode", "Sets the water mask debug mode for a given water surface.");
        static public readonly GUIContent k_WaterCurrentDebugMode = EditorGUIUtility.TrTextContent("Water Current Mode", "Sets the water current debug mode for a given water surface.");
        static public readonly GUIContent k_CurrentDebugMultiplier = EditorGUIUtility.TrTextContent("Arrow Density Multiplier", "Sets a multiplier for the arrow density in the current debug mode.");
        static public readonly GUIContent k_WaterFoamDebugMode = EditorGUIUtility.TrTextContent("Water Foam Mode", "Sets the water foam debug mode for a given water surface.");
    }
}
