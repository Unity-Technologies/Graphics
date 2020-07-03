using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class ShaderGraphUIBlock : MaterialUIBlock
    {
        [Flags]
        public enum Features
        {
            None = 0,
            MotionVector = 1 << 0,
            EmissionGI = 1 << 1,
            DiffusionProfileAsset = 1 << 2,
            EnableInstancing = 1 << 3,
            DoubleSidedGI = 1 << 4,
            ShadowMatte = 1 << 5,
            Unlit = MotionVector | EmissionGI | ShadowMatte,
            All = ~0,
        }

        protected static class Styles
        {
            public static readonly string header = "Exposed Properties";
        }

        Expandable  m_ExpandableBit;
        Features    m_Features;

        public ShaderGraphUIBlock(Expandable expandableBit = Expandable.ShaderGraph, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
        }

        public override void LoadMaterialProperties() {}

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                    DrawShaderGraphGUI();
            }
        }

        MaterialProperty[] oldProperties;

        bool CheckPropertyChanged(MaterialProperty[] properties)
        {
            bool propertyChanged = false;

            if (oldProperties != null)
            {
                // Check if shader was changed (new/deleted properties)
                if (properties.Length != oldProperties.Length)
                {
                    propertyChanged = true;
                }
                else
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        if (properties[i].type != oldProperties[i].type)
                            propertyChanged = true;
                        if (properties[i].displayName != oldProperties[i].displayName)
                            propertyChanged = true;
                        if (properties[i].flags != oldProperties[i].flags)
                            propertyChanged = true;
                        if (properties[i].name != oldProperties[i].name)
                            propertyChanged = true;
                        if (properties[i].floatValue != oldProperties[i].floatValue)
                            propertyChanged = true;
                        if (properties[i].vectorValue != oldProperties[i].vectorValue)
                            propertyChanged = true;
                        if (properties[i].colorValue != oldProperties[i].colorValue)
                            propertyChanged = true;
                        if (properties[i].textureValue != oldProperties[i].textureValue)
                            propertyChanged = true;
                    }
                }
            }

            oldProperties = properties;

            return propertyChanged;
        }

        void DrawShaderGraphGUI()
        {
            // Filter out properties we don't want to draw:
            PropertiesDefaultGUI(properties);

            // If we change a property in a shadergraph, we trigger a material keyword reset 
            if (CheckPropertyChanged(properties))
            {
                foreach (var material in materials)
                    HDShaderUtils.ResetMaterialKeywords(material);
            }

            if (properties.Length > 0)
                EditorGUILayout.Space();

            if ((m_Features & Features.DiffusionProfileAsset) != 0)
                DrawDiffusionProfileUI();

            if ((m_Features & Features.EnableInstancing) != 0)
                materialEditor.EnableInstancingField();

            if ((m_Features & Features.DoubleSidedGI) != 0)
            {
                // If the shader graph have a double sided flag, then we don't display this field.
                // The double sided GI value will be synced with the double sided property during the SetupBaseUnlitKeywords()
                if (!materials[0].HasProperty(kDoubleSidedEnable))
                    materialEditor.DoubleSidedGIField();
            }

            if ((m_Features & Features.EmissionGI) != 0)
                DrawEmissionGI();

            if ((m_Features & Features.MotionVector) != 0)
                DrawMotionVectorToggle();

            if ((m_Features & Features.ShadowMatte) != 0 && materials[0].HasProperty(kShadowMatteFilter))
                DrawShadowMatteToggle();
        }

        void PropertiesDefaultGUI(MaterialProperty[] properties)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                if ((properties[i].flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) != 0)
                    continue;

                float h = materialEditor.GetPropertyHeight(properties[i], properties[i].displayName);
                Rect r = EditorGUILayout.GetControlRect(true, h, EditorStyles.layerMaskField);

                materialEditor.ShaderProperty(r, properties[i], properties[i].displayName);
            }
        }

        void DrawEmissionGI()
        {
            if (materialEditor.EmissionEnabledProperty())
            {
                materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true, true);
            }
        }

        // Track additional velocity state. See SG-ADDITIONALVELOCITY-NOTE
        bool m_AddPrecomputedVelocity = false;

        void DrawMotionVectorToggle()
        {
            // I absolutely don't know what this is meant to do
            const string materialTag = "MotionVector";
            foreach (var material in materials)
            {
                string tag = material.GetTag(materialTag, false, "Nothing");
                if (tag == "Nothing")
                {
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, false);
                    material.SetOverrideTag(materialTag, "User");
                }
            }

            // If using multi-select, apply toggled material to all materials.
            bool enabled = materials[0].GetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr);
            EditorGUI.BeginChangeCheck();
            enabled = EditorGUILayout.Toggle("Motion Vector For Vertex Animation", enabled);

            // SG-ADDITIONALVELOCITY-NOTE:
            // We would like to automatically enable the motion vector pass (handled on material UI side)
            // in case we add precomputed velocity in a graph. Due to serialization of material, changing
            // a value in between shadergraph compilations would have no effect on a material, so we instead
            // inform the motion vector UI via the existence of the property at all and query against that.
            bool hasPrecomputedVelocity = materials[0].HasProperty(kAddPrecomputedVelocity);
            if (m_AddPrecomputedVelocity != hasPrecomputedVelocity)
            {
                enabled |= hasPrecomputedVelocity;
                m_AddPrecomputedVelocity = hasPrecomputedVelocity;
                GUI.changed = true;
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var material in materials)
                {
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, enabled);
                }
            }
        }

        void DrawShadowMatteToggle()
        {
            uint exponent = 0b10000000; // 0 as exponent
            uint mantissa = 0x007FFFFF;

            float value = materials[0].GetFloat(HDMaterialProperties.kShadowMatteFilter);
            uint uValue = HDShadowUtils.Asuint(value);
            uint filter = uValue & mantissa;

            bool shadowFilterPoint  = (filter & (uint)LightFeatureFlags.Punctual)       != 0;
            bool shadowFilterDir    = (filter & (uint)LightFeatureFlags.Directional)    != 0;
            bool shadowFilterRect   = (filter & (uint)LightFeatureFlags.Area)           != 0;
            uint finalFlag = 0x00000000;
            finalFlag |= EditorGUILayout.Toggle("Point/Spot Shadow",    shadowFilterPoint) ? (uint)LightFeatureFlags.Punctual    : 0x00000000u;
            finalFlag |= EditorGUILayout.Toggle("Directional Shadow",   shadowFilterDir)   ? (uint)LightFeatureFlags.Directional : 0x00000000u;
            finalFlag |= EditorGUILayout.Toggle("Area Shadow",          shadowFilterRect)  ? (uint)LightFeatureFlags.Area        : 0x00000000u;
            finalFlag &= mantissa;
            finalFlag |= exponent;

            materials[0].SetFloat(HDMaterialProperties.kShadowMatteFilter, HDShadowUtils.Asfloat(finalFlag));
        }

        void DrawDiffusionProfileUI()
        {
            if (DiffusionProfileMaterialUI.IsSupported(materialEditor))
                DiffusionProfileMaterialUI.OnGUI(FindProperty("_DiffusionProfileAsset"), FindProperty("_DiffusionProfileHash"));
        }
    }
}
