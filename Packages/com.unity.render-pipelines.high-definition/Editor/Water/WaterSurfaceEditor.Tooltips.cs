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
        static public readonly GUIContent k_GeometryType = EditorGUIUtility.TrTextContent("Geometry Type", "Specifies the type of geometry used to render the water surface.");
        static public string[] k_GeometryTypeEnum = { WaterGeometryType.Quad.ToString(), WaterGeometryType.CustomMesh.ToString() };
        static public readonly GUIContent k_Mesh = EditorGUIUtility.TrTextContent("Mesh", "Sets the geometry to use when rendering in quad and custom geometry type mode.The vertical position of the vertices will be overridden to keep the surface of water leveled.");

        // CPU Simulation
        static public readonly GUIContent k_CPUSimulation = EditorGUIUtility.TrTextContent("Script Interactions", "When enabled, HDRP will evaluate the water simulation on the CPU for C# script height requests. Enabling this will significantly increase the CPU cost of the feature.");
        static public readonly GUIContent k_CPUFullResolution = EditorGUIUtility.TrTextContent("Full Resolution", "Specifies if the CPU simulation should be evaluated at full or half resolution. When in full resolution, the visual fidelity will be higher but the cost of the simulation will increase.");
        static public readonly GUIContent k_CPUEvaluateRipples = EditorGUIUtility.TrTextContent("Evaluate Ripples", "When enabled, HDRP replicates the ripples on the CPU side when evaluating the water simulation for script interaction. Including ripples will allow a higher visual fidelity but the cost of the simulation will increase.");

        // Simulation
        static public readonly GUIContent k_TimeMultiplier = EditorGUIUtility.TrTextContent("Time Multiplier", "Sets the speed of the water simulation. This allows to slow down the wave's speed or to accelerate it.");
        static public readonly GUIContent k_WaterMaskSwell = EditorGUIUtility.TrTextContent("Water Mask", "Sets the texture used to attenuate or supress swell (red channel) and ripples (green channel) water frequencies.");
        static public readonly GUIContent k_WaterMaskAgitation = EditorGUIUtility.TrTextContent("Water Mask", "Sets the texture used to attenuate or supress agitation (red channel) and ripples (green channel) water frequencies.");
        static public readonly GUIContent k_WaterMaskRipples = EditorGUIUtility.TrTextContent("Water Mask", "Sets the texture used to attenuate or supress ripples (red channel) water frequencies.");

        // Swell
        static public readonly GUIContent k_SwellRepetitionSize = EditorGUIUtility.TrTextContent("Repetition Size", "Controls the size of the water patch in meters. Larger values results in less visible repetion across the water surface. This parameter also affects the maximum amplitude of the swell frequency bands.");
        static public readonly GUIContent k_SwellWindSpeed = EditorGUIUtility.TrTextContent("Distant Wind Speed", "Controls the speed of the distant wind in kilometers per hour that blowed over the water surface for a long period of time. This indirectly controls the maximum amplitude and shape of the swell in a non-linear way.");
        static public readonly GUIContent k_SwellWindOrientation = EditorGUIUtility.TrTextContent("Distant Wind Orientation", "Sets the orientation of distant wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the swell with a chaos value inferior to one. ");
        static public readonly GUIContent k_SwellChaos = EditorGUIUtility.TrTextContent("Chaos", "Controls how directional the swell is. The lower the value, the more the swell waves are traveling in the Distant Wind Orientation.");
        static public readonly GUIContent k_SwellCurrent = EditorGUIUtility.TrTextContent("Current", "Sets the properties of the current for the swell. This current translates the swell at a constant speed in a given direction.");
        static public readonly GUIContent k_SwellCurrentSpeed = EditorGUIUtility.TrTextContent("Speed", "Sets the speed of the current for the swell in kilometers per hour. This current translates the swell at a constant speed in a given direction.");
        static public readonly GUIContent k_SwellCurrentOrientation = EditorGUIUtility.TrTextContent("Orient", "Sets the orientation of the current for the swell in degrees in a counterclockwise fashion relative to the X world vector.");
        static public readonly GUIContent k_SwellTotalAmplitude = EditorGUIUtility.TrTextContent("Total Amplitude", "Displays the current maximum amplitude of the swell. This is the sum of the first and second frequency bands.");

        // Swell Bands
        static public readonly GUIContent k_SwellBand0Mutliplier = EditorGUIUtility.TrTextContent("Amplitude Dimmer", "Controls the attenuation of amplitude on the first frequency band of the swell.");
        static public readonly GUIContent k_SwellBand1Mutliplier = EditorGUIUtility.TrTextContent("Amplitude Dimmer", "Controls the attenuation of amplitude on the second frequency band of the swell.");
        static public readonly GUIContent k_SwellFadeToggle = EditorGUIUtility.TrTextContent("Fade", "When enabled, HDRP starts fading the contribution of this frequency band over a given range. This helps reduce the aliasing artifacts at a distance.");
        static public readonly GUIContent k_SwellMaxAmplitude = EditorGUIUtility.TrTextContent("Max Amplitude", "Displays the current maximum amplitude of this frequency band.");

        // Agitation
        static public readonly GUIContent k_AgitationRepetitionSize = EditorGUIUtility.TrTextContent("Repetition Size", "Controls the size of the water patch in meters. Larger values results in less visible repetion across the water surface. This parameter also affects the maximum amplitude of the agitation frequency bands.");
        static public readonly GUIContent k_AgitationWindSpeed = EditorGUIUtility.TrTextContent("Distant Wind Speed", "Controls the speed of the distant wind in kilometers per hour that blowed over the water surface for a long period of time. This indirectly controls the maximum amplitude and shape of the agitation in a non-linear way.");
        static public readonly GUIContent k_AgitationWindOrientation = EditorGUIUtility.TrTextContent("Distant Wind Orientation", "Sets the orientation of distant wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the agitation with a chaos value inferior to one. ");
        static public readonly GUIContent k_AgitationChaos = EditorGUIUtility.TrTextContent("Chaos", "Controls how directional the agitation is. The lower the value, the more the agitation waves are traveling in the Distant Wind Orientation.");
        static public readonly GUIContent k_AgitationCurrent = EditorGUIUtility.TrTextContent("Current", "Sets the properties of the current for the swell. This current translates the swell at a constant speed in a given direction.");
        static public readonly GUIContent k_AgitationCurrentSpeed = EditorGUIUtility.TrTextContent("Speed", "Sets the speed of the current for the swell in kilometers per hour. This current translates the swell at a constant speed in a given direction.");
        static public readonly GUIContent k_AgitationCurrentOrientation = EditorGUIUtility.TrTextContent("Orient", "Sets the orientation of the current for the swell in degrees in a counterclockwise fashion relative to the X world vector.");
        static public readonly GUIContent k_AgitationBandMutliplier = EditorGUIUtility.TrTextContent("Amplitude Dimmer", "Controls the attenuation of amplitude on the frequency band of the agitation.");
        static public readonly GUIContent k_AgitationTotalAmplitude = EditorGUIUtility.TrTextContent("Total Amplitude", "Displays the current maximum amplitude of the agitaion.");

        // Fade parameters
        static public readonly GUIContent k_SwellRangeToggle = EditorGUIUtility.TrTextContent("Range", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_SwellFadeStart = EditorGUIUtility.TrTextContent("Start", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_SwellFadeDistance = EditorGUIUtility.TrTextContent("Distance", "Sets the length in meters during which HDRP fades out the contribution of this frequency band.");

        // Ripples
		static public readonly GUIContent k_RipplesEnable = EditorGUIUtility.TrTextContent("Ripples", "When enabled, the Water System allows you to simulate and render ripples.\nThe frequency range is not affected by the swell/agitation parameters.");
        static public readonly GUIContent k_RipplesWindSpeed = EditorGUIUtility.TrTextContent("Local Wind Speed", "Controls the speed of the local wind in kilometers per hour that is blowing over the water surface. This indirectly controls the maximum amplitude and shape of the ripples in a non-linear way.");
        static public readonly GUIContent k_RipplesWindOrientationSwell = EditorGUIUtility.TrTextContent("Local Wind Orientation", "Specifies if the Local Wind Orientation is inherited from the swell's Distant Wind Orientation or set independently. Sets the orientation of local wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the ripples with a chaos value inferior to one.");
        static public readonly GUIContent k_RipplesWindOrientationAgitation = EditorGUIUtility.TrTextContent("Local Wind Orientation", "Specifies if the Local Wind Orientation is inherited from the agitation's Distant Wind Orientation or set independently. Sets the orientation of local wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the ripples with a chaos value inferior to one.");
        static public readonly GUIContent k_RipplesWindOrientationOnly = EditorGUIUtility.TrTextContent("Local Wind Orientation", "Sets the orientation of local wind in degrees in a counterclockwise fashion relative to the X world vector. This parameter only affects the ripples with a chaos value inferior to one.");
        static public readonly GUIContent k_RipplesChaos = EditorGUIUtility.TrTextContent("Chaos", "Controls how directional the ripples are. The lower the value, the more the ripples are traveling in the Local Wind Orientation.");
        static public readonly GUIContent k_RipplesCurrentEmpty = EditorGUIUtility.TrTextContent(" ", "");
        static public readonly GUIContent k_RipplesCurrentSwell = EditorGUIUtility.TrTextContent("Current", "Specifies if the ripples Current Orientation is inherited from the swell's Current Orientation or set independently.");
        static public readonly GUIContent k_RipplesCurrentAgitation = EditorGUIUtility.TrTextContent("Current", "Specifies if the ripples Current Orientation is inherited from the agitation's Current Orientation or set independently.");
        static public readonly GUIContent k_RipplesCurrentOnly = EditorGUIUtility.TrTextContent("Current", "Sets the properties of the current for the ripples. This current translates the swell at a constant speed in a given direction.");
        static public readonly GUIContent k_RipplesCurrentSpeed = EditorGUIUtility.TrTextContent("Speed", "Sets the speed of the current for the ripples in kilometers per hour. This current translates the ripples at a constant speed in a given direction.");
        static public readonly GUIContent k_RipplesCurrentOrientation = EditorGUIUtility.TrTextContent("Orient", "Sets the orientation of the current for the ripples in degrees in a counterclockwise fashion relative to the X world vector.");
        static public readonly GUIContent k_RipplesFadeToggle = EditorGUIUtility.TrTextContent("Fade", "When enabled, HDRP starts fading the contribution of this frequency band over a given range. This helps reduce the aliasing artifacts at a distance.");
        static public readonly GUIContent k_RipplesFadeRange = EditorGUIUtility.TrTextContent("Range", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_RipplesFadeStart = EditorGUIUtility.TrTextContent("Start", "Sets the distance in meters at which HDRP starts fading the contribution of this frequency band.");
        static public readonly GUIContent k_RipplesFadeDistance = EditorGUIUtility.TrTextContent("Distance", "Sets the length in meters during which HDRP fades out the contribution of this frequency band.");

        // Material
        static public readonly GUIContent k_CustomMaterial = EditorGUIUtility.TrTextContent("Custom Material", "Sets a custom material that will be used to render the water surface. If set to None a default material is used.");
        static public readonly GUIContent k_StartSmoothness = EditorGUIUtility.TrTextContent("Close", "Controls the smoothness when the water is closer than the smoothness Fade Start value.");
        static public readonly GUIContent k_EndSmoothness = EditorGUIUtility.TrTextContent("Distant", "Controls the smoothness when the water is further than the smoothness Fade Start + Fade Distance values.");
        static public readonly GUIContent k_SmoothnessFadeRange = EditorGUIUtility.TrTextContent("Fade Range", "Specifies the range over which the smoothness is interpolated from close to distant.");
        static public readonly GUIContent k_SmoothnessFadeStart = EditorGUIUtility.TrTextContent("Start", "Sets the distance in meters at which HDRP starts interpolating the smoothness value for the water surface. ");
        static public readonly GUIContent k_SmoothnessFadeDistance = EditorGUIUtility.TrTextContent("Distance", "Sets the length in meters during which HDRP interpolates the smoothness value for the water surface. ");

        static public readonly GUIContent k_Amplitude = EditorGUIUtility.TrTextContent("Amplitude", "Sets the normalized (between 0.0 and 1.0) amplitude of each simulation band (from lower to higher frequencies).");
        static public readonly GUIContent k_Choppiness = EditorGUIUtility.TrTextContent("Choppiness", "Sets the choppiness factor the waves. Higher values combined with high wind speed may introduce visual artifacts.");

        // Refraction
        static public readonly GUIContent k_RefractionColor = EditorGUIUtility.TrTextContent("Color", "Sets the color that is used to simulate the under-water refraction.");
        static public readonly GUIContent k_MaxRefractionDistance = EditorGUIUtility.TrTextContent("Maximum Distance", "Controls the maximum distance in meters used to clamp the under water refraction depth. Higher value increases the distortion amount.");
        static public readonly GUIContent k_AbsorptionDistance = EditorGUIUtility.TrTextContent("Absorption Distance", "Controls the approximative distance in meters that the camera can perceive through a water surface. This distance can vary widely depending on the intensity of the light the object receives.");

        // Scattering
        static public readonly GUIContent k_ScatteringColor = EditorGUIUtility.TrTextContent("Color", "Sets the color that is used to simulate the under-water refraction.");
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
        public static readonly string k_AddColliderMessage = "The water surface does not support an underwater scenario if the box collider is not set.";
        public static readonly GUIContent k_AddBoxColliderPopup = EditorGUIUtility.TrTextContent("Add a Box Collider");
        public static readonly GUIContent k_UseBoxColliderPopup = EditorGUIUtility.TrTextContent("Use an existing Box Collider");
    }
}
