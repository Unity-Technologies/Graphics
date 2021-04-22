using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDLightUI
    {
        sealed class Styles
        {
            // Headers
            public readonly GUIContent generalHeader = new GUIContent("General");
            public readonly GUIContent shapeHeader = new GUIContent("Shape");
            public readonly GUIContent celestialBodyHeader = new GUIContent("Celestial Body");
            public readonly GUIContent emissionHeader = new GUIContent("Emission");
            public readonly GUIContent volumetricHeader = new GUIContent("Volumetrics");
            public readonly GUIContent shadowHeader = new GUIContent("Shadows");
            public readonly GUIContent shadowMapSubHeader = new GUIContent("Shadow Map");
            public readonly GUIContent contactShadowsSubHeader = new GUIContent("Contact Shadows");
            public readonly GUIContent bakedShadowsSubHeader = new GUIContent("Baked Shadows");
            public readonly GUIContent veryHighShadowQualitySubHeader = new GUIContent("Very High Quality Settings");
            public readonly GUIContent highShadowQualitySubHeader = new GUIContent("High Quality Settings");
            public readonly GUIContent mediumShadowQualitySubHeader = new GUIContent("Medium Quality Settings");
            public readonly GUIContent lowShadowQualitySubHeader = new GUIContent("Low Quality Settings");

            // Base (copy from LightEditor.cs)
            public readonly GUIContent outterAngle = new GUIContent("Outer Angle", "Controls the angle, in degrees, at the base of a Spot Light's cone.");
            public readonly GUIContent cookieSizeX = new GUIContent("Size X", "Sets the width of the Cookie mask currently assigned to the Light.");
            public readonly GUIContent cookieSizeY = new GUIContent("Size Y", "Sets the height of the Cookie mask currently assigned to the Light.");
            public readonly GUIContent shadowBias = new GUIContent("Bias", "Controls the distance at which HDRP pushes shadows away from the Light. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent shadowNormalBias = new GUIContent("Normal Bias", "Controls distance at which HDRP shrinks the shadow casting surfaces along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent shadowNearPlane = new GUIContent("Near Plane", "Controls the value for the active Camera's Near clipping Plane for rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");
            public readonly GUIContent bakedShadowRadius = new GUIContent("Radius", "Sets the amount of artificial softening the baking process applies to the edges of shadows cast by this Point or Spot Light.");
            public readonly GUIContent bakedShadowAngle = new GUIContent("Angle", "Controls the amount of artificial softening the baking process applies to the edges of shadows cast by Directional Lights.");
            public readonly GUIContent lightBounceIntensity = new GUIContent("Indirect Multiplier", "Controls the intensity of the indirect light this Light contributes to the Scene. A value of 0 with a Realtime Light causes HDRP to remove it from realtime global illumination. A value of 0 for Baked and Mixed Lights cause them to no longer emit indirect lighting. This has no effect if you disable both Realtime and Baked global illumination.");
            public readonly GUIContent indirectBounceShadowWarning = new GUIContent("HDRP does not support real-time indirect bounce shadowing for Spot and Point lights.");
            public readonly GUIContent color = new GUIContent("Color", "Specifies the color this Light emits.");
            public readonly GUIContent lightAppearance = new GUIContent("Light Appearance", "Specifies the mode for how HDRP calculates this Light's color.");
            public readonly GUIContent colorFilter = new GUIContent("Filter", "Specifies a color which tints the Light source.");
            public readonly GUIContent colorTemperature = new GUIContent("Temperature", "Specifies a temperature (in Kelvin) HDRP uses to correlate a color for the Light. For reference, White is 6500K.");
            public readonly GUIContent areaLightCookie = new GUIContent("Cookie", "Cookie mask currently assigned to the area light.");
            public readonly GUIContent iesTexture = new GUIContent("IES Profile", "IES Profile (Support: Point, Spot, Rectangular-Area Lights).");
            public readonly GUIContent cookieTextureTypeError = new GUIContent("HDRP does not support the Cookie Texture type, only Default is supported.", EditorGUIUtility.IconContent("console.warnicon").image);
            public readonly string cookieNonPOT = "HDRP does not support non power of two cookie textures.";
            public readonly string cookieTooSmall = "Min texture size for cookies is 2x2 pixels.";
            public readonly string cookieBaking = "Light Baking for cookies disabled on the Project Settings.";
            public readonly GUIContent includeLightForRayTracing = new GUIContent("Include For RayTracing", "When enabled, the light affects the scene for cameras with the Ray-Tracing frame setting enabled.");

            // Additional light data
            public readonly GUIContent directionalIntensity = new GUIContent("Intensity (Lux)", "Illuminance of the Directional Light, at ground level, in lux.");
            public readonly GUIContent punctualIntensity = new GUIContent("Intensity (Lumen)", "Luminous power of the Light in lumen.");
            public readonly GUIContent areaIntensity = new GUIContent("Intensity (Lumen)", "Luminous power of the Light in Lumen.");
            public readonly GUIContent lightIntensity = new GUIContent("Intensity", "Sets the strength of the Light. Use the drop-down to select the light units to use.");

            // in casse that you want to keep the indentation but have nothing to write
            public readonly GUIContent empty = EditorGUIUtility.TrTextContent(" ");

            public readonly GUIContent lightRadius = new GUIContent("Radius", "Sets the radius of the light source. This affects the falloff of diffuse lighting, the spread of the specular highlight, and the softness of Ray Traced shadows.");
            public readonly GUIContent affectDiffuse = new GUIContent("Affect Diffuse", "When disabled, HDRP does not calculate diffuse lighting for this Light. Does not increase performance as HDRP still calculates the diffuse lighting.");
            public readonly GUIContent affectSpecular = new GUIContent("Affect Specular", "When disabled, HDRP does not calculate specular lighting for this Light. Does not increase performance as HDRP still calculates the specular lighting.");
            public readonly GUIContent nonLightmappedOnly = new GUIContent("Shadowmask Mode", "Species the behavior of  the shadowmask when using Mixed lighting. Distance Shadowmask: HDRP uses real-time shadows to Shadow Distance and baked shadows after. Shadowmask: Static shadow casters always use baked shadows.");
            public readonly GUIContent lightDimmer = new GUIContent("Intensity Multiplier", "Multiplies the intensity of the Light by the given number. This is useful for modifying the intensity of multiple Lights simultaneously without needing know the intensity of each Light.");
            public readonly GUIContent fadeDistance = new GUIContent("Fade Distance", "Sets the distance from the camera at which light smoothly fades out before HDRP culls it completely. This minimizes popping.");
            public readonly GUIContent spotInnerPercent = new GUIContent("Inner Angle (%)", "Controls size of the angular attenuation, in percent, of the base angle of the Spot Light's cone.");
            public readonly GUIContent spotIESCutoffPercent = new GUIContent("IES Cutoff Angle (%)", "Cutoff the IES Light in percent, of the base angle of the Spot Light's cone.");
            public readonly GUIContent spotLightShape = new GUIContent("Shape", "The shape of the Spot Light. Impacts the the cookie transformation and the Light's angular attenuation.");
            public readonly GUIContent areaLightShape = new GUIContent("Shape", "The shape of the Area Light. Note that some are Realtime only and some Baked only.");
            public readonly GUIContent[] areaShapeNames =
            {
                new GUIContent("Rectangle"),
                new GUIContent("Tube (Realtime only)"),
                new GUIContent("Disc (Baked only)")
            };
            public readonly GUIContent shapeWidthTube = new GUIContent("Length", "Length of the Tube Light.");
            public readonly GUIContent shapeWidthRect = new GUIContent("Size X", "Sets the width of the Rectangle Light.");
            public readonly GUIContent shapeHeightRect = new GUIContent("Size Y", "Sets the height of the Rectangle Light.");
            public readonly GUIContent barnDoorAngle = new GUIContent("Barn Door Angle", "Sets the angle of the Rectangle Light so that is behaves like a barn door.");
            public readonly GUIContent barnDoorLength = new GUIContent("Barn Door Length", "Sets the length for the barn door.");
            public readonly GUIContent aspectRatioPyramid = new GUIContent("Aspect ratio", "Controls the aspect ration of the Pyramid Light's projection. A value of 1 results in a square.");
            public readonly GUIContent shapeWidthBox = new GUIContent("Size X", "Sets the width of the Box Light.");
            public readonly GUIContent shapeHeightBox = new GUIContent("Size Y", "Sets the height of the Box Light.");
            public readonly GUIContent applyRangeAttenuation = new GUIContent("Range Attenuation", "Allows you to enable or disable range attenuation. Range attenuation is useful for indoor environments because you can avoid having to set up a large range for a Light to get correct inverse square attenuation that may leak out of the indoor environment.");
            public readonly GUIContent displayAreaLightEmissiveMesh = new GUIContent("Display Emissive Mesh", "Generate an emissive mesh using the size, Color and Intensity of the Area Light.");
            public readonly GUIContent areaLightEmissiveMeshCastShadow = new GUIContent("Cast Shadows", "Specify wether the generated geometry create shadow or not when a shadow casting Light shines on it");
            public readonly GUIContent areaLightEmissiveMeshMotionVector = new GUIContent("Motion Vectors", "Specify wether the generated Mesh renders 'Per Object Motion', 'Camera Motion' or 'No Motion' vectors to the Camera Motion Vector Texture.");
            public readonly GUIContent areaLightEmissiveMeshSameLayer = new GUIContent("Same Layer", "If checked, use the same Layer than the Light one.");
            public readonly GUIContent areaLightEmissiveMeshCustomLayer = new GUIContent("Custom Layer", "Specify on which layer the generated Mesh live.");
            public readonly GUIContent lightLayer = new GUIContent("Light Layer", "Specifies the current Light Layers that the Light affects. This Light illuminates corresponding Renderers with the same Light Layer flags.");

            public readonly GUIContent interactsWithSky = new GUIContent("Affect Physically Based Sky", "Check this option to make the light and the Physically Based sky affect one another.");
            public readonly GUIContent angularDiameter = new GUIContent("Angular Diameter", "Angular diameter of the emissive celestial body represented by the light as seen from the camera (in degrees). Used to render the sun/moon disk.");
            public readonly GUIContent flareSize = new GUIContent("Flare Size", "Size of the flare around the celestial body (in degrees).");
            public readonly GUIContent flareTint = new GUIContent("Flare Tint", "Tints the flare of the celestial body");
            public readonly GUIContent flareFalloff = new GUIContent("Flare Falloff", "The falloff rate of flare intensity as the angle from the light increases.");
            public readonly GUIContent surfaceTexture = new GUIContent("Surface Texture", "2D (disk) texture of the surface of the celestial body. Acts like a multiplier.");
            public readonly GUIContent surfaceTint = new GUIContent("Surface Tint", "Tints the surface of the celestial body");
            public readonly GUIContent distance = new GUIContent("Distance", "Distance from the camera (in meters) to the emissive celestial body represented by the light. Primarily used for sorting.");

            public readonly GUIContent shape = new GUIContent("Type", "Specifies the current type of Light. Possible Light types are Directional, Spot, Point, and Area.");
            public readonly GUIContent[] shapeNames;
            public readonly GUIContent enableSpotReflector = new GUIContent("Reflector", "When enabled, HDRP simulates a physically correct Spot Light using a reflector. This means the narrower the Outer Angle, the more intense the Spot Light.  When disabled, the intensity of the Light matches the one of a Point Light and thus remains constant regardless of the Outer Angle.");
            public readonly GUIContent luxAtDistance = new GUIContent("At", "Sets the distance, in meters, where a surface receives the amount of light equivalent to the provided number of Lux.");

            // Volumetric Additional light data
            public readonly GUIContent volumetricEnable = new GUIContent("Enable", "When enabled, this Light uses Volumetrics.");
            public readonly GUIContent volumetricDimmer = new GUIContent("Multiplier", "Controls the intensity of the scattered Volumetric lighting.");
            public readonly GUIContent volumetricFadeDistance = new GUIContent("Fade Distance", "Sets the distance from the camera at which light smoothly fades out from contributing to volumetric lighting.");
            // Volumetric Additional shadow data
            public readonly GUIContent volumetricShadowDimmer = new GUIContent("Shadow Dimmer", "Dims the volumetric shadows this Light casts.");

            // Additional shadow data
            public readonly GUIContent useShadowQualityResolution = new GUIContent("Use Quality Settings", "Allows to the resolution from the set of predetermined resolutions specified in the quality settings.");
            public readonly GUIContent shadowResolution = new GUIContent("Resolution", "Sets the rendered resolution of the shadow maps. A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage.");
            public readonly GUIContent shadowFadeDistance = new GUIContent("Fade Distance", "Sets the distance from the camera at which Shadows fade before HDRP culls them completely. This minimizes popping.");
            public readonly GUIContent shadowDimmer = new GUIContent("Dimmer", "Dims the shadows this Light casts.");
            public readonly GUIContent shadowTint = new GUIContent("Tint", "Specifies the color and transparency that HDRP tints this Light's shadows to. The tint affects dynamic shadows, Contact Shadows, and ShadowMask. It does not affect baked shadows.");
            public readonly GUIContent penumbraTint = new GUIContent("Penumbra Tint", "When enabled, the tint only affects the shadow's penumbra.");
            public readonly GUIContent contactShadows = new GUIContent("Enable", "Enable support for Contact Shadows on this Light. This is better for lights with a lot of visible shadows.");
            public readonly GUIContent rayTracedContactShadow = new GUIContent("Ray Tracing (Preview)", "Uses ray tracing to compute the contact shadow for a light.");
            public readonly GUIContent shadowUpdateMode = new GUIContent("Update Mode", "Specifies when HDRP updates the shadow map.");
            public readonly GUIContent shadowAlwaysDrawDynamic = new GUIContent("Always draw dynamic", "Specifies whether HDRP renders dynamic shadow caster every frame regardless of the update mode.");
            public readonly GUIContent shadowUpdateOnLightTransformChange = new GUIContent("Update on light movement", "Whether a cached shadow map will be automatically updated when the light transform changes.");
            public readonly GUIContent useCustomSpotLightShadowCone = new GUIContent("Custom Spot Angle", "When enabled, this Spot Light uses the custom angle for shadow map rendering.");
            public readonly GUIContent customSpotLightShadowCone = new GUIContent("Shadow Angle", "Controls the custom angle this Spot Light uses for shadow map rendering.");

            // Bias control
            public readonly GUIContent slopeBias = new GUIContent("Slope-Scale Depth Bias", "Controls the bias that HDRP adds to the rendered shadow map, it is proportional to the slope of the polygons relative to the light.");

            public readonly GUIContent normalBias = new GUIContent("Normal Bias", "Controls the bias this Light applies along the normal of surfaces it illuminates.");

            // Shadow filter settings
            public readonly GUIContent blockerSampleCount = new GUIContent("Blocker Sample Count", "Controls the number of samples that HDRP uses to determine the size of the blocker.");
            public readonly GUIContent filterSampleCount = new GUIContent("Filter Sample Count", "Controls the number of samples that HDRP uses to blur shadows.");
            public readonly GUIContent minFilterSize = new GUIContent("Minimum Blur Intensity", "Controls the minimum blur intensity regardless of the distance between the pixel and the shadow caster. The range [0..1] maps to [0..0.001] in UV space.");
            public readonly GUIContent radiusScaleForSoftness = new GUIContent("Radius Scale for Softness", "Scale the shape radius for the sake of softness calculation. Higher scales will result in higher softness.");
            public readonly GUIContent diameterScaleForSoftness = new GUIContent("Angular Diameter Scale for Softness", "Scale the angular diameter for the sake of softness calculation. Higher scales will result in higher softness.");
            public readonly GUIContent areaLightShadowCone = new GUIContent("Shadow Cone", "Aperture of the cone used for shadowing the area light.");
            public readonly GUIContent useScreenSpaceShadows = new GUIContent("Screen Space Shadows", "Render screen space shadow.");
            public readonly GUIContent useRayTracedShadows = new GUIContent("Ray Traced Shadows (Preview)", "If selected, ray traced shadows are used in place of rasterized ones.");
            public readonly GUIContent numRayTracingSamples = new GUIContent("Sample Count", "This defines the number of samples that will be used to evaluate this shadow.");
            public readonly GUIContent denoiseTracedShadow = new GUIContent("Denoise", "This defines if the ray traced shadow should be filtered.");
            public readonly GUIContent denoiserRadius = new GUIContent("Denoiser Radius", "This defines the denoiser's radius used for filtering ray traced shadows.");
            public readonly GUIContent distanceBasedFiltering = new GUIContent("Distance Based Denoising", "This defines if the denoiser should use the distance to the occluder to improve the filtering.");
            public readonly GUIContent semiTransparentShadow = new GUIContent("Semi Transparent Shadow", "When enabled, the opacity of shadow casters will be taken into account when generating the shadow.");
            public readonly GUIContent colorShadow = new GUIContent("Color Shadow", "When enabled, the opacity and transmittance color of shadow casters will be taken into account when generating the shadow.");
            public readonly GUIContent evsmExponent = new GUIContent("EVSM Exponent", "Exponent used for depth warping. Increasing this could reduce light leak and result in a change in appearance of the shadow.");
            public readonly GUIContent evsmLightLeakBias = new GUIContent("Light Leak Bias", "Increasing this value light leaking, but it eats up a bit of the softness of the shadow.");
            public readonly GUIContent evsmVarianceBias = new GUIContent("Variance Bias", "Variance Bias for EVSM. This is to contrast numerical accuracy issues. ");
            public readonly GUIContent evsmAdditionalBlurPasses = new GUIContent("Blur passes", "Increasing this will increase the softness of the shadow, but it will severely impact performance.");

            // Very high shadow settings
            public readonly GUIContent lightAngle = new GUIContent("Light Angle");
            public readonly GUIContent kernelSize = new GUIContent("Kernel size");
            public readonly GUIContent maxDepthBias = new GUIContent("Max Depth Bias");

            // Layers
            public readonly GUIContent linkLightAndShadowLayersText = new GUIContent("Link Light Layer", "When enabled, the Light Layer property in the General section specifies the light layers for both lighting and for shadows. When disabled, you can use the Light Layer property below to specify the light layers for shadows seperately to lighting.");
            public readonly GUIContent shadowLayerMaskText = new GUIContent("Light Layer", "Specifies the light layer to use for shadows.");

            // Settings
            public readonly GUIContent enableShadowMap = new GUIContent("Enable", "When enabled, this Light casts shadows.");

            public Styles()
            {
                shapeNames = Enum.GetNames(typeof(UnityEngine.Rendering.HighDefinition.HDLightType))
                    .Select(x => new GUIContent(x))
                    .ToArray();
            }
        }

        static Styles s_Styles = new Styles();
    }
}
