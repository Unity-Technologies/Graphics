using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    public class VFXTemporaryConstantRateWithHistory : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            public float lifeTime;
            public uint rate;
        }

        class Current
        {
            public VFXEventAttribute eventAttribute;
            public float ageRemaining;
            public float spawnCountToConsume;
        }
        List<Current> m_current = new List<Current>();
        int frameIndex = 0;

        static private readonly int lifeTimeID = Shader.PropertyToID("lifeTime");
        static private readonly int rateID = Shader.PropertyToID("rate");

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            var pending = new Current()
            {
                eventAttribute = new VFXEventAttribute(state.vfxEventAttribute),
                ageRemaining = vfxValues.GetFloat(lifeTimeID),
                spawnCountToConsume = 0
            };

            m_current.Add(pending);
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            int i = 0;

            float rate = vfxValues.GetFloat(rateID);

            while (i < m_current.Count)
            {
                m_current[i].ageRemaining -= state.deltaTime;
                m_current[i].spawnCountToConsume += rate; //increase spawnCount for every alive system
                if (m_current[i].ageRemaining <= 0.0f)
                {
                    m_current.RemoveAt(i);
                    continue;
                }
                i++;
            }

            if (m_current.Count > 0)
            {
                int currentSpawnerIndex = frameIndex++ % m_current.Count;    //because we can't simulate multiple state in parallel, switch source on each frame
                var current = m_current[currentSpawnerIndex];
                state.vfxEventAttribute.CopyValuesFrom(current.eventAttribute);
                state.spawnCount += current.spawnCountToConsume;
                current.spawnCountToConsume = 0.0f;
            }
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            /* has no effect */
        }
    }
}
