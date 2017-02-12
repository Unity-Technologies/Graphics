using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class UnlitGUI : BaseUnlitGUI
    {
        MaterialProperty color = null;
        MaterialProperty colorMap = null;
        MaterialProperty emissiveColor = null;
        MaterialProperty emissiveColorMap = null;
        MaterialProperty emissiveIntensity = null;

        protected const string kEmissiveColorMap = "_EmissiveColorMap";
        protected const string kEmissiveIntensity = "_EmissiveIntensity";

        override protected void FindMaterialProperties(MaterialProperty[] props)
        {
            color = FindProperty("_Color", props);
            colorMap = FindProperty("_ColorMap", props);
            emissiveColor = FindProperty("_EmissiveColor", props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
            emissiveIntensity = FindProperty("_EmissiveIntensity", props);
        }

        override protected void ShaderInputGUI()
        {
            EditorGUI.indentLevel++;

            GUILayout.Label(Styles.InputsText, EditorStyles.boldLabel);

            m_MaterialEditor.TexturePropertySingleLine(Styles.colorText, colorMap, color);

            m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
            m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
            m_MaterialEditor.LightmapEmissionProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);

            EditorGUI.indentLevel--;
        }

        override protected void ShaderInputOptionsGUI()
        {
        }

        protected override void SetupMaterialKeywords(Material material)
        {
			SetupCommonOptionsKeywords(material);
            SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
        }

        protected override bool ShouldEmissionBeEnabled(Material mat)
        {
            float emissiveIntensity = mat.GetFloat(kEmissiveIntensity);
            var realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
            return emissiveIntensity > 0.0f || realtimeEmission;
        }

    }

} // namespace UnityEditor
