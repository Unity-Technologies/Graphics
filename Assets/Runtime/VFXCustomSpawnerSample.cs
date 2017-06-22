using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    public class VFXCustomSpawnerSample : VFXSpawnerFunction
    {
        public class InputProperties
        {
            public float dummyX = 2;
            public float dummyY = 1;
            public Gradient dummyZ = new Gradient();
        }

        public override void OnStart(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
            var a = vfxValues.GetGradient("dummyZ");
            if (a != null)
            {
                state.spawnCount = 123.0f;
            }
            else
            {
                state.spawnCount = 456.0f;
            }
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }
    }
}
