using System;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class DiffusionProfileSettingsEditor
    {
        static class Styles
        {
            public static readonly GUIContent scatteringLabel = EditorGUIUtility.TrTextContent("Scattering");
            public static readonly GUIContent profileScatteringColor = EditorGUIUtility.TrTextContent("Scattering Color", "Controls the shape of the Diffusion Profile, and should be similar to the diffuse color of the Material.");
            public static readonly GUIContent profileScatteringDistanceMultiplier = EditorGUIUtility.TrTextContent("Multiplier", "Acts as a multiplier on the scattering color to control how far light travels below the surface, and controls the effective radius of the filter.");
            public static readonly GUIContent profileTransmissionTint = EditorGUIUtility.TrTextContent("Transmission Tint", "Specifies the tint of the translucent lighting transmitted through objects.");
            public static readonly GUIContent profileMaxRadius = EditorGUIUtility.TrTextContent("Max Radius", "The maximum radius of the effect you define in Scattering Color and Multiplier.\nWhen the world scale is 1, this value is in millimeters.");

            public static readonly GUIContent profileWorldScale = EditorGUIUtility.TrTextContent("World Scale", "Controls the scale of Unity's world units for this Diffusion Profile.");
            public static readonly GUIContent profileIor = EditorGUIUtility.TrTextContent("Index of Refraction", "Controls the refractive behavior of the Material, where larger values increase the intensity of specular reflection.");

            public static readonly GUIContent subsurfaceScatteringLabel = EditorGUIUtility.TrTextContent("Subsurface Scattering only");
            public static readonly GUIContent smoothnessMultipliers = EditorGUIUtility.TrTextContent("Dual Lobe Multipliers", "Mutlipliers for the smoothness of the two specular lobes");

            public static readonly GUIContent transmissionLabel = EditorGUIUtility.TrTextContent("Transmission only");
            public static readonly GUIContent profileTransmissionMode = EditorGUIUtility.TrTextContent("Transmission Mode", "Specifies how HDRP calculates light transmission.");
            public static readonly GUIContent profileMinMaxThickness = EditorGUIUtility.TrTextContent("Thickness Remap Values (Min-Max)", "Sets the range of thickness values (in millimeters) corresponding to the [0, 1] range of texel values stored in the Thickness Map.");
            public static readonly GUIContent profileThicknessRemap = EditorGUIUtility.TrTextContent("Thickness Remap (Min-Max)", profileMinMaxThickness.tooltip);


            public static readonly GUIContent profilePreview0 = EditorGUIUtility.TrTextContent("Diffusion Profile Preview");
            public static readonly GUIContent profilePreview1 = EditorGUIUtility.TrTextContent("Displays the fraction of lights scattered from the source located in the center.");
            public static readonly GUIContent transmittancePreview0 = EditorGUIUtility.TrTextContent("Transmittance Preview");
            public static readonly GUIContent transmittancePreview1 = EditorGUIUtility.TrTextContent("Displays the fraction of light passing through the GameObject depending on the values from the Thickness Remap (mm).");
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
