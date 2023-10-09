using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Represents an advanced options material UI block.
    /// </summary>
    public class AdvancedOptionsUIBlock : MaterialUIBlock
    {
        /// <summary>Options that define the visibility of fields in the block.</summary>
        [Flags]
        public enum Features
        {
            /// <summary>Hide all the fields in the block.</summary>
            None = 0,
            /// <summary>Display the instancing field.</summary>
            Instancing = 1 << 0,
            /// <summary>Display the specular occlusion field.</summary>
            SpecularOcclusion = 1 << 1,
            /// <summary>Display the add precomputed velocity field.</summary>
            AddPrecomputedVelocity = 1 << 2,
            /// <summary>Display the double sided GI field.</summary>
            DoubleSidedGI = 1 << 3,
            /// <summary>Display the emission GI field.</summary>
            EmissionGI = 1 << 4,
            /// <summary>Display the motion vector field.</summary>
            MotionVector = 1 << 5,
            /// <summary>Display the fields for the shaders.</summary>
            StandardLit = Instancing | SpecularOcclusion | AddPrecomputedVelocity,
            /// <summary>Display all the field.</summary>
            All = ~0
        }

        internal class Styles
        {
            public static GUIContent header { get; } = EditorGUIUtility.TrTextContent("Advanced Options");
            public static GUIContent specularOcclusionModeText { get; } = EditorGUIUtility.TrTextContent("Specular Occlusion Mode", "Determines the mode used to compute specular occlusion");
            public static GUIContent addPrecomputedVelocityText { get; } = EditorGUIUtility.TrTextContent("Add Precomputed Velocity", "When enabled, the material will use motion vectors from the Alembic animation cache. Should not be used on regular meshes or Alembic caches without precomputed motion vectors.");
            public static GUIContent motionVectorForVertexAnimationText { get; } = EditorGUIUtility.TrTextContent("Motion Vectors For Vertex Animation", "When enabled, motion vectors for the material will be forced to be calculated every frame (as opposed to only on object transform changes). This is only needed when the material has time-based vertex animation or when the ShaderGraph writes the 'Velocity' vertex output.");
            public static GUIContent bakedEmission = EditorGUIUtility.TrTextContent("Baked Emission", "");
        }

        MaterialProperty specularOcclusionMode = null;

        MaterialProperty addPrecomputedVelocity = null;
        const string kAddPrecomputedVelocity = HDMaterialProperties.kAddPrecomputedVelocity;

        Features m_Features;

        /// <summary>
        /// Constructs the AdvancedOptionsUIBlock based on the parameters.
        /// </summary>
        /// <param name="expandableBit">Bit index used to store the foldout state.</param>
        /// <param name="features">Features of the block.</param>
        public AdvancedOptionsUIBlock(ExpandableBit expandableBit, Features features = Features.All)
            : base(expandableBit, Styles.header)
        {
            m_Features = features;
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
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

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        protected override void OnGUIOpen()
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
                materialEditor.LightmapEmissionFlagsProperty(0, true);

            if ((m_Features & Features.MotionVector) != 0)
                DrawMotionVectorToggle();
            if ((m_Features & Features.SpecularOcclusion) != 0)
                materialEditor.ShaderProperty(specularOcclusionMode, Styles.specularOcclusionModeText);
            if ((m_Features & Features.AddPrecomputedVelocity) != 0)
            {
                if (addPrecomputedVelocity != null)
                    materialEditor.ShaderProperty(addPrecomputedVelocity, Styles.addPrecomputedVelocityText);
            }
        }

        void DrawMotionVectorToggle()
        {
            //In the case of additional velocity data we will enable the motion vector pass.
            bool enabled = GetEnabledMotionVectorVertexAnim(materials[0]);
            bool mixedValue = materials.Length > 1 && materials.Any(m => GetEnabledMotionVectorVertexAnim(m) != enabled);
            bool addPrecomputedVelocity = materials[0].GetAddPrecomputedVelocity();
            bool mixedPrecomputedVelocity = materials.Length > 1 && materials.Any(m => m.GetAddPrecomputedVelocity() != addPrecomputedVelocity);

            using (new EditorGUI.DisabledScope(addPrecomputedVelocity || mixedPrecomputedVelocity))
            {
                EditorGUI.showMixedValue = mixedValue;
                EditorGUI.BeginChangeCheck();
                enabled = EditorGUILayout.Toggle(Styles.motionVectorForVertexAnimationText, enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var mat in materials)
                        mat.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, enabled);
                }
            }

			// Check for SpeedTree materials and disable custom velocity per-obj motion vector pass
            // if the tree has no vertex shader based wind animation.
            foreach(Material mat in materials)
            {
                if(HDSpeedTree8MaterialUpgrader.IsHDSpeedTree8Material(mat))
                {
                    mat.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, SpeedTree8MaterialUpgrader.IsWindEnabled(mat));
                }
            }

            bool GetEnabledMotionVectorVertexAnim(Material material)
            {
                bool addPrecomputedVelocity = material.GetAddPrecomputedVelocity();
                bool currentMotionVectorState = material.GetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr);

                return currentMotionVectorState || addPrecomputedVelocity;
            }
        }
    }
}
