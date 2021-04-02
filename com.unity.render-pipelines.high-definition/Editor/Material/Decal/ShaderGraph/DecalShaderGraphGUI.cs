using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Represents the GUI for HDRP Shader Graph materials.
    /// </summary>
    public class DecalShaderGraphGUI : HDShaderGUI
    {
        [Flags]
        enum ExpandableBit : uint
        {
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
            Sorting = 1 << 2,
        }

        MaterialUIBlockList m_UIBlocks = new MaterialUIBlockList
        {
            new DecalSurfaceOptionsUIBlock((MaterialUIBlock.ExpandableBit)ExpandableBit.SurfaceOptions),
            new ShaderGraphUIBlock((MaterialUIBlock.ExpandableBit)ExpandableBit.SurfaceInputs, ShaderGraphUIBlock.Features.ExposedProperties),
            new DecalSortingInputsUIBlock((MaterialUIBlock.ExpandableBit)ExpandableBit.Sorting),
        };

        /// <summary>The list of UI Blocks Unity uses to render the material inspector.</summary>
        protected MaterialUIBlockList uiBlocks => m_UIBlocks;

        /// <summary>
        /// Override this function to implement your custom GUI. To display a user interface similar to HDRP shaders, use a MaterialUIBlockList.
        /// </summary>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="props">The list of properties the material has.</param>
        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            uiBlocks.OnGUI(materialEditor, props);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        /// <summary>
        /// Sets up the keywords and passes for a Decal Shader Graph material.
        /// </summary>
        /// <param name="material">The selected material.</param>
        public static void SetupDecalKeywordsAndPass(Material material) => DecalUI.SetupCommonDecalMaterialKeywordsAndPass(material);

        /// <summary>
        /// Sets up the keywords and passes for the current selected material.
        /// </summary>
        /// <param name="material">The selected material.</param>
        public override void ValidateMaterial(Material material) => SetupDecalKeywordsAndPass(material);
    }
}
