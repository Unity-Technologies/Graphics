using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

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

        enum BlendSource
        {
            AlbedoMapAlpha,
            MaskMapBlue
        }
        protected string[] blendSourceNames = Enum.GetNames(typeof(BlendSource));

        protected string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

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

        protected MaterialProperty normalBlendSrc = new MaterialProperty();
        protected const string kNormalBlendSrc = "_NormalBlendSrc";

        protected MaterialProperty maskBlendSrc = new MaterialProperty();
        protected const string kMaskBlendSrc = "_MaskBlendSrc";

        protected MaterialProperty maskBlendMode = new MaterialProperty();
        protected const string kMaskBlendMode = "_MaskBlendMode";
 
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
            normalBlendSrc = FindProperty(kNormalBlendSrc, props);
            maskBlendSrc = FindProperty(kMaskBlendSrc, props);
            maskBlendMode = FindProperty(kMaskBlendMode, props);
            
            // always instanced
            SerializedProperty instancing = m_MaterialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            Decal.MaskBlendFlags blendMode = (Decal.MaskBlendFlags)material.GetFloat(kMaskBlendMode);

            CoreUtils.SetKeyword(material, "_ALBEDOCONTRIBUTION", material.GetFloat(kAlbedoMode) == 1.0f);
            CoreUtils.SetKeyword(material, "_COLORMAP", material.GetTexture(kBaseColorMap));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap)); 
            CoreUtils.SetKeyword(material, "_NORMAL_BLEND_SRC_B", material.GetFloat(kNormalBlendSrc) == 1.0f);
            CoreUtils.SetKeyword(material, "_MASK_BLEND_SRC_B", material.GetFloat(kMaskBlendSrc) == 1.0f);

            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecals3RTStr, true);
            switch (blendMode)
            {
                case Decal.MaskBlendFlags.Metal:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMStr, true);
                    break;

                case Decal.MaskBlendFlags.AO:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOStr, true);
                    break;

                case Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsSStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMSStr, true);
                    break;

                case Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOSStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOSStr, true);
                    break;
            }
        }

        protected void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;
            float normalBlendSrcValue = normalBlendSrc.floatValue;
            float maskBlendSrcValue =  maskBlendSrc.floatValue;
            float maskBlendModeValue = maskBlendMode.floatValue;
            Decal.MaskBlendFlags maskBlendFlags = (Decal.MaskBlendFlags) maskBlendModeValue;              

            HDRenderPipelineAsset hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            bool perChannelMask = hdrp.renderPipelineSettings.decalSettings.perChannelMask;

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
                normalBlendSrcValue = EditorGUILayout.Popup( "Normal blend source", (int)normalBlendSrcValue, blendSourceNames);               
                m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapText, maskMap);
                maskBlendSrcValue = EditorGUILayout.Popup( "Mask blend source", (int)maskBlendSrcValue, blendSourceNames);
                EditorGUILayout.HelpBox("Individual mask map channel blending mode can be enabled/disabled in pipeline asset.\nEnabling this feature incurs a performance cost.", MessageType.Info);
                if(perChannelMask)
                {                   
                    maskBlendFlags = (Decal.MaskBlendFlags)EditorGUILayout.EnumFlagsField( "Mask blend mode", maskBlendFlags);
                    if (maskBlendFlags == 0)
                        maskBlendFlags = Decal.MaskBlendFlags.Smoothness; // can not have nothing, to achieve this effect remove the mask map from shader
                    if (maskBlendFlags == (Decal.MaskBlendFlags)(-1)) // everything
                        maskBlendFlags = Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness;
                }
                m_MaterialEditor.ShaderProperty(decalBlend, Styles.decalBlendText);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                normalBlendSrc.floatValue = normalBlendSrcValue;
                maskBlendSrc.floatValue = maskBlendSrcValue;
                maskBlendMode.floatValue = (float) maskBlendFlags;
                foreach (var obj in m_MaterialEditor.targets)
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
            }
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
