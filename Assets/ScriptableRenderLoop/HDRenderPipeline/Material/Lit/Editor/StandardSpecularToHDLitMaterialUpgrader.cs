using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class StandardSpecularToHDLitMaterialUpgrader : MaterialUpgrader
    {
        public StandardSpecularToHDLitMaterialUpgrader()
        {
            RenameShader("Standard (Specular setup)", "HDRenderPipeline/LitLegacySupport");

            RenameTexture("_MainTex", "_BaseColorMap");
            RenameColor("_Color", "_BaseColor");
            RenameFloat("_Glossiness", "_Smoothness");
            RenameTexture("_BumpMap", "_NormalMap");
            RenameFloat("_BumpScale", "_NormalScale");
            RenameColor("_EmissionColor", "_EmissiveColor");
            RenameFloat("_DetailNormalMapScale", "_DetailNormalScale");
            RenameTexture("_DetailNormalMap", "_DetailMap");

            // Anything reasonable that can be done here?
            //RenameFloat("_SpecColor", ...);

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
            var mat = new Material(Shader.Find("Standard (Specular setup)"));
            var albedo = new Texture2D(1, 1);
            var normals = new Texture2D(1, 1);
            var baseScale = new Vector2(1, 1);
            var color = Color.red;
            mat.mainTexture = albedo;
            mat.SetTexture("_BumpMap", normals);
            mat.color = color;
            mat.SetTextureScale("_MainTex", baseScale);

            MaterialUpgrader.Upgrade(mat, new StandardSpecularToHDLitMaterialUpgrader(), MaterialUpgrader.UpgradeFlags.CleanupNonUpgradedProperties);

            Assert.AreEqual(newShader, mat.shader);
            Assert.AreEqual(albedo, mat.GetTexture("_BaseColorMap"));
            Assert.AreEqual(color, mat.GetColor("_BaseColor"));
            Assert.AreEqual(baseScale, mat.GetTextureScale("_BaseColorMap"));
            Assert.AreEqual(normals, mat.GetTexture("_NormalMap"));
        }
    }
}

