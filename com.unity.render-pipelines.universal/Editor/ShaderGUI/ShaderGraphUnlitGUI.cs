using System;
using UnityEngine;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor
{
    // Used for ShaderGraph Unlit shaders
    class ShaderGraphUnlitGUI : BaseShaderGUI
    {
        MaterialProperty[] properties;

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            // save off the list of all properties for shadergraph
            this.properties = properties;

            base.FindProperties(properties);
        }

        public static void UpdateMaterial(Material material, MaterialUpdateType updateType)
        {
            BaseShaderGUI.UpdateMaterialSurfaceOptions(material, automaticRenderQueue: false);
        }

        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            UpdateMaterial(material, MaterialUpdateType.ModifiedMaterial);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(material, properties);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            materialEditor.RenderQueueField();
            base.DrawAdvancedOptions(material);
            materialEditor.DoubleSidedGIField();
        }
    }
} // namespace UnityEditor
