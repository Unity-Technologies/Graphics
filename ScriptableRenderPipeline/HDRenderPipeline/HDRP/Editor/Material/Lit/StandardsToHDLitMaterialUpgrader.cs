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

            if (sourceShaderName == Standard_Rough)
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
            Texture metallicMap;
            if ( (srcMaterial.shader.name == Standard) || (srcMaterial.shader.name == Standard_Rough) )
            {
                hasMetallic = srcMaterial.GetTexture("_MetallicGlossMap") != null;
                if (hasMetallic) metallicMap = TextureCombiner.GetTextureSafe(srcMaterial, "_MetallicGlossMap", Color.white);
            }
            else
                metallicMap = Texture2D.blackTexture;

            // Occlusion
            bool hasOcclusion = srcMaterial.GetTexture("_OcclusionMap") != null;
            Texture occlusionMap;
            if (hasOcclusion) occlusionMap = TextureCombiner.GetTextureSafe(srcMaterial, "_OcclusionMap", Color.white);

            // Detail Mask
            bool hasDetailMask = srcMaterial.GetTexture("_DetailMask") != null;
            Texture detailMaskMap;
            if (hasDetailMask) detailMaskMap = TextureCombiner.GetTextureSafe(srcMaterial, "_DetailMask", Color.white);

            // Build the mask map
            if ( hasMetallic || hasOcclusion || hasDetailMask )
            {
                Texture2D maskMap;

                // Get the Smoothness value that will be passed to the map.
                Texture2D smoothnessSource = TextureCombiner.TextureFromColor(Color.grey);
                string smoothnessTextureChannel = "_MainTex";

                if ( (srcMaterial.shader.name != Standard_Rough) && ( srcMaterial.GetFloat("_SmoothnessTextureChannel") == 0 ) ) // Standard Rough doesn't have the smoothness source selector
                {
                    if (srcMaterial.shader.name == Standard) smoothnessTextureChannel = "_MetallicGlossMap";
                    if (srcMaterial.shader.name == Standard_Spec) smoothnessTextureChannel = "_SpecGlossMap";
                }

                smoothnessSource = (Texture2D) srcMaterial.GetTexture( smoothnessTextureChannel );
                if (smoothnessSource == null || !TextureCombiner.TextureHasAlpha(smoothnessSource))
                {
                    smoothnessSource = TextureCombiner.TextureFromColor(Color.white * srcMaterial.GetFloat("_Glossiness"));
                }

                TextureCombiner maskMapCombiner = new TextureCombiner(
                    TextureCombiner.GetTextureSafe(srcMaterial, "_MetallicGlossMap", Color.white), 4,   // Metallic
                    TextureCombiner.GetTextureSafe(srcMaterial, "_OcclusionMap", Color.white), 4,       // Occlusion
                    TextureCombiner.GetTextureSafe(srcMaterial, "_DetailMask", Color.white), 4,         // Detail Mask
                    smoothnessSource, (srcMaterial.shader.name == Standard_Rough)?-4:3                  // Smoothness
                );

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
                if (hasDetailAlbedo) detailCombiner.SetRemapping(0, 0.5f, 1f);                          // Remap albedo from [0;1] to [0.5;1] to compensate the change from "Lerp to white" to "Overlay" blend mode
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
                    break;
                case 1: // Cutout
                    dstMaterial.SetFloat("_SurfaceType", 0);
                    dstMaterial.SetFloat("_BlendMode", 0);
                    dstMaterial.SetFloat("_AlphaCutoffEnable", 1);
                    break;
                case 2: // Fade -> Alpha
                    dstMaterial.SetFloat("_SurfaceType", 1);
                    dstMaterial.SetFloat("_BlendMode", 0);
                    dstMaterial.SetFloat("_AlphaCutoffEnable", 0);
                    break;
                case 3: // Transparent -> Alpha pre-multiply
                    dstMaterial.SetFloat("_SurfaceType", 1);
                    dstMaterial.SetFloat("_BlendMode", 4);
                    dstMaterial.SetFloat("_AlphaCutoffEnable", 0);
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
            
            dstMaterial.SetColor("_EmissiveColor", hdrEmission);
            dstMaterial.SetFloat("_EmissiveIntensity", intensity);
            
            HDEditorUtils.ResetMaterialKeywords(dstMaterial);
        }
	}
}