using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Events;
using System.IO;
using System.Linq;

public class GraphicTestsPlayMode : IPrebuildSetup
{
    public virtual string pipelineID { get { return "NoPipeline"; } }

#if UNITY_EDITOR
    UnityEditor.EditorBuildSettingsScene[] oldScenes;
#endif
    
    public void Setup()
    {
#if UNITY_EDITOR
        oldScenes = UnityEditor.EditorBuildSettings.scenes;

        List<UnityEditor.EditorBuildSettingsScene> sceneSetups = new List<UnityEditor.EditorBuildSettingsScene>();
        foreach ( TestFrameworkTools.TestInfo testInfo in TestFrameworkTools.CollectScenesPlayMode.GetScenesForPipelineID(pipelineID))
        {
            sceneSetups.Add(new UnityEditor.EditorBuildSettingsScene {
                path = testInfo.relativePath,
                enabled = true
            });
        }

        UnityEditor.EditorBuildSettings.scenes = sceneSetups.ToArray();
#endif
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
#if UNITY_EDITOR
        UnityEditor.EditorBuildSettings.scenes = oldScenes;
#endif
    }

    public IEnumerator TestScene(TestFrameworkTools.TestInfo testInfo)
    {
        // open the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene( testInfo.sceneListIndex , UnityEngine.SceneManagement.LoadSceneMode.Single);

        yield return null; // wait one "frame" to let the scene load

        SetupSceneForRenderPipelineTest testSetup = GameObject.FindObjectOfType<SetupSceneForRenderPipelineTest>();

        TestFrameworkTools.AssertFix.TestWithMessages(testSetup != null, "No SetupSceneForRenderPipelineTest in scene " + testInfo.name);
        TestFrameworkTools.AssertFix.TestWithMessages(testSetup.cameraToUse != null, "No configured camera in <SetupSceneForRenderPipelineTest> ");

        Time.captureFramerate = testSetup.forcedFrameRate;

        // Initialize
        testSetup.Setup();
        yield return null; // Wait one frame in case we changed the render pipeline

        testSetup.thingToDoBeforeTest.Invoke();

        // Setup Render Target
        Camera testCamera = testSetup.cameraToUse;
        var rtDesc = new RenderTextureDescriptor(
                         testSetup.width,
                         testSetup.height,
                         (testSetup.hdr && testCamera.allowHDR) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32,
                         24);
        //rtDesc.sRGB = PlayerSettings.colorSpace == ColorSpace.Linear;
        rtDesc.msaaSamples = testSetup.msaaSamples;

        var tempTarget = RenderTexture.GetTemporary(rtDesc);
        var oldTarget = testSetup.cameraToUse.targetTexture;
        testSetup.cameraToUse.targetTexture = tempTarget;

        for (int f = 0; f < testSetup.waitForFrames; ++f) yield return null;

        // render the scene

        testSetup.cameraToUse.Render();
        testSetup.cameraToUse.targetTexture = oldTarget;

        // Readback the rendered texture
        var oldActive = RenderTexture.active;
        RenderTexture.active = tempTarget;
        var captured = new Texture2D(tempTarget.width, tempTarget.height, TextureFormat.RGB24, false);
        captured.ReadPixels(new Rect(0, 0, testSetup.width, testSetup.height), 0, 0);
        RenderTexture.active = oldActive;

        // Load the template
        Texture2D fromDisk = new Texture2D(2, 2);
        string dumpFileLocation = "";
        if (!TestFrameworkTools.FindReferenceImage(testInfo, ref fromDisk, captured, ref dumpFileLocation))
        {
            throw new System.Exception(string.Format("Template file not found for {0}, creating it at {1}.", testInfo.name, dumpFileLocation));
        }

        // Compare
        var areEqual = TestFrameworkTools.CompareTextures(fromDisk, captured, testInfo.threshold);

        if (!areEqual)
        {
            var failedPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "SRP_Failed");
            Directory.CreateDirectory(failedPath);
            var misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.{1}", testInfo.name, "png"));
            var misMatchLocationTemplate = Path.Combine(failedPath, string.Format("{0}.template.{1}", testInfo.name, "png"));
            var generated = captured.EncodeToPNG();
            File.WriteAllBytes(misMatchLocationResult, generated);
            File.Copy(dumpFileLocation, misMatchLocationTemplate, true);

            throw new System.Exception(string.Format("Scene from {0}, did not match .template file.", testInfo.relativePath));
        }
        else
        {
            Assert.IsTrue(true);
        }

        testSetup.TearDown();

        yield return null;
    }

    public class HDRP : GraphicTestsPlayMode
    {
        public override string pipelineID { get { return "HDRP"; } }

        [UnityTest]
        new public IEnumerator TestScene([ValueSource(typeof(TestFrameworkTools.CollectScenesPlayMode), "HDRP")]TestFrameworkTools.TestInfo testInfo)
        {
            return base.TestScene(testInfo);
        }
    }
}
