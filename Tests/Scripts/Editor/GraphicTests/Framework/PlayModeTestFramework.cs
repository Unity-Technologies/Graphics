using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PlayModeTestFramework : EditorWindow
{
    static string scenesRootPath = "/Tests/GraphicsTests/RenderPipeline/HDRenderPipeline/Scenes";

    enum Platforms { PC, PS4};
    Platforms platform = Platforms.PC;

    bool developmentBuild = false;
    bool buildAndRun = true;

    [MenuItem("Internal/GraphicTest Tools/PlayMode Test Window")]
    public static void OpenPlayModeTestWindow()
    {
        PlayModeTestFramework window = GetWindow<PlayModeTestFramework>();

        // find all the scenes
        window.allPaths = System.IO.Directory.GetFiles(Application.dataPath+scenesRootPath, "*.unity", System.IO.SearchOption.AllDirectories);

        for (int i = 0; i < window.allPaths.Length; ++i)
        {
            window.allPaths[i] = "Assets" + window.allPaths[i].Replace(Application.dataPath, "");
        }

        //Debug.Log("Scenes found : " + window.allPaths.Length);

        for (int i = 0; i < window.allPaths.Length; ++i)
        {
            Debug.Log(window.allPaths[i]);
        }
    }

    string[] allPaths;

    private void OnGUI()
    {
        scenesRootPath = EditorGUILayout.TextField(scenesRootPath);

        for (int i = 0; i < allPaths.Length; ++i)
        {
            GUILayout.Label(allPaths[i]);
        }

        platform = (Platforms)EditorGUILayout.EnumPopup("Target Platform ", platform);

        developmentBuild = EditorGUILayout.Toggle("Development Build", developmentBuild);

        buildAndRun = EditorGUILayout.Toggle("Build and Run", buildAndRun);

        if (GUILayout.Button("Build Player"))
        {
            EditorBuildSettingsScene[] prevScenes = EditorBuildSettings.scenes;

            EditorBuildSettingsScene[] testScenes = new EditorBuildSettingsScene[allPaths.Length+1];

            testScenes[0] = new EditorBuildSettingsScene(Application.dataPath+ "/Tests/GraphicsTests/RenderPipeline/HDRenderPipeline/PlayModeTest/PlayModeTests.unity", true);

            for (int i=0; i<allPaths.Length;++i)
            {
                testScenes[i+1] = new EditorBuildSettingsScene(allPaths[i], true);
            }

            Debug.Log("Do build in : " + Application.dataPath + "/../Builds/GraphicTests/GraphicTestBuildPC.exe");

            // Move all templates to a Resources folder for build
            string[] templates = AssetDatabase.FindAssets("t:Texture2D" , new string[] { "Assets/ImageTemplates/HDRenderPipeline" });

            Debug.Log("Found " + templates.Length + " template images.");

            string[] oldPaths = new string[templates.Length];
            string[] newPaths = new string[templates.Length];

            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

            for (int i=0; i<templates.Length;++i)
            {
                oldPaths[i] = AssetDatabase.GUIDToAssetPath(templates[i]);
                newPaths[i] = "Assets/Resources/" + System.IO.Path.GetFileName( oldPaths[i] );

                //Debug.Log("Move " + oldPaths[i] + " to " + newPaths[i]);

                AssetDatabase.MoveAsset(oldPaths[i], newPaths[i]);
            }

            //string[] templates = System.IO.Directory.GetFiles(Application.dataPath + scenesRootPath, "*.unity", System.IO.SearchOption.AllDirectories);

            //System.IO.Directory.Move(Application.dataPath + "/ImageTemplates/HDRenderPipeline", Application.dataPath + "/ImageTemplates/Resources/HDRenderPipeline");

            BuildOptions options = BuildOptions.None;
            if (developmentBuild) options |= BuildOptions.Development;
            if (buildAndRun) options |= BuildOptions.AutoRunPlayer;

            switch (platform)
            {
                case Platforms.PC:
                    BuildPipeline.BuildPlayer(testScenes, Application.dataPath + "/../Builds/GraphicTests/PC/GraphicTestBuildPC.exe", BuildTarget.StandaloneWindows64, options);
                    break;
                case Platforms.PS4:
                    BuildPipeline.BuildPlayer(testScenes, Application.dataPath + "/../Builds/GraphicTests/PS4/GraphicTestBuildPS4.self", BuildTarget.PS4, options);
                    break;
            }

            // Move back Templates to their folder
            for (int i = 0; i < templates.Length; ++i)
            {
                AssetDatabase.MoveAsset(newPaths[i], oldPaths[i]);
            }
        }
    }
}
