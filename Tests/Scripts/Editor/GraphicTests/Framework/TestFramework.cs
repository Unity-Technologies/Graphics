using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
using UnityEngine.Experimental.Rendering;
using UnityEngine.TestTools;

namespace UnityEditor.Experimental.Rendering
{
    public class GraphicsTests
    {
        // Change the SRP before a full batch of tests
        public virtual string _SRP_ID { get { return "NONE"; } }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            BackupSceneManagerSetup();
            SetupRenderPipeAsset();
        }

        [OneTimeTearDown]
        public void OnTimeTearDown()
        {
            Debug.Log("OnTimeTearDown");
            RestoreRenderPipeAsset();
            RestoreSceneManagerSetup();
        }

        public static RenderPipelineAsset GetRenderPipelineAsset(string _SRP_ID)
        {
            string absolutePath = TestFrameworkTools.s_Path.Aggregate(TestFrameworkTools.s_RootPath, Path.Combine);

            string filePath = Path.Combine(absolutePath, TestFrameworkTools.renderPipelineAssets[_SRP_ID] );

            filePath = filePath.Replace(Application.dataPath, "");

            filePath = filePath.Remove(0, 1);

            //Debug.Log("Before combine: " + filePath);

            filePath = Path.Combine("Assets", filePath);

            //Debug.Log("RP Asset is at : " + filePath);

            return (RenderPipelineAsset)AssetDatabase.LoadAssetAtPath(filePath, typeof(RenderPipelineAsset));
        }

        public static RenderPipelineAsset beforeTestsRenderPipeAsset;
        public static RenderPipelineAsset wantedTestsRenderPipeAsset;

        public void SetupRenderPipeAsset()
        {
            //Debug.Log("Set " + _SRP_ID + " render pipeline. Previous was "+ ( (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null)? "null":UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.name) );

            beforeTestsRenderPipeAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
            wantedTestsRenderPipeAsset = GetRenderPipelineAsset(_SRP_ID);

            if (wantedTestsRenderPipeAsset != beforeTestsRenderPipeAsset)

                UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset = wantedTestsRenderPipeAsset;
        }

        public void RestoreRenderPipeAsset()
        {
            //Debug.Log("RestoreRenderPipeAsset from " + wantedTestsRenderPipeAsset.name + " to " + ((beforeTestsRenderPipeAsset == null)?"null":beforeTestsRenderPipeAsset.name));
            if (wantedTestsRenderPipeAsset != beforeTestsRenderPipeAsset)
            {
                //Debug.Log("RestoreRenderPipeAsset -> Actual restore");
                UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset = beforeTestsRenderPipeAsset;
            }
        }

        public static SceneSetup[] sceneManagerSetupBeforeTest;

        public void BackupSceneManagerSetup()
        {
            sceneManagerSetupBeforeTest = EditorSceneManager.GetSceneManagerSetup();
        }

        public void RestoreSceneManagerSetup()
        {
            if ( (sceneManagerSetupBeforeTest == null) || ( sceneManagerSetupBeforeTest.Length == 0 ) )
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }
            else
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneManagerSetupBeforeTest);
            }
        }


        // the actual test
        public static IEnumerator TestScene(TestFrameworkTools.TestInfo testInfo)
        {
			var prjRelativeGraphsPath = TestFrameworkTools.s_Path.Aggregate(TestFrameworkTools.s_RootPath, Path.Combine);
			var filePath = Path.Combine(prjRelativeGraphsPath, testInfo.relativePath);

			// open the scene
            EditorSceneManager.OpenScene(filePath);

            SetupSceneForRenderPipelineTest testSetup = Object.FindObjectOfType<SetupSceneForRenderPipelineTest> ();
			Assert.IsNotNull(testSetup, "No SetupSceneForRenderPipelineTest in scene " + testInfo.name);
			Assert.IsNotNull(testSetup.cameraToUse, "No configured camera in <SetupSceneForRenderPipelineTest>");

            testSetup.Setup();

            for (int i = 0; i < testInfo.frameWait; ++i)
            {
                yield return null;
            }

            while (UnityEditor.Lightmapping.isRunning)
            {
                yield return null;
            }

            // Force rendering of all realtime reflection probes
            ReflectionProbe[] probes = GameObject.FindObjectsOfType<ReflectionProbe>();
            int[] renderIDs = new int[probes.Length];
            for (int i=0; i<probes.Length; ++i)
            {
                if (probes[i].mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
                    renderIDs[i] = probes[i].RenderProbe();
                else
                    renderIDs[i] = -1;
            }
            for (int i=0; i<probes.Length; ++i)
            {
                if (renderIDs[i] != -1)
                {
                    while (!probes[i].IsFinishedRendering(renderIDs[i])) yield return null;
                }
            }

            testSetup.thingToDoBeforeTest.Invoke();

            // Render the camera
            Texture2D captured = TestFrameworkTools.RenderSetupToTexture(testSetup);

            // Load the template
            Texture2D fromDisk = new Texture2D(2, 2);
            string dumpFileLocation = "";
            if ( !TestFrameworkTools.FindReferenceImage( testInfo, ref fromDisk, captured, ref dumpFileLocation) )
                Assert.Fail("Template file not found for {0}, creating it at {1}.", testInfo.name, dumpFileLocation);

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
            }

			Assert.IsTrue(areEqual, "Scene from {0}, did not match .template file.", testInfo.relativePath);

            testSetup.TearDown();
        }

        // Graphic Tests Subclasses that inherit the functions bot provide different SRP_ID
        public class HDRP : GraphicsTests
        {
            public override string _SRP_ID { get { return "HDRP"; } }

            [UnityTest]
            public IEnumerator HDRP_Test([ValueSource(typeof(TestFrameworkTools.CollectScenes), "HDRP")]TestFrameworkTools.TestInfo testInfo)
            {
                return TestScene(testInfo);
            }
        }

        public class LWRP : GraphicsTests
        {
            public override string _SRP_ID { get { return "LWRP"; } }

            [UnityTest]
            public IEnumerator LWRP_Test([ValueSource(typeof(TestFrameworkTools.CollectScenes), "LWRP")]TestFrameworkTools.TestInfo testInfo)
            {
                return TestScene(testInfo);
            }
        }
    }
}
