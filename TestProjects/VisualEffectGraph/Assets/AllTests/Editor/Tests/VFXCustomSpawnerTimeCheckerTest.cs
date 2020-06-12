using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    class VFXCustomSpawnerTimeCheckerTest : VFXSpawnerCallbacks
    {
        static public float s_ReadTotalTimeThroughInput = -571.0f;
        static public float s_ReadInternalTotalTime = -27.0f;

        public class InputProperties
        {
            public float totalTime = 86;
        }

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }


        static private int s_totalTimeID = Shader.PropertyToID("totalTime");
        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            s_ReadTotalTimeThroughInput = vfxValues.GetFloat(s_totalTimeID);
            s_ReadInternalTotalTime = state.totalTime;
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
