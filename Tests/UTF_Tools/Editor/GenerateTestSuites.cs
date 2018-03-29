using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;
using GraphicsTestFramework;
using UnityEngine.Experimental.Rendering;

public class GenerateTestSuites
{
	[MenuItem("UTF/Generate SRP TestSuites")]
	private static void GenerateSRPTestSuites()
	{
		string[] scenesGUIDs = AssetDatabase.FindAssets("t:Scene", new string[]{hdrp_TestsFolder});

		foreach(string guid in scenesGUIDs)
		{
			Debug.Log(AssetDatabase.GUIDToAssetPath(guid));
		}

		// HDRP
		string hdrp_SuitesFolder = GetPathInTest(hdrp_TestSuitesFolder);
		string hdrp_PipelinesFolder = GetPathInTest(hdrp_CommonPipelineAssetsFolder);
		string[] hdrp_PipelineAssetsGUIDs = AssetDatabase.FindAssets("t:RenderPipelineAsset", new string[]{hdrp_PipelinesFolder});
		foreach( string guid in hdrp_PipelineAssetsGUIDs)
		{
			RenderPipelineAsset hdrp_PipelineAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>( AssetDatabase.GUIDToAssetPath(guid));

			GenerateTestSuiteFromScenes( hdrp_SuitesFolder , "HDRP_"+hdrp_PipelineAsset.name.Replace("HDRP_Test_", ""), scenesGUIDs, hdrp_PipelineAsset);
		}

		//GenerateTestSuiteFromScenes("Assets/", "HDRP_Tests", scenesGUIDs, null, HDRPPlatforms);
	}

	private static int HDRPPlatforms =
		1 << (int) RuntimePlatform.WindowsEditor |
		1 << (int) RuntimePlatform.WindowsPlayer |
		1 << (int) RuntimePlatform.OSXEditor |
		1 << (int) RuntimePlatform.OSXPlayer |
		1 << (int) RuntimePlatform.LinuxEditor |
		1 << (int) RuntimePlatform.LinuxPlayer |
		1 << (int) RuntimePlatform.WSAPlayerX86 |
		1 << (int) RuntimePlatform.WSAPlayerX64 |
		1 << (int) RuntimePlatform.PS4 |
		1 << (int) RuntimePlatform.XboxOne ;

	//Previously was : Directory.GetParent(Directory.GetFiles(Application.dataPath, "SRPMARKER", SearchOption.AllDirectories).First()).ToString();
	public static readonly string srp_RootPath = Path.GetDirectoryName( AssetDatabase.GUIDToAssetPath( AssetDatabase.FindAssets("SRPMARKER")[0] ) );

	public static readonly string[] hdrp_TestsFolderHierarchy = { "Tests", "UTF_Tests_HDRP", "Scenes" };
	public static readonly string[] hdrp_CommonPipelineAssetsFolder = { "Tests", "UTF_Tests_HDRP", "Common", "RP_Assets" };

	public static readonly string[] hdrp_TestSuitesFolder = {"Tests", "UTF_Suites_HDRP" , "Resources" };

	public static readonly string hdrp_TestsFolder = hdrp_TestsFolderHierarchy.Aggregate(srp_RootPath, Path.Combine);

	public static string GetPathInTest( string[] foldersPath )
	{
		return foldersPath.Aggregate( srp_RootPath, Path.Combine );
	}

	public static void GenerateTestSuiteFromScenes( string path, string name, string[] scenesGUIDs, RenderPipelineAsset renderPipelineAsset = null, int platforms = -1 )
	{
		Suite suite = new Suite();
		suite.suiteName = name;
		string suitePath = Path.Combine(path, name+".asset");
		if (renderPipelineAsset != null) suite.defaultRenderPipeline = renderPipelineAsset;
		
		Dictionary<string, Group> groups = new Dictionary<string, Group>();

		foreach ( string guid in scenesGUIDs)
		{
			string scenePath = AssetDatabase.GUIDToAssetPath(guid);
			SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>( scenePath );

			string testName = Path.GetFileName( scenePath );
			string groupName = Path.GetFileName( Path.GetDirectoryName( scenePath ) );
			groupName = groupName.Replace("xxx", "x");

			if (!groups.ContainsKey(groupName))
			{
				groups.Add(groupName, new Group());
				groups[groupName].tests = new List<Test>();
				groups[groupName].groupName = groupName;
			}

			Test test = new Test();
			test.name = testName;
			test.platforms = platforms;
			test.scene = scene;
			test.scenePath = scenePath;
			test.minimumUnityVersion = 4;
			test.testTypes = 2; // 2 = frame comparison
			test.run = true;

			groups[groupName].tests.Add(test);
		}

		suite.groups = groups.Values.ToList();

		Suite oldsuite = AssetDatabase.LoadAssetAtPath<Suite>(suitePath);

		if (oldsuite != null)
		{
			EditorUtility.CopySerialized(suite, oldsuite);
			AssetDatabase.Refresh();
		}
		else
		{
			AssetDatabase.CreateAsset(suite, suitePath);
		}
	}
}