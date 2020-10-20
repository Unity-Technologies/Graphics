using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class StandardsTerrainToHDTerrainLitUpgrader : MaterialUpgrader
    {
		
        public StandardsTerrainToHDTerrainLitUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer = null)
        {
            RenameShader(sourceShaderName, destShaderName, finalizer);
        }

        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            base.Convert(srcMaterial, dstMaterial);

            HDShaderUtils.ResetMaterialKeywords(dstMaterial);
        }
    }
}
