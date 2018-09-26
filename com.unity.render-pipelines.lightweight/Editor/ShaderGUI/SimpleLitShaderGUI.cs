using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    internal class SimpleLitShaderGUI : BaseShaderGUI
    {
        private const float kMinShininessValue = 0.01f;
        private MaterialProperty albedoMapProp;
        private MaterialProperty albedoColorProp;
        private MaterialProperty specularSourceProp;
        private MaterialProperty glossinessSourceProp;
        private MaterialProperty specularGlossMapProp;
        private MaterialProperty specularColorProp;
        private MaterialProperty shininessProp;
        private MaterialProperty bumpMapProp;
        private MaterialProperty emissionMapProp;
        private MaterialProperty emissionColorProp;

        private static class Styles
        {
            public static GUIContent[] albedoGlosinessLabels =
            {
                new GUIContent("Base (RGB) Glossiness (A)", "Base Color (RGB) and Glossiness (A)"),
                new GUIContent("Base (RGB)", "Base Color (RGB)")
            };

            public static GUIContent albedoAlphaLabel = new GUIContent("Base (RGB) Alpha (A)",
                "Base Color (RGB) and Transparency (A)");

            public static GUIContent[] specularGlossMapLabels =
            {
                new GUIContent("Specular Map (RGB)", "Specular Color (RGB)"),
                new GUIContent("Specular Map (RGB) Glossiness (A)", "Specular Color (RGB) Glossiness (A)")
            };

            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
            public static GUIContent emissionMapLabel = new GUIContent("Emission Map", "Emission Map");

            public static readonly string[] glossinessSourceNames = Enum.GetNames(typeof(GlossinessSource));

            public static string surfaceProperties = "Surface Properties";
            public static string specularSourceLabel = "Specular";
            public static string glossinessSourceLabel = "Glossiness Source";
            public static string glossinessSource = "Glossiness Source";
            public static string albedoColorLabel = "Base Color";
            public static string albedoMapAlphaLabel = "Base(RGB) Alpha(A)";
            public static string albedoMapGlossinessLabel = "Base(RGB) Glossiness (A)";
            public static string shininessLabel = "Shininess";
            public static string normalMapLabel = "Normal map";
            public static string emissionColorLabel = "Emission Color";
        }

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            albedoMapProp = FindProperty("_MainTex", properties);
            albedoColorProp = FindProperty("_Color", properties);
            specularSourceProp = FindProperty("_SpecSource", properties);
            glossinessSourceProp = FindProperty("_GlossinessSource", properties);
            specularGlossMapProp = FindProperty("_SpecGlossMap", properties);
            specularColorProp = FindProperty("_SpecColor", properties);
            shininessProp = FindProperty("_Shininess", properties);
            bumpMapProp = FindProperty("_BumpMap", properties);
            emissionMapProp = FindProperty("_EmissionMap", properties);
            emissionColorProp = FindProperty("_EmissionColor", properties);
        }

        public override void ShaderPropertiesGUI(Material material)
        {
            EditorGUI.BeginChangeCheck();
            {
                base.ShaderPropertiesGUI(material);
                GUILayout.Label(Styles.surfaceProperties, EditorStyles.boldLabel);
                DoSurfaceArea();
                DoSpecular();

                EditorGUILayout.Space();
                materialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMapProp);

                EditorGUILayout.Space();
                DoEmissionArea(material);

                EditorGUI.BeginChangeCheck();
                materialEditor.TextureScaleOffsetProperty(albedoMapProp);
                if (EditorGUI.EndChangeCheck())
                    emissionMapProp.textureScaleAndOffset = albedoMapProp.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendModeProp.targets)
                    MaterialChanged((Material)obj);
            }

            DoMaterialRenderingOptions();
        }

        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            material.shaderKeywords = null;
            SetupMaterialBlendMode(material);
            SetMaterialKeywords(material);
        }

        private void SetMaterialKeywords(Material material)
        {
            material.shaderKeywords = null;
            SetupMaterialBlendMode(material);
            UpdateMaterialSpecularSource(material);
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") == 0.0f);
        }

        private void UpdateMaterialSpecularSource(Material material)
        {
            SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecSource");
            if (specSource == SpecularSource.NoSpecular)
            {
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", false);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", false);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
            }
            else
            {
                GlossinessSource glossSource = (GlossinessSource)material.GetFloat("_GlossinessSource");
                bool hasGlossMap = material.GetTexture("_SpecGlossMap");
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasGlossMap);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", !hasGlossMap);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", glossSource == GlossinessSource.BaseAlpha);
            }
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            if (oldShader == null)
                throw new ArgumentNullException("oldShader");

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            // Shininess value cannot be zero since it will produce undefined values for cases where pow(0, 0).
            float shininess = material.GetFloat("_Shininess");
            material.SetFloat("_Shininess", Mathf.Clamp(shininess, kMinShininessValue, 1.0f));

            string oldShaderName = oldShader.name;
            string[] shaderStrings = oldShaderName.Split('/');

            if (shaderStrings[0].Equals("Legacy Shaders") || shaderStrings[0].Equals("Mobile"))
            {
                ConvertFromLegacy(material, oldShaderName);
            }

            StandardSimpleLightingUpgrader.UpdateMaterialKeywords(material);
        }

        private bool RequiresAlpha()
        {
            SurfaceType surfaceType = (SurfaceType)surfaceTypeProp.floatValue;
            return alphaClipProp.floatValue > 0.0f || surfaceType == SurfaceType.Transparent;
        }

        private void DoSurfaceArea()
        {
            EditorGUILayout.Space();

            int surfaceTypeValue = (int)surfaceTypeProp.floatValue;
            if ((SurfaceType)surfaceTypeValue == SurfaceType.Opaque)
            {
                int glossSource = (int)glossinessSourceProp.floatValue;
                materialEditor.TexturePropertySingleLine(Styles.albedoGlosinessLabels[glossSource], albedoMapProp,
                    albedoColorProp);
            }
            else
            {
                materialEditor.TexturePropertySingleLine(Styles.albedoAlphaLabel, albedoMapProp, albedoColorProp);
            }
        }

        private void DoSpecular()
        {
            EditorGUILayout.Space();

            SpecularSource specularSource = (SpecularSource)specularSourceProp.floatValue;
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle(Styles.specularSourceLabel, specularSource == SpecularSource.SpecularTextureAndColor);
            if (EditorGUI.EndChangeCheck())
                specularSourceProp.floatValue = enabled ? (float)SpecularSource.SpecularTextureAndColor : (float)SpecularSource.NoSpecular;

            SpecularSource specSource = (SpecularSource)specularSourceProp.floatValue;
            if (specSource != SpecularSource.NoSpecular)
            {
                bool hasSpecularMap = specularGlossMapProp.textureValue != null;
                materialEditor.TexturePropertySingleLine(Styles.specularGlossMapLabels[(int)glossinessSourceProp.floatValue], specularGlossMapProp, hasSpecularMap ? null : specularColorProp);

                EditorGUI.indentLevel += 2;
                if (RequiresAlpha())
                {
                    GUI.enabled = false;
                    glossinessSourceProp.floatValue = (float)EditorGUILayout.Popup(Styles.glossinessSourceLabel, (int)GlossinessSource.SpecularAlpha, Styles.glossinessSourceNames);
                    GUI.enabled = true;
                }
                else
                {
                    int glossinessSource = (int)glossinessSourceProp.floatValue;
                    EditorGUI.BeginChangeCheck();
                    glossinessSource = EditorGUILayout.Popup(Styles.glossinessSourceLabel, glossinessSource, Styles.glossinessSourceNames);
                    if (EditorGUI.EndChangeCheck())
                        glossinessSourceProp.floatValue = glossinessSource;
                    GUI.enabled = true;
                }

                EditorGUI.BeginChangeCheck();
                float shininess = EditorGUILayout.Slider(Styles.shininessLabel, shininessProp.floatValue,
                    kMinShininessValue, 1.0f);
                if (EditorGUI.EndChangeCheck())
                    shininessProp.floatValue = shininess;
                EditorGUI.indentLevel -= 2;
            }
        }

        void DoEmissionArea(Material material)
        {
            // Emission for GI?
            if (materialEditor.EmissionEnabledProperty())
            {
                bool hadEmissionTexture = emissionMapProp.textureValue != null;

                // Texture and HDR color controls
                materialEditor.TexturePropertyWithHDRColor(Styles.emissionMapLabel, emissionMapProp, emissionColorProp, false);

                // If texture was assigned and color was black set color to white
                float brightness = emissionColorProp.colorValue.maxColorComponent;
                if (emissionMapProp.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                    emissionColorProp.colorValue = Color.white;

                // LW does not support RealtimeEmissive. We set it to bake emissive and handle the emissive is black right.
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                if (brightness <= 0f)
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        private void ConvertFromLegacy(Material material, string oldShaderName)
        {
            UpgradeParams shaderUpgradeParams = new UpgradeParams();

            if (oldShaderName.Contains("Transp"))
            {
                shaderUpgradeParams.surfaceType = UpgradeSurfaceType.Transparent;
                shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
                shaderUpgradeParams.alphaClip = false;
                shaderUpgradeParams.glosinessSource = GlossinessSource.SpecularAlpha;
            }
            else if (oldShaderName.Contains("Cutout"))
            {
                shaderUpgradeParams.surfaceType = UpgradeSurfaceType.Opaque;
                shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
                shaderUpgradeParams.alphaClip = true;
                shaderUpgradeParams.glosinessSource = GlossinessSource.SpecularAlpha;
            }
            else
            {
                shaderUpgradeParams.surfaceType = UpgradeSurfaceType.Opaque;
                shaderUpgradeParams.blendMode = UpgradeBlendMode.Alpha;
                shaderUpgradeParams.alphaClip = false;
                shaderUpgradeParams.glosinessSource = GlossinessSource.BaseAlpha;
            }

            if (oldShaderName.Contains("Spec"))
                shaderUpgradeParams.specularSource = SpecularSource.SpecularTextureAndColor;
            else
                shaderUpgradeParams.specularSource = SpecularSource.NoSpecular;

            material.SetFloat("_Surface", (float)shaderUpgradeParams.surfaceType);
            material.SetFloat("_Blend", (float)shaderUpgradeParams.blendMode);
            material.SetFloat("_SpecSource", (float)shaderUpgradeParams.specularSource);
            material.SetFloat("_GlossinessSource", (float)shaderUpgradeParams.glosinessSource);

            if (oldShaderName.Contains("Self-Illumin"))
            {
                material.SetTexture("_EmissionMap", material.GetTexture("_MainTex"));
                material.SetTexture("_MainTex", null);
                material.SetColor("_EmissionColor", Color.white);
            }
        }
    }
}
