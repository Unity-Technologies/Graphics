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
    class VFXPerformance
    {
        [Test]
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

            //vfxAssets = vfxAssets.Where(o => AssetDatabase.GetAssetPath(o).Contains("/UnityLogo.vfx") || AssetDatabase.GetAssetPath(o).Contains("ParticleCountLimit")).ToList();

            //ReLoad all !
            foreach (var vfxAsset in vfxAssets)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(vfxAsset), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                vfxAsset.GetResource().GetOrCreateGraph();
            }

            long processCount = 8;

            VFXExpression.fakeHashSet.Clear();
            VFXExpression.use_FakeHasSet = false;
            long[] old = new long[vfxAssets.Count];
            for (int pass = 0; pass < processCount; ++pass)
                for (int i = 0; i < vfxAssets.Count; ++i)
                {
                    var graph = vfxAssets[i].GetResource().GetOrCreateGraph();
                    var sw = Stopwatch.StartNew();
                    graph.SetExpressionGraphDirty();
                    graph.RecompileIfNeeded();
                    sw.Stop();
                    old[i] += sw.ElapsedMilliseconds;
                }

            //ReLoad all !
            foreach (var vfxAsset in vfxAssets)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(vfxAsset), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                vfxAsset.GetResource().GetOrCreateGraph();
            }

            VFXExpression.fakeHashSet.Clear();
            VFXExpression.use_FakeHasSet = true;
            long[] newA = new long[vfxAssets.Count];
            for (int pass = 0; pass < processCount; ++pass)
                for (int i = 0; i < vfxAssets.Count; ++i)
                {
                    var graph = vfxAssets[i].GetResource().GetOrCreateGraph();
                    var sw = Stopwatch.StartNew();
                    graph.SetExpressionGraphDirty();
                    VFXExpression.fakeHashSet.Clear();
                    graph.RecompileIfNeeded();
                    sw.Stop();
                    newA[i] += sw.ElapsedMilliseconds;
                }

            long[] newB = new long[vfxAssets.Count];
            for (int pass = 0; pass < processCount; ++pass)
                for (int i = 0; i < vfxAssets.Count; ++i)
                {
                    var graph = vfxAssets[i].GetResource().GetOrCreateGraph();
                    var sw = Stopwatch.StartNew();
                    graph.SetExpressionGraphDirty();
                    graph.RecompileIfNeeded();
                    sw.Stop();
                    newB[i] += sw.ElapsedMilliseconds;
                }

            UnityEngine.Debug.unityLogger.logEnabled = true;
            //UnityEngine.Debug.ClearDeveloperConsole();

            var debugLogList = new List<KeyValuePair<long, string>>();
            for (int i = 0; i < vfxAssets.Count; ++i)
            {
                var nameAsset = AssetDatabase.GetAssetPath(vfxAssets[i]);

                var p_0 = ((double)newA[i] - (double)old[i]) / (double)old[i];
                var p_1 = ((double)newB[i] - (double)old[i]) / (double)old[i];

                var res = string.Format("{0, -90} | base : {1}ms(100%) | first : {2}ms({3}{4:P2}) | second : {5}ms({6}{7:P2})",
                                            nameAsset,
                                            old[i] / processCount,
                                            newA[i] / processCount, p_0 > 0 ? "+" : "", p_0,
                                            newB[i] / processCount, p_1 > 0 ? "+" : "", p_1);
                debugLogList.Add(new KeyValuePair<long, string>(old[i], res));
            }

            foreach (var log in debugLogList.OrderByDescending(o => o.Key))
            {
                UnityEngine.Debug.Log(log.Value);
            }

        }
    }
}
