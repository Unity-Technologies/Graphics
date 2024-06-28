using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class WaterDecalShaderGraphGUI : HDShaderGUI
    {
        [Flags]
        enum ExpandableBit : uint
        {
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
        }

        MaterialUIBlockList m_UIBlocks = new MaterialUIBlockList
        {
            new WaterDecalSurfaceOptionsUIBlock((MaterialUIBlock.ExpandableBit)ExpandableBit.SurfaceOptions),
            new ShaderGraphUIBlock((MaterialUIBlock.ExpandableBit)ExpandableBit.SurfaceInputs, ShaderGraphUIBlock.Features.ExposedProperties),
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

        /// <summary>
        /// Sets up the keywords and passes for a Decal Shader Graph material.
        /// </summary>
        /// <param name="material">The selected material.</param>
        public static void SetupDecalKeywordsAndPass(Material material) => WaterDecalAPI.SetupWaterDecalKeywordsAndProperties(material);

        /// <summary>
        /// Sets up the keywords and passes for the current selected material.
        /// </summary>
        /// <param name="material">The selected material.</param>
        public override void ValidateMaterial(Material material) => ShaderGraphAPI.ValidateWaterDecalMaterial(material);
    }
}
