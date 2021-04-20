using System;
using UnityEngine;
using UnityEngine.Rendering;

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

        public static void UpdateMaterial(Material material)
        {
            BaseShaderGUI.SetMaterialKeywords(material);
        }

        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            UpdateMaterial(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(material, properties);
        }
    }
} // namespace UnityEditor
