using System;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXCompilePerformanceTest
    {
        //[Test]  //Not really a test but an helper to measure compilation time for every existing visual effect
        public void MesureCompilationTime()
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;
            var vfxAssets = new List<VisualEffectAsset>();
            var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset");
            foreach (var guid in vfxAssetsGuid)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                if (vfxAsset != null)
                {
                    vfxAssets.Add(vfxAsset);
                }
            }

            foreach (var vfxAsset in vfxAssets)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(vfxAsset), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                vfxAsset.GetResource().GetOrCreateGraph();
            }

            uint processCount = 8;
            long[] elapsedTime = new long[vfxAssets.Count];
            for (int pass = 0; pass < processCount; ++pass)
                for (int i = 0; i < vfxAssets.Count; ++i)
                {
                    var graph = vfxAssets[i].GetResource().GetOrCreateGraph();

                    var sw = Stopwatch.StartNew();
                    graph.SetExpressionGraphDirty();
                    VFXExpression.ClearCache();
                    graph.RecompileIfNeeded();
                    sw.Stop();

                    elapsedTime[i] += sw.ElapsedMilliseconds;
                }

            UnityEngine.Debug.unityLogger.logEnabled = true;

            var debugLogList = new List<KeyValuePair<long, string>>();
            for (int i = 0; i < vfxAssets.Count; ++i)
            {
                var nameAsset = AssetDatabase.GetAssetPath(vfxAssets[i]);
                var res = string.Format("{0, -90} | second : {1}ms", nameAsset, elapsedTime[i] / processCount);
                debugLogList.Add(new KeyValuePair<long, string>(elapsedTime[i], res));
            }

            foreach (var log in debugLogList.OrderByDescending(o => o.Key))
            {
                UnityEngine.Debug.Log(log.Value);
            }
        }

        [Test]  //Not really a test but an helper to measure compilation time for every existing 
        public void MesureBackupTime()
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;
            var vfxAssets = new List<VisualEffectAsset>();
            var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset");
            foreach (var guid in vfxAssetsGuid)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                if (vfxAsset != null)
                {
                    vfxAssets.Add(vfxAsset);
                }
            }

            //vfxAssets = vfxAssets.Take(1).ToList();

            var dependenciesPerAsset = new List<ScriptableObject[]>();
            foreach (var vfxAsset in vfxAssets)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(vfxAsset), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var graph = vfxAsset.GetResource().GetOrCreateGraph();
                var hashset = new HashSet<ScriptableObject>();
                hashset.Add(graph);
                graph.CollectDependencies(hashset);
                dependenciesPerAsset.Add(hashset.Cast<ScriptableObject>().ToArray());
            }

            var report = new List<string>();
            var levels = new[] { CompressionLevel.None, CompressionLevel.Fastest };
            foreach (var level in levels)
            {
                byte[][] data = new byte[vfxAssets.Count][];
                long[] elapsedTimeCompression = new long[vfxAssets.Count];
                long[] elapsedTimeDecompression = new long[vfxAssets.Count];
                uint processCount = 8;
                for (int pass = 0; pass < processCount; ++pass)
                {
                    for (int i = 0; i < vfxAssets.Count; ++i)
                    {
                        var sw = Stopwatch.StartNew();
                        var currentData = VFXMemorySerializer.StoreObjectsToByteArray(dependenciesPerAsset[i], level);
                        sw.Stop();
                        elapsedTimeCompression[i] += sw.ElapsedMilliseconds;
                        data[i] = currentData;
                    }
                }

                for (int pass = 0; pass < processCount; ++pass)
                {
                    for (int i = 0; i < vfxAssets.Count; ++i)
                    {
                        var sw = Stopwatch.StartNew();
                        VFXMemorySerializer.ExtractObjects(data[i], false);
                        sw.Stop();
                        elapsedTimeDecompression[i] += sw.ElapsedMilliseconds;
                    }
                }

                report.Add(level.ToString());
                report.Add("asset;size;compression;decompression");
                for (int i = 0; i < vfxAssets.Count; ++i)
                {
                    var nameAsset = AssetDatabase.GetAssetPath(vfxAssets[i]);
                    report.Add(string.Format("{0};{1}kb;{2}ms;{3}ms", nameAsset, data[i].Length / (1024), elapsedTimeCompression[i] / processCount, elapsedTimeDecompression[i] / processCount));
                }
            }
            UnityEngine.Debug.unityLogger.logEnabled = true;
            foreach (var log in report)
            {
                UnityEngine.Debug.Log(log);
            }
        }
    }
}
