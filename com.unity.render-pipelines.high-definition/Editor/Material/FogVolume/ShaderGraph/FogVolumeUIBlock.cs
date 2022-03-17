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

        public FogVolumeUIBlock(ExpandableBit expandableBit) : base(expandableBit, Styles.FogVolumeHeader) {}

        public override void LoadMaterialProperties()
        {
            blendMode = FindProperty(FogVolumeAPI.k_BlendModeProperty);
        }

        protected override void OnGUIOpen()
        {
            materialEditor.ShaderProperty(blendMode, Styles.blendMode);
        }
    }
}
