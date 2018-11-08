using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public class LoopAndDelay : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            [Tooltip("Number of Loops (< 0 for infinite), evaluated when Context Start is hit")]
            public int LoopCount = 1;
            [Tooltip("Duration of one loop, evaluated every loop")]
            public float LoopDuration = 4.0f;
            [Tooltip("Duration of in-between delay (after each loop), evaluated every loop")]
            public float Delay = 1.0f;
        }

        bool m_Waiting;
        bool m_Playing;

        float m_WaitTTL;
        int m_RemainingLoops;
        int m_LoopCount;
        float m_Duration;

        static private readonly int loopCountPropertyID = Shader.PropertyToID("LoopCount");
        static private readonly int loopDurationPropertyID = Shader.PropertyToID("LoopDuration");
        static private readonly int delayPropertyID = Shader.PropertyToID("Delay");

        public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            // Evaluate Loop Count only when hitting start;
            // LoopCount < 0 means infinite mode
            // LoopCount == 0 means no spawn

            m_LoopCount = vfxValues.GetInt(loopCountPropertyID);

            if(m_LoopCount != 0)
            {
                m_RemainingLoops = m_LoopCount - 1;
                m_Duration = vfxValues.GetFloat(loopDurationPropertyID);
                m_Playing = true;
                m_Waiting = false;
            }
            else // no loops, no play
            {
                m_Playing = false;
                state.playing = false;
            }
        }

        public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            if (state.totalTime > m_Duration && m_Playing) // When we are past the loop duration (and we need to loop)...
            {
                if (!m_Waiting) // We need to wait
                {
                    m_WaitTTL = vfxValues.GetFloat(delayPropertyID); // Fetch Value for this loop
                    m_Waiting = true;
                    state.playing = false; // Stop the Spawn context for the duration of the delay
                }
                else // If we are in a wait loop....
                {
                    // Countdown... 
                    m_WaitTTL -= state.deltaTime;

                    // ....until delay expired
                    if (m_WaitTTL < 0.0f)
                    {
                        if (m_RemainingLoops > 0 || m_LoopCount < 0) // if remaining loops (or infinite), restart a loop
                        {
                            if (m_LoopCount >= 0) // only process remaining loops if we have a positive loop count
                                m_RemainingLoops--;

                            // ...Then restart a spawn loop
                            m_Waiting = false;
                            state.totalTime = 0.0f;
                            state.playing = true; // Re-enable the spawn context
                            m_Duration = vfxValues.GetFloat(loopDurationPropertyID); // Recompute a loop duration
                        }
                        else
                        {
                            m_Playing = false;
                        }

                    }
                }
            }
        }

        public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            m_Playing = false;
        }
    }
}
