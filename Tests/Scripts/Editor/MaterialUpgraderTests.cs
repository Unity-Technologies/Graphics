using NUnit.Framework;
using UnityEditor.Experimental.Rendering;
using UnityEditor.Experimental.Rendering.HDPipeline;
using UnityEngine;

namespace ScriptableRenderPipeline.Tests.Editor
{
    public class MaterialUpgraderTests
    {
        [Test]
        public void UpgradeStandardSpecularToHDLitMaterial()
        {
            var newShader = Shader.Find("HDRenderPipeline/Lit");
            var mat = new Material(Shader.Find("Standard (Specular setup)"));
            var albedo = new Texture2D(1, 1);
            var normals = new Texture2D(1, 1);
            var baseScale = new Vector2(1, 1);
            var color = Color.red;
            mat.mainTexture = albedo;
            mat.SetTexture("_BumpMap", normals);
            mat.color = color;
            mat.SetTextureScale("_MainTex", baseScale);

            var upgrader = new StandardSpecularToHDLitMaterialUpgrader();
            MaterialUpgrader.Upgrade(mat, upgrader, MaterialUpgrader.UpgradeFlags.CleanupNonUpgradedProperties);

            Assert.AreEqual(newShader, mat.shader);
            Assert.AreEqual(albedo, mat.GetTexture("_BaseColorMap"));
            Assert.AreEqual(color, mat.GetColor("_BaseColor"));
            Assert.AreEqual(baseScale, mat.GetTextureScale("_BaseColorMap"));
            Assert.AreEqual(normals, mat.GetTexture("_NormalMap"));
            Assert.IsTrue(mat.IsKeywordEnabled("_NORMALMAP"));
        }

        [Test]
        public void UpgradeStandardToHDLitMaterialUpgrader()
        {
            var newShader = Shader.Find("HDRenderPipeline/Lit");
            var mat = new Material(Shader.Find("Standard"));
            var albedo = new Texture2D(1, 1);
            var normals = new Texture2D(1, 1);
            var baseScale = new Vector2(1, 1);
            var color = Color.red;
            mat.mainTexture = albedo;
            mat.SetTexture("_BumpMap", normals);
            mat.color = color;
            mat.SetTextureScale("_MainTex", baseScale);

            var upgrader = new StandardToHDLitMaterialUpgrader();
            MaterialUpgrader.Upgrade(mat, upgrader, MaterialUpgrader.UpgradeFlags.CleanupNonUpgradedProperties);

            Assert.AreEqual(newShader, mat.shader);
            Assert.AreEqual(albedo, mat.GetTexture("_BaseColorMap"));
            Assert.AreEqual(color, mat.GetColor("_BaseColor"));
            Assert.AreEqual(baseScale, mat.GetTextureScale("_BaseColorMap"));
            Assert.AreEqual(normals, mat.GetTexture("_NormalMap"));
            Assert.IsTrue(mat.IsKeywordEnabled("_NORMALMAP"));
        }

    }
}
