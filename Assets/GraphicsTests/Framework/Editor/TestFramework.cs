using System;
using System.Collections;
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
		// path where the tests live
        private static readonly string[] s_Path =
        {
            "GraphicsTests",
            "RenderPipeline"
        };

        private static readonly string[] s_PipelinePath =
        {
            "LightweightPipeline",
            "HDRenderPipeline",
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

		// collect the scenes that we can use
        public static class CollectScenes
        {
            public static IEnumerable scenes
            {
                get
                {
                    var absoluteScenesPath = s_Path.Aggregate(Application.dataPath, Path.Combine);

                    foreach (var pipelinePath in s_PipelinePath)
                    {

                        var filesPath = Path.Combine(absoluteScenesPath, pipelinePath);

                        // find all the scenes
                        var allPaths = System.IO.Directory.GetFiles(filesPath, "*.unity", System.IO.SearchOption.AllDirectories);

                        // construct all the needed test infos
                        foreach (var path in allPaths)
                        {
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
            }
        }

        [TearDown]
        public void TearDown()
        {
            var testSetup = Object.FindObjectOfType<SetupSceneForRenderPipelineTest>();
            if (testSetup == null)
                return;

            testSetup.TearDown();
        }

        [UnityTest]
        public IEnumerator TestScene([ValueSource(typeof(CollectScenes), "scenes")]TestInfo testInfo)
        {
			var prjRelativeGraphsPath = s_Path.Aggregate("Assets", Path.Combine);
			var filePath = Path.Combine(prjRelativeGraphsPath, testInfo.relativePath);

			// open the scene
            EditorSceneManager.OpenScene(filePath);

            var testSetup = Object.FindObjectOfType<SetupSceneForRenderPipelineTest> ();
			Assert.IsNotNull(testSetup, "No SetupSceneForRenderPipelineTest in scene " + testInfo.name);
			Assert.IsNotNull(testSetup.cameraToUse, "No configured camera in <SetupSceneForRenderPipelineTest>");

            testSetup.Setup();

            for (int i = 0; i < testInfo.frameWait; ++i)
            {
                yield return null;
            }

            while (Lightmapping.isRunning)
            {
                yield return null;
            }

            var rtDesc = new RenderTextureDescriptor (
				             testSetup.width,
				             testSetup.height,
				             testSetup.hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32,
				             24);
			rtDesc.sRGB = PlayerSettings.colorSpace == ColorSpace.Linear;

			// render the scene
			var tempTarget = RenderTexture.GetTemporary (rtDesc);
			var oldTarget = testSetup.cameraToUse.targetTexture;
			testSetup.cameraToUse.targetTexture = tempTarget;
			testSetup.cameraToUse.Render ();
			testSetup.cameraToUse.targetTexture = oldTarget;

			// Readback the rendered texture
			var oldActive = RenderTexture.active;
			RenderTexture.active = tempTarget;
			var captured = new Texture2D(tempTarget.width, tempTarget.height, TextureFormat.ARGB32, false);
			captured.ReadPixels(new Rect(0, 0, testSetup.width, testSetup.height), 0, 0);
			RenderTexture.active = oldActive;

            var rootPath = Directory.GetParent(Application.dataPath).ToString();
            var templatePath = Path.Combine(rootPath.ToString(), "ImageTemplates");

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
                var failedPath = Path.Combine(rootPath.ToString(), "Failed");
                Directory.CreateDirectory(failedPath);
				var misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.{1}", testInfo.name, "png"));
				var misMatchLocationTemplate = Path.Combine(failedPath, string.Format("{0}.template.{1}", testInfo.name, "png"));
				var generated = captured.EncodeToPNG();
                File.WriteAllBytes(misMatchLocationResult, generated);
                File.WriteAllBytes(misMatchLocationTemplate, template);
            }

			Assert.IsTrue(areEqual, "Scene from {0}, did not match .template file.", testInfo.relativePath);
        }

		// compare textures, use RMS for this
        private bool CompareTextures(Texture2D fromDisk, Texture2D captured, float threshold)
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
    }
}
