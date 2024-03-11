using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.Linq;

public class SetupGraphicsTestCases : IPrebuildSetup
{
    public void Setup()
    {
        // Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
        // Once that's fixed, this class can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
        // can be used directly instead.
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(GraphicsTests.path);

        // Configure project for XR tests
        Unity.Testing.XR.Editor.SetupMockHMD.SetupLoader();
    }

    // Disable ErrorMaterial scene in build settings. This method is called from the command line before running
    // the build job in Yamato. It is necessary to do so because the ErrorMaterial scene is using shaders with
    // errors that would fail the build job in Yamato. UTR allows to ignore compilation errors
    // but shader errors are error logs, not compilation errors.
    [MenuItem("Tools/Remove ErrorMaterial scene from Build")]
    public static void DisableErrorSceneInBuildProfile()
    {
        string sceneFilename = "ErrorMaterial.unity";

        var newScenes = EditorBuildSettings.scenes;
        var sceneToDisable = newScenes.FirstOrDefault(x => x.path.Contains(sceneFilename));

        if(sceneToDisable != null)
        {
            sceneToDisable.enabled = false;
            EditorBuildSettings.scenes = newScenes;
            UnityEngine.Debug.Log("Scene ErrorMaterial has been disabled in build settings.");

        }
        else
        {
            UnityEngine.Debug.Log("Attempted to disable scene ErrorMaterial but it's not in build settings.");
        }
    }
}
