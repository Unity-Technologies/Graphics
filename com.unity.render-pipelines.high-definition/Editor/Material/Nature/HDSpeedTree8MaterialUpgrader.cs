using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// SpeedTree8 material upgrader for HDRP.
    /// </summary>
	class HDSpeedTree8MaterialUpgrader : SpeedTree8MaterialUpgrader
    {
        private struct HDSpeedTree8PropertiesToRestore
        {
            public int windQuality;
            public bool isBillboard;
            public float cullMode;
        }
        private static HDSpeedTree8PropertiesToRestore propsToRestore;
        /// <summary>
        /// Creates a SpeedTree8 material upgrader for HDRP.
        /// </summary>
        /// <param name="sourceShaderName">Original shader name.</param>
        /// <param name="destShaderName">Upgrade shader name.</param>
        public HDSpeedTree8MaterialUpgrader(string sourceShaderName, string destShaderName)
			: base(sourceShaderName, destShaderName, HDSpeedTree8MaterialFinalizer)
		{
        }

        private static void HDSpeedTree8MaterialFinalizer(Material mat)
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
        /// Saves SpeedTree8-specific material properties and keywords that were set during import and should not be reset.
        /// </summary>
        /// <param name="mat">SpeedTree8 material.</param>
        public static void SaveHDSpeedTree8Setup(Material mat)
        {
            propsToRestore.windQuality = (int)mat.GetFloat("_WINDQUALITY");
            propsToRestore.isBillboard = mat.IsKeywordEnabled("EFFECT_BILLBOARD");
            propsToRestore.cullMode = mat.GetFloat("_CullMode");
        }
        /// <summary>
        /// Restores SpeedTree8-specific material properties and keywords that were set during import and should not be reset.
        /// </summary>
        /// <param name="mat">SpeedTree8 material.</param>
        public static void RestoreHDSpeedTree8Setup(Material mat)
        {
            int wq = propsToRestore.windQuality;
            mat.SetFloat("_WINDQUALITY", wq);
            mat.EnableKeyword(WindQualityString[wq]);

            if (propsToRestore.isBillboard)
            {
                mat.EnableKeyword("EFFECT_BILLBOARD");
                if (mat.HasProperty("EFFECT_BILLBOARD"))
                    mat.SetFloat("EFFECT_BILLBOARD", 1.0f);
            }

            mat.SetFloat("_CullMode", propsToRestore.cullMode);
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
