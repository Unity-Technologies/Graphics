using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    /// <summary>
    /// Editor script for the SimpleLit material inspector.
    /// </summary>
    public static class SimpleLitGUI
    {
        /// <summary>
        /// Options for specular source.
        /// </summary>
        public enum SpecularSource
        {
            /// <summary>
            /// Use this to use specular texture and color.
            /// </summary>
            SpecularTextureAndColor,

            /// <summary>
            /// Use this when not using specular.
            /// </summary>
            NoSpecular
        }

        /// <summary>
        /// Options to select the texture channel where the smoothness value is stored.
        /// </summary>
        public enum SmoothnessMapChannel
        {
            /// <summary>
            /// Use this when smoothness is stored in the alpha channel of the Specular Map.
            /// </summary>
            SpecularAlpha,

            /// <summary>
            /// Use this when smoothness is stored in the alpha channel of the Albedo Map.
            /// </summary>
            AlbedoAlpha,
        }

        /// <summary>
        /// Container for the text and tooltips used to display the shader.
        /// </summary>
        public static class Styles
        {
            /// <summary>
            /// The text and tooltip for the specular map GUI.
            /// </summary>
            public static GUIContent specularMapText =
                EditorGUIUtility.TrTextContent("Specular Map", "Designates a Specular Map and specular color determining the apperance of reflections on this Material's surface.");
        }

        /// <summary>
        /// Container for the properties used in the <c>SimpleLitGUI</c> editor script.
        /// </summary>
        public struct SimpleLitProperties
        {
            // Surface Input Props

            /// <summary>
            /// The MaterialProperty for specular color.
            /// </summary>
            public MaterialProperty specColor;

            /// <summary>
            /// The MaterialProperty for specular smoothness map.
            /// </summary>
            public MaterialProperty specGlossMap;

            /// <summary>
            /// The MaterialProperty for specular highlights.
            /// </summary>
            public MaterialProperty specHighlights;

            /// <summary>
            /// The MaterialProperty for smoothness alpha channel.
            /// </summary>
            public MaterialProperty smoothnessMapChannel;

            /// <summary>
            /// The MaterialProperty for smoothness value.
            /// </summary>
            public MaterialProperty smoothness;

            /// <summary>
            /// The MaterialProperty for normal map.
            /// </summary>
            public MaterialProperty bumpMapProp;

            /// <summary>
            /// Constructor for the <c>SimpleLitProperties</c> container struct.
            /// </summary>
            /// <param name="properties"></param>
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

        /// <summary>
        /// Draws the surface inputs GUI.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="materialEditor"></param>
        /// <param name="material">The material to use.</param>
        public static void Inputs(SimpleLitProperties properties, MaterialEditor materialEditor, Material material)
        {
            DoSpecularArea(properties, materialEditor, material);
            BaseShaderGUI.DrawNormalArea(materialEditor, properties.bumpMapProp);
        }

        /// <summary>
        /// Draws the advanced GUI.
        /// </summary>
        /// <param name="properties"></param>
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

        /// <summary>
        /// Draws the specular area GUI.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="materialEditor"></param>
        /// <param name="material">The material to use.</param>
        public static void DoSpecularArea(SimpleLitProperties properties, MaterialEditor materialEditor, Material material)
        {
            SpecularSource specSource = (SpecularSource)properties.specHighlights.floatValue;
            EditorGUI.BeginDisabledGroup(specSource == SpecularSource.NoSpecular);
            BaseShaderGUI.TextureColorProps(materialEditor, Styles.specularMapText, properties.specGlossMap, properties.specColor, true);
            LitGUI.DoSmoothness(materialEditor, material, properties.smoothness, properties.smoothnessMapChannel, LitGUI.Styles.specularSmoothnessChannelNames);
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Sets up the keywords for the material and shader.
        /// </summary>
        /// <param name="material">The material to use.</param>
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
