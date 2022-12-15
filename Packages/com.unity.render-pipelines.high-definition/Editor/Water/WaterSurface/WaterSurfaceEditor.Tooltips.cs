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
        static public string[] k_GeometryTypeEnum = { WaterGeometryType.Quad.ToString(), WaterGeometryType.Custom.ToString(), WaterGeometryType.InstancedQuads.ToString() };
        static public readonly GUIContent k_MeshRenderers = EditorGUIUtility.TrTextContent("Mesh Renderers", "Sets the geometries to use when rendering in custom geometry type mode.");
        public static readonly string k_FixTransform = "Infinite water surfaces do not support rotation or scale.";
        public static readonly GUIContent k_ResetTransformPopup = EditorGUIUtility.TrTextContent("Reset Transform");

        // CPU Simulation
        static public readonly GUIContent k_CPUSimulation = EditorGUIUtility.TrTextContent("Script Interactions", "When enabled, HDRP will evaluate the water simulation on the CPU for C# script height requests. Enabling this will significantly increase the CPU cost of the feature.");
        static public readonly GUIContent k_CPUFullResolution = EditorGUIUtility.TrTextContent("Full Resolution", "Specifies if the CPU simulation should be evaluated at full or half resolution. When in full resolution, the visual fidelity will be higher but the cost of the simulation will increase.");
        static public readonly GUIContent k_CPUEvaluateRipples = EditorGUIUtility.TrTextContent("Evaluate Ripples", "When enabled, HDRP replicates the ripples on the CPU side when evaluating the water simulation for script interaction. Including ripples will allow a higher visual fidelity but the cost of the simulation will increase.");

        // Simulation
        static public readonly GUIContent k_TimeMultiplier = EditorGUIUtility.TrTextContent("Time Multiplier", "Sets the speed of the water simulation. This allows to slow down the wave's speed or to accelerate it.");
        static public readonly GUIContent k_WaterMaskSwell = EditorGUIUtility.TrTextContent("Water Mask", "Sets the texture used to attenuate or suppress swell (red and green channels) and ripples (blue channel) water frequencies.");
        static public readonly GUIContent k_WaterMaskAgitation = EditorGUIUtility.TrTextContent("Water Mask", "Sets the texture used to attenuate or suppress agitation (red channel) and ripples (green channel) water frequencies.");
        static public readonly GUIContent k_WaterMaskRipples = EditorGUIUtility.TrTextContent("Water Mask", "Sets the texture used to attenuate or suppress ripples (red channel) water frequencies.");

        static public readonly GUIContent k_WaterMaskExtent = EditorGUIUtility.TrTextContent("Extent", "Sets the extent of the foam mask in meters.");
        static public readonly GUIContent k_WaterMaskOffset = EditorGUIUtility.TrTextContent("Offset", "Sets the offset of the foam mask in meters.");
        static public readonly GUIContent k_WaterMaskRemap = EditorGUIUtility.TrTextContent("Remapping", "Sets the remapped water mask value.");

        // Large current
        static public readonly GUIContent k_LargeCurrentSpeed = EditorGUIUtility.TrTextContent("Current Speed", "Sets the speed of the current for the swell in kilometers per hour. This current translates the swell at a constant speed.");
        static public readonly GUIContent k_LargeCurrentMap = EditorGUIUtility.TrTextContent("Current Map", "Sets the texture used to modify the local swell or agitation currents. The Red and Green channel of the texture contain the 2D direction of the current and Blue channel contains the influence of the current map.");
        static public readonly GUIContent k_LargeCurrentRegionExtent = EditorGUIUtility.TrTextContent("Region Extent", "Sets extent in meters of the current region.");
        static public readonly GUIContent k_LargeCurrentRegionOffset = EditorGUIUtility.TrTextContent("Region Offset", "Sets offset in meters of the current region.");
        static public readonly GUIContent k_LargeCurrentMapInfluence = EditorGUIUtility.TrTextContent("Influence", "Sets the influence of the current map over the global current. This value is used to interpolate between the global current and the current specified by the current map.");

        // Swell
        static public readonly GUIContent k_SwellRepetitionSize = EditorGUIUtility.TrTextContent("Repetition Size", "Controls the size of the water patch in meters. Larger values results in less visible repetition across the water surface. This parameter also affects the maximum amplitude of the swell frequency bands.");
        static public readonly GUIContent k_SwellOrientation = EditorGUIUtility.TrTextContent("Orientation", "Sets the orientation of distant wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the swell with a chaos value inferior to one. ");
        static public readonly GUIContent k_SwellWindSpeed = EditorGUIUtility.TrTextContent("Distant Wind Speed", "Controls the speed of the distant wind in kilometers per hour that blowed over the water surface for a long period of time. This indirectly controls the maximum amplitude and shape of the swell in a non-linear way.");
        static public readonly GUIContent k_SwellChaos = EditorGUIUtility.TrTextContent("Chaos", "Controls how directional the swell is. The lower the value, the more the swell waves are traveling in the Distant Wind Orientation.");
        static public readonly GUIContent k_SwellTotalAmplitude = EditorGUIUtility.TrTextContent("Total Amplitude", "Displays the current maximum amplitude of the swell. This is the sum of the first and second frequency bands.");

        // Swell Bands
        static public readonly GUIContent k_SwellBand0Mutliplier = EditorGUIUtility.TrTextContent("Amplitude Multiplier", "Controls the attenuation of amplitude on the first frequency band of the swell.");
        static public readonly GUIContent k_SwellBand1Mutliplier = EditorGUIUtility.TrTextContent("Amplitude Multiplier", "Controls the attenuation of amplitude on the second frequency band of the swell.");
        static public readonly GUIContent k_SwellFadeToggle = EditorGUIUtility.TrTextContent("Fade", "When enabled, HDRP starts fading the contribution of this frequency band over a given range. This helps reduce the aliasing artifacts at a distance.");
        static public readonly GUIContent k_SwellMaxAmplitude = EditorGUIUtility.TrTextContent("Max Amplitude", "Displays the current maximum amplitude of this frequency band.");

        // Agitation
        static public readonly GUIContent k_AgitationRepetitionSize = EditorGUIUtility.TrTextContent("Repetition Size", "Controls the size of the water patch in meters. Larger values results in less visible repetition across the water surface. This parameter also affects the maximum amplitude of the agitation frequency bands.");
        static public readonly GUIContent k_AgitationWindSpeed = EditorGUIUtility.TrTextContent("Distant Wind Speed", "Controls the speed of the distant wind in kilometers per hour that blowed over the water surface for a long period of time. This indirectly controls the maximum amplitude and shape of the agitation in a non-linear way.");
        static public readonly GUIContent k_AgitationOrientation = EditorGUIUtility.TrTextContent("Orientation", "Sets the orientation of distant wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the agitation with a chaos value inferior to one. ");
        static public readonly GUIContent k_AgitationChaos = EditorGUIUtility.TrTextContent("Chaos", "Controls how directional the agitation is. The lower the value, the more the agitation waves are traveling in the Distant Wind Orientation.");
        static public readonly GUIContent k_AgitationBandMutliplier = EditorGUIUtility.TrTextContent("Amplitude Multiplier", "Controls the attenuation of amplitude on the frequency band of the agitation.");
        static public readonly GUIContent k_AgitationTotalAmplitude = EditorGUIUtility.TrTextContent("Total Amplitude", "Displays the current maximum amplitude of the agitation.");

        // Fade parameters
        static public readonly GUIContent k_SwellRangeToggle = EditorGUIUtility.TrTextContent("Range", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_SwellFadeStart = EditorGUIUtility.TrTextContent("Start", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_SwellFadeDistance = EditorGUIUtility.TrTextContent("Distance", "Sets the length in meters during which HDRP fades out the contribution of this frequency band.");

        // Ripples
        static public readonly GUIContent k_RipplesEnable = EditorGUIUtility.TrTextContent("Ripples", "When enabled, the Water System allows you to simulate and render ripples for finer details. The frequencies range is not affected by the swell/agitation parameters. When enabled, the Water System allows you to simulate and render water ripples.");
        static public readonly GUIContent k_RipplesWindSpeed = EditorGUIUtility.TrTextContent("Local Wind Speed", "Controls the speed of the local wind in kilometers per hour that is blowing over the water surface.This indirectly controls the maximum amplitude and shape of the ripples in a non-linear way.");
        static public readonly GUIContent k_RipplesChaos = EditorGUIUtility.TrTextContent("Chaos", "Controls how directional the ripples are. The lower the value, the more the ripples are traveling in the Local Wind Orientation.");
        static public readonly GUIContent k_RipplesMotionInherit = EditorGUIUtility.TrTextContent("Motion", "Specifies if the Local Wind's Orientation and Current properties are inherited from the Swell/Agitation or set independently.");
        static public readonly GUIContent k_RipplesOrientation = EditorGUIUtility.TrTextContent("Orientation", "Sets the orientation of local wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the ripples with a chaos value inferior to one.");

        // Ripples current
        static public readonly GUIContent k_RipplesCurrentSpeed = EditorGUIUtility.TrTextContent("Speed", "Sets the speed of the current for the ripples in kilometers per hour. This current translates the ripples at a constant speed in a given direction.");
        static public readonly GUIContent k_RipplesCurrentMap = EditorGUIUtility.TrTextContent("Current Map", "Sets the texture used to modify the local ripples currents. The Red and Green channel of the texture contain the 2D direction of the current and Blue channel contains the influence of the current map.");
        static public readonly GUIContent k_RipplesCurrentRegionExtent = EditorGUIUtility.TrTextContent("Region Extent", "Sets extent in meters of the current region.");
        static public readonly GUIContent k_RipplesCurrentRegionOffset = EditorGUIUtility.TrTextContent("Region Offset", "Sets offset in meters of the current region.");
        static public readonly GUIContent k_RipplesCurrentMapInfluence = EditorGUIUtility.TrTextContent("Influence", "Sets the influence of the current map over the global current. This value is used to interpolate between the global current and the current specified by the current map.");

        // Fade
        static public readonly GUIContent k_RipplesFadeToggle = EditorGUIUtility.TrTextContent("Fade", "When enabled, HDRP starts fading the contribution of this frequency band over a given range. This helps reduce the aliasing artifacts at a distance.");
        static public readonly GUIContent k_RipplesFadeRange = EditorGUIUtility.TrTextContent("Range", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_RipplesFadeStart = EditorGUIUtility.TrTextContent("Start", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_RipplesFadeDistance = EditorGUIUtility.TrTextContent("Distance", "Sets the length in meters during which HDRP fades out the contribution of this frequency band.");

        // Material
        static public readonly GUIContent k_CustomMaterial = EditorGUIUtility.TrTextContent("Custom Material", "Sets a custom material that will be used to render the water surface. If set to None a default material is used.");
        static public readonly GUIContent k_SmoothnessRange = EditorGUIUtility.TrTextContent("Value Range", "Controls the smoothness value range over the Fade region.");
        static public readonly GUIContent k_SmoothnessFadeRange = EditorGUIUtility.TrTextContent("Fade Range", "Specifies the range over which the smoothness is interpolated from close to distant.");
        static public readonly GUIContent k_SmoothnessFadeStart = EditorGUIUtility.TrTextContent("Start", "Sets the distance in meters at which HDRP starts interpolating the smoothness value for the water surface. ");
        static public readonly GUIContent k_SmoothnessFadeDistance = EditorGUIUtility.TrTextContent("Distance", "Sets the length in meters during which HDRP interpolates the smoothness value for the water surface. ");
        public static readonly GUIContent k_WaterNewLMaterialLabel = EditorGUIUtility.TrTextContent("New", "Create a new water material.");

        static public readonly GUIContent k_Amplitude = EditorGUIUtility.TrTextContent("Amplitude", "Sets the normalized (between 0.0 and 1.0) amplitude of each simulation band (from lower to higher frequencies).");
        static public readonly GUIContent k_Choppiness = EditorGUIUtility.TrTextContent("Choppiness", "Sets the choppiness factor the waves. Higher values combined with high wind speed may introduce visual artifacts.");

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
        static public readonly GUIContent k_CausticsVirtualPlaneDistance = EditorGUIUtility.TrTextContent("Virtual Plane Distance", "Sets the distance at which the simulated caustics are projected. High values generate sharper caustics but can cause artefacts. The larger the waves are, the further the plane distance should be to obtain sharp caustics.");
        static public readonly GUIContent k_CausticsBandSwell = EditorGUIUtility.TrTextContent("Simulation Band", "Controls which simulation band is used for the caustics evaluation. The first (index 0) and second band (index 1) come from the swell simulation and the third (index 2) one from the ripples.");
        static public readonly GUIContent k_CausticsBandAgitation = EditorGUIUtility.TrTextContent("Simulation Band", "Controls which simulation band is used for the caustics evaluation. The first band (index 0) comes from the agitation simulation and the second one (index 1) from the ripples.");

        static public readonly GUIContent k_SimulationFoamSmoothness = EditorGUIUtility.TrTextContent("Simulation Foam Smoothness", "Controls the simulation foam smoothness.");
        static public readonly GUIContent k_SimulationFoamDrag = EditorGUIUtility.TrTextContent("Simulation Foam Drag", "Controls the life span of the surface foam. A higher value will cause the foam to persist longer and leave a trail.");
        static public readonly GUIContent k_SimulationFoamAmount = EditorGUIUtility.TrTextContent("Simulation Foam Amount", "Controls the simulation foam amount. Higher values generate larger foam patches. Foam presence is highly dependent on the Distant Wind Speed.");
        static public readonly GUIContent k_FoamTexture = EditorGUIUtility.TrTextContent("Custom Texture", "Set the texture used to define a visual appearance for the foam.");
        static public readonly GUIContent k_FoamTextureTiling = EditorGUIUtility.TrTextContent("Texture Tiling", "Set the per meter tiling for the foam texture.");
        static public readonly GUIContent k_FoamMask = EditorGUIUtility.TrTextContent("Mask", "Sets the texture used to attenuate or suppress the simulation foam. The red channel of the texture is used for the masking.");
        static public readonly GUIContent k_WindFoamCurve = EditorGUIUtility.TrTextContent("Wind Speed Dimmer", "Controls the foam intensity depending on the normalized Distant Wind Speed. The X axis refers to the normalized Distant Wind Speed, the Y axis refers to the dimmer value.");

        // Underwater
        public static readonly GUIContent k_UnderWater = EditorGUIUtility.TrTextContent("Underwater", "When enabled, HDRP will apply a fog and color shift to the final image when the camera is under the surface.This feature has a cost even when the camera is above the water surface.");
        public static readonly string k_AddColliderMessage = "The water surface does not support an underwater scenario if the box collider is not set.";
        public static readonly GUIContent k_AddBoxColliderPopup = EditorGUIUtility.TrTextContent("Add a Box Collider");
        public static readonly GUIContent k_UseBoxColliderPopup = EditorGUIUtility.TrTextContent("Use an existing Box Collider");
        public static readonly GUIContent k_ColorPyramidOffset = EditorGUIUtility.TrTextContent("Color Pyramid Mip Offset", "Sets the mip offset used to evaluated the underwater refraction. Higher values produce blurrier results but introduce aliasing artifacts.");
        public static readonly GUIContent k_UnderWaterScatteringColorMode = EditorGUIUtility.TrTextContent("Scattering Color Mode", "Sets how the underwater scattering color is specified.");
        public static readonly GUIContent k_UnderWaterScatteringColor = EditorGUIUtility.TrTextContent("Scattering Color", "Sets the color that is used to simulate the scattering when the camera is underwater.");
        public static readonly GUIContent k_UnderWaterAmbientProbeContribution = EditorGUIUtility.TrTextContent("Ambient Probe Contribution", "Sets the contribution of the ambient probe to the underwater scattering color.");

        // Misc
        static public readonly GUIContent k_DebugMode = EditorGUIUtility.TrTextContent("Debug Mode", "Sets the debug mode for the water surface.");
        static public readonly GUIContent k_WaterMaskDebugMode = EditorGUIUtility.TrTextContent("Water Mask Mode", "Sets the water mask debug mode for a given water surface.");
        static public readonly GUIContent k_WaterCurrentDebugMode = EditorGUIUtility.TrTextContent("Water Current Mode", "Sets the water current debug mode for a given water surface.");
        static public readonly GUIContent k_CurrentDebugMultiplier = EditorGUIUtility.TrTextContent("Arrow Density Multiplier", "Sets a multiplier for the arrow density in the current debug mode.");
    }
}
