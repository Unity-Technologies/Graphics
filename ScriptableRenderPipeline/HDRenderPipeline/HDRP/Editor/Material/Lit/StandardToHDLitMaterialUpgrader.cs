using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class StandardToHDLitMaterialUpgrader : MaterialUpgrader
    {
        public StandardToHDLitMaterialUpgrader() : this("Standard", "HDRenderPipeline/Lit", LitGUI.SetupMaterialKeywordsAndPass) {}

        public StandardToHDLitMaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer)
        {
            RenameShader(sourceShaderName, destShaderName, finalizer);

            RenameTexture("_MainTex", "_BaseColorMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_Glossiness", "_Smoothness");
            RenameTexture("_BumpMap", "_NormalMap");
            RenameFloat("_BumpScale", "_NormalScale");
            RenameColor("_EmissionColor", "_EmissiveColor");
            RenameFloat("_DetailNormalMapScale", "_DetailNormalScale");
            RenameFloat("_Cutoff", "_AlphaCutoff");
            RenameKeywordToFloat("_ALPHATEST_ON", "_AlphaCutoffEnable", 1f, 0f);

            // the HD renderloop packs detail albedo and detail normals into a single texture.
            // mapping the detail normal map, if any, to the detail map, should do the right thing if
            // there is no detail albedo.
            RenameTexture("_DetailAlbedoMap", "_DetailMap");

            // Moved to convert function

            // Metallic uses [Gamma] attribute in standard shader but not in Lit.
            // @Seb: Should we convert?
            RenameFloat("_Metallic", "_Metallic");

            //@TODO: Seb. Why do we multiply color by intensity
            //       in shader when we can just store a color?
            // builtinData.emissiveColor * builtinData.emissiveIntensity
        }

        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            dstMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;

            base.Convert(srcMaterial, dstMaterial);
            //@TODO: Find a good way of setting up keywords etc from properties.
            // Code should be shared with material UI code.

            //*
            Texture2D maskMap;
            TextureCombiner maskMapCombiner = new TextureCombiner(
                TextureCombiner.GetTextureSafe(srcMaterial, "_MetallicGlossMap", 1), 4,
                TextureCombiner.GetTextureSafe(srcMaterial, "_OcclusionMap", 0), 4,
                TextureCombiner.GetTextureSafe(srcMaterial, "_DetailMask", 0), 4,
                TextureCombiner.GetTextureSafe(srcMaterial, (srcMaterial.GetFloat("_SmoothnessTextureChannel") == 0)?"_MetallicGlossMap": "_MainTex", 2), 3
            );
            string maskMapPath = AssetDatabase.GetAssetPath(srcMaterial).Replace(".mat", "_MaskMap.exr");
            maskMap = maskMapCombiner.Combine( maskMapPath );
            dstMaterial.SetTexture("_MaskMap", maskMap);
            // */

            //*
            Texture2D detailMap;
            TextureCombiner detailCombiner = new TextureCombiner(
                TextureCombiner.GetTextureSafe(srcMaterial, "_DetailAlbedoMap", 2), 4,
                TextureCombiner.GetTextureSafe(srcMaterial, "_DetailNormalMap", 2), 1,
                TextureCombiner.midGrey, 1,
                TextureCombiner.GetTextureSafe(srcMaterial, "_DetailNormalMap", 2), 0
            );
            string detailMapPath = AssetDatabase.GetAssetPath(srcMaterial).Replace(".mat", "_DetailMap.exr");
            detailMap = detailCombiner.Combine( detailMapPath );
            Debug.Log("Coucou");
            dstMaterial.SetTexture("_DetailMap", detailMap);
            //*/

            HDEditorUtils.ResetMaterialKeywords(dstMaterial);
        }

    }
}
