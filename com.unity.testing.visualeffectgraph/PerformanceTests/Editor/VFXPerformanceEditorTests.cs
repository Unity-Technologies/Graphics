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

namespace UnityEditor.VFX.PerformanceTest
{
    public class VFXCompilePerformanceTests : EditorPerformanceTests
    {
        const int k_BuildTimeout = 10 * 60 * 1000;

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

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(fullPath);

            var resource = k_fnGetResource.Invoke(null, new object[] { vfxAsset });
            if (resource == null)
            {
                graph = null;
            }

            graph = k_fnGetOrCreateGraph.Invoke(null, new object[] { resource }) as VFXGraph;
        }

        static readonly string[] allForceShaderValidation = { "ShaderValidation_On", "ShaderValidation_Off" };

        [Timeout(k_BuildTimeout), Version("1"), Test, Performance]
        public void Load_VFXLibrary()
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;
            VFXLibrary.ClearLibrary();
            VFXLibrary.Load();
            UnityEngine.Debug.unityLogger.logEnabled = true;
        }

        [Timeout(k_BuildTimeout), Version("1"), UnityTest, Performance]
        public IEnumerator VFXViewWindow_Open_And_Render([ValueSource("allVisualEffectAsset")] string vfxAssetPath)
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;

            VFXGraph graph;
            string fullPath;
            LoadVFXGraph(vfxAssetPath, out fullPath, out graph);

            var window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Show();
            window.maximized = true;
            window.autoCompile = false;
            window.Repaint();

            yield return null;
            var asset = k_fnGetAsset.Invoke(graph.visualEffectResource, new object[] { }) as VisualEffectAsset;
            window.LoadAsset(asset, null);
            window.graphView.FrameAll();

            for (int i = 0; i < 16; ++i) //Render n frames
            {
                window.Repaint();
                yield return null;
            }

            window.Close();
            yield return null; //Ensure window is closed for next test

            UnityEngine.Debug.unityLogger.logEnabled = true;
        }

        //Measure backup (for undo/redo & Duplicate) time for every existing asset
        [Timeout(k_BuildTimeout), Version("1"), Test, Performance]
        public void Backup_And_Restore([ValueSource("allVisualEffectAsset")] string vfxAssetPath)
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;

            VFXGraph graph;
            string fullPath;
            LoadVFXGraph(vfxAssetPath, out fullPath, out graph);

            if (graph)
            {
                var backup = graph.Backup();
                graph.Restore(backup);
                AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }

            UnityEngine.Debug.unityLogger.logEnabled = true;
        }

        [Timeout(k_BuildTimeout), Version("1"), Test, Performance]
        public void Compilation([ValueSource("allForceShaderValidation")] string forceShaderValidationModeName, [ValueSource("allCompilationMode")] string compilationModeName, [ValueSource("allVisualEffectAsset")] string vfxAssetPath)
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;

            VFXCompilationMode compilationMode;
            Enum.TryParse<VFXCompilationMode>(compilationModeName, out compilationMode);

            bool forceShaderValidationMode = forceShaderValidationModeName == "ShaderValidation_On";

            VFXGraph graph;
            string fullPath;
            LoadVFXGraph(vfxAssetPath, out fullPath, out graph);
            if (graph)
            {
                VFXExpression.ClearCache();
                graph.SetExpressionGraphDirty();
                graph.SetForceShaderValidation(forceShaderValidationMode, false);
                graph.SetCompilationMode(compilationMode, false);
                AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }

            UnityEngine.Debug.unityLogger.logEnabled = true;
        }
    }
}
