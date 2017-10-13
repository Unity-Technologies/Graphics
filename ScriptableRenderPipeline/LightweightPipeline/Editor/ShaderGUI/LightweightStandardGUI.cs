using System;
using UnityEditor.Experimental.Rendering.LightweightPipeline;
using UnityEngine;

namespace UnityEditor
{
    internal class LightweightStandardGUI : ShaderGUI
    {
        public enum WorkflowMode : int
        {
            Specular = 0,
            Metallic
        }

        public enum BlendMode : int
        {
            Opaque,
            Cutout,
            Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        public enum SmoothnessMapChannel : int
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }

        private static class Styles
        {
            public static GUIContent uvSetLabel = new GUIContent("UV Set");

            public static GUIContent albedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
            public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
            public static GUIContent specularMapText = new GUIContent("Specular", "Specular (RGB) and Smoothness (A)");
            public static GUIContent metallicMapText = new GUIContent("Metallic", "Metallic (R) and Smoothness (A)");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness value");
            public static GUIContent smoothnessScaleText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent smoothnessMapChannelText = new GUIContent("Source", "Smoothness texture and channel");
            public static GUIContent highlightsText = new GUIContent("Specular Highlights", "Specular Highlights");
            public static GUIContent reflectionsText = new GUIContent("Reflections", "Glossy Reflections");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
            public static GUIContent heightMapText = new GUIContent("Height Map", "Height Map (G)");
            public static GUIContent occlusionText = new GUIContent("Occlusion", "Occlusion (G)");
            public static GUIContent emissionText = new GUIContent("Color", "Emission (RGB)");
            public static GUIContent detailMaskText = new GUIContent("Detail Mask", "Mask for Secondary Maps (A)");
            public static GUIContent detailAlbedoText = new GUIContent("Detail Albedo x2", "Albedo (RGB) multiplied by 2");
            public static GUIContent detailNormalMapText = new GUIContent("Normal Map", "Normal Map");

            public static string primaryMapsText = "Main Maps";
            public static string secondaryMapsText = "Secondary Maps";
            public static string forwardText = "Forward Rendering Options";
            public static string workflowModeText = "Workflow Mode";
            public static string renderingMode = "Rendering Mode";
            public static string advancedText = "Advanced Options";
            public static readonly string[] workflowNames = Enum.GetNames(typeof(WorkflowMode));
            public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));
            public static readonly string[] metallicSmoothnessChannelNames = {"Metallic Alpha", "Albedo Alpha"};
            public static readonly string[] specularSmoothnessChannelNames = {"Specular Alpha", "Albedo Alpha"};
        }

        #pragma warning disable 0414
        private MaterialProperty workflowMode = null;
        private MaterialProperty blendMode = null;

        private MaterialProperty albedoColor = null;
        private MaterialProperty albedoMap = null;
        private MaterialProperty alphaCutoff = null;

        private MaterialProperty smoothness = null;
        private MaterialProperty smoothnessScale = null;
        private MaterialProperty smoothnessMapChannel = null;

        private MaterialProperty metallic = null;
        private MaterialProperty specColor = null;
        private MaterialProperty metallicGlossMap = null;
        private MaterialProperty specGlossMap = null;
        private MaterialProperty highlights = null;
        private MaterialProperty reflections = null;

        private MaterialProperty bumpScale = null;
        private MaterialProperty bumpMap = null;
        private MaterialProperty occlusionStrength = null;
        private MaterialProperty occlusionMap = null;
        private MaterialProperty heigtMapScale = null;
        private MaterialProperty heightMap = null;
        private MaterialProperty emissionColorForRendering = null;
        private MaterialProperty emissionMap = null;
        private MaterialProperty detailMask = null;
        private MaterialProperty detailAlbedoMap = null;
        private MaterialProperty detailNormalMapScale = null;
        private MaterialProperty detailNormalMap = null;
        private MaterialProperty uvSetSecondary = null;

        private MaterialEditor m_MaterialEditor;
        private const float kMaxfp16 = 65536f; // Clamp to a value that fits into fp16.
        private ColorPickerHDRConfig m_ColorPickerHDRConfig = new ColorPickerHDRConfig(0f, kMaxfp16, 1 / kMaxfp16, 3f);

        private bool m_FirstTimeApply = true;
        #pragma warning restore 0414

        public void FindProperties(MaterialProperty[] props)
        {
            workflowMode = FindProperty("_WorkflowMode", props);
            blendMode = FindProperty("_Mode", props);
            albedoColor = FindProperty("_Color", props);
            albedoMap = FindProperty("_MainTex", props);
            alphaCutoff = FindProperty("_Cutoff", props);

            smoothness = FindProperty("_Glossiness", props);
            smoothnessScale = FindProperty("_GlossMapScale", props, false);
            smoothnessMapChannel = FindProperty("_SmoothnessTextureChannel", props, false);

            metallic = FindProperty("_Metallic", props);
            specColor = FindProperty("_SpecColor", props);
            metallicGlossMap = FindProperty("_MetallicGlossMap", props);
            specGlossMap = FindProperty("_SpecGlossMap", props);
            highlights = FindProperty("_SpecularHighlights", props);
            reflections = FindProperty("_GlossyReflections", props);

            bumpScale = FindProperty("_BumpScale", props);
            bumpMap = FindProperty("_BumpMap", props);
            heigtMapScale = FindProperty("_Parallax", props);
            heightMap = FindProperty("_ParallaxMap", props);
            occlusionStrength = FindProperty("_OcclusionStrength", props);
            occlusionMap = FindProperty("_OcclusionMap", props);
            emissionColorForRendering = FindProperty("_EmissionColor", props);
            emissionMap = FindProperty("_EmissionMap", props);
            detailMask = FindProperty("_DetailMask", props);
            detailAlbedoMap = FindProperty("_DetailAlbedoMap", props);
            detailNormalMapScale = FindProperty("_DetailNormalMapScale", props);
            detailNormalMap = FindProperty("_DetailNormalMap", props);
            uvSetSecondary = FindProperty("_UVSec", props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            m_MaterialEditor = materialEditor;
            Material material = materialEditor.target as Material;

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a lightweight shader.
            if (m_FirstTimeApply)
            {
                MaterialChanged(material);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                DoPopup(Styles.workflowModeText, workflowMode, Styles.workflowNames);
                DoPopup(Styles.renderingMode, blendMode, Styles.blendNames);

                // Primary properties
                GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);
                DoAlbedoArea(material);
                DoMetallicSpecularArea();
                DoNormalArea();

                m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);

                DoEmissionArea(material);
                EditorGUI.BeginChangeCheck();
                m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
                if (EditorGUI.EndChangeCheck())
                    emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

                EditorGUILayout.Space();

                m_MaterialEditor.ShaderProperty(highlights, Styles.highlightsText);
                m_MaterialEditor.ShaderProperty(reflections, Styles.reflectionsText);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendMode.targets)
                    MaterialChanged((Material)obj);
            }

            EditorGUILayout.Space();

            // NB renderqueue editor is not shown on purpose: we want to override it based on blend mode
            GUILayout.Label(Styles.advancedText, EditorStyles.boldLabel);
            m_MaterialEditor.EnableInstancingField();
            m_MaterialEditor.DoubleSidedGIField();
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));
                return;
            }

            BlendMode blendMode = BlendMode.Opaque;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                blendMode = BlendMode.Cutout;
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                blendMode = BlendMode.Fade;
            }
            material.SetFloat("_Mode", (float)blendMode);

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float) WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float) WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }

            MaterialChanged(material);
        }

        private void DoPopup(string label, MaterialProperty property, string[] options)
        {
            EditorGUI.showMixedValue = property.hasMixedValue;

            var mode = property.floatValue;
            EditorGUI.BeginChangeCheck();
            mode = EditorGUILayout.Popup(label, (int) mode, options);
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.RegisterPropertyChangeUndo(label);
                property.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        void DoNormalArea()
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap);
        }

        void DoAlbedoArea(Material material)
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
            if (((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout))
            {
                m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);
            }
        }

        void DoEmissionArea(Material material)
        {
            // Emission for GI?
            if (m_MaterialEditor.EmissionEnabledProperty())
            {
                bool hadEmissionTexture = emissionMap.textureValue != null;

                // Texture and HDR color controls
                m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionText, emissionMap, emissionColorForRendering, m_ColorPickerHDRConfig, false);

                // If texture was assigned and color was black set color to white
                float brightness = emissionColorForRendering.colorValue.maxColorComponent;
                if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                    emissionColorForRendering.colorValue = Color.white;

                // LW does not support RealtimeEmissive. We set it to bake emissive and handle the emissive is black right.
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                if (brightness <= 0f)
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        void DoMetallicSpecularArea()
        {
            string[] metallicSpecSmoothnessChannelName;
            bool hasGlossMap = false;
            if ((WorkflowMode) workflowMode.floatValue == WorkflowMode.Metallic)
            {
                hasGlossMap = metallicGlossMap.textureValue != null;
                metallicSpecSmoothnessChannelName = Styles.metallicSmoothnessChannelNames;
                m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicGlossMap,
                    hasGlossMap ? null : metallic);
            }
            else
            {
                hasGlossMap = specGlossMap.textureValue != null;
                metallicSpecSmoothnessChannelName = Styles.specularSmoothnessChannelNames;
                m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specGlossMap,
                    hasGlossMap ? null : specColor);
            }

            bool showSmoothnessScale = hasGlossMap;
            if (smoothnessMapChannel != null)
            {
                int smoothnessChannel = (int)smoothnessMapChannel.floatValue;
                if (smoothnessChannel == (int)SmoothnessMapChannel.AlbedoAlpha)
                    showSmoothnessScale = true;
            }

            int indentation = 2; // align with labels of texture properties
            m_MaterialEditor.ShaderProperty(showSmoothnessScale ? smoothnessScale : smoothness, showSmoothnessScale ? Styles.smoothnessScaleText : Styles.smoothnessText, indentation);

            int prevIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 3;
            if (smoothnessMapChannel != null)
                DoPopup(Styles.smoothnessMapChannelText.text, smoothnessMapChannel, metallicSpecSmoothnessChannelName);
            EditorGUI.indentLevel = prevIndentLevel;
        }

        public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    break;
                case BlendMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
                case BlendMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
            }
        }

        static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;
            else
                return SmoothnessMapChannel.SpecularMetallicAlpha;
        }

        static void SetMaterialKeywords(Material material)
        {
            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            bool isSpecularWorkFlow = (WorkflowMode)material.GetFloat("_WorkflowMode") == WorkflowMode.Specular;
            bool hasGlossMap = false;
            if (isSpecularWorkFlow)
                hasGlossMap = material.GetTexture("_SpecGlossMap");
            else
                hasGlossMap = material.GetTexture("_MetallicGlossMap");

            LightweightShaderHelper.SetKeyword(material, "_SPECULAR_SETUP", isSpecularWorkFlow);
            LightweightShaderHelper.SetKeyword(material, "_METALLIC_SETUP", !isSpecularWorkFlow);

            LightweightShaderHelper.SetKeyword(material, "_METALLICSPECGLOSSMAP", hasGlossMap);
            LightweightShaderHelper.SetKeyword(material, "_SPECGLOSSMAP", hasGlossMap && isSpecularWorkFlow);
            LightweightShaderHelper.SetKeyword(material, "_METALLICGLOSSMAP", hasGlossMap && !isSpecularWorkFlow);

            LightweightShaderHelper.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));

            LightweightShaderHelper.SetKeyword(material, "_SPECULARHIGHLIGHTS_OFF", material.GetFloat("_SpecularHighlights") == 0.0f);
            LightweightShaderHelper.SetKeyword(material, "_GLOSSYREFLECTIONS_OFF", material.GetFloat("_GlossyReflections") == 0.0f);

            LightweightShaderHelper.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
            LightweightShaderHelper.SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
            LightweightShaderHelper.SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            LightweightShaderHelper.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                LightweightShaderHelper.SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
            }
        }

        static void MaterialChanged(Material material)
        {
            material.shaderKeywords = null;
            SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));
            SetMaterialKeywords(material);
        }
    }
}
