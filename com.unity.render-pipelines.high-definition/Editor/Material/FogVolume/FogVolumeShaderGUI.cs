using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{

    /// <summary>
    /// GUI for Volumetric Fog Unlit shader graphs
    /// </summary>
    internal class FogVolumeShaderGUI : HDShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent blendMode = new GUIContent("Blend Mode", "Specifies how the fog will be blended with the global fog.");
        }

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            // For now we only expose the fog blending mode
            materialEditor.ShaderProperty(FindProperty(FogVolumeSubTarget.k_BlendModeProperty, props), Styles.blendMode);
        }

        public override void ValidateMaterial(Material material) => ShaderGraphAPI.ValidateFogVolumeMaterial(material);
    }
}
