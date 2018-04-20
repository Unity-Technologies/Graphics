using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public class LoopAndDelay : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            public uint NumLoops = 2;
            public float LoopDuration = 4.0f;
            public float Delay = 1.0f;
        }

        public bool waiting;
        public float waitTTL;
        public uint remainingLoops;

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            remainingLoops = vfxValues.GetUInt("NumLoops");
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            if (state.totalTime > vfxValues.GetFloat("LoopDuration"))
            {
                if (!waiting)
                {
                    waitTTL = vfxValues.GetFloat("Delay");
                    waiting = true;
                }
                else
                {
                    waitTTL -= state.deltaTime;

                    if (waitTTL < 0.0f && remainingLoops > 0)
                    {
                        waiting = false;
                        state.totalTime = 0.0f;
                        remainingLoops--;
                    }
                    else
                    {
                        //state.Stop();
                        // prevent spawning
                        state.deltaTime = 0.0f; // if late
                        state.spawnCount = 0; // if early
                    }
                }
            }
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
