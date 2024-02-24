using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.IO;
using Object = UnityEngine.Object;

//Not strictly test, these helpers are used to upgrade project
namespace UnityEditor.VFX.Update
{
    [TestFixture]
    public class VFXUpdate
    {
        [UnityTest, Timeout(6000 * 1000)]
        public IEnumerator Upgrade_And_Save_VFX()
        {
            VFXAssetManager.Build(true);
            yield return null;
            AssetDatabase.SaveAssets();
            yield return null;
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
            AssetDatabase.ExportPackage(exportedPackageAssetList.ToArray(), absolutePath, ExportPackageOptions.Recurse);
            yield return null;
            Directory.Delete(expectedDirectory, true);
            File.Delete(expectedDirectory + ".meta");
            yield return null;
            Debug.unityLogger.logHandler = quietLog.m_ForwardLog;

            Assert.IsFalse(Directory.Exists(expectedDirectory));
        }
#endif
    }
}
