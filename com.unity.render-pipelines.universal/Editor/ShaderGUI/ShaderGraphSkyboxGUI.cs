using System;
using UnityEngine;
using static Unity.Rendering.Universal.ShaderUtils;

namespace UnityEditor
{
    // Used for ShaderGraph Skybox shaders
    class ShaderGraphSkyboxGUI : BaseShaderGUI
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

        public override void ValidateMaterial(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            UpdateMaterial(material, MaterialUpdateType.ModifiedMaterial);
        }
    }
} // namespace UnityEditor
