using System;
using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    class VFXCustomSpawnerRecordSpawnCount : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
        }

        public static List<int> s_ReceivedSpawnCount = new List<int>();

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            s_ReceivedSpawnCount.Add((int)state.vfxEventAttribute.GetFloat("spawnCount"));
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
