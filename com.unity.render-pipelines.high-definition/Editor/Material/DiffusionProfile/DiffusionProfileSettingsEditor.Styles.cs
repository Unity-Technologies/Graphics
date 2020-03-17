using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class DiffusionProfileSettingsEditor
    {
        sealed class Styles
        {
            public readonly GUIContent   profilePreview0           = new GUIContent("Diffusion Profile Preview");
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
                new GUIContent("Pre- and post-scatter", "HDRP performs texturing during both the lighting and the subsurface scattering passes. This blurs the diffuse texture. Choose this mode if your diffuse texture contains little to no subsurface scattering lighting."),
                new GUIContent("Post-scatter",          "HDRP performs texturing only during the subsurface scattering pass. Preserves the sharpness of the diffuse texture. Choose this mode if your diffuse texture contains subsurface scattering lighting (for example a photo of skin).")
            };
            public readonly GUIContent   profileTransmissionMode = new GUIContent("Transmission Mode", "Configures the simulation of light passing through thin objects. Depends on the thickness value (which HDRP applies in the normal direction).");
            public readonly GUIContent[] transmissionModeOptions = new GUIContent[2]
            {
                new GUIContent("Thick Object",      "Choose this mode for thick objects. For performance reasons, transmitted light ignores occlusion (shadows)."),
                new GUIContent("Thin Object",  		"Choose this mode for thin objects, such as paper or leaves. Transmitted light reuses the shadowing state of the surface.")
            };
            public readonly GUIContent   profileMinMaxThickness = new GUIContent("Thickness Remap Values (Min-Max)", "Shows the values of the thickness remap below (in millimeters).");
            public readonly GUIContent   profileThicknessRemap  = new GUIContent("Thickness Remap (Min-Max)", "Remaps the thickness parameter from [0, 1] to the desired range (in millimeters).");
            public readonly GUIContent   profileWorldScale      = new GUIContent("World Scale", "Size of the world unit in meters.");
            public readonly GUIContent   profileIor             = new GUIContent("Index of Refraction", "Select the index of refraction for this Diffusion Profile. For reference, skin is 1.4 and most materials are between 1.3 and 1.5.");
            public readonly GUIStyle     centeredMiniBoldLabel  = new GUIStyle(GUI.skin.label);

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
