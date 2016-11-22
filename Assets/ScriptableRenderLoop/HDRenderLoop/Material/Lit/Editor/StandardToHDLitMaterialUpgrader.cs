using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.ScriptableRenderLoop
{
	class StandardToHDLitMaterialUpgrader : MaterialUpgrader
	{
		public StandardToHDLitMaterialUpgrader()
		{
			RenameShader("Standard", "HDRenderLoop/Lit");
			
			RenameTexture("_MainTex", "_BaseColorMap");
			RenameColor("_Color", "_BaseColor");
			RenameFloat("_Glossiness", "_Smoothness");
			RenameTexture("_BumpMap", "_NormalMap");
			RenameColor("_EmissionColor", "_EmissiveColor");

			//@Seb: Bumpmap scale doesn't exist in new shader
			//_BumpScale("Scale", Float) = 1.0

			// Metallic uses [Gamma] attribute in standard shader but not in Lit. 
			// @Seb: Should we convert?
			RenameFloat("_Metallic", "_Metallic");

			//@TODO: Seb. Why do we multiply color by intensity
			//       in shader when we can just store a color?
			// builtinData.emissiveColor * builtinData.emissiveIntensity
		}

		public override void Convert(Material srcMaterial, Material dstMaterial)
		{
			base.Convert (srcMaterial, dstMaterial);
			//@TODO: Find a good way of setting up keywords etc from properties. 
			// Code should be shared with material UI code.
		}	

		[Test]
		public void UpgradeMaterial()
		{
			var newShader = Shader.Find("HDRenderLoop/Lit");
			var mat = new Material (Shader.Find("Standard"));
			var albedo = new Texture2D(1, 1);
			var color = Color.red;
			mat.mainTexture = albedo;
			mat.color = color;

			MaterialUpgrader.Upgrade(mat, new StandardToHDLitMaterialUpgrader (), MaterialUpgrader.UpgradeFlags.CleanupNonUpgradedProperties);

			Assert.AreEqual (newShader, mat.shader);
			Assert.AreEqual (albedo, mat.GetTexture("_BaseColorMap"));
			Assert.AreEqual (color, mat.GetColor("_BaseColor"));
		}
	}
}

