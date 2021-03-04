using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
	class HDSpeedTree8MaterialUpgrader : SpeedTree8MaterialUpgrader
	{
		public HDSpeedTree8MaterialUpgrader(string sourceShaderName, string destShaderName)
			: base(sourceShaderName, destShaderName, HDSpeedTree8MaterialFinalizer)
		{
        }
        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            int oldwq = (int)dstMaterial.GetFloat("_WindQuality");
            int oldisbillboard = (int)dstMaterial.GetFloat("_BillboardKwToggle");
            int oldalphacutoff = (int)dstMaterial.GetFloat("_Cutoff");
            int oldcullmode = (int)dstMaterial.GetFloat("_TwoSided");
            base.Convert(srcMaterial, dstMaterial);
            int wq = (int)dstMaterial.GetFloat("_WINDQUALITY");
            int isbillboard = (int)dstMaterial.GetFloat("EFFECT_BILLBOARD");
            int alphacutoff = (int)dstMaterial.GetFloat("_AlphaClipThreshold");
            int cullmode = (int)dstMaterial.GetFloat("_CullMode");
            oldwq = (int)dstMaterial.GetFloat("_WindQuality");
            oldisbillboard = (int)dstMaterial.GetFloat("_BillboardKwToggle");
            oldalphacutoff = (int)dstMaterial.GetFloat("_Cutoff");
            oldcullmode = (int)dstMaterial.GetFloat("_TwoSided");
            EditorUtility.SetDirty(dstMaterial);
        }

        public static void HDSpeedTree8MaterialFinalizer(Material mat)
        {
            int wq = (int)mat.GetFloat("_WINDQUALITY");
            int isbillboard = (int)mat.GetFloat("EFFECT_BILLBOARD");
            int alphacutoff = (int)mat.GetFloat("_AlphaClipThreshold");
            int cullmode = (int)mat.GetFloat("_CullMode");

            int oldwq = (int)mat.GetFloat("_WindQuality");
            int oldisbillboard = (int)mat.GetFloat("_BillboardKwToggle");
            int oldalphacutoff = (int)mat.GetFloat("_Cutoff");
            int oldcullmode = (int)mat.GetFloat("_TwoSided");
            HDShaderUtils.ResetMaterialKeywords(mat);
            wq = (int)mat.GetFloat("_WINDQUALITY");
            isbillboard = (int)mat.GetFloat("EFFECT_BILLBOARD");
            alphacutoff = (int)mat.GetFloat("_AlphaClipThreshold");
            cullmode = (int)mat.GetFloat("_CullMode");
            mat.EnableKeyword(WindQualityString[wq]);
            if (isbillboard > 0)
                mat.EnableKeyword("EFFECT_BILLBOARD");
        }
	}
}
