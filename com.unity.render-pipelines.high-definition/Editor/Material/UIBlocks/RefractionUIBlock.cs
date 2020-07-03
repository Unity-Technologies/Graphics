using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class RefractionUIBlock : MaterialUIBlock
    {
        protected static class Styles
        {
            public static string refractionModelText = "Refraction Model";
            public static GUIContent refractionIorText = new GUIContent("Index Of Refraction", "Controls the index of refraction for this Material.");
            public static GUIContent refractionThicknessText = new GUIContent("Thickness", "Controls the thickness for rough refraction.");
            public static GUIContent refractionThicknessMapText = new GUIContent("Thickness Map", "Specifies the Refraction Thickness Map (R) for this Material - This acts as a thickness multiplier map.");
            public static GUIContent refractionThicknessRemappingText = new GUIContent("Thickness Remapping", "Controls the maximum thickness for rough refraction.");
            public static GUIContent thicknessMapText = new GUIContent("Thickness Map", "Specifies the Thickness Map (R) for this Material - This map describes the thickness of the object. When subsurface scattering is enabled, low values allow some light to transmit through the object.");
            public static GUIContent transmittanceColorText = new GUIContent("Transmittance Color", "Specifies the Transmittance Color (RGB) for this Material.");
            public static GUIContent atDistanceText = new GUIContent("Transmittance Absorption Distance", "Sets the absorption distance reference in meters.");
        }

        protected MaterialProperty refractionModel = null;
        protected const string kRefractionModel = "_RefractionModel";
        protected MaterialProperty ssrefractionProjectionModel = null;
        protected const string kSSRefractionProjectionModel = "_SSRefractionProjectionModel";
        protected MaterialProperty atDistance = null;
        protected const string kATDistance = "_ATDistance";
        protected MaterialProperty[] thickness = null;
        protected const string kThickness = "_Thickness";
        protected MaterialProperty[] thicknessRemap = null;
        protected const string kThicknessRemap = "_ThicknessRemap";
        protected MaterialProperty[] thicknessMap = null;
        protected const string kThicknessMap = "_ThicknessMap";
        protected MaterialProperty ior = null;
        protected const string kIor = "_Ior";
        protected MaterialProperty transmittanceColorMap = null;
        protected const string kTransmittanceColorMap = "_TransmittanceColorMap";
        protected MaterialProperty transmittanceColor = null;
        protected const string kTransmittanceColor = "_TransmittanceColor";

        int m_LayerCount;

        public RefractionUIBlock(int layerCount)
        {
            m_LayerCount = layerCount;
        }

        public override void LoadMaterialProperties()
        {
            refractionModel = FindProperty(kRefractionModel, false);
            ssrefractionProjectionModel = FindProperty(kSSRefractionProjectionModel, false);
            atDistance = FindProperty(kATDistance, false);
            transmittanceColorMap = FindProperty(kTransmittanceColorMap, false);
            transmittanceColor = FindProperty(kTransmittanceColor, false);
            thicknessMap = FindPropertyLayered(kThicknessMap, m_LayerCount, false);
            thickness = FindPropertyLayered(kThickness, m_LayerCount, false);
            thicknessRemap = FindPropertyLayered(kThicknessRemap, m_LayerCount, false);
            ior = FindProperty(kIor, false);
        }

        public override void OnGUI()
        {
            // TODO: this does not works with multiple materials !
            var isPrepass = HDRenderQueue.k_RenderQueue_PreRefraction.Contains(materials[0].renderQueue);
            if (refractionModel != null
                // Refraction is not available for pre-refraction objects
                && !isPrepass)
            {
                materialEditor.ShaderProperty(refractionModel, Styles.refractionModelText);
                var mode = (ScreenSpaceRefraction.RefractionModel)refractionModel.floatValue;
                switch (mode)
                {
                    case ScreenSpaceRefraction.RefractionModel.Box:
                    case ScreenSpaceRefraction.RefractionModel.Sphere:
                    {
                        materialEditor.ShaderProperty(ior, Styles.refractionIorText);
                        // TODO: change check
                        foreach (var material in materials)
                        {
                            // TODO
                            // material.SetBlendMode(BlendMode.Alpha);
                            // blendMode.floatValue = (float)BlendMode.Alpha;
                        }

                        if (thicknessMap[0].textureValue == null)
                        {
                            materialEditor.TexturePropertySingleLine(Styles.refractionThicknessText, thicknessMap[0], thickness[0]);
                        }
                        else
                        {
                            materialEditor.TexturePropertySingleLine(Styles.refractionThicknessMapText, thicknessMap[0]);
                            // Display the remap of texture values.
                            Vector2 remap = thicknessRemap[0].vectorValue;
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.MinMaxSlider(Styles.refractionThicknessRemappingText, ref remap.x, ref remap.y, 0.0f, 1.0f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                thicknessRemap[0].vectorValue = remap;
                            }
                        }


                        materialEditor.TexturePropertySingleLine(Styles.transmittanceColorText, transmittanceColorMap, transmittanceColor);
                        ++EditorGUI.indentLevel;
                        materialEditor.ShaderProperty(atDistance, Styles.atDistanceText);
                        atDistance.floatValue = Mathf.Max(atDistance.floatValue, 0);
                        --EditorGUI.indentLevel;
                    }
                    break;
                    case ScreenSpaceRefraction.RefractionModel.Thin:
                    {
                        materialEditor.ShaderProperty(ior, Styles.refractionIorText);
                        materialEditor.TexturePropertySingleLine(Styles.transmittanceColorText, transmittanceColorMap, transmittanceColor);
                    }
                    break;
                    default:
                        break;
                }
            }
        }
    }
}
