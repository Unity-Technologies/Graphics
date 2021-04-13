using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that displays distortion properties for materials.
    /// </summary>
    public class DistortionUIBlock : MaterialUIBlock
    {
        internal static class Styles
        {
            public static GUIContent distortionEnableText = new GUIContent("Distortion", "When enabled, HDRP processes distortion for this Material.");
            public static GUIContent distortionOnlyText = new GUIContent("Distortion Only", "When enabled, HDRP only uses this Material to render distortion.");
            public static GUIContent distortionDepthTestText = new GUIContent("Distortion Depth Test", "When enabled, HDRP calculates a depth test for distortion.");
            public static GUIContent distortionVectorMapText = new GUIContent("Distortion Vector Map (RGB)", "Specifies the Vector Map HDRP uses for the distortion effect\nDistortion 2D vector (RG) and Blur amount (B)\nHDRP applies the scale and bias to the distortion vector only, not the blur amount.");
            public static GUIContent distortionBlendModeText = new GUIContent("Distortion Blend Mode", "Specifies the mode HDRP uses to calculate distortion.");
            public static GUIContent distortionScaleText = new GUIContent("Distortion Scale", "Sets the scale HDRP applies to the Distortion Vector Map.");
            public static GUIContent distortionBlurScaleText = new GUIContent("Distortion Blur Scale", "Sets the scale HDRP applies to the distortion blur effect.");
            public static GUIContent distortionBlurRemappingText = new GUIContent("Distortion Blur Remapping", "Controls a remap for the Distortion Blur effect.");
            public static GUIContent distortionRoughInfoText = new GUIContent("Blur parameters will have an effect on the distortion if the Rough Distortion Frame Setting is enabled on the target camera.");
        }

        MaterialProperty distortionEnable = null;
        const string kDistortionEnable = "_DistortionEnable";
        MaterialProperty distortionOnly = null;
        const string kDistortionOnly = "_DistortionOnly";
        MaterialProperty distortionDepthTest = null;
        const string kDistortionDepthTest = "_DistortionDepthTest";
        MaterialProperty distortionVectorMap = null;
        const string kDistortionVectorMap = "_DistortionVectorMap";
        MaterialProperty distortionBlendMode = null;
        const string kDistortionBlendMode = "_DistortionBlendMode";
        MaterialProperty distortionScale = null;
        const string kDistortionScale = "_DistortionScale";
        MaterialProperty distortionVectorScale = null;
        const string kDistortionVectorScale = "_DistortionVectorScale";
        MaterialProperty distortionVectorBias = null;
        const string kDistortionVectorBias = "_DistortionVectorBias";
        MaterialProperty distortionBlurScale = null;
        const string kDistortionBlurScale = "_DistortionBlurScale";
        MaterialProperty distortionBlurRemapMin = null;
        const string kDistortionBlurRemapMin = "_DistortionBlurRemapMin";
        MaterialProperty distortionBlurRemapMax = null;
        const string kDistortionBlurRemapMax = "_DistortionBlurRemapMax";

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            distortionEnable = FindProperty(kDistortionEnable, false);
            distortionOnly = FindProperty(kDistortionOnly, false);
            distortionDepthTest = FindProperty(kDistortionDepthTest, false);
            distortionVectorMap = FindProperty(kDistortionVectorMap, false);
            distortionBlendMode = FindProperty(kDistortionBlendMode, false);
            distortionScale = FindProperty(kDistortionScale, false);
            distortionVectorScale = FindProperty(kDistortionVectorScale, false);
            distortionVectorBias = FindProperty(kDistortionVectorBias, false);
            distortionBlurScale = FindProperty(kDistortionBlurScale, false);
            distortionBlurRemapMin = FindProperty(kDistortionBlurRemapMin, false);
            distortionBlurRemapMax = FindProperty(kDistortionBlurRemapMax, false);
        }

        /// <summary>
        /// Renders the distortion properties.
        /// </summary>
        public override void OnGUI()
        {
            if (distortionEnable != null)
            {
                materialEditor.ShaderProperty(distortionEnable, Styles.distortionEnableText);

                if (distortionEnable.floatValue == 1.0f)
                {
                    EditorGUILayout.HelpBox(Styles.distortionRoughInfoText.text, MessageType.Info, true);
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(distortionBlendMode, Styles.distortionBlendModeText);
                    if (distortionOnly != null)
                        materialEditor.ShaderProperty(distortionOnly, Styles.distortionOnlyText);
                    materialEditor.ShaderProperty(distortionDepthTest, Styles.distortionDepthTestText);

                    EditorGUI.indentLevel++;
                    materialEditor.TexturePropertySingleLine(Styles.distortionVectorMapText, distortionVectorMap, distortionVectorScale, distortionVectorBias);
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(distortionScale, Styles.distortionScaleText);
                    materialEditor.ShaderProperty(distortionBlurScale, Styles.distortionBlurScaleText);
                    float remapMin = distortionBlurRemapMin.floatValue;
                    float remapMax = distortionBlurRemapMax.floatValue;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(Styles.distortionBlurRemappingText, ref remapMin, ref remapMax, 0.0f, 1.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        distortionBlurRemapMin.floatValue = remapMin;
                        distortionBlurRemapMax.floatValue = remapMax;
                    }
                    EditorGUI.indentLevel--;

                    EditorGUI.indentLevel--;

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
