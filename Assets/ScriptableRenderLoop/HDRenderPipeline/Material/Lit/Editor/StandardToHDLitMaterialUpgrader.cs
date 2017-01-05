using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.ScriptableRenderLoop
{
    class StandardToHDLitMaterialUpgrader : MaterialUpgrader
    {
        public StandardToHDLitMaterialUpgrader()
        {
            RenameShader("Standard", "HDRenderPipeline/LitLegacySupport");

            RenameTexture("_MainTex", "_BaseColorMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_Glossiness", "_Smoothness");
            RenameTexture("_BumpMap", "_NormalMap");
            RenameFloat("_BumpScale", "_NormalScale");
            RenameColor("_EmissionColor", "_EmissiveColor");
            RenameFloat("_DetailNormalMapScale", "_DetailNormalScale");

            // the HD renderloop packs detail albedo and detail normals into a single texture.
            // mapping the detail normal map, if any, to the detail map, should do the right thing if
            // there is no detail albedo.
            RenameTexture("_DetailNormalMap", "_DetailMap");

            // Metallic uses [Gamma] attribute in standard shader but not in Lit. 
            // @Seb: Should we convert?
            RenameFloat("_Metallic", "_Metallic");

            //@TODO: Seb. Why do we multiply color by intensity
            //       in shader when we can just store a color?
            // builtinData.emissiveColor * builtinData.emissiveIntensity
        }

        public override void Convert(Material srcMaterial, Material dstMaterial)
        {
            base.Convert(srcMaterial, dstMaterial);
            //@TODO: Find a good way of setting up keywords etc from properties. 
            // Code should be shared with material UI code.
        }

        [Test]
        public void UpgradeMaterial()
        {
            var newShader = Shader.Find("HDRenderPipeline/LitLegacySupport");
            var mat = new Material(Shader.Find("Standard"));
            var albedo = new Texture2D(1, 1);
            var normals = new Texture2D(1, 1);
            var baseScale = new Vector2(1, 1);
            var color = Color.red;
            mat.mainTexture = albedo;
            mat.SetTexture("_BumpMap", normals);
            mat.color = color;
            mat.SetTextureScale("_MainTex", baseScale);


            MaterialUpgrader.Upgrade(mat, new StandardToHDLitMaterialUpgrader(), MaterialUpgrader.UpgradeFlags.CleanupNonUpgradedProperties);

            Assert.AreEqual(newShader, mat.shader);
            Assert.AreEqual(albedo, mat.GetTexture("_BaseColorMap"));
            Assert.AreEqual(color, mat.GetColor("_BaseColor"));
            Assert.AreEqual(baseScale, mat.GetTextureScale("_BaseColorMap"));
            Assert.AreEqual(normals, mat.GetTexture("_NormalMap"));
        }
    }
}

