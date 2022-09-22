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
            public readonly GUIContent celestialBodyHeader = EditorGUIUtility.TrTextContent("Celestial Body");
            public readonly GUIContent volumetricHeader = EditorGUIUtility.TrTextContent("Volumetrics");
            public readonly GUIContent shadowMapSubHeader = EditorGUIUtility.TrTextContent("Shadow Map");
            public readonly GUIContent contactShadowsSubHeader = EditorGUIUtility.TrTextContent("Contact Shadows");
            public readonly GUIContent bakedShadowsSubHeader = EditorGUIUtility.TrTextContent("Baked Shadows");
            public readonly GUIContent veryHighShadowQualitySubHeader = EditorGUIUtility.TrTextContent("Very High Quality Settings");
            public readonly GUIContent highShadowQualitySubHeader = EditorGUIUtility.TrTextContent("High Quality Settings");
            public readonly GUIContent mediumShadowQualitySubHeader = EditorGUIUtility.TrTextContent("Medium Quality Settings");
            public readonly GUIContent lowShadowQualitySubHeader = EditorGUIUtility.TrTextContent("Low Quality Settings");

            // Base (copy from LightEditor.cs)
            public readonly GUIContent outterAngle = EditorGUIUtility.TrTextContent("Outer Angle", "Controls the angle, in degrees, at the base of a Spot Light's cone.");
            public readonly GUIContent cookieSize = EditorGUIUtility.TrTextContent("Size", "Sets the size of the Cookie mask currently assigned to the Light.");
            public readonly GUIContent shadowBias = EditorGUIUtility.TrTextContent("Bias", "Controls the distance at which HDRP pushes shadows away from the Light. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent shadowNormalBias = EditorGUIUtility.TrTextContent("Normal Bias", "Controls distance at which HDRP shrinks the shadow casting surfaces along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent shadowNearPlane = EditorGUIUtility.TrTextContent("Near Plane", "Controls the value of the shadow camera's near clipping plane for rendering shadows. Clamped to [0.01, 10] for Cone, Pyramid and Point Lights, and [0, 10] for Box and Area Lights.");
            public readonly GUIContent bakedShadowRadius = EditorGUIUtility.TrTextContent("Radius", "Sets the amount of artificial softening the baking process applies to the edges of shadows cast by this Point or Spot Light.");
            public readonly GUIContent bakedShadowAngle = EditorGUIUtility.TrTextContent("Angle", "Controls the amount of artificial softening the baking process applies to the edges of shadows cast by Directional Lights.");
            public readonly GUIContent lightBounceIntensity = EditorGUIUtility.TrTextContent("Indirect Multiplier", "Controls the intensity of the indirect light this Light contributes to the Scene. A value of 0 with a Realtime Light causes HDRP to remove it from realtime global illumination. A value of 0 for Baked and Mixed Lights cause them to no longer emit indirect lighting. This has no effect if you disable both Realtime and Baked global illumination.");
            public readonly GUIContent areaLightCookie = EditorGUIUtility.TrTextContent("Cookie", "Cookie mask currently assigned to the area light.");
            public readonly GUIContent iesTexture = EditorGUIUtility.TrTextContent("IES Profile", "IES Profile (Support: Point, Spot, Rectangular-Area Lights).");
            public readonly GUIContent cookieTextureTypeError = EditorGUIUtility.TrTextContent("HDRP does not support the Cookie Texture type, only Default is supported.", EditorGUIUtility.IconContent("console.warnicon").image);
            public readonly string cookieNonPOT = "HDRP does not support non power of two cookie textures.";
            public readonly string cookieTooSmall = "Min texture size for cookies is 2x2 pixels.";
            public readonly string cookieBaking = "Light Baking for cookies disabled on the Project Settings.";
            public readonly GUIContent includeLightForRayTracing = EditorGUIUtility.TrTextContent("Include For RayTracing", "When enabled, the light affects the scene for cameras with the Ray-Tracing frame setting enabled.");

            // Additional light data
            public readonly GUIContent directionalIntensity = EditorGUIUtility.TrTextContent("Intensity (Lux)", "Illuminance of the Directional Light, at ground level, in lux.");
            public readonly GUIContent punctualIntensity = EditorGUIUtility.TrTextContent("Intensity (Lumen)", "Luminous power of the Light in lumen.");
            public readonly GUIContent areaIntensity = EditorGUIUtility.TrTextContent("Intensity (Lumen)", "Luminous power of the Light in Lumen.");
            public readonly GUIContent lightIntensity = EditorGUIUtility.TrTextContent("Intensity", "Sets the strength of the Light. Use the drop-down to select the light units to use.");

            public readonly GUIContent lightRadius = EditorGUIUtility.TrTextContent("Radius", "Sets the radius of the light source. This affects the falloff of diffuse lighting, the spread of the specular highlight, and the softness of Ray Traced shadows.");
            public readonly GUIContent affectDiffuse = EditorGUIUtility.TrTextContent("Affect Diffuse", "When disabled, HDRP does not calculate diffuse lighting for this Light. Does not increase performance as HDRP still calculates the diffuse lighting.");
            public readonly GUIContent affectSpecular = EditorGUIUtility.TrTextContent("Affect Specular", "When disabled, HDRP does not calculate specular lighting for this Light. Does not increase performance as HDRP still calculates the specular lighting.");
            public readonly GUIContent nonLightmappedOnly = EditorGUIUtility.TrTextContent("Shadowmask Mode", "Determines Shadowmask functionality when using Mixed lighting. Distance Shadowmask casts real-time shadows within the Shadow Distance, and baked shadows beyond. In Shadowmask mode, static GI contributors always cast baked shadows.\nEnable Shadowmask support in the HDRP asset to make use of this feature. Only available when Lighting Mode is set to Shadowmask in the Lighting window.");
            public readonly GUIContent lightDimmer = EditorGUIUtility.TrTextContent("Intensity Multiplier", "Multiplies the intensity of the Light by the given number. This is useful for modifying the intensity of multiple Lights simultaneously without needing know the intensity of each Light.");
            public readonly GUIContent fadeDistance = EditorGUIUtility.TrTextContent("Fade Distance", "Sets the distance from the camera at which light smoothly fades out before HDRP culls it completely. This minimizes popping.");
            public readonly GUIContent spotInnerPercent = EditorGUIUtility.TrTextContent("Inner Angle (%)", "Controls size of the angular attenuation, in percent, of the base angle of the Spot Light's cone.");
            public readonly GUIContent spotIESCutoffPercent = EditorGUIUtility.TrTextContent("IES Cutoff Angle (%)", "Cutoff the IES Light in percent, of the base angle of the Spot Light's cone.");
            public readonly GUIContent spotLightShape = EditorGUIUtility.TrTextContent("Shape", "The shape of the Spot Light. Impacts the the cookie transformation and the Light's angular attenuation.");
            public readonly GUIContent areaLightShape = EditorGUIUtility.TrTextContent("Shape", "The shape of the Area Light. Note that some are Realtime only and some Baked only.");
            public readonly GUIContent[] areaShapeNames =
            {
                EditorGUIUtility.TrTextContent("Rectangle"),
                EditorGUIUtility.TrTextContent("Tube (Realtime only)"),
                EditorGUIUtility.TrTextContent("Disc (Baked only)")
            };
            public readonly GUIContent shapeWidthTube = EditorGUIUtility.TrTextContent("Length", "Length of the Tube Light.");
            public readonly GUIContent shapeWidthRect = EditorGUIUtility.TrTextContent("Size X", "Sets the width of the Rectangle Light.");
            public readonly GUIContent shapeHeightRect = EditorGUIUtility.TrTextContent("Size Y", "Sets the height of the Rectangle Light.");
            public readonly GUIContent barnDoorAngle = EditorGUIUtility.TrTextContent("Barn Door Angle", "Sets the angle of the Rectangle Light so that is behaves like a barn door.");
            public readonly GUIContent barnDoorLength = EditorGUIUtility.TrTextContent("Barn Door Length", "Sets the length for the barn door.");
            public readonly GUIContent aspectRatioPyramid = EditorGUIUtility.TrTextContent("Aspect ratio", "Controls the aspect ration of the Pyramid Light's projection. A value of 1 results in a square.");
            public readonly GUIContent shapeWidthBox = EditorGUIUtility.TrTextContent("Size X", "Sets the width of the Box Light.");
            public readonly GUIContent shapeHeightBox = EditorGUIUtility.TrTextContent("Size Y", "Sets the height of the Box Light.");
            public readonly GUIContent applyRangeAttenuation = EditorGUIUtility.TrTextContent("Range Attenuation", "Allows you to enable or disable range attenuation. Range attenuation is useful for indoor environments because you can avoid having to set up a large range for a Light to get correct inverse square attenuation that may leak out of the indoor environment.");
            public readonly GUIContent displayAreaLightEmissiveMesh = EditorGUIUtility.TrTextContent("Display Emissive Mesh", "Generate an emissive mesh using the size, Color and Intensity of the Area Light.");
            public readonly GUIContent areaLightEmissiveMeshCastShadow = EditorGUIUtility.TrTextContent("Cast Shadows", "Specify wether the generated geometry create shadow or not when a shadow casting Light shines on it");
            public readonly GUIContent areaLightEmissiveMeshMotionVector = EditorGUIUtility.TrTextContent("Motion Vectors", "Specify wether the generated Mesh renders 'Per Object Motion', 'Camera Motion' or 'No Motion' vectors to the Camera Motion Vector Texture.");
            public readonly GUIContent areaLightEmissiveMeshSameLayer = EditorGUIUtility.TrTextContent("Same Layer", "If checked, use the same Layer than the Light one.");
            public readonly GUIContent areaLightEmissiveMeshCustomLayer = EditorGUIUtility.TrTextContent("Custom Layer", "Specify on which layer the generated Mesh live.");

            public readonly GUIContent interactsWithSky = EditorGUIUtility.TrTextContent("Affect Physically Based Sky", "Check this option to make the light and the Physically Based sky affect one another.");
            public readonly GUIContent angularDiameter = EditorGUIUtility.TrTextContent("Angular Diameter", "Angular diameter of the emissive celestial body represented by the light as seen from the camera (in degrees). Used to render the sun/moon disk.");
            public readonly GUIContent flareSize = EditorGUIUtility.TrTextContent("Flare Size", "Size of the flare around the celestial body (in degrees).");
            public readonly GUIContent flareTint = EditorGUIUtility.TrTextContent("Flare Tint", "Tints the flare of the celestial body");
            public readonly GUIContent flareFalloff = EditorGUIUtility.TrTextContent("Flare Falloff", "The falloff rate of flare intensity as the angle from the light increases.");
            public readonly GUIContent surfaceTexture = EditorGUIUtility.TrTextContent("Surface Texture", "2D (disk) texture of the surface of the celestial body. Acts like a multiplier.");
            public readonly GUIContent surfaceTint = EditorGUIUtility.TrTextContent("Surface Tint", "Tints the surface of the celestial body");
            public readonly GUIContent distance = EditorGUIUtility.TrTextContent("Distance", "Distance from the camera (in meters) to the emissive celestial body represented by the light. Primarily used for sorting.");

            public readonly GUIContent shape = EditorGUIUtility.TrTextContent("Type", "Specifies the current type of Light. Possible Light types are Directional, Spot, Point, and Area.");
            public readonly GUIContent enableSpotReflector = EditorGUIUtility.TrTextContent("Reflector", "When enabled, HDRP simulates a physically correct Spot Light using a reflector. This means the narrower the Outer Angle, the more intense the Spot Light.  When disabled, the intensity of the Light matches the one of a Point Light and thus remains constant regardless of the Outer Angle.");
            public readonly GUIContent luxAtDistance = EditorGUIUtility.TrTextContent("At", "Sets the distance, in meters, where a surface receives the amount of light equivalent to the provided number of Lux.");

            // Volumetric Additional light data
            public readonly GUIContent volumetricEnable = EditorGUIUtility.TrTextContent("Enable", "When enabled, this Light uses Volumetrics.");
            public readonly GUIContent volumetricDimmer = EditorGUIUtility.TrTextContent("Multiplier", "Controls the intensity of the scattered Volumetric lighting.");
            public readonly GUIContent volumetricFadeDistance = EditorGUIUtility.TrTextContent("Fade Distance", "Sets the distance from the camera at which light smoothly fades out from contributing to volumetric lighting.");
            // Volumetric Additional shadow data
            public readonly GUIContent volumetricShadowDimmer = EditorGUIUtility.TrTextContent("Shadow Dimmer", "Dims the volumetric shadows this Light casts.");

            // Additional shadow data
            public readonly GUIContent useShadowQualityResolution = EditorGUIUtility.TrTextContent("Use Quality Settings", "Allows to the resolution from the set of predetermined resolutions specified in the quality settings.");
            public readonly GUIContent shadowResolution = EditorGUIUtility.TrTextContent("Resolution", "Sets the rendered resolution of the shadow maps. A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage.");
            public readonly GUIContent shadowFadeDistance = EditorGUIUtility.TrTextContent("Fade Distance", "Sets the distance from the camera at which Shadows fade before HDRP culls them completely. This minimizes popping.");
            public readonly GUIContent shadowDimmer = EditorGUIUtility.TrTextContent("Dimmer", "Dims the shadows this Light casts.");
            public readonly GUIContent shadowTint = EditorGUIUtility.TrTextContent("Tint", "Specifies the color and transparency that HDRP tints this Light's shadows to. The tint affects dynamic shadows, Contact Shadows, and ShadowMask. It does not affect baked shadows.");
            public readonly GUIContent penumbraTint = EditorGUIUtility.TrTextContent("Penumbra Tint", "When enabled, the tint only affects the shadow's penumbra.");
            public readonly GUIContent contactShadows = EditorGUIUtility.TrTextContent("Enable", "Enable support for Contact Shadows on this Light. This is better for lights with a lot of visible shadows.");
            public readonly GUIContent rayTracedContactShadow = EditorGUIUtility.TrTextContent("Ray Tracing (Preview)", "Uses ray tracing to compute the contact shadow for a light.");
            public readonly GUIContent shadowUpdateMode = EditorGUIUtility.TrTextContent("Update Mode", "Specifies when HDRP updates the shadow map.");
            public readonly GUIContent shadowAlwaysDrawDynamic = EditorGUIUtility.TrTextContent("Always draw dynamic", "Specifies whether HDRP renders dynamic shadow caster every frame regardless of the update mode.");
            public readonly GUIContent shadowUpdateOnLightTransformChange = EditorGUIUtility.TrTextContent("Update on light movement", "Whether a cached shadow map will be automatically updated when the light transform changes.");
            public readonly GUIContent useCustomSpotLightShadowCone = EditorGUIUtility.TrTextContent("Custom Spot Angle", "When enabled, this Spot Light uses the custom angle for shadow map rendering.");
            public readonly GUIContent customSpotLightShadowCone = EditorGUIUtility.TrTextContent("Shadow Angle", "Controls the custom angle this Spot Light uses for shadow map rendering.");

            // Bias control
            public readonly GUIContent slopeBias = EditorGUIUtility.TrTextContent("Slope-Scale Depth Bias", "Controls the bias that HDRP adds to the rendered shadow map, it is proportional to the slope of the polygons relative to the light.");

            public readonly GUIContent normalBias = EditorGUIUtility.TrTextContent("Normal Bias", "Controls the bias this Light applies along the normal of surfaces it illuminates.");

            // Shadow filter settings
            public readonly GUIContent blockerSampleCount = EditorGUIUtility.TrTextContent("Blocker Sample Count", "Controls the number of samples that HDRP uses to determine the size of the blocker.");
            public readonly GUIContent filterSampleCount = EditorGUIUtility.TrTextContent("Filter Sample Count", "Controls the number of samples that HDRP uses to blur shadows.");
            public readonly GUIContent minFilterSize = EditorGUIUtility.TrTextContent("Minimum Blur Intensity", "Controls the minimum blur intensity regardless of the distance between the pixel and the shadow caster. The range [0..1] maps to [0..0.001] in UV space.");
            public readonly GUIContent radiusScaleForSoftness = EditorGUIUtility.TrTextContent("Radius Scale for Softness", "Scale the shape radius for the sake of softness calculation. Higher scales will result in higher softness.");
            public readonly GUIContent diameterScaleForSoftness = EditorGUIUtility.TrTextContent("Angular Diameter Scale for Softness", "Scale the angular diameter for the sake of softness calculation. Higher scales will result in higher softness.");
            public readonly GUIContent areaLightShadowCone = EditorGUIUtility.TrTextContent("Shadow Cone", "Aperture of the cone used for shadowing the area light.");
            public readonly GUIContent useScreenSpaceShadows = EditorGUIUtility.TrTextContent("Screen Space Shadows", "Render screen space shadow.");
            public readonly GUIContent useRayTracedShadows = EditorGUIUtility.TrTextContent("Ray Traced Shadows (Preview)", "If selected, ray traced shadows are used in place of rasterized ones.");
            public readonly GUIContent numRayTracingSamples = EditorGUIUtility.TrTextContent("Sample Count", "This defines the number of samples that will be used to evaluate this shadow.");
            public readonly GUIContent denoiseTracedShadow = EditorGUIUtility.TrTextContent("Denoise", "This defines if the ray traced shadow should be filtered.");
            public readonly GUIContent denoiserRadius = EditorGUIUtility.TrTextContent("Denoiser Radius", "This defines the denoiser's radius used for filtering ray traced shadows.");
            public readonly GUIContent distanceBasedFiltering = EditorGUIUtility.TrTextContent("Distance Based Denoising", "This defines if the denoiser should use the distance to the occluder to improve the filtering.");
            public readonly GUIContent semiTransparentShadow = EditorGUIUtility.TrTextContent("Semi Transparent Shadow", "When enabled, the opacity of shadow casters will be taken into account when generating the shadow.");
            public readonly GUIContent colorShadow = EditorGUIUtility.TrTextContent("Color Shadow", "When enabled, the opacity and transmittance color of shadow casters will be taken into account when generating the shadow.");
            public readonly GUIContent evsmExponent = EditorGUIUtility.TrTextContent("EVSM Exponent", "Exponent used for depth warping. Increasing this could reduce light leak and result in a change in appearance of the shadow.");
            public readonly GUIContent evsmLightLeakBias = EditorGUIUtility.TrTextContent("Light Leak Bias", "Increasing this value light leaking, but it eats up a bit of the softness of the shadow.");
            public readonly GUIContent evsmVarianceBias = EditorGUIUtility.TrTextContent("Variance Bias", "Variance Bias for EVSM. This is to contrast numerical accuracy issues. ");
            public readonly GUIContent evsmAdditionalBlurPasses = EditorGUIUtility.TrTextContent("Blur passes", "Increasing this will increase the softness of the shadow, but it will severely impact performance.");

            // Very high shadow settings
            public readonly GUIContent lightAngle = EditorGUIUtility.TrTextContent("Light Angle");
            public readonly GUIContent kernelSize = EditorGUIUtility.TrTextContent("Kernel size");
            public readonly GUIContent maxDepthBias = EditorGUIUtility.TrTextContent("Max Depth Bias");

            // Layers
            public readonly GUIContent unlinkLightAndShadowLayersText = EditorGUIUtility.TrTextContent("Custom Shadow Layers", "When enabled, you can use the Layer property below to specify the layers for shadows seperately to lighting. When disabled, the Light Layer property in the General section specifies the layers for both lighting and for shadows.");
            public readonly GUIContent shadowLayerMaskText = EditorGUIUtility.TrTextContent("Layer", "Specifies the light layer to use for shadows.");

            // Settings
            public readonly GUIContent enableShadowMap = EditorGUIUtility.TrTextContent("Enable", "When enabled, this Light casts shadows.");

            // Warnings
            public readonly string unsupportedLightShapeWarning = L10n.Tr("This light shape is not supported by Realtime Global Illumination.");
        }

        static Styles s_Styles = new Styles();
    }
}
