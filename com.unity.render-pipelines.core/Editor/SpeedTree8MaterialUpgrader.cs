using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.Rendering
{
	/// <summary>
	/// Material upgrader and relevant utilities for SpeedTree8.
	/// </summary>
	public class SpeedTree8MaterialUpgrader : MaterialUpgrader
    {
        /// <summary>
        /// Creates a material upgrader with only the renames in common between HD and Universal.
        /// </summary>
        /// <param name="sourceShaderName">Original ST8 shader name.</param>
        /// <param name="destShaderName">New ST8 shader name.</param>
        /// <param name="finalizer">A delegate that postprocesses the material as needed by the render pipeline.</param>
        public SpeedTree8MaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer = null)
        {
            RenameShader(sourceShaderName, destShaderName, finalizer);
            RenameFloat("_WindQuality", "_WINDQUALITY");
            RenameFloat("_TwoSided", "_CullMode"); // Currently only used in HD. Update this once URP per-material cullmode is enabled via shadergraph. 
        }

        private static void ImportNewSpeedTree8Material(Material mat, int windQuality, bool isBillboard)
        {
            int cullmode = 0;
            mat.SetFloat("_WINDQUALITY", windQuality);
            if (isBillboard)
            {
                mat.EnableKeyword("EFFECT_BILLBOARD");
                cullmode = 2;
            }
            if (mat.HasProperty("_CullMode"))
                mat.SetFloat("_CullMode", cullmode);
        }

        /// <summary>
        /// Postprocess materials when importing a SpeedTree8 asset. Call from OnPostprocessSpeedTree in a MaterialPostprocessor.
        /// </summary>
        /// <param name="speedtree">Game object for the SpeedTree asset being imported.</param>
        /// <param name="stImporter">The assetimporter used to import the SpeedTree asset.</param>
        /// <param name="finalizer">Render pipeline-specific material finalizer.</param>
        public static void PostprocessSpeedTree8Materials(GameObject speedtree, SpeedTreeImporter stImporter, MaterialFinalizer finalizer = null)
        {
            LODGroup lg = speedtree.GetComponent<LODGroup>();
            LOD[] lods = lg.GetLODs();
            for (int l = 0; l < lods.Length; l++)
            {
                LOD lod = lods[l];
                bool isBillboard = stImporter.hasBillboard && (l == lods.Length - 1);
                int wq = Mathf.Min(stImporter.windQualities[l], stImporter.bestWindQuality);
                foreach (Renderer r in lod.renderers)
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        ImportNewSpeedTree8Material(m, wq, isBillboard);
                        finalizer(m);
                    }
                }
            }
        }
    }
}
