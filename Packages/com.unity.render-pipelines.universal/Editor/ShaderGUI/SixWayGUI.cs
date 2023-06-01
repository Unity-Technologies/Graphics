using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor
{
    // Used for ShaderGraph Lit shaders
    class SixWayGUI : ShaderGraphLitGUI
    {
        private MaterialProperty useColorAbsorption;

        /// <summary>
        /// Container for the text and tooltips used to display the shader.
        /// </summary>
        protected class SixWayStyles
        {
            // Categories
            /// <summary>
            /// The text and tooltip for the surface options GUI.
            /// </summary>
            public static readonly GUIContent UseColorAbsorptionText =
                EditorGUIUtility.TrTextContent("Use Color Absorption", "When enabled, the lightmaps are used to simulate color absorption whose strength can be tuned with the Absorption Strength parameter.");
        }

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            useColorAbsorption = FindProperty(UniversalSixWaySubTarget.SixWayProperties.UseColorAbsorption, properties, false);
        }
        public override void DrawSurfaceOptions(Material material)
        {
            base.DrawSurfaceOptions(material);
            if(useColorAbsorption != null)
                materialEditor.ShaderProperty(useColorAbsorption, SixWayStyles.UseColorAbsorptionText);

        }

        public static void UpdateSixWayKeywords(Material material)
        {
            bool useColorAbsorptionValue = false;
            if (material.HasProperty(UniversalSixWaySubTarget.SixWayProperties.UseColorAbsorption))
                useColorAbsorptionValue = material.GetFloat(UniversalSixWaySubTarget.SixWayProperties.UseColorAbsorption) > 0.0f;
            CoreUtils.SetKeyword(material, "_SIX_WAY_COLOR_ABSORPTION", useColorAbsorptionValue);
        }

        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            UpdateSixWayKeywords(material);
        }
    }
} // namespace UnityEditor
