using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public class LoopAndDelay : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            public int NumLoops = 2;
            public float LoopDuration = 4.0f;
            public float Delay = 1.0f;
        }

        bool m_Waiting;
        float m_WaitTTL;
        int m_RemainingLoops = int.MinValue;

        static private readonly int numLoopsPropertyID = Shader.PropertyToID("NumLoops");
        static private readonly int loopDurationPropertyID = Shader.PropertyToID("LoopDuration");
        static private readonly int delayPropertyID = Shader.PropertyToID("Delay");

        public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }

        public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            if (m_RemainingLoops == int.MinValue)
                m_RemainingLoops = vfxValues.GetInt(numLoopsPropertyID);

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

                    if (m_WaitTTL < 0.0f)
                    {
                        m_Waiting = false;
                        state.totalTime = 0.0f;

                        if (m_RemainingLoops > 0) // if positive, remove one from count
                            m_RemainingLoops--;

                        if (m_RemainingLoops != 0) // if 0, stop forever
                            state.playing = true; // Re-enable the spawn context

                        m_RemainingLoops = Math.Max(-1, m_RemainingLoops); // sustain at -1 if in infinite mode
                    }
                    else
                    {
                        state.playing = false; // Stop the Spawn context for the duration of the delay
                    }
                }
            }
        }

        public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
