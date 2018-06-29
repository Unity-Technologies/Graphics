using System;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.Experimental.VFX;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXCompilePerformanceTest
    {
        //[Test]
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
            long[] newA = new long[vfxAssets.Count];
            for (int pass = 0; pass < processCount; ++pass)
                for (int i = 0; i < vfxAssets.Count; ++i)
                {
                    var graph = vfxAssets[i].GetResource().GetOrCreateGraph();

                    var filename = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(vfxAssets[i]));

                    var sw = Stopwatch.StartNew();
                    graph.SetExpressionGraphDirty();
                    VFXExpression.ClearCacheOfExpressions();
                    graph.RecompileIfNeeded();
                    sw.Stop();
                    newA[i] += sw.ElapsedMilliseconds;
                }

            UnityEngine.Debug.unityLogger.logEnabled = true;

            var debugLogList = new List<KeyValuePair<long, string>>();
            for (int i = 0; i < vfxAssets.Count; ++i)
            {
                var nameAsset = AssetDatabase.GetAssetPath(vfxAssets[i]);
                var res = string.Format("{0, -90} | second : {1}ms", nameAsset, newA[i] / processCount);
                debugLogList.Add(new KeyValuePair<long, string>(newA[i], res));
            }

            foreach (var log in debugLogList.OrderByDescending(o => o.Key))
            {
                UnityEngine.Debug.Log(log.Value);
            }
        }
    }
}
