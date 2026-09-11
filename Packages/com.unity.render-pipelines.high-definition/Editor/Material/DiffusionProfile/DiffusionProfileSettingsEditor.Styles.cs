using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class DiffusionProfileSettingsEditor
    {
        static class Styles
        {
            public static readonly GUIContent scatteringLabel = L10n.TextContent("Scattering", null, null, null);
            public static readonly GUIContent profileScatteringColor = L10n.TextContent("Scattering Color", "Controls the shape of the Diffusion Profile, and should be similar to the diffuse color of the Material.", null, null);
            public static readonly GUIContent profileScatteringDistanceMultiplier = L10n.TextContent("Multiplier", "Acts as a multiplier on the scattering color to control how far light travels below the surface, and controls the effective radius of the filter.", null, null);
            public static readonly GUIContent profileTransmissionTint = L10n.TextContent("Transmission Tint", "Specifies the tint of the translucent lighting transmitted through objects.", null, null);
            public static readonly GUIContent profileMaxRadius = L10n.TextContent("Max Radius", "The maximum radius of the effect you define in Scattering Color and Multiplier.\nWhen the world scale is 1, this value is in millimeters.", null, null);

            public static readonly GUIContent profileWorldScale = L10n.TextContent("World Scale", "Controls the scale of Unity's world units for this Diffusion Profile.", null, null);
            public static readonly GUIContent profileIor = L10n.TextContent("Index of Refraction", "Controls the refractive behavior of the Material, where larger values increase the intensity of specular reflection.", null, null);

            public static readonly GUIContent subsurfaceScatteringLabel = L10n.TextContent("Subsurface Scattering only", null, null, null);
            public static readonly GUIContent smoothnessMultipliers = L10n.TextContent("Dual Lobe Multipliers", "Mutlipliers for the smoothness of the two specular lobes", null, null);

            public static readonly GUIContent transmissionLabel = L10n.TextContent("Transmission only", null, null, null);
            public static readonly GUIContent profileTransmissionMode = L10n.TextContent("Transmission Mode", "Specifies how HDRP calculates light transmission.", null, null);
            public static readonly GUIContent profileMinMaxThickness = L10n.TextContent("Thickness Remap Values (Min-Max)", "Sets the range of thickness values (in millimeters) corresponding to the [0, 1] range of texel values stored in the Thickness Map.", null, null);
            public static readonly GUIContent profileThicknessRemap = L10n.TextContent("Thickness Remap (Min-Max)", profileMinMaxThickness.tooltip, null, null);


            public static readonly GUIContent profilePreview0 = L10n.TextContent("Diffusion Profile Preview", null, null, null);
            public static readonly GUIContent profilePreview1 = L10n.TextContent("Displays the fraction of lights scattered from the source located in the center.", null, null, null);
            public static readonly GUIContent transmittancePreview0 = L10n.TextContent("Transmittance Preview", null, null, null);
            public static readonly GUIContent transmittancePreview1 = L10n.TextContent("Displays the fraction of light passing through the GameObject depending on the values from the Thickness Remap (mm).", null, null, null);
            public static GUIStyle miniBoldButton => s_MiniBoldButton.Value;
            static readonly Lazy<GUIStyle> s_MiniBoldButton = new ( () => new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold
            });
        }
    }
}
