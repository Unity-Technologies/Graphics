using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[assembly: InternalsVisibleTo("LightingShaderGraphGUI")]
[assembly: InternalsVisibleTo("MaterialPostProcessor")]
namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Configures keywords and internal properties for HD's SpeedTree8 shadergraph.
    /// </summary>
    public class SpeedTree8MaterialUpgrader : MaterialUpgrader
    {
        // _DoubleSidedConstants value should match comments in MaterialUtilities.hlsl
        static Vector4 kDoubleSidedFlip = new Vector4(-1, -1, -1, 0);
        static Vector4 kDoubleSidedNone = new Vector4(1, 1, 1, 0);
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
            "_WINDQUALITY_FAST",
            "_WINDQUALITY_FASTEST",
            "_WINDQUALITY_BETTER",
            "_WINDQUALITY_BEST",
            "_WINDQUALITY_PALM"
        };
        /// <summary>
        /// Upgrades materials using the built-in SpeedTree8 shader to use HDRP's SpeedTree8 ShaderGraph.
        /// </summary>
        /// <param name="sourceShaderName">Name of the built-in SpeedTree8 shader.</param>
        /// <param name="destShaderName">Name of the HD SpeedTree8 shader.</param>
        public SpeedTree8MaterialUpgrader(string sourceShaderName, string destShaderName)
        {
            RenameShader(sourceShaderName, destShaderName, SpeedTree8MaterialFinalizer);
        }

        private static void SpeedTree8MaterialFinalizer(Material material)
        {
            // Run after upgrading from built-in SpeedTree8 shader.
            if (material.name.Contains("Billboard"))
            {
                material.SetFloat("EFFECT_BILLBOARD", 1);
                material.EnableKeyword("EFFECT_BILLBOARD");
            }
            HDShaderUtils.ResetMaterialKeywords(material);
        }

        internal static void SetST8MaterialKeywords(Material material, int windInt = -1)
        {
            // When a speedtree8 is imported or a speedtree8 material is upgraded, internal floats are set correctly
            // and can be read to reapply keywords after resetting them via HDShaderUtils.ResetMaterialKeywords.
            // However, when a material inspector is rendered for the first time, it does not have the correct
            // property values but does have the correct keywords.
            // In both cases, a keyword is only ever set if it is correct.
            // So in order to sync keywords with exposed properties, we check first if a relevant keyword is set.
            // Only if it is not set do we check the property to get the value.
            // For wind in particular, we only ever pass in non-negative windInt when we intend to set it to that value.
            // So in summary:
            // Priority order of property-controlled keywords that require syncing:
            // EFFECT_BILLBOARD: enabled keyword > property value
            // _WINDQUALITY*: valid windInt > enabled keyword > property value

            if (material != null)
            {
                if (material.shader.name.Equals("HDRP/Nature/SpeedTree8"))
                {
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("ENABLE_WIND");
                    // Assumes all ST8 materials are double-sided.
                    material.EnableKeyword("_DOUBLESIDED_ON");

                    if (!WindIntValid(windInt))
                    {
                        windInt = GetWindQualityFromKeywords(material.shaderKeywords);
                        if (!WindIntValid(windInt))
                            windInt = material.HasFloat("_WindQuality") ? (int)material.GetFloat("_WindQuality") : 0;
                        if (!WindIntValid(windInt))
                            windInt = 0;
                    }

                    material.EnableKeyword(WindQualityString[windInt]);
                    material.SetFloat("_WindQuality", windInt); // A legacy float used in native code to apply wind data
                    material.SetFloat("_WINDQUALITY", windInt); // The actual name of the keyword enum for the shadergraph

                    // Shadergraph sets cull mode from _CullMode, not _TwoSided (which is what ST uses)
                    if (material.name.Contains("Billboard"))
                    {
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
                        // Billboards have double sided mode = none.
                        material.SetVector("_DoubleSidedConstants", kDoubleSidedNone);
                        material.SetFloat("_CullMode", 2); // Cull billboard backfaces to prevent Z-fighting
                    }
                    else
                    {
                        // Non-billboards have double sided mode = flipped.
                        // _DoubleSidedConstants value should match comments in MaterialUtilities.hlsl
                        material.SetVector("_DoubleSidedConstants", kDoubleSidedFlip);
                        material.SetFloat("_CullMode", 0); // Non-billboards don't need backfaces culled.
                    }
                }
            }
        }

        private static int GetWindQualityFromKeywords(string[] matKws)
        {
            foreach (string kw in matKws)
            {
                if (kw.StartsWith("_WINDQUALITY_"))
                {
                    for (int i = 0; i < (int)WindQuality.Count; i++)
                    {
                        if (kw.Equals(WindQualityString[i]))
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
