using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.Rendering
{
	/// <summary>
	/// Base material Upgrader for SpeedTree8. Overload in render pipelines as needed.
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

        public SpeedTree8MaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer = null)
        {
            RenameShader(sourceShaderName, destShaderName, finalizer);
            RenameFloat("_BillboardKwToggle", "EFFECT_BILLBOARD");
            RenameFloat("_WindQuality", "_WINDQUALITY");
            RenameFloat("_Cutoff", "_AlphaClipThreshold");
            // Currently not implemented in SG RenameFloat("_SubsurfaceKwToggle", "EFFECT_SUBSURFACE");
            RenameFloat("_TwoSided", "_CullMode"); // Currently only used in HD. Update this once URP per-material cullmode is enabled via shadergraph. 
        }

        public virtual void SetupKeywordsAndProperties(Material material, int windQuality = -1)
        {
            if (material != null)
            {
                if (material.shader.name.Equals(targetShaderName) || targetShaderName.Equals("*"))
                {
                    windQuality = GetWindQuality(material, windQuality);
                    SetWindQuality(material, windQuality);
                    if (material.name.Contains("Billboard"))
                        SetBillboard(material);
                }
            }
        }

        public static int GetWindQuality(Material material, int windQuality = -1)
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
            for (int i = 0; i < (int)WindQuality.Count; i++)
            {
                material.DisableKeyword(WindQualityString[i]);
            }
        }

        public static void SetWindQuality(Material material, int windQuality)
        {
            Debug.Assert(WindIntValid(windQuality), "Attempting to set invalid wind quality on material " + material.name);

            if (windQuality != GetWindQualityFromKeywords(material.shaderKeywords))
            {
                ClearWindKeywords(material);
            }

            material.EnableKeyword(WindQualityString[windQuality]);
            material.SetFloat("_WindQuality", windQuality); // A legacy float used in native code to apply wind data
            material.SetFloat("_WINDQUALITY", windQuality); // The actual name of the keyword enum for the shadergraph
        }

        public static bool SetBillboard(Material material)
        {
            // Billboard priority:
            // enabled keyword > EFFECT_BILLBOARD float value
            bool billboardEnabled = material.IsKeywordEnabled("EFFECT_BILLBOARD")
                || material.GetFloat("EFFECT_BILLBOARD") != 0;

            if (billboardEnabled)
            {
                material.EnableKeyword("EFFECT_BILLBOARD");
                material.SetFloat("EFFECT_BILLBOARD", 1);
            }
            else
            {
                material.DisableKeyword("EFFECT_BILLBOARD");
                material.SetFloat("EFFECT_BILLBOARD", 0);
            }

            return billboardEnabled;
        }

        internal static int GetWindQualityFromKeywords(string[] matKws)
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
