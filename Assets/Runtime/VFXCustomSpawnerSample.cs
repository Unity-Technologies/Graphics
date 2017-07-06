using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    public class VFXCustomSpawnerSample : VFXSpawnerFunction //voir vrai test
    {
        public override void OnStart(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
            state.spawnCount = 123.0f;
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }
    }
}
