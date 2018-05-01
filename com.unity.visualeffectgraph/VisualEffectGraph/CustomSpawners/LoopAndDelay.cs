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

        bool m_Waiting;
        float m_WaitTTL;
        uint m_RemainingLoops;

        static private readonly int numLoopsPropertyID = Shader.PropertyToID("NumLoops");
        static private readonly int loopDurationPropertyID = Shader.PropertyToID("LoopDuration");
        static private readonly int delayPropertyID = Shader.PropertyToID("Delay");

        public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            m_RemainingLoops = vfxValues.GetUInt(numLoopsPropertyID);
        }

        public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            if (!state.playing) return;

            if (state.totalTime > vfxValues.GetFloat(loopDurationPropertyID))
            {
                if (!m_Waiting)
                {
                    m_WaitTTL = vfxValues.GetFloat(delayPropertyID);
                    m_Waiting = true;
                }
                else
                {
                    m_WaitTTL -= state.deltaTime;

                    if (m_WaitTTL < 0.0f && m_RemainingLoops > 0)
                    {
                        m_Waiting = false;
                        state.totalTime = 0.0f;
                        m_RemainingLoops--;
                    }
                    else
                    {
                        state.playing = false; // Stop the Spawn context
                    }
                }
            }
        }

        public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
