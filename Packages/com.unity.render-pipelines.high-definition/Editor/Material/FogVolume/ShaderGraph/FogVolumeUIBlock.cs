using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// The UI block that represents fog volume option for materials.
    /// </summary>
    public class FogVolumeUIBlock : MaterialUIBlock
    {
        static class Styles
        {
            public static GUIContent FogVolumeHeader = new GUIContent("Fog Options", "Controls the settings of the fog.");
            public static readonly GUIContent blendMode = new GUIContent("Blend Mode", "Specifies how the fog will be blended with the global fog.");
        }

        MaterialProperty blendMode = null;

        /// <summary>
        /// Create the UI block for the fog volume material type.
        /// </summary>
        /// <param name="expandableBit"></param>
        public FogVolumeUIBlock(ExpandableBit expandableBit) : base(expandableBit, Styles.FogVolumeHeader) {}

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            blendMode = FindProperty(FogVolumeAPI.k_BlendModeProperty);
        }

        /// <summary>
        /// GUI callback when the header is open
        /// </summary>
        protected override void OnGUIOpen()
        {
            // Disabled for now since we already have the option in the local volumetric fog.
            // We'll enable this for VFX graph integration
            // materialEditor.ShaderProperty(blendMode, Styles.blendMode);
        }
    }
}
