using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    public class VFXBurstWithHistory : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            public float delay;
            public uint count;
        }

        class Pending
        {
            public VFXEventAttribute eventAttribute;
            public float delay;
            public uint count;
        }
        List<Pending> m_pending = new List<Pending>();

        static private readonly int delayID = Shader.PropertyToID("delay");
        static private readonly int countID = Shader.PropertyToID("count");

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            var pending = new Pending()
            {
                eventAttribute = new VFXEventAttribute(state.vfxEventAttribute),
                delay = vfxValues.GetFloat(delayID),
                count = vfxValues.GetUInt(countID)
            };

            m_pending.Add(pending);
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            for (int i = 0; i < m_pending.Count; i++)
            {
                m_pending[i].delay -= state.deltaTime;
            }

            for (int i = 0; i < m_pending.Count; i++)
            {
                if (m_pending[i].delay < 0)
                {
                    var execute = m_pending[i];
                    m_pending.RemoveAt(i);
                    state.vfxEventAttribute.CopyValuesFrom(execute.eventAttribute);
                    state.spawnCount += (float)execute.count;
                    break;
                }
            }
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            /* has no effect */
        }
    }
}
