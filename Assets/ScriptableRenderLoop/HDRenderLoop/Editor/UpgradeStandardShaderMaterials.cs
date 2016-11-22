using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using NUnit.Framework;

namespace UnityEditor.Experimental.ScriptableRenderLoop
{
	public class UpgradeStandardShaderMaterials
	{
		static List<MaterialUpgrader> GetHDUpgraders()
		{
			var upgraders = new List<MaterialUpgrader> ();
			upgraders.Add(new StandardToHDLitMaterialUpgrader ());
			return upgraders;
		}

		[MenuItem("HDRenderLoop/Upgrade Materials - Project")]
		static void UpgradeMaterialsProject()
		{
			MaterialUpgrader.UpgradeProjectFolder (GetHDUpgraders(), "Upgrade to HD Material");
		}
			
		[MenuItem("HDRenderLoop/Upgrade Materials - Selection")]
		static void UpgradeMaterialsSelection()
		{
			MaterialUpgrader.UpgradeSelection (GetHDUpgraders(), "Upgrade to HD Material");
		}
	}
}