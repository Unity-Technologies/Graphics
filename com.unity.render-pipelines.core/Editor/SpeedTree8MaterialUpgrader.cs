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
        public string targetShaderName = "";
        public enum WindQuality
        {
            None = 0,
            Fastest,
            Fast,
            Better,
            Best,
            Palm,
            Count
        }

        public static string[] WindQualityString =
        {
            "_WINDQUALITY_NONE",
            "_WINDQUALITY_FASTEST",
            "_WINDQUALITY_FAST",
            "_WINDQUALITY_BETTER",
            "_WINDQUALITY_BEST",
            "_WINDQUALITY_PALM"
        };
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
        /// <summary>
        /// Set default property values for SpeedTree8 properties in common between HD and Universal and not initialized by the Lit subtarget.
        /// </summary>
        /// <param name="mat">SpeedTree8 material.</param>
        public static void SetSpeedTree8MaterialDefaults(Material mat)
        {
            if (mat == null)
                return;
            mat.SetFloat("_AlphaClipThreshold", 0.33f); // ST8 assets don't have a cutoff set by default, but the ST7 default cutoff is 0.33f.
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
        /// <summary>
        /// Overwrite wind quality, including associated properties and keywords, on a SpeedTree8 material.
        /// </summary>
        /// <param name="material">SpeedTree8 material to update.</param>
        /// <param name="windQuality">Wind quality to set.</param>
        public static void SetWindQuality(Material material, int windQuality)
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
