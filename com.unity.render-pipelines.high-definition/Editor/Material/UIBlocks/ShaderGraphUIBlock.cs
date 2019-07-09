using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class ShaderGraphUIBlock : MaterialUIBlock
    {
        [Flags]
        public enum Features
        {
            None                    = 0,
            MotionVector            = 1 << 0,
            EmissionGI              = 1 << 1,
            DiffusionProfileAsset   = 1 << 2,
            EnableInstancing        = 1 << 3,
            DoubleSidedGI           = 1 << 4,
            Unlit                   = MotionVector | EmissionGI,
            All                     = ~0,
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

        void DrawShaderGraphGUI()
        {
            // Filter out properties we don't want to draw:
            PropertiesDefaultGUI(properties);

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
        }

        void PropertiesDefaultGUI(MaterialProperty[] properties)
        {
            for (var i = 0; i < properties.Length - 2; i++)
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
        bool m_AdditionalVelocityChange = false;

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
            // in case we have additional velocity change enabled in a graph. Due to serialization of material, changing
            // a value in between shadergraph compilations would have no effect on a material, so we instead
            // inform the motion vector UI via the existence of the property at all and query against that.
            bool hasAdditionalVelocityChange = materials[0].HasProperty(kAdditionalVelocityChange);
            if (m_AdditionalVelocityChange != hasAdditionalVelocityChange)
            {
                enabled |= hasAdditionalVelocityChange;
                m_AdditionalVelocityChange = hasAdditionalVelocityChange;
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

        void DrawDiffusionProfileUI()
        {
            if (DiffusionProfileMaterialUI.IsSupported(materialEditor))
                DiffusionProfileMaterialUI.OnGUI(FindProperty("_DiffusionProfileAsset"), FindProperty("_DiffusionProfileHash"));
        }
    }
}
