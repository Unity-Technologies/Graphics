using System.Linq;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools.Graphics;
using UnityEditor.SceneManagement;
using UnityEditor;

public class Editmode_ParametricReflectionProbeTests
{
	private readonly string sceneOutputPath = "Assets/EditModeTestAssets/Lighting_ReflectionProbeBaking/ReflectionProbeBake";
	private readonly string sceneFileName = "Assets/EditModeTestAssets/Lighting_ReflectionProbeBaking/ReflectionProbeBake.unity";
	private readonly string[] foldersToLookIn = { "Assets/EditModeTestAssets/Lighting_ReflectionProbeBaking/settings" };
    
	[TestCaseSource("FixtureArgs")]
	public void ReflectionProbeTest(string name)
	{
		EditorSceneManager.OpenScene(sceneFileName, OpenSceneMode.Single);

		// Bake with a lighting settings asset.
		string[] settingsAssets = AssetDatabase.FindAssets(name + " t:lightingsettings", foldersToLookIn);
		Debug.Log("Found " + settingsAssets.Length + " matching lighting settings assets in " + foldersToLookIn[0]);
		Assert.That(settingsAssets.Length, Is.EqualTo(1));
		string lsaPath = AssetDatabase.GUIDToAssetPath(settingsAssets[0]);
		Debug.Log("Loading " + lsaPath);
		LightingSettings lightingSettings = (LightingSettings)AssetDatabase.LoadAssetAtPath(lsaPath, typeof(LightingSettings));
		Lightmapping.lightingSettings = lightingSettings;
		string fileName = System.IO.Path.GetFileNameWithoutExtension(lsaPath);
		Assert.That(fileName, Is.EqualTo(name));
		Lightmapping.Clear();
        Lightmapping.ClearDiskCache();
        Debug.Log("Baking " + fileName);
		bool result = Lightmapping.Bake();
		Assert.That(result, Is.True);
		
		// Get Test settings.
		var graphicsTestSettingsCustom = Object.FindObjectOfType<GraphicsTestSettingsCustom>();
		Assert.That(graphicsTestSettingsCustom, !Is.EqualTo(null), "Couldn't find GraphicsTestSettingsCustom");
		
		// Load reference image.
		var referenceImagePath = System.IO.Path.Combine("Assets/ReferenceImages", string.Format("{0}/{1}/{2}/{3}/{4}",
			UseGraphicsTestCasesAttribute.ColorSpace,
			UseGraphicsTestCasesAttribute.Platform,
			UseGraphicsTestCasesAttribute.GraphicsDevice,
			UseGraphicsTestCasesAttribute.LoadedXRDevice,
			"ReflectionProbeTest(" + name + ").png"));
		Debug.Log("referenceImagePath " + referenceImagePath);
		var referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(referenceImagePath);
		
		// Compare screenshots.
		GraphicsTestCase testCase = new GraphicsTestCase(name, referenceImage);
		var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x=>x.GetComponent<Camera>());
		ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), graphicsTestSettingsCustom.ImageComparisonSettings);
		UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
	}
	
	static string[] FixtureArgs = {
//		"Auto-Progressive",
//        "Auto-nonProgressive",
        "nonAuto-Progressive",
        "nonAuto-nonProgressive",
	};

    [OneTimeTearDown]
    public void Cleanup()
    {
        // Remove all lightmaps.
        System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(sceneOutputPath);
        foreach (System.IO.FileInfo file in di.GetFiles())
            file.Delete();
        foreach (System.IO.DirectoryInfo dir in di.GetDirectories())
            dir.Delete(true);
        System.IO.Directory.Delete(sceneOutputPath);
    }
}
