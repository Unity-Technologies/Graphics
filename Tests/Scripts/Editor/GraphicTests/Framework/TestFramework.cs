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

        [MenuItem("Internal/GraphicTest Tools/Set Tests Pipelines",false, 0)]
        public static void SetTestsPipelines()
        {
            _doOnlyFirstRenderPipelineAsset = !EditorUtility.DisplayDialog(
                "Graphic Tests",
                "Do you want to run the test(s) on all available Render Pipeline assets or only the first (main) one ?",
                "Hell YEAH, go for it !",
                "No thanks, just one please.");
        }

        private static bool? _doOnlyFirstRenderPipelineAsset;
        private static bool doOnlyFirstRenderPipelineAsset
        {
            get
            {
                if (!_doOnlyFirstRenderPipelineAsset.HasValue) SetTestsPipelines();
                return _doOnlyFirstRenderPipelineAsset.Value;
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            BackupSceneManagerSetup();
            SetupRenderPipeAsset();
        }

        [OneTimeTearDown]
        public void OnTimeTearDown()
        {
            //Debug.Log("OneTimeTearDown");
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

            if (testSetup.renderPipelines == null || testSetup.renderPipelines.Length == 0)
                yield break;

            for (int r = 0; r < (doOnlyFirstRenderPipelineAsset?1:testSetup.renderPipelines.Length); ++r)
            {
                if (r==0)
                    testSetup.Setup();
                else
                    testSetup.Setup(r);

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
                for (int i = 0; i < probes.Length; ++i)
                {
                    if (probes[i].mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
                        renderIDs[i] = probes[i].RenderProbe();
                    else
                        renderIDs[i] = -1;
                }
                for (int i = 0; i < probes.Length; ++i)
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
                if (!TestFrameworkTools.FindReferenceImage(testInfo, ref fromDisk, captured, ref dumpFileLocation))
                    Assert.Fail("Template file not found for {0}, creating it at {1}.", testInfo.name, dumpFileLocation);

                // Compare
                var areEqual = TestFrameworkTools.CompareTextures(fromDisk, captured, testInfo.threshold);

                if (!areEqual)
                {
                    var failedPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "SRP_Failed");
                    Directory.CreateDirectory(failedPath);
                    var misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.{1}", testInfo.name, "png"));
                    var misMatchLocationTemplate = Path.Combine(failedPath, string.Format("{0}.template.{1}", testInfo.name, "png"));

                    // Add associated renderpipeline label image if it exists
                    string rpLabelPath = AssetDatabase.GetAssetPath(testSetup.renderPipelines[r]);
                    Texture2D rpLabel = AssetDatabase.LoadAssetAtPath<Texture2D>(rpLabelPath.Remove(rpLabelPath.Length - 5)+"png");
                    if (rpLabel != null)
                    {
                        Color[] rpLabelPixels = rpLabel.GetPixels();
                        Color[] targetPixels = captured.GetPixels(0, 0, rpLabel.width, rpLabel.height);

                        for (int p = 0; p < rpLabelPixels.Length; ++p)
                        {
                            targetPixels[p] = Color.Lerp(targetPixels[p], rpLabelPixels[p], rpLabelPixels[p].a);
                            targetPixels[p].a = 1f;
                        }

                        captured.SetPixels(0, 0, rpLabel.width, rpLabel.height, targetPixels);
                        captured.Apply();
                    }

                    var generated = captured.EncodeToPNG();
                    File.WriteAllBytes(misMatchLocationResult, generated);
                    File.Copy(dumpFileLocation, misMatchLocationTemplate, true);

                    Object.DestroyImmediate(captured);
                }

                Assert.IsTrue(areEqual, "Scene from {0}, did not match .template file.", testInfo.relativePath);

                if (!areEqual) // No need to continue the test on other renderpipelines, it would overwrite the fail capture
                {
                    testSetup.TearDown();
                    yield break;
                }
            }

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
