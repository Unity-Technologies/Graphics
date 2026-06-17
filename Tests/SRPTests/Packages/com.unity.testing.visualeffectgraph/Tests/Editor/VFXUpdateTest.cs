using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Object = UnityEngine.Object;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.Compilation;
using UnityEditor.AssetPackage;

//Not strictly test, these helpers are used to upgrade project
namespace UnityEditor.VFX.Update
{
    [TestFixture]
    public class VFXUpdate
    {
        [UnityTest, Timeout(4 * 60 * 60 * 1000)]
        public IEnumerator Upgrade_And_Save_VFX()
        {
            VFXAssetManager.BuildAndSave();
            yield return null;
        }
        
        static string[] s_AllTestUnityPackages;
        public static IEnumerable<string> AllTestUnityPackages
        {
            get
            {
                if (s_AllTestUnityPackages != null)
                    return s_AllTestUnityPackages;

                var found = new List<string>();
                foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] {"Assets", "Packages/com.unity.testing.visualeffectgraph", "Packages/com.unity.testing.common-graphics"}))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                        found.Add(path);
                }
                s_AllTestUnityPackages = found.ToArray();
                return s_AllTestUnityPackages;
            }
        }

        [UnityTest, Timeout(10 * 60 * 1000), Description("Run from an Editor launched with -organization-id XXXXXXXXXXXXX"), Ignore("This helper is only useful locally")]
        public IEnumerator Sign_UnityPackage([ValueSource(nameof(AllTestUnityPackages))] string unityPackagePath)
        {
            bool alreadySigned = false;
            try
            {
                using var file = File.OpenRead(unityPackagePath);
                using var gz = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionMode.Decompress);

                //Tar begins with a 512 byte header, the first 100 bytes are the entry name
                var header = new byte[100];
                int read = 0, n;
                while (read < header.Length && (n = gz.Read(header, read, header.Length - read)) > 0)
                    read += n;

                int nameLen = Array.IndexOf(header, (byte)0, 0, read);
                if (nameLen < 0) nameLen = read;
                var firstEntry = System.Text.Encoding.ASCII.GetString(header, 0, nameLen);
                alreadySigned = firstEntry == "package/.attestation.p7m";
            }
            catch { /* treat unreadable as error */ }

            if (alreadySigned)
                Assert.Ignore("Package is already signed.");

            //See GetFirstValueForARGV("organization-id") in Modules/AssetPackageEditor/AssetPackage.cpp
            string ownerOrgId = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-organization-id")
                {
                    ownerOrgId = args[i + 1];
                    break;
                }
            }
            Assert.IsFalse(string.IsNullOrEmpty(ownerOrgId), "Missing -organization-id on the Editor command line");

            //SignedAssetPackage and SignatureService are internal to AssetPackageEditor, reproduce attestation usage from RequestAttestationFromPackageRegistry
            var asm = typeof(ExportPackageParameters).Assembly;
            var signedAssetPackageType = asm.GetType("UnityEditor.AssetPackage.SignedAssetPackage", true);
            var signatureServiceType = asm.GetType("UnityEditor.AssetPackage.SignatureService", true);
            var insertAttestationMethod = signedAssetPackageType.GetMethod("InsertAttestationFileIntoTarball", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(insertAttestationMethod, "SignedAssetPackage.InsertAttestationFileIntoTarball not found");

            //Request signature
            var signatureService = Activator.CreateInstance(signatureServiceType);
            var task = (Task)insertAttestationMethod.Invoke(null, new [] { unityPackagePath, unityPackagePath, ownerOrgId, signatureService });
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception.GetBaseException();

            Debug.Log($"{unityPackagePath} successfully signed.");
        }

#if VFX_TESTS_HAS_HDRP
        private static bool s_ImportPackageCompleted = false;
        private static void OnImportPackageCompleted(string packagename)
        {
            s_ImportPackageCompleted = true;
        }

        class QuietLogHandler : ILogHandler
        {
            public ILogHandler m_ForwardLog;
            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                m_ForwardLog.LogFormat(LogType.Log, context, format, args);
            }

            public void LogException(Exception exception, Object context)
            {
                m_ForwardLog.LogFormat(LogType.Log, context, exception.ToString());
            }
        }

        [UnityTest, Timeout(6000 * 1000)]
        public IEnumerator Upgrade_Many_VFX_Package()
        {
            var packageName = "Packages/com.unity.testing.visualeffectgraph/PerformanceTests/Editor/Benchmark_ManyVFX.unitypackage";
            var expectedDirectory = "Assets/Repro_Many_Assets";

            var absolutePath = Path.GetFullPath(packageName);
            var quietLog = new QuietLogHandler()
            {
                m_ForwardLog = Debug.unityLogger.logHandler
            };
            Debug.unityLogger.logHandler = quietLog;
            if (Directory.Exists(expectedDirectory))
                Directory.Delete(expectedDirectory, true);

            s_ImportPackageCompleted = false;
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.ImportPackage(packageName, false);
            while (!s_ImportPackageCompleted)
                yield return null;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;

            var exportedPackageAssetList = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { expectedDirectory }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                exportedPackageAssetList.Add(path);
            }
            Package.Export(new ExportPackageParameters(exportedPackageAssetList.ToArray(), absolutePath, "", ExportPackageOptions.Recurse));
            yield return null;

            AssetDatabase.DeleteAsset(expectedDirectory);
            yield return null;
            Debug.unityLogger.logHandler = quietLog.m_ForwardLog;

            Assert.IsFalse(Directory.Exists(expectedDirectory));
        }

        private static void CopyFiles(string sourcePath, string targetPath)
        {
            foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }


        [SerializeField] private string m_Version;
        private static readonly string kSampleExpectedBasePath = "Assets/Samples";

        [UnityTest, Timeout(6000 * 1000)]
        public IEnumerator Upgrade_Additional_Sample()
        {
            var searchRequest = Client.Search("com.unity.visualeffectgraph", true);
            while (!searchRequest.IsCompleted)
            {
                yield return null;
            }

            Assert.AreEqual(StatusCode.Success, searchRequest.Status);
            Assert.AreEqual(1, searchRequest.Result.Length);
            m_Version = searchRequest.Result[0].version;

            var addRequest = Client.Add(@"file:../../../../../Packages/com.unity.render-pipelines.universal");
            while (!addRequest.IsCompleted)
                yield return null;
            Assert.AreEqual(StatusCode.Success, addRequest.Status);

            //Workaround for UUM-63664
            {
                var tempSamples = Sample.FindByPackage("com.unity.visualeffectgraph", m_Version);
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageSelectionChange(searchRequest.Result[0]);
                foreach (var sample in tempSamples)
                {
                    //Force import of dependencies before importing anything else
                    var samplePath = Path.Combine(kSampleExpectedBasePath, "Visual Effect Graph", m_Version, sample.displayName);
                    Directory.CreateDirectory(samplePath);
                    var dummy = samplePath + "/dummy.txt";
                    File.WriteAllText(dummy, "UUM-63664 workaround for test.");
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    AssetDatabase.DeleteAsset(dummy);
                }
            }

            CompilationPipeline.RequestScriptCompilation();
            yield return new WaitForDomainReload();

            var samples = Sample.FindByPackage("com.unity.visualeffectgraph", m_Version);
            foreach (var sample in samples)
            {
                var result = sample.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
                Assert.IsTrue(result);
            }

            //VFXAssetManager.Build(true); //Shouldn't be needed to upgrade dirty imported asset
            AssetDatabase.SaveAssets();

            var baseTarget = @"../../../../Packages/com.unity.visualeffectgraph";
            Directory.Delete(baseTarget + "/Samples~", true);

            var commonAssetPath = Path.Combine(kSampleExpectedBasePath, "Visual Effect Graph", "Common");
            var commonTargetPath = Path.Combine(baseTarget, "Samples~", "Common");
            CopyFiles(commonAssetPath, commonTargetPath);

            samples = Sample.FindByPackage("com.unity.visualeffectgraph", m_Version);
            foreach (var sample in samples)
            {
                var assetPath = Path.Combine(kSampleExpectedBasePath, "Visual Effect Graph", m_Version, sample.displayName);
                var targetPath = sample.resolvedPath;
                CopyFiles(assetPath, targetPath);
            }
            AssetDatabase.DeleteAsset(kSampleExpectedBasePath);

            var removeRequest = Client.Remove("com.unity.render-pipelines.universal");
            while (!removeRequest.IsCompleted)
                yield return null;
            Assert.AreEqual(StatusCode.Success, removeRequest.Status);
            yield return new WaitForDomainReload();
        }

#endif
    }
}
