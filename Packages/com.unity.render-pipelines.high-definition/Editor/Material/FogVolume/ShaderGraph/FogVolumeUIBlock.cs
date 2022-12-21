using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;
using System.Linq;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

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
            public static GUIContent FogVolumeHeader = new GUIContent("Fog Volume Options", "Controls the settings of the fog.");
        }

        MaterialProperty blendMode;
        MaterialProperty singleScatteringAlbedo;
        MaterialProperty fogDistance;

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
            singleScatteringAlbedo = FindProperty(FogVolumeAPI.k_SingleScatteringAlbedoProperty);
            fogDistance = FindProperty(FogVolumeAPI.k_FogDistanceProperty);
        }

        /// <summary>
        /// GUI callback when the header is open
        /// </summary>
        protected override void OnGUIOpen()
        {
            materialEditor.ShaderProperty(singleScatteringAlbedo, FogVolumePropertyBlock.Styles.singleScatteringAlbedo);
            materialEditor.ShaderProperty(fogDistance, FogVolumePropertyBlock.Styles.fogDistance);
            materialEditor.ShaderProperty(blendMode, FogVolumePropertyBlock.Styles.blendMode);
        }
    }
}
