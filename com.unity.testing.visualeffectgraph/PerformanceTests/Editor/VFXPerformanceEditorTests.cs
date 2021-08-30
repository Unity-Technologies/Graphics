using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using static PerformanceTestUtils;
using static PerformanceMetricNames;
using UnityEngine.VFX;
using System.Reflection;
using UnityEditor.VFX.UI;
using UnityEngine.TestTools;
using System.Collections;
using UnityEngine;

namespace UnityEditor.VFX.PerformanceTest
{
    public class VFXCompilePerformanceTests : EditorPerformanceTests
    {
        const int k_BuildTimeout = 600 * 1000;

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

        static IEnumerable<string> allCompilationMode
        {
            get
            {
                foreach (var mode in Enum.GetValues(typeof(VFXCompilationMode)))
                    yield return mode.ToString();
            }
        }

        private static Type GetVisualEffectResourceType()
        {
            var visualEffectResouceType = AppDomain.CurrentDomain.GetAssemblies().Select(o => o.GetType("UnityEditor.VFX.VisualEffectResource"))
                                                                    .Where(o => o != null)
                                                                    .FirstOrDefault();
            return visualEffectResouceType;
        }

        private static MethodInfo k_fnGetResource = typeof(VisualEffectObjectExtensions).GetMethod("GetResource");
        private static MethodInfo k_fnGetOrCreateGraph = typeof(VisualEffectResourceExtensions).GetMethod("GetOrCreateGraph");
        private static MethodInfo k_fnGetAsset = GetVisualEffectResourceType().GetProperty("asset").GetMethod;

        private static void LoadVFXGraph(string vfxAssetName, out string fullPath, out VFXGraph graph)
        {
            var vfxAssets = new List<VisualEffectAsset>();
            var vfxAssetsGuids = AssetDatabase  .FindAssets("t:VisualEffectAsset " + vfxAssetName)
                                                .Where(o => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(o)) == vfxAssetName);
            if (vfxAssetsGuids.Count() != 1)
                throw new InvalidOperationException("Cannot retrieve (or several asset with same name) " + vfxAssetName);

            var vfxAssetsGuid = vfxAssetsGuids.First();
            fullPath = AssetDatabase.GUIDToAssetPath(vfxAssetsGuid);

            using (Measure.Scope("VFXGraphLoad.ImportAsset"))
            {
                AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }

            VisualEffectAsset vfxAsset = null;
            using (Measure.Scope("VFXGraphLoad.LoadAsset"))
            {
                vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(fullPath);
            }

            using (Measure.Scope("VFXGraphLoad.GetResource"))
            {
                var resource = k_fnGetResource.Invoke(null, new object[] { vfxAsset });
                if (resource == null)
                {
                    graph = null;
                }
                graph = k_fnGetOrCreateGraph.Invoke(null, new object[] { resource }) as VFXGraph;
            }
        }

        [Timeout(k_BuildTimeout), Version("1"), Test, Performance]
        public void Load_VFXLibrary()
        {
            for (int i = 0; i < 16; i++) //Doing this multiple time to have an average result
            {
                VFXLibrary.ClearLibrary();
                using (Measure.Scope("VFXLibrary.Load"))
                {
                    VFXLibrary.Load();
                }
            }
        }

        [Timeout(k_BuildTimeout), Version("1"), UnityTest, Performance]
        public IEnumerator VFXViewWindow_Open_And_Render([ValueSource("allVisualEffectAsset")] string vfxAssetPath)
        {
            VFXGraph graph;
            string fullPath;
            LoadVFXGraph(vfxAssetPath, out fullPath, out graph);

            VFXViewWindow window = null;
            using (Measure.Scope("VFXViewWindow.Show"))
            {
                window = EditorWindow.GetWindow<VFXViewWindow>();
                window.Show();
                window.position = new UnityEngine.Rect(0, 0, 1600, 900);
                window.autoCompile = false;
                window.Repaint();
            }
            yield return null;

            using (Measure.Scope("VFXViewWindow.LoadAsset"))
            {
                var asset = k_fnGetAsset.Invoke(graph.visualEffectResource, new object[] { }) as VisualEffectAsset;
                window.LoadAsset(asset, null);
                window.graphView.FrameAll();
            }

            for (int i = 0; i < 8; ++i) //Render n frames
            {
                var position = window.graphView.viewTransform.position;
                position.x += i%2 == 1 ? 3.0f : -3.0f;
                window.graphView.viewTransform.position = position;
                window.Repaint();
                yield return Measure.Frames().SampleGroup("VFXViewWindow.Render").MeasurementCount(4).Run();
            }

            using (Measure.Scope("VFXViewWindow.Close"))
            {
                window.Close();
                yield return null; //Ensure window is closed for next test
            }
        }

        //Measure backup (for undo/redo & Duplicate) time for every existing asset
        [Timeout(k_BuildTimeout), Version("1"), Test, Performance]
        public void Backup_And_Restore([ValueSource("allVisualEffectAsset")] string vfxAssetPath)
        {
            VFXGraph graph;
            string fullPath;
            LoadVFXGraph(vfxAssetPath, out fullPath, out graph);

            if (graph)
            {
                for (int i = 0; i < 4; ++i)
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

        static readonly string[] allForceShaderValidation = { "ShaderValidation_On", "ShaderValidation_Off" };
        [Timeout(k_BuildTimeout), Version("1"), Test, Performance]
        public void Compilation(/*[ValueSource("allForceShaderValidation")] string forceShaderValidationModeName,*/ [ValueSource("allCompilationMode")] string compilationModeName, [ValueSource("allVisualEffectAsset")] string vfxAssetPath)
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
                for (int i = 0; i < 4; ++i)
                {
                    using (Measure.Scope("VFXGraph.Compile"))
                    {
                        VFXExpression.ClearCache();
                        graph.SetExpressionGraphDirty();
                        graph.SetForceShaderValidation(forceShaderValidationMode, false);
                        graph.SetCompilationMode(compilationMode, false);
                        AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    }
                }
            }
        }
    }
}
