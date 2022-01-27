using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Decal materials (does not include ShaderGraphs)
    /// </summary>
    class DecalUI : HDShaderGUI
    {
        [Flags]
        enum Expandable : uint
        {
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
            Sorting = 1 << 2,
        }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new DecalSurfaceOptionsUIBlock((MaterialUIBlock.ExpandableBit)Expandable.SurfaceOptions),
            new DecalSurfaceInputsUIBlock((MaterialUIBlock.ExpandableBit)Expandable.SurfaceInputs),
            new DecalSortingInputsUIBlock((MaterialUIBlock.ExpandableBit)Expandable.Sorting),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            uiBlocks.OnGUI(materialEditor, props);
        }

        public override void ValidateMaterial(Material material) => DecalAPI.ValidateMaterial(material);
    }
}
