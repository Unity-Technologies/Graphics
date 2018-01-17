using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public sealed partial class DiffusionProfileSettingsEditor
    {
        sealed class Styles
        {
            public readonly GUIContent   profilePreview0           = new GUIContent("Profile Preview");
            public readonly GUIContent   profilePreview1           = new GUIContent("Shows the fraction of light scattered from the source (center).");
            public readonly GUIContent   profilePreview2           = new GUIContent("The distance to the boundary of the image corresponds to the Max Radius.");
            public readonly GUIContent   profilePreview3           = new GUIContent("Note that the intensity of pixels around the center may be clipped.");
            public readonly GUIContent   transmittancePreview0     = new GUIContent("Transmittance Preview");
            public readonly GUIContent   transmittancePreview1     = new GUIContent("Shows the fraction of light passing through the object for thickness values from the remap.");
            public readonly GUIContent   transmittancePreview2     = new GUIContent("Can be viewed as a cross section of a slab of material illuminated by white light from the left.");
            public readonly GUIContent   profileScatteringDistance = new GUIContent("Scattering Distance", "Determines the shape of the profile, and the blur radius of the filter per color channel. Alpha is ignored.");
            public readonly GUIContent   profileTransmissionTint   = new GUIContent("Transmission tint", "Color which tints transmitted light. Alpha is ignored.");
            public readonly GUIContent   profileMaxRadius          = new GUIContent("Max Radius", "Effective radius of the filter (in millimeters). The blur is energy-preserving, so a wide filter results in a large area with small contributions of individual samples. Reducing the distance increases the sharpness of the result.");
            public readonly GUIContent   texturingMode             = new GUIContent("Texturing Mode", "Specifies when the diffuse texture should be applied.");
            public readonly GUIContent[] texturingModeOptions      = new GUIContent[2]
            {
                new GUIContent("Pre- and post-scatter", "Texturing is performed during both the lighting and the SSS passes. Slightly blurs the diffuse texture. Choose this mode if your diffuse texture contains little to no SSS lighting."),
                new GUIContent("Post-scatter",          "Texturing is performed only during the SSS pass. Effectively preserves the sharpness of the diffuse texture. Choose this mode if your diffuse texture already contains SSS lighting (e.g. a photo of skin).")
            };
            public readonly GUIContent   profileTransmissionMode = new GUIContent("Transmission Mode", "Configures the simulation of light passing through thin objects. Depends on the thickness value (which is applied in the normal direction).");
            public readonly GUIContent[] transmissionModeOptions = new GUIContent[3]
            {
                new GUIContent("None",         "Disables transmission. Choose this mode for completely opaque, or very thick translucent objects."),
                new GUIContent("Thin Object",  "Choose this mode for thin objects, such as paper or leaves. Transmitted light reuses the shadowing state of the surface."),
                new GUIContent("Regular",      "Choose this mode for moderately thick objects. For performance reasons, transmitted light ignores occlusion (shadows).")
            };
            public readonly GUIContent   profileMinMaxThickness = new GUIContent("Min-Max Thickness (mm)", "Shows the values of the thickness remap below (in millimeters).");
            public readonly GUIContent   profileThicknessRemap  = new GUIContent("Thickness Remap (mm)", "Remaps the thickness parameter from [0, 1] to the desired range (in millimeters).");
            public readonly GUIContent   profileWorldScale      = new GUIContent("World Scale", "Size of the world unit in meters.");
            public readonly GUIContent   profileIor             = new GUIContent("Index of Refraction", "Index of refraction. 1.4 for skin. Between 1.3-1.5 for most other material.");
            // Jimenez SSS Model
            public readonly GUIContent   profileScatterDistance1 = new GUIContent("Scattering Distance #1", "The radius (in centimeters) of the 1st Gaussian filter, one per color channel. Alpha is ignored. The blur is energy-preserving, so a wide filter results in a large area with small contributions of individual samples. Smaller values increase the sharpness.");
            public readonly GUIContent   profileScatterDistance2 = new GUIContent("Scattering Distance #2", "The radius (in centimeters) of the 2nd Gaussian filter, one per color channel. Alpha is ignored. The blur is energy-preserving, so a wide filter results in a large area with small contributions of individual samples. Smaller values increase the sharpness.");
            public readonly GUIContent   profileLerpWeight       = new GUIContent("Filter Interpolation", "Controls linear interpolation between the two Gaussian filters.");
            // End Jimenez SSS Model
            public readonly GUIStyle     centeredMiniBoldLabel     = new GUIStyle(GUI.skin.label);

            public readonly GUIContent SubsurfaceScatteringLabel = new GUIContent("Subsurface Scattering only");
            public readonly GUIContent TransmissionLabel = new GUIContent("Transmission only");


            public Styles()
            {
                centeredMiniBoldLabel.alignment = TextAnchor.MiddleCenter;
                centeredMiniBoldLabel.fontSize  = 10;
                centeredMiniBoldLabel.fontStyle = FontStyle.Bold;
            }
        }

        static Styles s_Styles;

        // Can't use a static initializer in case we need to create GUIStyle in the Styles class as
        // these can only be created with an active GUI rendering context
        void CheckStyles()
        {
            if (s_Styles == null)
                s_Styles = new Styles();
        }
    }
}
