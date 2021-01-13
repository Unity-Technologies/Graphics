using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    class VFXCustomSpawnerUpdateCounterTest : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
        }

        public static uint s_UpdateCount = 0u;
        public static float s_LastDeltaTime = 0.0f;
        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            if (state.deltaTime != 0.0f)
            {
                s_UpdateCount++;
                s_LastDeltaTime = state.deltaTime;
            }
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
