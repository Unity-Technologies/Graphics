using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDLightUI
    {
        sealed class Styles
        {
            // Headers
            public readonly GUIContent generalHeader = new GUIContent("General");
            public readonly GUIContent shapeHeader = new GUIContent("Shape");
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
            public readonly GUIContent outterAngle = new GUIContent("Outter Angle", "Controls the angle, in degrees, at the base of a Spot Light's cone.");
            public readonly GUIContent cookieSizeX = new GUIContent("Size X", "Controls the width of the Cookie mask currently assigned to the Light.");
            public readonly GUIContent cookieSizeY = new GUIContent("Size Y", "Controls the height of the Cookie mask currently assigned to the Light.");
            public readonly GUIContent shadowBias = new GUIContent("Bias", "Controls the distance at which HDRP pushes shadows away from the Light. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent shadowNormalBias = new GUIContent("Normal Bias", "Controls distance at which HDRP shrinks the shadow casting surfaces along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public readonly GUIContent shadowNearPlane = new GUIContent("Near Plane", "Controls the value for the active Camera's Near clipping Plane for rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");
            public readonly GUIContent bakedShadowRadius = new GUIContent("Radius", "Sets the amount of artificial softening the baking process applies to the edges of shadows cast by this Point or Spot Light.");
            public readonly GUIContent bakedShadowAngle = new GUIContent("Angle", "Controls the amount of artificial softening the baking process applies to the edges of shadows cast by Directional Lights.");
            public readonly GUIContent lightBounceIntensity = new GUIContent("Indirect Multiplier", "Controls the intensity of the indirect light this Light contributes to the Scene. A value of 0 with a Realtime Light causes HDRP to remove it from realtime global illumination. A value of 0 for Baked and Mixed Lights cause them to no longer emit indirect lighting. This has no effect if you disable both Realtime and Baked global illumination.");
            public readonly GUIContent indirectBounceShadowWarning = new GUIContent("HDRP does not support real-time indirect bounce shadowing for Spot and Point lights.");
            public readonly GUIContent color = new GUIContent("Color", "Specifies the color this Light emits.");
            public readonly GUIContent useColorTemperature = new GUIContent("Color Temperature", "When enabled, HDRP uses Temperature mode for this Light's color.");
            public readonly GUIContent colorFilter = new GUIContent("Filter", "Specifies a color which tints the Light source.");
            public readonly GUIContent colorTemperature = new GUIContent("Temperature", "Specifies a temperature (in Kelvin) HDRP uses to correlate a color for the Light. For reference, White is 6500K.");
            public readonly GUIContent areaLightCookie = new GUIContent("Cookie", "Cookie mask currently assigned to the area light.");


            // Additional light data
            public readonly GUIContent directionalIntensity = new GUIContent("Intensity (Lux)", "Illuminance of the Directional Light, at ground level, in lux.");
            public readonly GUIContent punctualIntensity = new GUIContent("Intensity (Lumen)", "Luminous power of the Light in lumen.");
            public readonly GUIContent areaIntensity = new GUIContent("Intensity (Lumen)", "Luminous power of the Light in Lumen.");
            public readonly GUIContent lightIntensity = new GUIContent("Intensity", "");

            public readonly GUIContent maxSmoothness = new GUIContent("Max Smoothness", "Controls the roughness of the Material and is useful when the specular highlight is either too small or too sharp. Very low cost way of faking spherical area lighting.");
            public readonly GUIContent lightRadius = new GUIContent("Emission Radius", "Sets the softness of the core of the Punctual Light to create fill lighting.");
            public readonly GUIContent affectDiffuse = new GUIContent("Affect Diffuse", "When disabled, HDRP does not calculate diffuse lighting for this Light. Does not increase performance as HDRP still calculates the diffuse lighting.");
            public readonly GUIContent affectSpecular = new GUIContent("Affect Specular", "When disabled, HDRP does not calculate specular lighting for this Light. Does not increase performance as HDRP still calculates the specular lighting.");
            public readonly GUIContent nonLightmappedOnly = new GUIContent("Shadowmask Mode", "Species the behavior of  the shadowmask when using Mixed lighting. Distance Shadowmask: HDRP uses real-time shadows to Shadow Distance and baked shadows after. Shadowmask: Static shadow casters always use baked shadows.");
            public readonly GUIContent lightDimmer = new GUIContent("Dimmer", "Controls a dimming effect of the Light as a percentage of its intensity. This is useful for reducing the intensity of multiple Lights simultaneously without needing know the intensity of each Light.");
            public readonly GUIContent fadeDistance = new GUIContent("Fade Distance", "The distance at which light smoothly fades out before HDRP culls it completely. This minimizes popping.");
            public readonly GUIContent spotInnerPercent = new GUIContent("Inner Angle (%)", "Controls size of the angular attenuation, in percent, of the base angle of the Spot Light's cone.");
            public readonly GUIContent spotLightShape = new GUIContent("Shape", "The shape of the Spot Light. Impacts the the cookie transformation and the Light's angular attenuation.");
            public readonly GUIContent shapeWidthTube = new GUIContent("Length", "Length of the Tube Light.");
            public readonly GUIContent shapeWidthRect = new GUIContent("Size X", "Sets the width of the Rectangle Light.");
            public readonly GUIContent shapeHeightRect = new GUIContent("Size Y", "Sets the height of the Rectangle Light.");
            public readonly GUIContent aspectRatioPyramid = new GUIContent("Aspect ratio", "Controls the aspect ration of the Pyramid Light's projection. A value of 1 results in a square.");
            public readonly GUIContent shapeWidthBox = new GUIContent("Size X", "Sets the width of the Box Light.");
            public readonly GUIContent shapeHeightBox = new GUIContent("Size Y", "Sets the height of the Box Light.");
            public readonly GUIContent applyRangeAttenuation = new GUIContent("Range Attenuation", "Allows you to enable or disable range attenuation. Range attenuation is useful for indoor environments because you can avoid having to set up a large range for a Light to get correct inverse square attenuation that may leak out of the indoor environment.");
            public readonly GUIContent displayAreaLightEmissiveMesh = new GUIContent("Display Emissive Mesh", "Generate an emissive mesh using the size, Color and Intensity of the Area Light.");
            public readonly GUIContent lightLayer = new GUIContent("Light Layer", "Specifies the current Light Layers that the Light affects. This Light illuminates corresponding Renderers with the same Light Layer flags.");

            public readonly GUIContent sunDiskSize = new GUIContent("Sun Highlight Disk Size", "Controls the size of the highlight of the sun disk which is the angle of the sun cone in degrees.");
            public readonly GUIContent sunHaloSize = new GUIContent("Sun Highlight Halo Size", "Controls the size of the halo around the highlight of the sun disk.");

            public readonly GUIContent shape = new GUIContent("Type", "Specifies the current type of Light. Possible Light types are Directional, Spot, Point, Rectangle, and Tube.");
            public readonly GUIContent[] shapeNames;
            public readonly GUIContent enableSpotReflector = new GUIContent("Reflector", "When enabled, HDRP simulates a Spot Light with a reflector making the intensity of the Light more focused with a narrower angle. When disabled, the intensity is constent whatever the size of the cone.");
            public readonly GUIContent luxAtDistance = new GUIContent("At", "Sets the distance, in meters, where a surface receives the amount of light equivalent to the provided number of Lux.");

            // Volumetric Additional light data
            public readonly GUIContent volumetricEnable = new GUIContent("Enable", "When enabled, this Light uses Volumetrics.");
            public readonly GUIContent volumetricDimmer = new GUIContent("Dimmer", "Controls the intensity of the scattered Volumetric lighting.");
            // Volumetric Additional shadow data
            public readonly GUIContent volumetricShadowDimmer = new GUIContent("Shadow Dimmer", "Dims the volumetric shadows this Light casts.");

            // Additional shadow data
            public readonly GUIContent shadowResolution = new GUIContent("Resolution", "Sets the rendered resolution of the shadow maps. A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage.");
            public readonly GUIContent shadowFadeDistance = new GUIContent("Fade Distance", "Sets the distance at which Shadows fade before HDRP culls them completely. This minimizes popping.");
            public readonly GUIContent shadowDimmer = new GUIContent("Dimmer", "Dims the shadows this Light casts.");
            public readonly GUIContent contactShadows = new GUIContent("Enable", "Enable support for Contact Shadows on this Light. This is better for lights with a lot of visible shadows.");

            // Bias control
            public readonly GUIContent viewBiasMin = new GUIContent("View Bias");
            public readonly GUIContent viewBiasMax = new GUIContent("View Bias Max");
            public readonly GUIContent viewBiasScale = new GUIContent("View Bias Scale");
            public readonly GUIContent normalBiasMin = new GUIContent("Normal Bias");
            public readonly GUIContent normalBiasMax = new GUIContent("Normal Bias Max");
            public readonly GUIContent normalBiasScale = new GUIContent("Normal Bias Scale");
            public readonly GUIContent sampleBiasScale = new GUIContent("Sample Bias Scale");
            public readonly GUIContent edgeLeakFixup = new GUIContent("Edge Leak Fixup");
            public readonly GUIContent edgeToleranceNormal = new GUIContent("Edge Tolerance Normal");
            public readonly GUIContent edgeTolerance = new GUIContent("Edge Tolerance");

            // Shadow filter settings
            public readonly GUIContent shadowSoftness = new GUIContent("Shadow Softness", "Size of the penumbra.");
            public readonly GUIContent blockerSampleCount = new GUIContent("Blocker Sample Count", "Sample count HDRP uses to determine the size of the blocker.");
            public readonly GUIContent filterSampleCount = new GUIContent("Filter Sample Count");
            public readonly GUIContent minFilterSize = new GUIContent("Minimal size of the filter");

            // Very high shadow settings
            public readonly GUIContent lightAngle = new GUIContent("Light Angle");
            public readonly GUIContent kernelSize = new GUIContent("Kernel size");
            public readonly GUIContent maxDepthBias = new GUIContent("Max Depth Bias");

            // Settings
            public readonly GUIContent enableShadowMap = new GUIContent("Enable");

            public Styles()
            {
                shapeNames = Enum.GetNames(typeof(HDLightUI.LightShape))
                    .Select(x => new GUIContent(x))
                    .ToArray();
            }
        }

        static Styles s_Styles = new Styles();
    }
}
