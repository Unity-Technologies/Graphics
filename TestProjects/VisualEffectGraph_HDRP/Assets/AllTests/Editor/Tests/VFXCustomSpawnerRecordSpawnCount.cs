using System;
using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    //Warning: This class is only used for editor test purpose
    class VFXCustomSpawnerRecordSpawnCount : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
        }

        private static List<int> s_ReceivedSpawnCount = new List<int>();

        public static IEnumerable<int> GetReceivedSpawnCount()
        {
            return s_ReceivedSpawnCount;
        }

        public static void ClearReceivedSpawnCount()
        {
            s_ReceivedSpawnCount.Clear();
        }

        private static readonly int kSpawnCount = Shader.PropertyToID("spawnCount");

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            s_ReceivedSpawnCount.Add((int)state.vfxEventAttribute.GetFloat(kSpawnCount));
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
