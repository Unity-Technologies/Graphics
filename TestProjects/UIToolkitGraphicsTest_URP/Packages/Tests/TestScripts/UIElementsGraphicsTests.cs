
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tests
{
    public class UIElementsGraphicsTests
    {
        public const string referenceImagesPath = "Assets/ReferenceImages";

        private const string kEmptyScenePath = "Scenes/SampleScene.unity";

        [UnityTest, Category("UniversalRP")]
        [PrebuildSetup("SetupGraphicsTestCases")]
        [UseGraphicsTestCases(referenceImagesPath)]
        public IEnumerator Run(GraphicsTestCase testCase)
        {
            if (string.IsNullOrEmpty(testCase.ScenePath) || testCase.ScenePath.Contains(kEmptyScenePath))
                yield break; // Skip empty scene, it's only there to unload everything at the end of this method

            SceneManager.LoadScene(testCase.ScenePath);
            var isTextTest = testCase.ScenePath.Contains("/Text/");
            var width = isTextTest ? 1024 : 512;
            var height = isTextTest ? 1024 : 512;

            // Wait a few frames to allow tests to do some dynamic changes
            for (int frame = 0; frame < 10; ++frame)
                yield return null;

            ImageComparisonSettings settings = null;
            var customSettings = Object.FindObjectOfType<GfxTestCustomSettings>();
            if (customSettings != null)
                settings = customSettings.settings;
            else
            {
                // Use default settings
                settings = new ImageComparisonSettings() {
                    TargetWidth = 512,
                    TargetHeight = 512,
                    ActiveImageTests = ImageComparisonSettings.ImageTests.IncorrectPixelsCount,
                    ActivePixelTests = ImageComparisonSettings.PixelTests.DeltaGamma | ImageComparisonSettings.PixelTests.DeltaAlpha,
                    PerPixelGammaThreshold = 0.5f / 255,
                    PerPixelAlphaThreshold = 0.5f / 255,
                    IncorrectPixelsThreshold = 0f / 512 / 512
                };
            }

            var testBase = Object.FindObjectOfType<GraphicTestBase>();
            ImageAssert.onAllCamerasRendered = rt => testBase.DrawUI(rt);
            var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x => x.GetComponent<Camera>());
            ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings);

            // Switch to an empty scene to avoid any pending panels
            SceneManager.LoadScene(getEmptyScene()) ;
        }


        int getEmptyScene()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                if (SceneUtility.GetScenePathByBuildIndex(i).Contains(kEmptyScenePath))
                    return i;
            }
            return -1;
        }



        #if UNITY_EDITOR
        [TearDown]
        public void DumpImagesInEditor()
        {
            UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
        }

        #endif
    }
}

