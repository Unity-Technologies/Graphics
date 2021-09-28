using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public static class SimpleLitGUI
    {
        public enum SpecularSource
        {
            SpecularTextureAndColor,
            NoSpecular
        }

        public enum SmoothnessMapChannel
        {
            SpecularAlpha,
            AlbedoAlpha,
        }

        public static class Styles
        {
            public static GUIContent specularMapText =
                EditorGUIUtility.TrTextContent("Specular Map", "Designates a Specular Map and specular color determining the apperance of reflections on this Material's surface.");
        }

        public struct SimpleLitProperties
        {
            // Surface Input Props
            public MaterialProperty specColor;
            public MaterialProperty specGlossMap;
            public MaterialProperty specHighlights;
            public MaterialProperty smoothnessMapChannel;
            public MaterialProperty smoothness;
            public MaterialProperty bumpMapProp;

            public SimpleLitProperties(MaterialProperty[] properties)
            {
                // Surface Input Props
                specColor = BaseShaderGUI.FindProperty("_SpecColor", properties);
                specGlossMap = BaseShaderGUI.FindProperty("_SpecGlossMap", properties, false);
                specHighlights = BaseShaderGUI.FindProperty("_SpecularHighlights", properties, false);
                smoothnessMapChannel = BaseShaderGUI.FindProperty("_SmoothnessSource", properties, false);
                smoothness = BaseShaderGUI.FindProperty("_Smoothness", properties, false);
                bumpMapProp = BaseShaderGUI.FindProperty("_BumpMap", properties, false);
            }
        }

        public static void Inputs(SimpleLitProperties properties, MaterialEditor materialEditor, Material material)
        {
            DoSpecularArea(properties, materialEditor, material);
            BaseShaderGUI.DrawNormalArea(materialEditor, properties.bumpMapProp);
        }

        public static void Advanced(SimpleLitProperties properties)
        {
            SpecularSource specularSource = (SpecularSource)properties.specHighlights.floatValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = properties.specHighlights.hasMixedValue;
            bool enabled = EditorGUILayout.Toggle(LitGUI.Styles.highlightsText, specularSource == SpecularSource.SpecularTextureAndColor);
            if (EditorGUI.EndChangeCheck())
                properties.specHighlights.floatValue = enabled ? (float)SpecularSource.SpecularTextureAndColor : (float)SpecularSource.NoSpecular;
            EditorGUI.showMixedValue = false;
        }

        public static void DoSpecularArea(SimpleLitProperties properties, MaterialEditor materialEditor, Material material)
        {
            SpecularSource specSource = (SpecularSource)properties.specHighlights.floatValue;
            EditorGUI.BeginDisabledGroup(specSource == SpecularSource.NoSpecular);
            BaseShaderGUI.TextureColorProps(materialEditor, Styles.specularMapText, properties.specGlossMap, properties.specColor, true);
            LitGUI.DoSmoothness(materialEditor, material, properties.smoothness, properties.smoothnessMapChannel, LitGUI.Styles.specularSmoothnessChannelNames);
            EditorGUI.EndDisabledGroup();
        }

        public static void SetMaterialKeywords(Material material)
        {
            UpdateMaterialSpecularSource(material);
        }

        private static void UpdateMaterialSpecularSource(Material material)
        {
            var opaque = ((BaseShaderGUI.SurfaceType)material.GetFloat("_Surface") ==
                BaseShaderGUI.SurfaceType.Opaque);
            SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecularHighlights");
            if (specSource == SpecularSource.NoSpecular)
            {
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", false);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", false);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
            }
            else
            {
                var smoothnessSource = (SmoothnessMapChannel)material.GetFloat("_SmoothnessSource");
                bool hasMap = material.GetTexture("_SpecGlossMap");
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasMap);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", !hasMap);
                if (opaque)
                    CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", smoothnessSource == SmoothnessMapChannel.AlbedoAlpha);
                else
                    CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);

                string color;
                if (smoothnessSource != SmoothnessMapChannel.AlbedoAlpha || !opaque)
                    color = "_SpecColor";
                else
                    color = "_BaseColor";

                var col = material.GetColor(color);
                float smoothness = material.GetFloat("_Smoothness");
                if (smoothness != col.a)
                {
                    col.a = smoothness;
                    material.SetColor(color, col);
                }
            }
        }
    }
}
