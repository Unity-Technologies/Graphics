using System;
using System.Collections.Generic;
using System.Collections;

using NUnit.Framework;

using Unity.PerformanceTesting;
using UnityEngine.TestTools.Graphics.Performance;
using UnityEngine.TestTools.Graphics.Performance.Editor;
using UnityEngine.VFX;
using UnityEditor.VFX.UI;
using UnityEngine.TestTools;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX.PerformanceTest
{
    public class VFXCompilePerformanceTests : EditorPerformanceTests
    {
        const int k_BuildTimeout = 600 * 1000;
        const string k_Version = "3";

        private bool m_PreviousAsyncShaderCompilation;
        [OneTimeSetUp]
        public void Init()
        {
            m_PreviousAsyncShaderCompilation = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;
        }

        [OneTimeTearDown]
        public void Clear()
        {
            EditorSettings.asyncShaderCompilation = m_PreviousAsyncShaderCompilation;
        }

        static IEnumerable<string> allVisualEffectAsset
        {
            get
            {
                var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset");
                foreach (var guid in vfxAssetsGuid)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    yield return System.IO.Path.GetFileNameWithoutExtension(assetPath);
                }
            }
        }

        //Expecting only one value in this field in editor mode, it's actually a dummy variadic parameter to help filter in observer database
        static IEnumerable<string> allActiveSRP
        {
            get
            {
                yield return UnityEngine.VFX.PerformanceTest.VFXPerformanceUseGraphicsTestCasesAttribute.GetPrefix();
            }
        }

        static IEnumerable<string> allCompilationMode
        {
            get
            {
                foreach (var mode in Enum.GetValues(typeof(VFXCompilationMode)))
                    yield return mode.ToString();
            }
        }

        private static void LoadVFXGraph(string vfxAssetName, out string fullPath, out VFXGraph graph)
        {
            using (Measure.Scope("VFXGraphLoad.FindAsset"))
            {
                var vfxAssetsGuids = new List<string>(AssetDatabase.FindAssets("t:VisualEffectAsset " + vfxAssetName));
                vfxAssetsGuids.RemoveAll(guid => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) != vfxAssetName);
                if (vfxAssetsGuids.Count != 1)
                    throw new InvalidOperationException("Cannot retrieve (or several asset with same name) " +
                                                        vfxAssetName);
                var vfxAssetsGuid = vfxAssetsGuids[0];
                fullPath = AssetDatabase.GUIDToAssetPath(vfxAssetsGuid);
            }

            using (Measure.Scope("VFXGraphLoad.ImportAsset"))
            {
                AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }

            VisualEffectAsset vfxAsset;
            using (Measure.Scope("VFXGraphLoad.LoadAsset"))
            {
                vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(fullPath);
            }

            using (Measure.Scope("VFXGraphLoad.GetResource"))
            {
                var resource = vfxAsset.GetResource();
                graph = resource.GetOrCreateGraph();
            }
        }

        [Timeout(k_BuildTimeout), Version(k_Version), UnityTest, Performance]
        public IEnumerator Reference_Benchmark_Disk([ValueSource(nameof(allActiveSRP))] string srp)
        {
            GC.Collect();
            for (int i = 0; i < 4; i++)
                yield return null;

            using (Measure.Scope("Reference_Benchmark_Disk"))
            {
                var kPath = "Assets/Reference_Benchmark_Disk.txt";
                var write = new string('A', 1024 * 1024 * 128);
                File.WriteAllText(kPath, write);
                var read = File.ReadAllText(kPath);
                Assert.IsTrue(read.EndsWith('A'));
                File.Delete(kPath);
            }
            yield return null;
        }

        [Timeout(k_BuildTimeout), Version(k_Version), UnityTest, Performance]
        public IEnumerator Reference_Benchmark_CPU([ValueSource(nameof(allActiveSRP))] string srp)
        {
            GC.Collect();
            for (int i = 0; i < 4; i++)
                yield return null;

            using (Measure.Scope("Reference_Benchmark_CPU"))
            {
                var kCount = 32768;
                var random = new System.Random(0x123);
                var data = new int[kCount];
                for (int i = 0; i < kCount; i++)
                    data[i] = random.Next(0, int.MaxValue);

                for (int j = data.Length - 1; j > 0; j--)
                    for (int i = 0; i < j; i++)
                        if (data[i] > data[i + 1])
                            (data[i], data[i + 1]) = (data[i + 1], data[i]);

                for (int i = 0; i < data.Length - 1; i++)
                    Assert.GreaterOrEqual(data[i + 1], data[i]);
            }

            yield return null;
        }

        [Timeout(k_BuildTimeout), Version(k_Version), UnityTest, Performance]
        public IEnumerator VFXViewWindow_Open_And_Render([ValueSource(nameof(allActiveSRP))] string srp, [ValueSource(nameof(allVisualEffectAsset))] string vfxAssetPath)
        {
            VFXGraph graph;
            LoadVFXGraph(vfxAssetPath, out var fullPath, out graph);

            var sceneView = SceneView.GetWindow(typeof(SceneView));
            sceneView.position = new Rect(0, 0, 1600, 900);
            Assert.IsFalse(sceneView.docked);

            using (Measure.Scope("VFXViewWindow.Main"))
            {
                VFXViewWindow window = null;
                using (Measure.Scope("VFXViewWindow.Show"))
                {
                    window = VFXViewWindow.GetWindow(graph, true, true);
                    yield return null;
                }

                using (Measure.Scope("VFXViewWindow.LoadAsset"))
                {
                    var asset = graph.visualEffectResource.asset;
                    window.LoadAsset(asset, null);
                }
                using (Measure.Scope("VFXViewWindow.FirstFrames"))
                {
                    window.graphView.FrameAll();
                    for (int i = 0; i < 4; ++i)
                        yield return null;
                }

                using (Measure.Scope("VFXViewWindow.Render"))
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        var position = window.graphView.viewTransform.position;
                        position.x += i % 2 == 1 ? 3.0f : -3.0f;
                        window.graphView.viewTransform.position = position;
                        window.Repaint();
                        yield return null;
                    }
                }

                using (Measure.Scope("VFXViewWindow.ExplicitCompile"))
                {
                    window.graphView.Compile();
                    for (int i = 0; i < 4; ++i)
                        yield return null;
                }

                VFXViewWindow emptyVFXWindow;
                using (Measure.Scope("VFXViewWindow.SwitchTab"))
                {
                    emptyVFXWindow = VFXViewWindow.GetWindow((VFXGraph)null, true, true);
                    window.ShowTab();
                    for (int i = 0; i < 4; ++i)
                        yield return null;
                }

                using (Measure.Scope("VFXViewWindow.CloseDummyTab"))
                {
                    emptyVFXWindow.Close();
                    for (int i = 0; i < 4; ++i)
                        yield return null;
                }

                using (Measure.Scope("VFXViewWindow.Close"))
                {
                    window.Close();
                    for (int i = 0; i < 4; ++i)
                        yield return null; //Ensure window is closed for next test
                }
            }
        }

        private const uint kRepeatCount = 1;

        //Measure backup (for undo/redo & Duplicate) time for every existing asset
        [Timeout(k_BuildTimeout), Version(k_Version), Test, Performance]
        public void Backup_And_Restore([ValueSource(nameof(allActiveSRP))] string srp, [ValueSource(nameof(allVisualEffectAsset))] string vfxAssetPath)
        {
            VFXGraph graph;
            string fullPath;
            LoadVFXGraph(vfxAssetPath, out fullPath, out graph);

            if (graph)
            {
                for (int i = 0; i < kRepeatCount; ++i)
                {
                    using (Measure.Scope("VFXGraph.Backup_And_Restore.Main"))
                    {
                        object backup = null;
                        using (Measure.Scope("VFXGraph.Backup"))
                        {
                            backup = graph.Backup();
                        }

                        using (Measure.Scope("VFXGraph.Restore"))
                        {
                            graph.Restore(backup);
                        }

                        using (Measure.Scope("VFXGraph.Reimport"))
                        {
                            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                        }
                    }
                }
            }
        }

        static readonly string[] allForceShaderValidation = { "ShaderValidation_On", "ShaderValidation_Off" };
        [Timeout(k_BuildTimeout), Version(k_Version), Test, Performance]
        public void Compilation([ValueSource(nameof(allActiveSRP))] string srp, /*[ValueSource("allForceShaderValidation")] string forceShaderValidationModeName,*/ [ValueSource("allCompilationMode")] string compilationModeName, [ValueSource("allVisualEffectAsset")] string vfxAssetPath)
        {
            VFXCompilationMode compilationMode;
            Enum.TryParse<VFXCompilationMode>(compilationModeName, out compilationMode);

            //This compilation test shouldn't measure shader compilation time, furthermore, the first variant isn't always relevant.
            bool forceShaderValidationMode = false;

            VFXGraph graph;
            string fullPath;
            LoadVFXGraph(vfxAssetPath, out fullPath, out graph);
            if (graph)
            {
                for (int i = 0; i < kRepeatCount; ++i)
                {
                    using (Measure.Scope("VFXGraph.Compile.Main"))
                    {
                        VFXExpression.ClearCache();
                        graph.SetExpressionGraphDirty();
                        graph.SetForceShaderValidation(forceShaderValidationMode, false);
                        graph.SetCompilationMode(compilationMode, false);
                        AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    }
                }

                var resource = graph.GetResource();
                if (resource)
                {
                    var totalShaderLength = 0u;
                    var shaderSourceCount = resource.GetShaderSourceCount();
                    for (int shaderIndex = 0; shaderIndex < shaderSourceCount; ++shaderIndex)
                    {
                        var source = resource.GetShaderSource(shaderIndex);
                        totalShaderLength += (uint)source.Length;
                    }

                    var resourceObject = new SerializedObject(resource.visualEffectObject);
                    resourceObject.Update();
                    var infos = resourceObject.FindProperty("m_Infos");
                    var expressionsContainer = infos.FindPropertyRelative("m_Expressions");
                    var expressionArray = expressionsContainer.FindPropertyRelative("m_Expressions");
                    var expressionSize = expressionArray.arraySize * 6 * 4;

                    Measure.Custom(new SampleGroup("ShaderSize", SampleUnit.Byte, false), totalShaderLength);
                    Measure.Custom(new SampleGroup("ExpressionSize", SampleUnit.Byte, false), expressionSize);
                }
            }
        }

        enum ImportPackageStatus
        {
            None,
            Started,
            Failed,
            Cancelled,
            Completed
        }

        private static ImportPackageStatus s_ImportPackageStatus = ImportPackageStatus.None;
        private static string s_LastErrorMessage = string.Empty;

        private static void OnImportPackageStarted(string packagename)
        {
            s_ImportPackageStatus = ImportPackageStatus.Started;
        }

        private static void OnImportPackageCancelled(string packageName)
        {
            s_ImportPackageStatus = ImportPackageStatus.Cancelled;
        }

        private static void OnImportPackageFailed(string packagename, string errormessage)
        {
            s_ImportPackageStatus = ImportPackageStatus.Failed;
            s_LastErrorMessage = errormessage;
        }

        private static void OnImportPackageCompleted(string packagename)
        {
            s_ImportPackageStatus = ImportPackageStatus.Completed;
        }

        //Simply redirect all error to default log instead of stopping the test (the performance test isn't testing validity of this package)
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

        [Timeout(k_BuildTimeout * 10), UnityTest, Version(k_Version), Performance]
        public IEnumerator ImportManyVFX([ValueSource(nameof(allActiveSRP))] string srp)
        {
            var quietLog = new QuietLogHandler()
            {
                m_ForwardLog = Debug.unityLogger.logHandler
            };

            Debug.unityLogger.logHandler = quietLog;

            var expectedDirectory = "Assets/Repro_Many_Assets";

            if (Directory.Exists(expectedDirectory))
                Directory.Delete(expectedDirectory, true);

            AssetDatabase.importPackageStarted += OnImportPackageStarted;
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;

            using (Measure.Scope("ImportManyVFX.Main"))
            {
                AssetDatabase.ImportPackage("Packages/com.unity.testing.visualeffectgraph/PerformanceTests/Editor/Benchmark_ManyVFX.unitypackage", false);
                while (s_ImportPackageStatus != ImportPackageStatus.Cancelled &&
                       s_ImportPackageStatus != ImportPackageStatus.Completed)
                    yield return null;
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            AssetDatabase.importPackageStarted -= OnImportPackageStarted;
            AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
            AssetDatabase.importPackageFailed -= OnImportPackageFailed;
            AssetDatabase.importPackageCancelled -= OnImportPackageCancelled;

            Assert.IsTrue(Directory.Exists(expectedDirectory));
            Directory.Delete(expectedDirectory, true);
            File.Delete(expectedDirectory + ".meta");
            Debug.unityLogger.logEnabled = true;

            Assert.IsTrue(s_ImportPackageStatus == ImportPackageStatus.Completed, s_LastErrorMessage);
            s_ImportPackageStatus = ImportPackageStatus.None;

            Debug.unityLogger.logHandler = quietLog.m_ForwardLog;
        }

    }
}
