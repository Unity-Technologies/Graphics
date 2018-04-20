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
		// ---------- HDRP ----------

		string[] scenesGUIDs = AssetDatabase.FindAssets("t:Scene", new string[]{hdrp_TestsFolder});

		// Find the different RP assets sub folders
		string[] subFolders = AssetDatabase.GetSubFolders(hdrp_PipelinesFolder);

		foreach (string subFolder in subFolders)
		{
			string subFolderName = Path.GetFileName(subFolder);

			string[] hdrp_PipelineAssetsGUIDs = AssetDatabase.FindAssets("t:RenderPipelineAsset", new string[]{subFolder});

			if (hdrp_PipelineAssetsGUIDs.Length == 0) continue;

			var hdrp_PipelineAssets = new UnityEngine.Experimental.Rendering.HDPipeline.HDRenderPipelineAsset[ hdrp_PipelineAssetsGUIDs.Length ];

			for ( int i=0 ; i<hdrp_PipelineAssets.Length ; ++i)
			{
				hdrp_PipelineAssets[i] = AssetDatabase.LoadAssetAtPath<UnityEngine.Experimental.Rendering.HDPipeline.HDRenderPipelineAsset>( AssetDatabase.GUIDToAssetPath(hdrp_PipelineAssetsGUIDs[i]));
			}

			GenerateTestSuiteFromScenes( hdrp_SuitesFolder , "HDRP_"+subFolderName, scenesGUIDs, hdrp_PipelineAssets);
		}

		// ---------- LWRP ----------
		/*
		string[] lwrp_PipelineAssetsGUIDs = AssetDatabase.FindAssets("t:RenderPipelineAsset", new string[]{lwrp_PipelinesFolder});
		foreach( string guid in lwrp_PipelineAssetsGUIDs)
		{
			RenderPipelineAsset lwrp_PipelineAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>( AssetDatabase.GUIDToAssetPath(guid));

			GenerateTestSuiteFromScenes( lwrp_SuitesFolder , "LWRP_"+lwrp_PipelineAsset.name.Replace("LWRP_Test_", ""), scenesGUIDs, lwrp_PipelineAsset, lwrp_Platform);
		}
		//*/

	}

	private static int hdrp_Platforms =
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

	private static int lwrp_Platform =
		1 << (int) RuntimePlatform.WindowsEditor |
		1 << (int) RuntimePlatform.WindowsPlayer |
		1 << (int) RuntimePlatform.OSXEditor |
		1 << (int) RuntimePlatform.OSXPlayer |
		1 << (int) RuntimePlatform.LinuxEditor |
		1 << (int) RuntimePlatform.LinuxPlayer |
		1 << (int) RuntimePlatform.WSAPlayerX86 |
		1 << (int) RuntimePlatform.WSAPlayerX64 |
		1 << (int) RuntimePlatform.PS4 |
		1 << (int) RuntimePlatform.XboxOne |
		1 << (int) RuntimePlatform.Android |
		1 << (int) RuntimePlatform.IPhonePlayer |
		1 << (int) RuntimePlatform.Switch |
		1 << (int) RuntimePlatform.PS3 |
		1 << (int) RuntimePlatform.XBOX360 |
		1 << (int) RuntimePlatform.WiiU |
		1 << (int) RuntimePlatform.WebGLPlayer ;
	
	public static readonly string srp_RootPath = Path.GetDirectoryName( AssetDatabase.GUIDToAssetPath( AssetDatabase.FindAssets("SRPMARKER")[0] ) );

	public static string GetPathInSRP( string[] foldersPath )
	{
		return foldersPath.Aggregate( srp_RootPath, Path.Combine );
	}

	public static readonly string hdrp_TestsFolder = GetPathInSRP( new string[]{ "Tests", "UTF_Tests_HDRP", "Scenes" } );
	public static readonly string hdrp_PipelinesFolder = GetPathInSRP( new string[] { "Tests", "UTF_Tests_HDRP", "Common", "RP_Assets" } );
	public static readonly string hdrp_SuitesFolder = GetPathInSRP( new string[] {"Tests", "UTF_Suites_HDRP" , "Resources" } );

	public static readonly string lwrp_TestsFolder = GetPathInSRP( new string[]{ "Tests", "UTF_Tests_LWRP", "Scenes" } );
	public static readonly string lwrp_PipelinesFolder = GetPathInSRP( new string[] { "Tests", "UTF_Tests_LWRP", "Common", "RP_Assets" } );
	public static readonly string lwrp_SuitesFolder = GetPathInSRP( new string[] {"Tests", "UTF_Suites_LWRP" , "Resources" } );

	public static void GenerateTestSuiteFromScenes( string path, string name, string[] scenesGUIDs, RenderPipelineAsset[] renderPipelineAssets = null, int platforms = -1 )
	{
		Suite suite = new Suite();
		suite.suiteName = name;
		string suitePath = Path.Combine(path, name+".asset");

		if (renderPipelineAssets != null)
		{
			if (renderPipelineAssets.Length > 0)
				suite.defaultRenderPipeline = renderPipelineAssets[0];
			
			if (renderPipelineAssets.Length > 1)
			{
				AlternateSettings[] alternateSettings = new AlternateSettings[renderPipelineAssets.Length-1];

				for (int i=1 ; i<renderPipelineAssets.Length ; ++i)
				{
					alternateSettings[i-1] = new AlternateSettings(){
						renderPipeline = renderPipelineAssets[i],
						testSettings = null
					};
				}

				suite.alternateSettings = alternateSettings;
			}
		}
		
		Dictionary<string, Group> groups = new Dictionary<string, Group>();

		foreach ( string guid in scenesGUIDs)
		{
			string scenePath = AssetDatabase.GUIDToAssetPath(guid);
			SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>( scenePath );

			string testName = Path.GetFileName( scenePath );
			string groupName = Path.GetFileName( Path.GetDirectoryName( scenePath ) );

			var regEx = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9]"); // Regex to remove non alphanumerical character from the test and group name

			testName = regEx.Replace( testName, "");
			groupName = regEx.Replace( groupName, "");

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
			test.minimumUnityVersion = 5;	// See Common.cs unityVersionList	5 = 2018.2
			test.testTypes = 2; 			// 2 = frame comparison
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