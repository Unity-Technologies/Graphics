using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    internal class ShaderGraphTerrainLitGUI : TerrainLitShaderGUI
    {
        protected override uint materialFilter => (uint)(Expandable.SurfaceOptions | Expandable.SurfaceInputs);
        private MaterialProperty[] properties;

        public override void FindProperties(MaterialProperty[] properties)
        {
            this.properties = properties;

            var material = materialEditor?.target as Material;
            if (material == null)
                return;

            base.FindProperties(properties);
            FindMaterialProperties(properties);
        }

        public override void DrawSurfaceInputs(Material material)
        {
            DrawShaderGraphProperties(material, properties);
        }
    }
}
