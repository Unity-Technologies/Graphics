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
        static readonly string s_RootPath = Directory.GetParent(Directory.GetFiles(Application.dataPath, "SRPMARKER", SearchOption.AllDirectories).First()).ToString();

		// path where the tests live
        private static readonly string[] s_Path =
        {
            "Tests",
            "GraphicsTests",
            "RenderPipeline"
        };

        // info that gets generated for use
        // in a dod way
        public struct TestInfo
        {
            public string name;
            public float threshold;
			public string relativePath;
            public int frameWait;

            public override string ToString()
            {
                return name;
            }

        }

        // Renderpipeline assets used for the tests
        public static Dictionary<string, string> renderPipelineAssets = new Dictionary<string, string>()
        {
            { "HDRP", "HDRenderPipeline/CommonAssets/HDRP_GraphicTests_Asset.asset" },
            { "LWRP", "LightweightPipeline/LightweightPipelineAsset.asset" }
        };

        // Renderpipeline assets used for the tests
        public static Dictionary<string, string> renderPipelineScenesFolder = new Dictionary<string, string>()
        {
            { "HDRP", "HDRenderPipeline/Scenes" },
            { "LWRP", "LightweightPipeline" }
        };

        // collect the scenes that we can use
        public static class CollectScenes
        {
            public static IEnumerable HDRP
            {
                get
                {
                    return GetScenesForPipelineID("HDRP");
                }
            }

            public static IEnumerable LWRP
            {
                get
                {
                    return GetScenesForPipelineID("LWRP");
                }
            }

            public static  IEnumerable GetScenesForPipelineID( string _pipelineID )
            {
                return GetScenesForPipeline( renderPipelineScenesFolder[_pipelineID] );
            }

            public static IEnumerable GetScenesForPipeline(string _pipelinePath)
            {
                var absoluteScenesPath = s_Path.Aggregate(s_RootPath, Path.Combine);

                var filesPath = Path.Combine(absoluteScenesPath, _pipelinePath);

                // find all the scenes
                var allPaths = Directory.GetFiles(filesPath, "*.unity", SearchOption.AllDirectories);

                // Convert to List for easy sorting in alphabetical ordre
                List<string> allPaths_List = new List<string>(allPaths);
                allPaths_List.Sort();

                // construct all the needed test infos
                for (int i=0; i<allPaths_List.Count; ++i)
                {
                    var path = allPaths_List[i];

                    var p = new FileInfo(path);
                    var split = s_Path.Aggregate("", Path.Combine);
                    split = string.Format("{0}{1}", split, Path.DirectorySeparatorChar);
                    var splitPaths = p.FullName.Split(new[] { split }, StringSplitOptions.RemoveEmptyEntries);

                    yield return new TestInfo
                    {
                        name = p.Name,
                        relativePath = splitPaths.Last(),
                        threshold = 0.02f,
                        frameWait = 100
                    };
                }
            }
        }


        // Change the SRP before a full batch of tests
        public virtual string _SRP_ID { get { return "NONE"; } }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SetupRenderPipeAsset();
        }

        [OneTimeTearDown]
        public void OnTimeTearDown()
        {
            RestoreRenderPipeAsset();
        }

        public static RenderPipelineAsset GetRenderPipelineAsset(string _SRP_ID)
        {
            string absolutePath = s_Path.Aggregate(s_RootPath, Path.Combine);

            string filePath = Path.Combine(absolutePath, renderPipelineAssets[_SRP_ID] );

            return (RenderPipelineAsset)AssetDatabase.LoadAssetAtPath(filePath, typeof(RenderPipelineAsset));
        }

        public static RenderPipelineAsset beforeTestsRenderPipeAsset;
        public static RenderPipelineAsset wantedTestsRenderPipeAsset;

        public void SetupRenderPipeAsset()
        {
            Debug.Log("Set " + _SRP_ID + " render pipeline.");

            beforeTestsRenderPipeAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
            wantedTestsRenderPipeAsset = GetRenderPipelineAsset(_SRP_ID);

            if (wantedTestsRenderPipeAsset != beforeTestsRenderPipeAsset)
                UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset = wantedTestsRenderPipeAsset;
        }

        public void RestoreRenderPipeAsset()
        {
            if (wantedTestsRenderPipeAsset != beforeTestsRenderPipeAsset)
                UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset = beforeTestsRenderPipeAsset;
        }


        // the actual test
        public static IEnumerator TestScene(TestInfo testInfo)
        {
			var prjRelativeGraphsPath = s_Path.Aggregate(s_RootPath, Path.Combine);
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

            Camera testCamera = testSetup.cameraToUse;
            var rtDesc = new RenderTextureDescriptor (
				             testSetup.width,
				             testSetup.height,
				             (testSetup.hdr && testCamera.allowHDR) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32,
				             24);
			rtDesc.sRGB = PlayerSettings.colorSpace == ColorSpace.Linear;
            rtDesc.msaaSamples = testSetup.msaaSamples;

			// render the scene
			var tempTarget = RenderTexture.GetTemporary (rtDesc);
			var oldTarget = testSetup.cameraToUse.targetTexture;
			testSetup.cameraToUse.targetTexture = tempTarget;
			testSetup.cameraToUse.Render ();
			testSetup.cameraToUse.targetTexture = oldTarget;

			// Readback the rendered texture
			var oldActive = RenderTexture.active;
			RenderTexture.active = tempTarget;
			var captured = new Texture2D(tempTarget.width, tempTarget.height, TextureFormat.RGB24, false);
			captured.ReadPixels(new Rect(0, 0, testSetup.width, testSetup.height), 0, 0);
			RenderTexture.active = oldActive;

            var templatePath = Path.Combine(s_RootPath, "ImageTemplates");

            // find the reference image
			var dumpFileLocation = Path.Combine(templatePath, string.Format("{0}.{1}", testInfo.relativePath, "png"));
			if (!File.Exists(dumpFileLocation))
            {
				// no reference exists, create it
				var fileInfo = new FileInfo (dumpFileLocation);
				fileInfo.Directory.Create();

				var generated = captured.EncodeToPNG();
                File.WriteAllBytes(dumpFileLocation, generated);
				Assert.Fail("Template file not found for {0}, creating it at {1}.", testInfo.name, dumpFileLocation);
            }

            var template = File.ReadAllBytes(dumpFileLocation);
            var fromDisk = new Texture2D(2, 2);
            fromDisk.LoadImage(template, false);

            var areEqual = CompareTextures(fromDisk, captured, testInfo.threshold);

            if (!areEqual)
            {
                var failedPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "SRP_Failed");
                Directory.CreateDirectory(failedPath);
				var misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.{1}", testInfo.name, "png"));
				var misMatchLocationTemplate = Path.Combine(failedPath, string.Format("{0}.template.{1}", testInfo.name, "png"));
				var generated = captured.EncodeToPNG();
                File.WriteAllBytes(misMatchLocationResult, generated);
                File.WriteAllBytes(misMatchLocationTemplate, template);
            }

			Assert.IsTrue(areEqual, "Scene from {0}, did not match .template file.", testInfo.relativePath);

            testSetup.TearDown();
        }

		// compare textures, use RMS for this
        private static bool CompareTextures(Texture2D fromDisk, Texture2D captured, float threshold)
        {
            if (fromDisk == null || captured == null)
                return false;

            if (fromDisk.width != captured.width
                || fromDisk.height != captured.height)
                return false;

            var pixels1 = fromDisk.GetPixels();
            var pixels2 = captured.GetPixels();

			if (pixels1.Length != pixels2.Length)
				return false;

			int numberOfPixels = pixels1.Length;

			float sumOfSquaredColorDistances = 0;
			for (int i = 0; i < numberOfPixels; i++)
			{
				Color p1 = pixels1[i];
				Color p2 = pixels2[i];

				Color diff = p1 - p2;
				diff = diff * diff;
				sumOfSquaredColorDistances += (diff.r + diff.g + diff.b) / 3.0f;
			}
			float rmse = Mathf.Sqrt(sumOfSquaredColorDistances / numberOfPixels);
			return rmse < threshold;
        }

        // Graphic Tests Subclasses that inherit the functions bot provide different SRP_ID
        public class HDRP : GraphicsTests
        {
            public override string _SRP_ID { get { return "HDRP"; } }

            [UnityTest]
            public IEnumerator HDRP_Test([ValueSource(typeof(CollectScenes), "HDRP")]TestInfo testInfo)
            {
                return TestScene(testInfo);
            }
        }

        public class LTRP : GraphicsTests
        {
            public override string _SRP_ID { get { return "LWRP"; } }

            [UnityTest]
            public IEnumerator LWRP_Test([ValueSource(typeof(CollectScenes), "LWRP")]TestInfo testInfo)
            {
                return TestScene(testInfo);
            }
        }
    }
}
