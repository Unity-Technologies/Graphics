using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class AdvancedOptionsUIBlock : MaterialUIBlock
    {
        [Flags]
        public enum Features
        {
            None                    = 0,
            Instancing              = 1 << 0,
            SpecularOcclusion       = 1 << 1,
            AddPrecomputedVelocity  = 1 << 2,
            DoubleSidedGI           = 1 << 3,
            EmissionGI              = 1 << 4,
            MotionVector            = 1 << 5,
            StandardLit             = Instancing | SpecularOcclusion | AddPrecomputedVelocity,
            All                     = ~0
        }

        public class Styles
        {
            public const string header = "Advanced Options";
            public static GUIContent specularOcclusionModeText = new GUIContent("Specular Occlusion Mode", "Determines the mode used to compute specular occlusion");
            public static GUIContent addPrecomputedVelocityText = new GUIContent("Add Precomputed Velocity", "Requires additional per vertex velocity info");
            public static readonly GUIContent bakedEmission = new GUIContent("Baked Emission", "");
            public static readonly GUIContent motionVectorForVertexAnimationText = new GUIContent("Motion Vector For Vertex Animation", "When enabled, HDRP will correctly handle velocity for vertex animated object. Only enable if there is vertex animation in the ShaderGraph.");
        }

        protected MaterialProperty specularOcclusionMode = null;
        protected MaterialProperty addPrecomputedVelocity = null;

        protected const string kSpecularOcclusionMode = "_SpecularOcclusionMode";
        protected const string kAddPrecomputedVelocity = HDMaterialProperties.kAddPrecomputedVelocity;

        Expandable  m_ExpandableBit;
        Features    m_Features;

        public AdvancedOptionsUIBlock(Expandable expandableBit, Features features = Features.All)
        {
            m_ExpandableBit = expandableBit;
            m_Features = features;
        }

        public override void LoadMaterialProperties()
        {
            specularOcclusionMode = FindProperty(kSpecularOcclusionMode);

            // Migration code for specular occlusion mode. If we find a keyword _ENABLESPECULAROCCLUSION
            // it mean the material have never been migrated, so force the specularOcclusionMode to 2 in this case
            if (materials.Length == 1)
            {
                if (materials[0].IsKeywordEnabled("_ENABLESPECULAROCCLUSION"))
                {
                    specularOcclusionMode.floatValue = 2.0f;
                }
            }

            addPrecomputedVelocity = FindProperty(kAddPrecomputedVelocity);

        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                    DrawAdvancedOptionsGUI();
            }
        }

        void DrawAdvancedOptionsGUI()
        {
            if ((m_Features & Features.Instancing) != 0)
                materialEditor.EnableInstancingField();

            if ((m_Features & Features.DoubleSidedGI) != 0)
            {
                // If the shader graph have a double sided flag, then we don't display this field.
                // The double sided GI value will be synced with the double sided property during the SetupBaseUnlitKeywords()
                if (!materials.All(m => m.HasProperty(kDoubleSidedEnable)))
                    materialEditor.DoubleSidedGIField();
            }

            if ((m_Features & Features.EmissionGI) != 0)
                DrawEmissionGI();

            if ((m_Features & Features.MotionVector) != 0)
                DrawMotionVectorToggle();
            if ((m_Features & Features.SpecularOcclusion) != 0)
                materialEditor.ShaderProperty(specularOcclusionMode, Styles.specularOcclusionModeText);
            if ((m_Features & Features.AddPrecomputedVelocity) != 0)
            {
                if ( addPrecomputedVelocity != null)
                    materialEditor.ShaderProperty(addPrecomputedVelocity, Styles.addPrecomputedVelocityText);
            }
        }

        void DrawEmissionGI()
        {
            EmissionUIBlock.BakedEmissionEnabledProperty(materialEditor);
        }

        void DrawMotionVectorToggle()
        {
            // We have no way to setup motion vector pass to be false by default for a shader graph
            // So here we workaround it with materialTag system by checking if a tag exist to know if it is
            // the first time we display this information. And thus setup the MotionVector Pass to false.
            const string materialTag = "MotionVector";
            
            string tag = materials[0].GetTag(materialTag, false, "Nothing");
            if (tag == "Nothing")
            {
                materials[0].SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, false);
                materials[0].SetOverrideTag(materialTag, "User");
            }

            //In the case of additional velocity data we will enable the motion vector pass.
            bool addPrecomputedVelocity = false;
            if (materials[0].HasProperty(kAddPrecomputedVelocity))
            {
                addPrecomputedVelocity = materials[0].GetInt(kAddPrecomputedVelocity) != 0;
            }

            bool currentMotionVectorState = materials[0].GetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr);
            bool enabled = currentMotionVectorState || addPrecomputedVelocity;

            EditorGUI.BeginChangeCheck();

            using (new EditorGUI.DisabledScope(addPrecomputedVelocity))
            {
                enabled = EditorGUILayout.Toggle(Styles.motionVectorForVertexAnimationText, enabled);
            }

            if (EditorGUI.EndChangeCheck() || currentMotionVectorState != enabled)
            {
                materials[0].SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, enabled);
            }
        }
    }
}
