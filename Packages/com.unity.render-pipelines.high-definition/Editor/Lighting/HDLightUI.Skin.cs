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
            public readonly GUIContent celestialBodyHeader = L10n.TextContent("Celestial Body", null, null, null);
            public readonly GUIContent volumetricHeader = L10n.TextContent("Volumetrics", null, null, null);
            public readonly GUIContent shadowMapSubHeader = L10n.TextContent("Shadow Map", null, null, null);
            public readonly GUIContent contactShadowsSubHeader = L10n.TextContent("Contact Shadows", null, null, null);
            public readonly GUIContent bakedShadowsSubHeader = L10n.TextContent("Baked Shadows", null, null, null);
            public readonly GUIContent veryHighShadowQualitySubHeader = L10n.TextContent("Very High Quality Settings", null, null, null);
            public readonly GUIContent highShadowQualitySubHeader = L10n.TextContent("High Quality Settings", null, null, null);
            public readonly GUIContent mediumShadowQualitySubHeader = L10n.TextContent("Medium Quality Settings", null, null, null);
            public readonly GUIContent lowShadowQualitySubHeader = L10n.TextContent("Low Quality Settings", null, null, null);

            // Base (copy from LightEditor.cs)
            public readonly GUIContent outerAngle = L10n.TextContent("Outer Angle", "Controls the angle, in degrees, at the base of a Spot Light's cone.", null, null);
            public readonly GUIContent cookieSize = L10n.TextContent("Size", "Sets the size of the Cookie mask currently assigned to the Light.", null, null);
            public readonly GUIContent shadowBias = L10n.TextContent("Bias", "Controls the distance at which HDRP pushes shadows away from the Light. Useful for avoiding false self-shadowing artifacts.", null, null);
            public readonly GUIContent shadowNormalBias = L10n.TextContent("Normal Bias", "Controls distance at which HDRP shrinks the shadow casting surfaces along the surface normal. Useful for avoiding false self-shadowing artifacts.", null, null);
            public readonly GUIContent shadowNearPlane = L10n.TextContent("Near Plane", "Controls the value of the shadow camera's near clipping plane for rendering shadows. Clamped to [0.01, 10] for Cone, Pyramid and Point Lights, and [0, 10] for Box and Area Lights.", null, null);
            public readonly GUIContent bakedShadowRadius = L10n.TextContent("Radius", "Sets the amount of artificial softening the baking process applies to the edges of shadows cast by this Point or Spot Light.", null, null);
            public readonly GUIContent bakedShadowAngle = L10n.TextContent("Angle", "Controls the amount of artificial softening the baking process applies to the edges of shadows cast by Directional Lights.", null, null);
            public readonly GUIContent lightBounceIntensity = L10n.TextContent("Indirect Multiplier", "Controls the intensity of the indirect light this Light contributes to the Scene. A value of 0 with a Realtime Light causes HDRP to remove it from realtime global illumination. A value of 0 for Baked and Mixed Lights cause them to no longer emit indirect lighting. This has no effect if you disable both Realtime and Baked global illumination.", null, null);
            public readonly GUIContent areaLightCookie = L10n.TextContent("Cookie", "Cookie mask currently assigned to the area light.", null, null);
            public readonly GUIContent iesTexture = L10n.TextContent("IES Profile", "IES Profile (Support: Point, Spot, Rectangular-Area Lights).", null, null);
            public readonly GUIContent cookieTextureTypeError = L10n.TextContent("HDRP does not support the Cookie Texture type, only Default is supported.", EditorGUIUtility.IconContent("console.warnicon").image, null);
            public readonly string cookieNonPOT = "HDRP does not support non power of two cookie textures.";
            public readonly string cookieTooSmall = "Min texture size for cookies is 2x2 pixels.";
            public readonly GUIContent includeLightForRayTracing = L10n.TextContent("Include For Ray Tracing", "When enabled, the light affects the scene for cameras with the Ray-Tracing frame setting enabled.", null, null);
            public readonly GUIContent includeLightForPathTracing = L10n.TextContent("Include For Path Tracing", "When enabled, the light affects the scene for cameras with Path Tracing enabled.", null, null);

            // Additional light data
            public readonly GUIContent lightRadius = L10n.TextContent("Radius", "Sets the radius of the light source. This affects the falloff of diffuse lighting, the spread of the specular highlight, and the softness of Ray Traced shadows.", null, null);
            public readonly GUIContent affectDiffuse = L10n.TextContent("Affect Diffuse", "When disabled, HDRP does not calculate diffuse lighting for this Light. Does not increase performance as HDRP still calculates the diffuse lighting.", null, null);
            public readonly GUIContent affectSpecular = L10n.TextContent("Affect Specular", "When disabled, HDRP does not calculate specular lighting for this Light. Does not increase performance as HDRP still calculates the specular lighting.", null, null);
            public readonly GUIContent shadowMaskMode = L10n.TextContent("Shadowmask Mode", "Determines Shadowmask functionality when using Mixed lighting. Distance Shadowmask casts real-time shadows within the Shadow Distance, and baked shadows beyond. In Shadowmask mode, static GI contributors always cast baked shadows.\nEnable Shadowmask support in the HDRP asset to make use of this feature. Only available when Lighting Mode is set to Shadowmask in the Lighting window.", null, null);
            public readonly GUIContent lightDimmer = L10n.TextContent("Intensity Multiplier", "Multiplies the intensity of the Light by the given number. This is useful for modifying the intensity of multiple Lights simultaneously without needing know the intensity of each Light. This property does not affect the Physically Based Sky rendering for the main directionnal light.", null, null);
            public readonly GUIContent fadeDistance = L10n.TextContent("Fade Distance", "Sets the distance from the camera at which light smoothly fades out before HDRP culls it completely. This minimizes popping.", null, null);
            public readonly GUIContent innerOuterSpotAngle = L10n.TextContent("Inner / Outer Spot Angle", "Controls the inner and outer angles in degrees, at the base of a Spot light's cone.", null, null);
            public readonly GUIContent spotIESCutoffPercent = L10n.TextContent("IES Cutoff Angle (%)", "Cutoff the IES Light in percent, of the base angle of the Spot Light's cone.", null, null);
            public readonly GUIContent spotLightShape = L10n.TextContent("Shape", "The shape of the Spot Light. Impacts the the cookie transformation and the Light's angular attenuation.", null, null);
            public readonly GUIContent areaLightShape = L10n.TextContent("Shape", "The shape of the Area Light. Note that some are Realtime only and some Baked only.", null, null);
            public readonly GUIContent[] areaShapeNames =
            {
                L10n.TextContent("Rectangle", null, null, null),
                L10n.TextContent("Tube (Realtime only)", null, null, null),
                L10n.TextContent("Disc (Baked only)", null, null, null)
            };
            public readonly GUIContent shapeWidthTube = L10n.TextContent("Length", "Length of the Tube Light.", null, null);
            public readonly GUIContent shapeWidthRect = L10n.TextContent("Size X", "Sets the width of the Rectangle Light.", null, null);
            public readonly GUIContent shapeHeightRect = L10n.TextContent("Size Y", "Sets the height of the Rectangle Light.", null, null);
            public readonly GUIContent shapeRadiusDisc = L10n.TextContent("Radius", "Sets the radius of the Disc Light.", null, null);
            public readonly GUIContent barnDoorAngle = L10n.TextContent("Barn Door Angle", "Sets the angle of the Rectangle Light so that is behaves like a barn door.", null, null);
            public readonly GUIContent barnDoorLength = L10n.TextContent("Barn Door Length", "Sets the length for the barn door.", null, null);
            public readonly GUIContent aspectRatioPyramid = L10n.TextContent("Aspect Ratio", "Controls the aspect ration of the Pyramid Light's projection. A value of 1 results in a square.", null, null);
            public readonly GUIContent shapeWidthBox = L10n.TextContent("Size X", "Sets the width of the Box Light.", null, null);
            public readonly GUIContent shapeHeightBox = L10n.TextContent("Size Y", "Sets the height of the Box Light.", null, null);
            public readonly GUIContent applyRangeAttenuation = L10n.TextContent("Range Attenuation", "Allows you to enable or disable range attenuation. Range attenuation is useful for indoor environments because you can avoid having to set up a large range for a Light to get correct inverse square attenuation that may leak out of the indoor environment.", null, null);
            public readonly GUIContent displayAreaLightEmissiveMesh = L10n.TextContent("Display Emissive Mesh", "Generate an emissive mesh using the size, Color and Intensity of the Area Light.", null, null);
            public readonly GUIContent areaLightEmissiveMeshCastShadow = L10n.TextContent("Cast Shadows", "Specify wether the generated geometry create shadow or not when a shadow casting Light shines on it", null, null);
            public readonly GUIContent areaLightEmissiveMeshMotionVector = L10n.TextContent("Motion Vectors", "Specify wether the generated Mesh renders 'Per Object Motion', 'Camera Motion' or 'No Motion' vectors to the Camera Motion Vector Texture.", null, null);
            public readonly GUIContent areaLightEmissiveMeshSameLayer = L10n.TextContent("Same Layer", "If checked, use the same Layer than the Light one.", null, null);
            public readonly GUIContent areaLightEmissiveMeshCustomLayer = L10n.TextContent("Custom Layer", "Specify on which layer the generated Mesh live.", null, null);

            public readonly GUIContent interactsWithSky = L10n.TextContent("Affect Physically Based Sky", "Check this option to make the light and the Physically Based sky affect one another.", null, null);
            public readonly GUIContent angularDiameter = L10n.TextContent("Angular Diameter", "Angular diameter of the emissive celestial body represented by the light as seen from the camera (in degrees). Used to render the sun/moon disk and affects the sharpness of shadows.", null, null);

            // Celestial Body
            public readonly GUIContent diameterMultiplier = L10n.TextContent("Angular Diameter Multiplier", "Angular diameter used to render the celestial body in the sky without affecting the sharpness of shadows. This value is multiplied by the Angular Diameter set in the Shape section.", null, null);
            public readonly GUIContent diameterOverride = L10n.TextContent("Angular Diameter", "Angular diameter used to render the celestial body in the sky without affecting the sharpness of shadows.", null, null);
            public readonly GUIContent distance = L10n.TextContent("Distance", "Distance from the camera (in meters) to the emissive celestial body represented by the light. This value is only used for sorting.", null, null);
            public readonly GUIContent surfaceColor = L10n.TextContent("Surface Color", "Texture of the surface of the celestial body.", null, null);
            public readonly GUIContent shadingSource = L10n.TextContent("Shading", "Specify the light source used for shading of the Celestial Body.\nIt can either emit it's own light, receive it from a Light in the scene, or using manual settings.", null, null);
            public readonly GUIContent sunLightOverride = L10n.TextContent("Sun Light Override", "Specifiy the Directional Light that should illuminate this Celestial Body.\nIf not specified, HDRP will use the directional light in the scene with the highest intensity.", null, null);
            public readonly GUIContent sunColor = L10n.TextContent("Sun Color", "Color of the light source.", null, null);
            public readonly GUIContent sunIntensity = L10n.TextContent("Sun Intensity", "Intensity of the light source.", null, null);
            public readonly GUIContent phase = L10n.TextContent("Phase", "Controls the area of the surface illuminated by the Sun.", null, null);
            public readonly GUIContent phaseRotation = L10n.TextContent("Phase Rotation", "Rotates the Light Source relatively to the Celestial Body.", null, null);
            public readonly GUIContent earthshine = L10n.TextContent("Earthshine", "Intensity of the light reflected from the planet onto the Celestial Body.", null, null);
            public readonly GUIContent intensity = L10n.TextContent("Intensity", "Intensity of the light emitted by the Celestial Body.", null, null);
            public readonly GUIContent flareSize = L10n.TextContent("Flare Size", "Size of the flare around the celestial body (in degrees).", null, null);
            public readonly GUIContent flareTint = L10n.TextContent("Flare Tint", "Tints the flare of the celestial body", null, null);
            public readonly GUIContent flareFalloff = L10n.TextContent("Flare Falloff", "The falloff rate of flare intensity as the angle from the light increases.", null, null);
            public readonly GUIContent flareMultiplier = L10n.TextContent("Flare Multiplier", "Multiplier applied on the flare intensity.", null, null);


            public readonly GUIContent shape = L10n.TextContent("Type", "Specifies the current type of Light. Possible Light types are Directional, Spot, Point, and Area.", null, null);

            // Volumetric Additional light data
            public readonly GUIContent volumetricEnable = L10n.TextContent("Enable", "When enabled, this Light uses Volumetrics.", null, null);
            public readonly GUIContent volumetricDimmer = L10n.TextContent("Multiplier", "Controls the intensity of the scattered Volumetric lighting.", null, null);
            public readonly GUIContent volumetricFadeDistance = L10n.TextContent("Fade Distance", "Sets the distance from the camera at which light smoothly fades out from contributing to volumetric lighting.", null, null);
            // Volumetric Additional shadow data
            public readonly GUIContent volumetricShadowDimmer = L10n.TextContent("Shadow Dimmer", "Dims the volumetric shadows this Light casts.", null, null);

            // Additional shadow data
            public readonly GUIContent useShadowQualityResolution = L10n.TextContent("Use Quality Settings", "Allows to the resolution from the set of predetermined resolutions specified in the quality settings.", null, null);
            public readonly GUIContent shadowResolution = L10n.TextContent("Resolution", "Sets the rendered resolution of the shadow maps. A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage.", null, null);
            public readonly GUIContent shadowFadeDistance = L10n.TextContent("Fade Distance", "Sets the distance from the camera at which Shadows fade before HDRP culls them completely. This minimizes popping.", null, null);
            public readonly GUIContent shadowDimmer = L10n.TextContent("Dimmer", "Dims the shadows this Light casts.", null, null);
            public readonly GUIContent shadowTint = L10n.TextContent("Tint", "Specifies the color and transparency that HDRP tints this Light's shadows to. The tint affects dynamic shadows, Contact Shadows, and ShadowMask. It does not affect baked shadows.", null, null);
            public readonly GUIContent penumbraTint = L10n.TextContent("Penumbra Tint", "When enabled, the tint only affects the shadow's penumbra.", null, null);
            public readonly GUIContent contactShadows = L10n.TextContent("Enable", "Enable support for Contact Shadows on this Light. This is better for lights with a lot of visible shadows.", null, null);
            public readonly GUIContent rayTracedContactShadow = L10n.TextContent("Ray Tracing", "Uses ray tracing to compute the contact shadow for a light.", null, null);
            public readonly GUIContent shadowUpdateMode = L10n.TextContent("Update Mode", "Specifies when HDRP updates the shadow map.", null, null);
            public readonly GUIContent shadowAlwaysDrawDynamic = L10n.TextContent("Always draw dynamic", "Specifies whether HDRP renders dynamic shadow caster every frame regardless of the update mode.", null, null);
            public readonly GUIContent shadowUpdateOnLightTransformChange = L10n.TextContent("Update on light movement", "Whether a cached shadow map will be automatically updated when the light transform changes.", null, null);
            public readonly GUIContent useCustomSpotLightShadowCone = L10n.TextContent("Custom Spot Angle", "When enabled, this Spot Light uses the custom angle for shadow map rendering.", null, null);
            public readonly GUIContent customSpotLightShadowCone = L10n.TextContent("Shadow Angle", "Controls the custom angle this Spot Light uses for shadow map rendering.", null, null);

            // Bias control
            public readonly GUIContent slopeBias = L10n.TextContent("Slope-Scale Depth Bias", "Controls the bias that HDRP adds to the rendered shadow map, it is proportional to the slope of the polygons relative to the light.", null, null);

            public readonly GUIContent normalBias = L10n.TextContent("Normal Bias", "Controls the bias this Light applies along the normal of surfaces it illuminates.", null, null);

            // Shadow filter settings
            public readonly GUIContent blockerSampleCount = L10n.TextContent("Blocker Sample Count", "Controls the number of samples that HDRP uses to determine the size of the blocker.", null, null);
            public readonly GUIContent filterSampleCount = L10n.TextContent("Filter Sample Count", "Controls the number of samples that HDRP uses to blur shadows.", null, null);
            public readonly GUIContent minFilterSize = L10n.TextContent("Minimum Blur Intensity", "Controls the minimum blur intensity regardless of the distance between the pixel and the shadow caster. The range [0..1] maps to [0..0.001] in UV space.", null, null);
            public readonly GUIContent radiusScaleForSoftness = L10n.TextContent("Radius Scale for Softness", "Scale the shape radius for the sake of softness calculation. Higher scales will result in higher softness.", null, null);
            public readonly GUIContent diameterScaleForSoftness = L10n.TextContent("Angular Diameter Scale for Softness", "Scale the angular diameter for the sake of softness calculation. Higher scales will result in higher softness.", null, null);
            public readonly GUIContent areaLightShadowCone = L10n.TextContent("Shadow Cone", "Aperture of the cone used for shadowing the area light.", null, null);
            public readonly GUIContent useScreenSpaceShadows = L10n.TextContent("Screen Space Shadows", "Render screen space shadow.", null, null);
            public readonly GUIContent useRayTracedShadows = L10n.TextContent("Ray Traced Shadows", "If selected, ray traced shadows are used in place of rasterized ones.", null, null);
            public readonly GUIContent numRayTracingSamples = L10n.TextContent("Sample Count", "This defines the number of samples that will be used to evaluate this shadow.", null, null);
            public readonly GUIContent denoiseTracedShadow = L10n.TextContent("Denoise", "This defines if the ray traced shadow should be filtered.", null, null);
            public readonly GUIContent denoiserRadius = L10n.TextContent("Denoiser Radius", "This defines the denoiser's radius used for filtering ray traced shadows.", null, null);
            public readonly GUIContent distanceBasedFiltering = L10n.TextContent("Distance Based Denoising", "This defines if the denoiser should use the distance to the occluder to improve the filtering.", null, null);
            public readonly GUIContent semiTransparentShadow = L10n.TextContent("Semi Transparent Shadow", "When enabled, the opacity of shadow casters will be taken into account when generating the shadow.", null, null);
            public readonly GUIContent colorShadow = L10n.TextContent("Color Shadow", "When enabled, the opacity and transmittance color of shadow casters will be taken into account when generating the shadow.", null, null);
            public readonly GUIContent evsmExponent = L10n.TextContent("EVSM Exponent", "Exponent used for depth warping. Increasing this could reduce light leak and result in a change in appearance of the shadow.", null, null);
            public readonly GUIContent evsmLightLeakBias = L10n.TextContent("Light Leak Bias", "Increasing this value light leaking, but it eats up a bit of the softness of the shadow.", null, null);
            public readonly GUIContent evsmVarianceBias = L10n.TextContent("Variance Bias", "Variance Bias for EVSM. This is to contrast numerical accuracy issues. ", null, null);
            public readonly GUIContent evsmAdditionalBlurPasses = L10n.TextContent("Blur Passes", "Increasing this will increase the softness of the shadow, but it will severely impact performance.", null, null);
            public readonly GUIContent dirLightPCSSMaxPenumbraSize = L10n.TextContent("Max Penumbra Size", "Maximum size (in world space) of PCSS shadow penumbra limiting blur filter kernel size, larger kernels may require more samples to avoid quality degradation.", null, null);
            public readonly GUIContent dirLightPCSSMaxSamplingDistance = L10n.TextContent("Max Sampling Distance", "Maximum distance (in world space) from the receiver PCSS shadow sampling occurs, lower to avoid light bleeding but may cause self-shadowing", null, null);
            public readonly GUIContent dirLightPCSSMinFilterSizeTexels = L10n.TextContent("Min Filter", "Minimum filter size (in shadowmap texels) to avoid aliasing close to caster", null, null);
            public readonly GUIContent dirLightPCSSMinFilterMaxAngularDiameter = L10n.TextContent("Min Filter Max Angular Diameter", "Maximum angular diameter to reach minimum filter size, lower to avoid self-shadowing but may cause light bleeding", null, null);
            public readonly GUIContent dirLightPCSSBlockerSearchAngularDiameter = L10n.TextContent("Blocker Search Angular Diameter", "Angular diameter to use for blocker search, increase to avoid missing hidden close blockers but may cause self-shadowing", null, null);
            public readonly GUIContent dirLightPCSSBlockerSamplingClumpExponent = L10n.TextContent("Blocker Sampling Clump Exponent", "Affects how blocker search samples are distributed.  Samples distance to center is elevated to this power.", null, null);
            public readonly GUIContent dirLightPCSSBlockerSampleCount = L10n.TextContent("Blocker Sample Count", "Controls the number of samples that HDRP uses to determine average blocker distance, increases quality at the expense of performance.", null, null);
            public readonly GUIContent dirLightPCSSFilterSampleCount = L10n.TextContent("Filter Sample Count", "Controls the number of samples that HDRP uses to blur shadows over the kernel, increases quality at the expense of performance.", null, null);

            // Very high shadow settings
            public readonly GUIContent lightAngle = L10n.TextContent("Light Angle", null, null, null);
            public readonly GUIContent kernelSize = L10n.TextContent("Kernel size", null, null, null);
            public readonly GUIContent maxDepthBias = L10n.TextContent("Max Depth Bias", null, null, null);

            // Layers
            public readonly GUIContent unlinkLightAndShadowLayersText = L10n.TextContent("Custom Shadow Layers", "When enabled, you can use the Layer property below to specify the layers for shadows separately to lighting. When disabled, the Rendering Layer Mask property in the General section specifies the layers for both lighting and shadows.", null, null);
            public readonly GUIContent shadowLayerMaskText = L10n.TextContent("Shadow Layers", "Specifies the rendering layer mask to use for shadows.", null, null);

            // Settings
            public readonly GUIContent enableShadowMap = L10n.TextContent("Enable", "When enabled, this Light casts shadows.", null, null);

            // Warnings
            public readonly string unsupportedLightShapeWarning = L10n.Tr("This light shape is not supported by Realtime Global Illumination.", null);
            public readonly GUIContent areaLightVolumetricsWarning = L10n.TextContent("Area Light Volumetrics settings are only taken into account with Path Tracing.", null, null, null);
        }

        static Styles s_Styles = new Styles();
    }
}
