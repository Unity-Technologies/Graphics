using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering;

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
        
        static private class Uniforms
        {
            internal static int _WINDQUALITY = Shader.PropertyToID("_WINDQUALITY");
            internal static int EFFECT_BILLBOARD = Shader.PropertyToID("EFFECT_BILLBOARD");
            internal static int EFFECT_EXTRA_TEX = Shader.PropertyToID("EFFECT_EXTRA_TEX");
            internal static int _TwoSided = Shader.PropertyToID("_TwoSided");
            internal static int _WindQuality = Shader.PropertyToID("_WindQuality");
        }
        /// <summary>
        /// Returns true if the material contains a SpeedTree Wind keyword.
        /// </summary>
        /// <param name="material">Material to check</param>
        /// <returns> true if the material has a SpeedTree wind keyword that enables Vertex Shader wind animation </returns>
        public static bool DoesMaterialHaveSpeedTreeWindKeyword(Material material)
        {
            foreach(string keyword in WindQualityString)
                if(material.IsKeywordEnabled(keyword))
                    return true;
            return false;
        }

        /// <summary>
        /// Checks the material for SpeedTree keywords to determine if the wind is enabled.
        /// </summary>
        /// <param name="material">Material to check</param>
        /// <returns> true if the material has a SpeedTree wind keyword that enables Vertex Shader wind animation and WindQuality other than None (0) </returns>
        public static bool IsWindEnabled(Material material) 
        { 
            return HasWindEnabledKeyword(material) && HasWindQualityPropertyEnabled(material); 
        }
        private static bool HasWindEnabledKeyword(Material material)
        {
            for(int i=1/*skip NONE*/; i<WindQualityString.Length; ++i)
            {
                if(material.IsKeywordEnabled(WindQualityString[i]))
                    return true;
            }
            return false;
        }
        private static bool HasWindQualityPropertyEnabled(Material material)
        {
            return material.HasProperty("_WindQuality") && material.GetFloat(Uniforms._WindQuality) > 0.0f;
        }

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
            RenameFloat("_BillboardKwToggle", "EFFECT_BILLBOARD");
            RenameKeywordToFloat("EFFECT_EXTRA_TEX", "EFFECT_EXTRA_TEX", 1, 0);
            RenameKeywordToFloat("EFFECT_SUBSURFACE", "_SubsurfaceKwToggle", 1, 0);
            RenameKeywordToFloat("EFFECT_BUMP", "_NormalMapKwToggle", 1, 0);
            RenameKeywordToFloat("EFFECT_HUE_VARIATION", "_HueVariationKwToggle", 1, 0);
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
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m == null)
                            continue;

                        float cutoff = stImporter.alphaTestRef;
                        int cullmode = isBillboard ? 2 : 0;

                        m.SetFloat(Uniforms._WINDQUALITY, wq);
                        if (isBillboard)
                        {
                            m.SetFloat(Uniforms.EFFECT_BILLBOARD, 1.0f);
                        }
                        m.SetFloat(Uniforms._TwoSided, cullmode); // Temporary; Finalizer should read from this and apply the value to a pipeline-specific cull property
                        if (m.IsKeywordEnabled("EFFECT_EXTRA_TEX"))
                            m.SetFloat(Uniforms.EFFECT_EXTRA_TEX, 1.0f);

                        if (finalizer != null)
                            finalizer(m);
                    }
                }
            }
        }

        /// <summary>
        /// Preserves wind quality and billboard settings while you are upgrading a SpeedTree 8 material from previous versions of SpeedTree 8.
        /// Wind priority order is _WindQuality float value > enabled keyword.
        /// Should work for upgrading versions within a pipeline and from standard to current pipeline.
        /// </summary>
        /// <param name="material">SpeedTree 8 material to upgrade.</param>
        public static void SpeedTree8MaterialFinalizer(Material material)
        {
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
                windQuality = material.HasProperty(Uniforms._WindQuality) ? (int)material.GetFloat(Uniforms._WindQuality) : 0;
                if (!WindIntValid(windQuality))
                {
                    windQuality = GetWindQualityFromKeywords(material.shaderKeywords);
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
            material.SetFloat(Uniforms._WindQuality, windQuality); // A legacy float used in native code to apply wind data
            if (material.HasProperty("_WINDQUALITY"))
                material.SetFloat(Uniforms._WINDQUALITY, windQuality); // The actual name of the keyword enum for the shadergraph
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
