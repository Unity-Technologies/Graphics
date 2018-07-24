using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Test framework prebuild step to collect reference images for the current test run and prepare them for use in the
    /// player.
    /// </summary>
    public class SetupGraphicsTestCases : IPrebuildSetup
    {
        private static bool IsBuildingForEditorPlaymode
        {
            get
            {
                var playmodeLauncher =
                    typeof(UnityEditor.TestTools.RequirePlatformSupportAttribute).Assembly.GetType(
                        "UnityEditor.TestTools.TestRunner.PlaymodeLauncher");
                var isRunningField = playmodeLauncher.GetField("IsRunning");

                return (bool)isRunningField.GetValue(null);
            }
        }

        public void Setup()
        {
            ColorSpace colorSpace;
            BuildTarget buildPlatform;
            RuntimePlatform runtimePlatform;
            GraphicsDeviceType[] graphicsDevices;

            // Figure out if we're preparing to run in Editor playmode, or if we're building to run outside the Editor
            if (IsBuildingForEditorPlaymode)
            {
                colorSpace = QualitySettings.activeColorSpace;
                buildPlatform = BuildTarget.NoTarget;
                runtimePlatform = Application.platform;
                graphicsDevices = new[] {SystemInfo.graphicsDeviceType};
            }
            else
            {
                buildPlatform = EditorUserBuildSettings.activeBuildTarget;
                runtimePlatform = Utils.BuildTargetToRuntimePlatform(buildPlatform);
                colorSpace = PlayerSettings.colorSpace;
                graphicsDevices = PlayerSettings.GetGraphicsAPIs(buildPlatform);
            }

            var bundleBuilds = new List<AssetBundleBuild>();

            foreach (var api in graphicsDevices)
            {
                var images = EditorGraphicsTestCaseProvider.CollectReferenceImagePathsFor(colorSpace, runtimePlatform, api);

                Utils.SetupReferenceImageImportSettings(images.Values);

                if (buildPlatform == BuildTarget.NoTarget)
                    continue;

                bundleBuilds.Add(new AssetBundleBuild
                {
                    assetBundleName = string.Format("referenceimages-{0}-{1}-{2}", colorSpace, runtimePlatform, api),
                    addressableNames = images.Keys.ToArray(),
                    assetNames = images.Values.ToArray()
                });
            }

            if (bundleBuilds.Count > 0)
            {
                if (!Directory.Exists("Assets/StreamingAssets"))
                    Directory.CreateDirectory("Assets/StreamingAssets");

                foreach (var bundle in bundleBuilds)
                {
                    BuildPipeline.BuildAssetBundles("Assets/StreamingAssets", new [] { bundle }, BuildAssetBundleOptions.None,
                        buildPlatform);
                }
            }

            if (!IsBuildingForEditorPlaymode)
                new CreateSceneListFileFromBuildSettings().Setup();
        }
    }
}
