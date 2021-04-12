using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Material upgrader and relevant utilities for SpeedTree 8.
    /// </summary>
    public class SpeedTree8MaterialUpgrader : MaterialUpgrader
    {
        private enum WindQuality
        {
            None = 0,
            Fastest,
            Fast,
            Better,
            Best,
            Palm,
            Count
        }

        private static string[] WindQualityString =
        {
            "_WINDQUALITY_NONE",
            "_WINDQUALITY_FASTEST",
            "_WINDQUALITY_FAST",
            "_WINDQUALITY_BETTER",
            "_WINDQUALITY_BEST",
            "_WINDQUALITY_PALM"
        };

        /// <summary>
        /// Creates a material upgrader that handles the property renames that HD and Universal have in common when upgrading
        /// from the built-in SpeedTree 8 shader.
        /// </summary>
        /// <param name="sourceShaderName">Original SpeedTree8 shader name.</param>
        /// <param name="destShaderName">New SpeedTree 8 shader name.</param>
        /// <param name="finalizer">A delegate that postprocesses the material for the render pipeline in use.</param>
        public SpeedTree8MaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer = null)
        {
            RenameShader(sourceShaderName, destShaderName, finalizer);
            RenameFloat("_WindQuality", "_WINDQUALITY");
            RenameFloat("_TwoSided", "_CullMode"); // Currently only used in HD. Update this once URP per-material cullmode is enabled via shadergraph.
        }

        private static void ImportNewSpeedTree8Material(Material mat, int windQuality, bool isBillboard)
        {
            if (mat == null)
                return;

            int cullmode = 0;
            mat.SetFloat("_WINDQUALITY", windQuality);
            if (isBillboard)
            {
                mat.SetFloat("EFFECT_BILLBOARD", 1.0f);
                cullmode = 2;
            }
            if (mat.HasProperty("_CullMode"))
                mat.SetFloat("_CullMode", cullmode);

            if (mat.IsKeywordEnabled("EFFECT_EXTRA_TEX"))
                mat.SetFloat("EFFECT_EXTRA_TEX", 1.0f);
        }

        /// <summary>
        /// Postprocesses materials while you are importing a SpeedTree 8 asset. Call from OnPostprocessSpeedTree in a MaterialPostprocessor.
        /// </summary>
        /// <param name="speedtree">The GameObject Unity creates from this imported SpeedTree.</param>
        /// <param name="stImporter">The asset importer used to import this SpeedTree asset.</param>
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
                    // Override default motion vector generation mode pending
                    // proper motion vector integration in SRPs.
                    r.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
                    foreach (Material m in r.sharedMaterials)
                    {
                        float cutoff = stImporter.alphaTestRef;
                        ImportNewSpeedTree8Material(m, wq, isBillboard);
                        if (finalizer != null)
                            finalizer(m);
                    }
                }
            }
        }

        /// <summary>
        /// Preserves wind quality and billboard settings while you are upgrading a SpeedTree 8 material from previous versions of SpeedTree 8.
        /// Wind priority order is enabled keyword > _WindQuality float value.
        /// Should work for upgrading versions within a pipeline and from standard to current pipeline.
        /// </summary>
        /// <param name="material">SpeedTree 8 material to upgrade.</param>
        public static void SpeedTree8MaterialFinalizer(Material material)
        {
            if (material.HasProperty("_TwoSided") && material.HasProperty("_CullMode"))
                material.SetFloat("_CullMode", material.GetFloat("_TwoSided"));

            if (material.IsKeywordEnabled("EFFECT_EXTRA_TEX"))
                material.SetFloat("EFFECT_EXTRA_TEX", 1.0f);

            bool isBillboard = material.IsKeywordEnabled("EFFECT_BILLBOARD");
            if (material.HasProperty("EFFECT_BILLBOARD"))
                material.SetFloat("EFFECT_BILLBOARD", isBillboard ? 1.0f : 0.0f);

            UpgradeWindQuality(material);
        }

        private static void UpgradeWindQuality(Material material, int windQuality = -1)
        {
            int wq = GetWindQuality(material, windQuality);
            SetWindQuality(material, wq);
        }

        private static int GetWindQuality(Material material, int windQuality = -1)
        {
            // Conservative wind quality priority:
            // input WindQuality > enabled keyword > _WindQuality float value
            if (!WindIntValid(windQuality))
            {
                windQuality = GetWindQualityFromKeywords(material.shaderKeywords);
                if (!WindIntValid(windQuality))
                {
                    windQuality = material.HasProperty("_WindQuality") ? (int)material.GetFloat("_WindQuality") : 0;

                    if (!WindIntValid(windQuality))
                        windQuality = 0;
                }
            }
            return windQuality;
        }

        private static void ClearWindKeywords(Material material)
        {
            if (material == null)
                return;
            for (int i = 0; i < (int)WindQuality.Count; i++)
            {
                material.DisableKeyword(WindQualityString[i]);
            }
        }

        private static void SetWindQuality(Material material, int windQuality)
        {
            Debug.Assert(WindIntValid(windQuality), "Attempting to set invalid wind quality on material " + material.name);

            if (material == null)
                return;

            if (windQuality != GetWindQualityFromKeywords(material.shaderKeywords))
            {
                ClearWindKeywords(material);
            }

            material.EnableKeyword(WindQualityString[windQuality]);
            material.SetFloat("_WindQuality", windQuality); // A legacy float used in native code to apply wind data
            if (material.HasProperty("_WINDQUALITY"))
                material.SetFloat("_WINDQUALITY", windQuality); // The actual name of the keyword enum for the shadergraph
        }

        private static int GetWindQualityFromKeywords(string[] matKws)
        {
            foreach (string kw in matKws)
            {
                if (kw.StartsWith("_WINDQUALITY_"))
                {
                    for (int i = 0; i < (int)WindQuality.Count; i++)
                    {
                        if (kw.EndsWith(WindQualityString[i]))
                            return i;
                    }
                }
            }
            return -1;
        }

        private static bool WindIntValid(int windInt)
        {
            return ((int)WindQuality.None <= windInt) && (windInt < (int)WindQuality.Count);
        }
    }
}
