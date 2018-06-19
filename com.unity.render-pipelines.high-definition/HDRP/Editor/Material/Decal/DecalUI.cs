using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class DecalUI : ShaderGUI
    {
        protected static class Styles
        {
            public static string InputsText = "Inputs";

            public static GUIContent baseColorText = new GUIContent("Albedo (RGB) and Blend Factor (A)", "Albedo (RGB) and Blend Factor (A)");
            public static GUIContent baseColorText2 = new GUIContent("Blend Factor (A)", "Blend Factor (A)");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC7/BC5/DXT5(nm))");
            public static GUIContent maskMapText = new GUIContent("Mask Map - M(R), AO(G), D(B), S(A)", "Mask map");
            public static GUIContent decalBlendText = new GUIContent("Decal Blend", "Whole decal blend");
            public static GUIContent BlendText = new GUIContent("Decal Blend", "Whole decal blend");
            public static GUIContent AlbedoModeText = new GUIContent("Albedo contribution", "Base color + Blend, Blend only");
        }

        enum NormalPerPixelBlendSource
        {
            AlbedoMapAlpha,
            MaskMapBlue
        }
        protected string[] normalPerPixelBlendSourceNames = Enum.GetNames(typeof(NormalPerPixelBlendSource));

        enum MaskPerPixelBlendSource
        {
            Zero,
            AlbedoMapAlpha,
            MaskMapBlue
        }
        protected string[] maskPerPixelBlendSourceNames = Enum.GetNames(typeof(MaskPerPixelBlendSource));

        protected MaterialProperty baseColorMap = new MaterialProperty();
        protected const string kBaseColorMap = "_BaseColorMap";

        protected MaterialProperty baseColor = new MaterialProperty();
        protected const string kBaseColor = "_BaseColor";

        protected MaterialProperty normalMap = new MaterialProperty();
        protected const string kNormalMap = "_NormalMap";

        protected MaterialProperty maskMap = new MaterialProperty();
        protected const string kMaskMap = "_MaskMap";

        protected MaterialProperty decalBlend = new MaterialProperty();
        protected const string kDecalBlend = "_DecalBlend";

        protected MaterialProperty albedoMode = new MaterialProperty();
        protected const string kAlbedoMode = "_AlbedoMode";

        protected MaterialProperty normalPerPixelBlend = new MaterialProperty();
        protected const string kNormalPerPixelBlend = "_NormalPerPixelBlend";

        protected MaterialProperty metalnessPerPixelBlend = new MaterialProperty();
        protected const string kMetalnessPerPixelBlend = "_MetalnessPerPixelBlend";

        protected MaterialProperty AOPerPixelBlend = new MaterialProperty();
        protected const string kAOPerPixelBlend = "_AOPerPixelBlend";

        protected MaterialProperty smoothnessPerPixelBlend = new MaterialProperty();
        protected const string kSmoothnessPerPixelBlend = "_SmoothnessPerPixelBlend";

      
        protected MaterialEditor m_MaterialEditor;

        // This is call by the inspector

        void FindMaterialProperties(MaterialProperty[] props)
        {
            baseColor = FindProperty(kBaseColor, props);
            baseColorMap = FindProperty(kBaseColorMap, props);
            normalMap = FindProperty(kNormalMap, props);
            maskMap = FindProperty(kMaskMap, props);
            decalBlend = FindProperty(kDecalBlend, props);
            albedoMode = FindProperty(kAlbedoMode, props);
            normalPerPixelBlend = FindProperty(kNormalPerPixelBlend, props);
            metalnessPerPixelBlend = FindProperty(kMetalnessPerPixelBlend, props);
            AOPerPixelBlend = FindProperty(kAOPerPixelBlend, props);
            smoothnessPerPixelBlend = FindProperty(kSmoothnessPerPixelBlend, props);
            // always instanced
            SerializedProperty instancing = m_MaterialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            CoreUtils.SetKeyword(material, "_ALBEDOCONTRIBUTION", material.GetFloat(kAlbedoMode) == 1.0f);
            CoreUtils.SetKeyword(material, "_COLORMAP", material.GetTexture(kBaseColorMap));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
        }

        protected void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;
            float normalPerPixelBlendValue = normalPerPixelBlend.floatValue;
            float metalnessPerPixelBlendValue =  metalnessPerPixelBlend.floatValue;
            float AOPerPixelBlendValue = AOPerPixelBlend.floatValue;
            float smoothnessPerPixelBlendValue = smoothnessPerPixelBlend.floatValue;

            EditorGUI.showMixedValue = normalPerPixelBlend.hasMixedValue;
            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.LabelField(Styles.InputsText, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(albedoMode, Styles.AlbedoModeText);
                if (material.GetFloat(kAlbedoMode) == 1.0f)
                {
                    m_MaterialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap, baseColor);                    
                }
                else
                {
                    m_MaterialEditor.TexturePropertySingleLine(Styles.baseColorText2, baseColorMap, baseColor);                    
                }
                m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);                               
                normalPerPixelBlendValue = EditorGUILayout.Popup( "Normal Per Pixel Blend", (int)normalPerPixelBlendValue, normalPerPixelBlendSourceNames);               
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapText, maskMap);
                metalnessPerPixelBlendValue = EditorGUILayout.Popup( "Metalness Per Pixel Blend", (int)metalnessPerPixelBlendValue, maskPerPixelBlendSourceNames);               
                smoothnessPerPixelBlendValue = EditorGUILayout.Popup( "Smoothness Per Pixel Blend", (int)smoothnessPerPixelBlendValue, maskPerPixelBlendSourceNames);            
                EditorGUILayout.Space();
                AOPerPixelBlendValue = EditorGUILayout.Popup( "AO Per Pixel Blend", (int)AOPerPixelBlendValue, maskPerPixelBlendSourceNames);   
                EditorGUILayout.Space();                               
                m_MaterialEditor.ShaderProperty(decalBlend, Styles.decalBlendText);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo( "Normal Per Pixel Blend");
                normalPerPixelBlend.floatValue = normalPerPixelBlendValue;
                m_MaterialEditor.RegisterPropertyChangeUndo( "Metalness Per Pixel Blend");
                metalnessPerPixelBlend.floatValue = metalnessPerPixelBlendValue;
                m_MaterialEditor.RegisterPropertyChangeUndo( "AO Per Pixel Blend");
                AOPerPixelBlend.floatValue = AOPerPixelBlendValue;
                m_MaterialEditor.RegisterPropertyChangeUndo( "Smoothness Per Pixel Blend");
                smoothnessPerPixelBlend.floatValue = smoothnessPerPixelBlendValue;
                foreach (var obj in m_MaterialEditor.targets)
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
            }
            EditorGUI.showMixedValue = false;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_MaterialEditor = materialEditor;
            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();

            FindMaterialProperties(props);

            Material material = materialEditor.target as Material;
            ShaderPropertiesGUI(material);

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
}
