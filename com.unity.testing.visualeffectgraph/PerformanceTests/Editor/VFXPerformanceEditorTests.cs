using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using static PerformanceTestUtils;
using static PerformanceMetricNames;
using UnityEngine.VFX;
using System.Reflection;

namespace UnityEditor.VFX.PerformanceTest
{
    public class VFXCompilePerformanceTests : EditorPerformanceTests
    {
        const int k_BuildTimeout = 10 * 60 * 1000;

        [Timeout(k_BuildTimeout), Version("1"), Test, Performance]
        public void MeasureLoadLibraryTime()
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;
            VFXLibrary.ClearLibrary();
            VFXLibrary.Load();
            UnityEngine.Debug.unityLogger.logEnabled = true;
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
        

        private static MethodInfo k_fnGetResource = typeof(VisualEffectObjectExtensions).GetMethod("GetResource");
        private static MethodInfo k_fnGetOrCreateGraph = typeof(VisualEffectResourceExtensions).GetMethod("GetOrCreateGraph");

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
        public void MeasureCompilationTime([ValueSource("allForceShaderValidation")] string forceShaderValidationModeName, [ValueSource("allCompilationMode")] string compilationModeName, [ValueSource("allVisualEffectAsset")] string vfxAssetPath)
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

        //Measure backup (for undo/redo & Duplicate) time for every existing asset
        [Timeout(k_BuildTimeout), Version("1"), Test, Performance]
        public void MeasureBackup([ValueSource("allVisualEffectAsset")] string vfxAssetPath)
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
    }
}
