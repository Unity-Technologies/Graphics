using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXData : VFXModel
    {
        public abstract VFXDataType type { get; }

        public static VFXData CreateDataType(VFXDataType type)
        {
            switch (type)
            {
                case VFXDataType.kParticle:     return ScriptableObject.CreateInstance<VFXDataParticle>();
                case VFXDataType.kSpawnEvent:   return ScriptableObject.CreateInstance<VFXDataSpawnEvent>();
                default:                        return null;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_Owners == null)
                m_Owners = new List<VFXContext>();

            if (m_TestId == 0)
                m_TestId = UnityEngine.Random.Range(0, int.MaxValue);
        }

        // Never call this directly ! Only context must call this through SetData
        public void OnContextAdded(VFXContext context)
        {
            m_Owners.Add(context);
        }

        // Never call this directly ! Only context must call this through SetData
        public void OnContextRemoved(VFXContext context)
        {
            m_Owners.Remove(context);
        }

        [SerializeField]
        private List<VFXContext> m_Owners;

        //[NonSerialized]
        public int m_TestId;
    }
}
