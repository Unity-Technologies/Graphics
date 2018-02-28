using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
	public class StandardsToHDLitMaterialUpgrader : MaterialUpgrader
	{
        static readonly string Standard = "Standard";
        static readonly string Standard_Spec = "Standard (Specular setup)";
        static readonly string Standard_Rough = "Standard (Roughness setup)";

		public StandardsToHDLitMaterialUpgrader(string sourceShaderName, string destShaderName, MaterialFinalizer finalizer = null)
        {
            RenameShader(sourceShaderName, destShaderName, finalizer);

            RenameTexture("_MainTex", "_BaseColorMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_Glossiness", "_Smoothness");
            RenameTexture("_BumpMap", "_NormalMap");
            RenameFloat("_BumpScale", "_NormalScale");
            RenameTexture("_ParallaxMap", "_HeightMap");
            RenameTexture("_EmissionMap", "_EmissiveColorMap");
            RenameTexture("_DetailAlbedoMap", "_DetailMap");
            RenameFloat("_UVSec", "_UVDetail");
            SetFloat("_LinkDetailsWithBase", 0);
            RenameFloat("_DetailNormalMapScale", "_DetailNormalScale");
            RenameFloat("_Cutoff", "_AlphaCutoff");
            RenameKeywordToFloat("_ALPHATEST_ON", "_AlphaCutoffEnable", 1f, 0f);


            if (sourceShaderName == Standard)
            {
                SetFloat("_MaterialID", 1f);
            }

            if (sourceShaderName == Standard_Spec)
            {
                SetFloat("_MaterialID", 4f);

                RenameColor("_SpecColor", "_SpecularColor");
                RenameTexture("_SpecGlossMap", "_SpecularColorMap");
            }
        }

        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            dstMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;

            base.Convert(srcMaterial, dstMaterial);

            // ---------- Mask Map ----------

            // Metallic
            bool hasMetallic = false;
            Texture metallicMap = Texture2D.blackTexture;
            if ( (srcMaterial.shader.name == Standard) || (srcMaterial.shader.name == Standard_Rough) )
            {
                hasMetallic = srcMaterial.GetTexture("_MetallicGlossMap") != null;
                if (hasMetallic)
                {
                    metallicMap = TextureCombiner.GetTextureSafe(srcMaterial, "_MetallicGlossMap", Color.white);
                }
                else
                {
                    float metallicValue = Mathf.Pow(srcMaterial.GetFloat("_Metallic"), 2.2f); // Convert _Metallic value from Gamma to Linear

                    dstMaterial.SetFloat("_Metallic", metallicValue);
                    metallicMap = TextureCombiner.TextureFromColor(Color.white * metallicValue);
                }
            }

            // Occlusion
            bool hasOcclusion = srcMaterial.GetTexture("_OcclusionMap") != null;
            Texture occlusionMap = Texture2D.whiteTexture;
            if (hasOcclusion) occlusionMap = TextureCombiner.GetTextureSafe(srcMaterial, "_OcclusionMap", Color.white);

            // Detail Mask
            bool hasDetailMask = srcMaterial.GetTexture("_DetailMask") != null;
            Texture detailMaskMap = Texture2D.whiteTexture;
            if (hasDetailMask) detailMaskMap = TextureCombiner.GetTextureSafe(srcMaterial, "_DetailMask", Color.white);

            // Smoothness
            bool hasSmoothness = false;
            Texture2D smoothnessMap = TextureCombiner.TextureFromColor(Color.grey);

            if (srcMaterial.shader.name == Standard_Rough)
            {
                hasSmoothness = srcMaterial.GetTexture("_SpecGlossMap") != null;

                if (hasSmoothness)
                    smoothnessMap = (Texture2D)TextureCombiner.GetTextureSafe(srcMaterial, "_SpecGlossMap", Color.grey);
            }
            else
            {
                string smoothnessTextureChannel = "_MainTex";

                if ( srcMaterial.GetFloat("_SmoothnessTextureChannel") == 0 )
                {
                    if (srcMaterial.shader.name == Standard) smoothnessTextureChannel = "_MetallicGlossMap";
                    if (srcMaterial.shader.name == Standard_Spec) smoothnessTextureChannel = "_SpecGlossMap";
                }

                smoothnessMap = (Texture2D) srcMaterial.GetTexture( smoothnessTextureChannel );
                if (smoothnessMap != null)
                {
                    hasSmoothness = true;

                    if (!TextureCombiner.TextureHasAlpha(smoothnessMap))
                    {
                        smoothnessMap = TextureCombiner.TextureFromColor(Color.white);
                    }
                }
                else
                {
                    smoothnessMap = TextureCombiner.TextureFromColor(Color.white * srcMaterial.GetFloat("_Glossiness"));
                }
            }


            // Build the mask map
            if ( hasMetallic || hasOcclusion || hasDetailMask || hasSmoothness )
            {
                Texture2D maskMap;

                TextureCombiner maskMapCombiner = new TextureCombiner(
                    metallicMap, 0,                                                     // R: Metallic from red
                    occlusionMap, 0,                                                    // G: Occlusion from red
                    detailMaskMap, 0,                                                   // B: Detail Mask from red
                    smoothnessMap, (srcMaterial.shader.name == Standard_Rough)?-4:3     // A: Smoothness Texture from inverse greyscale for roughness setup, or alpha
                );

                dstMaterial.SetFloat("_Metallic", 1f);                                  // Force _Metallic value to 1, to use the value stored in the mask map without modification

                string maskMapPath = AssetDatabase.GetAssetPath(srcMaterial);
                maskMapPath = maskMapPath.Remove(maskMapPath.Length-4) + "_MaskMap.png";
                maskMap = maskMapCombiner.Combine( maskMapPath );
                dstMaterial.SetTexture("_MaskMap", maskMap);
            }

            dstMaterial.SetFloat("_AORemapMin", 1f - srcMaterial.GetFloat("_OcclusionStrength"));

            // Specular Setup Specific
            if (srcMaterial.shader.name == Standard_Spec)
            {
                // if there is a specular map, change the specular color to white
                if (srcMaterial.GetTexture("_SpecGlossMap") != null ) dstMaterial.SetColor("_SpecularColor", Color.white);
            }

            // ---------- Height Map ----------
            bool hasHeightMap = srcMaterial.GetTexture("_ParallaxMap") != null;
            if (hasHeightMap) // Enable Parallax Occlusion Mapping
            {
                dstMaterial.SetFloat("_DisplacementMode", 2);
                dstMaterial.SetFloat("_HeightPoMAmplitude", srcMaterial.GetFloat("_Parallax") * 2f);
            }

            // ---------- Detail Map ----------
            bool hasDetailAlbedo = srcMaterial.GetTexture("_DetailAlbedoMap") != null;
            bool hasDetailNormal = srcMaterial.GetTexture("_DetailNormalMap") != null;
            if ( hasDetailAlbedo || hasDetailNormal )
            {
                Texture2D detailMap;
                TextureCombiner detailCombiner = new TextureCombiner(
                    TextureCombiner.GetTextureSafe(srcMaterial, "_DetailAlbedoMap", Color.grey), 4,     // Albedo (overlay)
                    TextureCombiner.GetTextureSafe(srcMaterial, "_DetailNormalMap", Color.grey), 1,     // Normal Y
                    TextureCombiner.midGrey, 1,                                                         // Smoothness
                    TextureCombiner.GetTextureSafe(srcMaterial, "_DetailNormalMap", Color.grey), 0      // Normal X
                );
                string detailMapPath = AssetDatabase.GetAssetPath(srcMaterial);
                detailMapPath = detailMapPath.Remove(detailMapPath.Length-4) + "_DetailMap.png";
                detailMap = detailCombiner.Combine( detailMapPath );
                dstMaterial.SetTexture("_DetailMap", detailMap);
            }


            // Blend Mode
            int previousBlendMode = srcMaterial.GetInt("_Mode");
            switch (previousBlendMode)
            {
                case 0: // Opaque
                    dstMaterial.SetFloat("_SurfaceType", 0);
                    dstMaterial.SetFloat("_BlendMode", 0);
                    dstMaterial.SetFloat("_AlphaCutoffEnable", 0);
                    dstMaterial.SetFloat("_EnableBlendModePreserveSpecularLighting", 1);
                    break;
                case 1: // Cutout
                    dstMaterial.SetFloat("_SurfaceType", 0);
                    dstMaterial.SetFloat("_BlendMode", 0);
                    dstMaterial.SetFloat("_AlphaCutoffEnable", 1);
                    dstMaterial.SetFloat("_EnableBlendModePreserveSpecularLighting", 1);
                    break;
                case 2: // Fade -> Alpha + Disable preserve specular
                    dstMaterial.SetFloat("_SurfaceType", 1);
                    dstMaterial.SetFloat("_BlendMode", 0);
                    dstMaterial.SetFloat("_AlphaCutoffEnable", 0);
                    dstMaterial.SetFloat("_EnableBlendModePreserveSpecularLighting", 0);
                    break;
                case 3: // Transparent -> Alpha
                    dstMaterial.SetFloat("_SurfaceType", 1);
                    dstMaterial.SetFloat("_BlendMode", 0);
                    dstMaterial.SetFloat("_AlphaCutoffEnable", 0);
                    dstMaterial.SetFloat("_EnableBlendModePreserveSpecularLighting", 1);
                    break;
            }

            // Emission: Convert the HDR emissive color to ldr color + intensity
            Color hdrEmission = srcMaterial.GetColor("_EmissionColor");
            float intensity = Mathf.Max(hdrEmission.r, Mathf.Max(hdrEmission.g, hdrEmission.b));

            if (intensity > 1f)
            {
                hdrEmission.r /= intensity;
                hdrEmission.g /= intensity;
                hdrEmission.b /= intensity;
            }
            else
                intensity = 1f;

            intensity = Mathf.Pow(intensity, 2.2f); // Gamma to Linear conversion

            dstMaterial.SetColor("_EmissiveColor", hdrEmission);
            dstMaterial.SetFloat("_EmissiveIntensity", intensity);

            HDEditorUtils.ResetMaterialKeywords(dstMaterial);
        }
	}
}
