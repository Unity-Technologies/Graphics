using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// SpeedTree8 material upgrader for HDRP.
    /// </summary>
	class HDSpeedTree8MaterialUpgrader : SpeedTree8MaterialUpgrader
    {
        /// <summary>
        /// Creates a SpeedTree8 material upgrader for HDRP.
        /// </summary>
        /// <param name="sourceShaderName">Original shader name.</param>
        /// <param name="destShaderName">Upgrade shader name.</param>
        public HDSpeedTree8MaterialUpgrader(string sourceShaderName, string destShaderName)
			: base(sourceShaderName, destShaderName, HDSpeedTree8MaterialFinalizer)
		{
        }

        public static void HDSpeedTree8MaterialFinalizer(Material mat)
        {
            SetHDSpeedTree8Defaults(mat);
            HDShaderUtils.ResetMaterialKeywords(mat);
        }
        /// <summary>
        /// Checks if a given material is an HD SpeedTree8 material.
        /// </summary>
        /// <param name="mat">Material to check.</param>
        /// <returns></returns>
        public static bool IsHDSpeedTree8Material(Material mat)
        {
            return (mat.shader.name == "HDRP/Nature/SpeedTree8");
        }

        /// <summary>
        /// Restores SpeedTree8-specific material properties and keywords that were set during import and should not be reset.
        /// </summary>
        /// <param name="mat">SpeedTree8 material.</param>
        public static void RestoreHDSpeedTree8Keywords(Material mat)
        {
            int wq = (int)mat.GetFloat("_WINDQUALITY");
            mat.EnableKeyword(WindQualityString[wq]);

            if (mat.name.Contains("Billboard")) // Hacky but it'll hold until newer versions of shadergraph with keyword toggle support
            {
                mat.EnableKeyword("EFFECT_BILLBOARD");
            }
        }

        private static void SetHDSpeedTree8Defaults(Material mat)
        {
            if (mat.IsKeywordEnabled("EFFECT_BILLBOARD"))
            {
                mat.SetFloat("_DoubleSidedEnable", 0.0f);
                mat.SetFloat("_DoubleSidedNormalMode", (int)DoubleSidedNormalMode.None);
            }
            else
            {
                mat.SetFloat("_DoubleSidedEnable", 1.0f);
                mat.SetFloat("_DoubleSidedNormalMode", (int)DoubleSidedNormalMode.Flip);
            }
        }
	}
}
