using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class RefractionUIBlock : MaterialUIBlock
    {
        protected static class Styles
        {
            public static string refractionModelText = "Refraction Model";
            public static GUIContent refractionIorText = new GUIContent("Index Of Refraction", "Controls the index of refraction for this Material.");
            public static GUIContent refractionThicknessText = new GUIContent("Refraction Thickness", "Controls the thickness for rough refraction.");
            public static GUIContent refractionThicknessMultiplierText = new GUIContent("Refraction Thickness Multiplier", "Sets an overall thickness multiplier in meters.");
            public static GUIContent refractionThicknessMapText = new GUIContent("Refraction Thickness Map", "Specifies the Refraction Thickness Map (R) for this Material - This acts as a thickness multiplier map.");
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
        protected MaterialProperty thicknessMultiplier = null;
        protected const string kThicknessMultiplier = "_ThicknessMultiplier";
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
            thicknessMultiplier = FindProperty(kThicknessMultiplier, false);
            transmittanceColorMap = FindProperty(kTransmittanceColorMap, false);
            transmittanceColor = FindProperty(kTransmittanceColor, false);
            thicknessMap = FindPropertyLayered(kThicknessMap, m_LayerCount, false);
            thickness = FindPropertyLayered(kThickness, m_LayerCount, false);
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
                if (mode != ScreenSpaceRefraction.RefractionModel.None)
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
                        materialEditor.ShaderProperty(thickness[0], Styles.refractionThicknessText);
                    materialEditor.TexturePropertySingleLine(Styles.refractionThicknessMapText, thicknessMap[0]);

                    ++EditorGUI.indentLevel;
                    materialEditor.ShaderProperty(thicknessMultiplier, Styles.refractionThicknessMultiplierText);
                    thicknessMultiplier.floatValue = Mathf.Max(thicknessMultiplier.floatValue, 0);
                    --EditorGUI.indentLevel;

                    materialEditor.TexturePropertySingleLine(Styles.transmittanceColorText, transmittanceColorMap, transmittanceColor);
                    ++EditorGUI.indentLevel;
                    materialEditor.ShaderProperty(atDistance, Styles.atDistanceText);
                    atDistance.floatValue = Mathf.Max(atDistance.floatValue, 0);
                    --EditorGUI.indentLevel;
                }
            }
        }
    }
}