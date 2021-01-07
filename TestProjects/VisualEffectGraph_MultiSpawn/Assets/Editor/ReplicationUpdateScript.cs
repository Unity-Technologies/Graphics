using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.VFX;
using UnityEngine.VFX;
using UnityEditor;
using UnityEngine;

//This asmdef has access to internal VisualEffectGraph, see PackageInfo.cs [assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests")]
namespace Test
{
    class Sample
    {
        [MenuItem("Edit/Visual Effects//Modify Replication Count (test)", priority = 321)]
        public static void ModifyReplicationCOunt()
        {
            //Retrieve VisualEffectASset
            var vfxAssets = new List<VisualEffectAsset>();
            var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset");
            foreach (var guid in vfxAssetsGuid)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.Contains("ReplicateUpdateScript_Data"))
                {
                    var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                    if (vfxAsset != null)
                        vfxAssets.Add(vfxAsset);
                }
            }

            foreach (var vfxAsset in vfxAssets)
            {
                var graph = vfxAsset.GetResource().GetOrCreateGraph();

                //Change the replication
                foreach (var spawn in graph.children.OfType<VFXBasicSpawner>())
                {
                    if (!spawn.inputFlowSlot[0].link.Any())
                        continue; //If there isn't any event plugged, we should skip this : OnPlay doesn't replicate to OnPlay_1, OnPlay_2, ...
                    spawn.SetSettingValue("replicationCount", (uint)32);
                }

                //Change the capacity
                foreach (var init in graph.children.OfType<VFXBasicInitialize>())
                {
                    init.SetSettingValue("capacity", (uint)128); //capacity is actually stored in VFXParticleData but this context supports a bridge.
                }

                //Change the bounding box value (which is store in a slot)
                foreach (var init in graph.children.OfType<VFXBasicInitialize>())
                {
                    var boundingBoxSlot = init.inputSlots.First(o => o.name == "bounds");
                    if (boundingBoxSlot.HasLink())
                        continue;

                    var bounds = (AABox)boundingBoxSlot.value;
                    bounds.size = new Vector3(1, 1, 1);
                    boundingBoxSlot.value = bounds;
                }

                //Actually, SetExpressionGraphDirty isn't needed if modifications has been done with a notification.
                graph.SetExpressionGraphDirty();
                graph.OnSaved();
            }

            AssetDatabase.SaveAssets();
        }
    }
}
