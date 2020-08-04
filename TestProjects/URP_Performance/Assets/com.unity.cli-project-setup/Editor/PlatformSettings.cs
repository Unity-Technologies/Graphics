#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using com.unity.test.performance.runtimesettings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.PackageManager;
#if URP
using UnityEngine.Rendering.Universal;
#endif

namespace com.unity.cliprojectsetup
{
    public class PlatformSettings
    {
        public BuildTarget BuildTarget => EditorUserBuildSettings.activeBuildTarget;

        public GraphicsDeviceType PlayerGraphicsApi;
        public string PackageUnderTestName;
        public string PackageUnderTestRevision;
        public string PackageUnderTestRevisionDate;
        public string PackageUnderTestBranch;

        private static readonly string resourceDir = "Assets/Resources";
        private static readonly string settingsAssetName = "/settings.asset";
        private readonly Regex revisionValueRegex = new Regex("\"revision\": \"([a-f0-9]*)\"",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex majorMinorVersionValueRegex = new Regex("([0-9]*\\.[0-9]*\\.)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public ColorSpace ColorSpace = ColorSpace.Gamma;
        public bool EnableBurst = true;
        public bool GraphicsJobs;
        public bool MtRendering = true;
        public string RenderPipeline;
        public ScriptingImplementation ScriptingImplementation = ScriptingImplementation.IL2CPP;
        public string TestsBranch;
        public string TestsRevision;
        public string TestsRevisionDate;
        public string Username;
        public string JobLink;
        public int JobWorkerCount = -1; // sentinel value indicating we don't want to set the JobWorkerCount

        public void SerializeToAsset()
        {
            var settingsAsset = ScriptableObject.CreateInstance<CurrentSettings>();

            settingsAsset.PlayerGraphicsApi = PlayerGraphicsApi.ToString();
            settingsAsset.MtRendering = MtRendering;
            settingsAsset.GraphicsJobs = GraphicsJobs;
            settingsAsset.EnableBurst = EnableBurst;
            settingsAsset.ScriptingBackend = ScriptingImplementation.ToString();
            settingsAsset.ColorSpace = ColorSpace.ToString();
            settingsAsset.Username = Username = Environment.UserName;
            settingsAsset.PackageUnderTestName = PackageUnderTestName;
            settingsAsset.TestsRevision = TestsRevision;
            settingsAsset.TestsRevisionDate = TestsRevisionDate;
            settingsAsset.TestsBranch = TestsBranch;
            settingsAsset.JobLink = JobLink;
            settingsAsset.JobWorkerCount = Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount;

            GetPackageUnderTestVersionInfo(settingsAsset);
            settingsAsset.RenderPipeline = RenderPipeline =  $"{(GraphicsSettings.renderPipelineAsset != null ? GraphicsSettings.renderPipelineAsset.name : "BuiltInRenderer")}";

#if URP
            settingsAsset.AntiAliasing = GraphicsSettings.renderPipelineAsset != null
                ? ((UniversalRenderPipelineAsset) GraphicsSettings.renderPipelineAsset).msaaSampleCount
                : QualitySettings.antiAliasing;
#else
            settingsAsset.AntiAliasing = QualitySettings.antiAliasing;
#endif
            SaveSettingsAssetOnStartup(settingsAsset);
        }

        private void GetPackageUnderTestVersionInfo(CurrentSettings settingsAsset)
        {
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted)
            {
            }

            if (listRequest.Result.Any(r => r.name.Equals(PackageUnderTestName)))
            {
                var packageUnderTestInfo =
                    listRequest.Result.First(r => r.name.Equals(PackageUnderTestName));

                settingsAsset.PackageUnderTestVersion = packageUnderTestInfo.version;

                // if PackageRevision is empty, then it wasn't passed in on the command line (which is
                // usually going to be the case if we're running in tests at the PR level for the package).
                // In this case, we most likely are using a released package reference, so let's try to get
                // the revision from the package.json.
                settingsAsset.PackageUnderTestRevision = string.IsNullOrEmpty(PackageUnderTestRevision) ? 
                    TryGetRevisionFromPackageJson(PackageUnderTestRevision) 
                    : PackageUnderTestRevision;

                // if PackageUnderTestRevisionDate is empty, then it wasn't passed in on the command line (which is
                // usually going to be the case if we're running in tests at the PR level for the package).
                // In this case, we most likely are using a released package reference, so let's try to get
                // the revision date from the package manager api instead.
                settingsAsset.PackageUnderTestRevisionDate = string.IsNullOrEmpty(PackageUnderTestRevisionDate)
                    ? TryGetPackageUnderTestRevisionDate(packageUnderTestInfo.datePublished)
                    : PackageUnderTestRevisionDate;

                // if PackageUnderTestBranch is empty, then it wasn't passed in on the command line (which is
                // usually going to be the case if we're running in tests at the PR level for the package).
                // In this case, we most likely are using a released package reference, so let's try to infer
                // the branch from the major.minor version of the package via the package manager API
                settingsAsset.PackageUnderTestPackageBranch = string.IsNullOrEmpty(PackageUnderTestBranch)
                    ? TryGetPackageUnderTestBranch(packageUnderTestInfo.version)
                    : PackageUnderTestBranch;
            }
        }

        private string TryGetPackageUnderTestBranch(string version)
        {
            var matches = majorMinorVersionValueRegex.Matches(version);
            return matches.Count > 0 ? string.Concat(matches[0].Groups[0].Value, "x") : "release";
        }

        private string TryGetPackageUnderTestRevisionDate(DateTime? datePublished)
        {
            return datePublished != null ?
                    ((DateTime)datePublished).ToString("s", DateTimeFormatInfo.InvariantInfo) : "unavailable";
        }

        private string TryGetRevisionFromPackageJson(string packageName)
        {
            string revision = null;
            var packageAsString = File.ReadAllText(string.Format("Packages/{0}/package.json", packageName));
            var matches = revisionValueRegex.Matches(packageAsString);
            if (matches.Count > 0)
            {
                revision = matches[0].Groups[1].Value;
            }

            return revision;
        }

        public static void SaveSettingsAsset(CurrentSettings settingsAsset)
        {
            if (!Directory.Exists(resourceDir))
            {
                Directory.CreateDirectory(resourceDir);
            }
            if (!Resources.FindObjectsOfTypeAll<CurrentSettings>().Any())
            {
                AssetDatabase.CreateAsset(settingsAsset, resourceDir + settingsAssetName);
            }
            EditorUtility.SetDirty(settingsAsset);
            AssetDatabase.SaveAssets();
        }

        private void SaveSettingsAssetOnStartup(CurrentSettings settingsAsset)
        {
            if (!Directory.Exists(resourceDir))
            {
                Directory.CreateDirectory(resourceDir);
            }
            AssetDatabase.CreateAsset(settingsAsset, resourceDir + settingsAssetName);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif