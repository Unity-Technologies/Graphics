using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    public class VFXBurstWithHistory : VFXSpawnerFunction
    {
        public class InputProperties
        {
            public float delay;
            public uint count;
        }

        class Pending
        {
            public Vector3 position; //hacky, should be directly the whole eventAttribute state => need a function to copy it
            public float delay;
            public uint count;
        }
        List<Pending> m_pending = new List<Pending>();

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VFXComponent vfxComponent)
        {
            var pending = new Pending()
            {
                position = state.vfxEventAttribute.GetVector3("position"), //hacky
                delay = vfxValues.GetFloat("delay"),
                count = vfxValues.GetUInt("count")
            };

            m_pending.Add(pending);
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VFXComponent vfxComponent)
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
                    state.vfxEventAttribute.SetVector3("position", execute.position);
                    state.spawnCount = (float)execute.count;
                    break;
                }
            }
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VFXComponent vfxComponent)
        {
            /* has no effect */
        }
    }
}
