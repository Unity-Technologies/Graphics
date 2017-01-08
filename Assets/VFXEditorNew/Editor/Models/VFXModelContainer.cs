using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    // Just a temp asset to hold a VFX graph
    [Serializable]
    class VFXModelContainer : ScriptableObject,  ISerializationCallbackReceiver
    {
        [NonSerialized]
        public List<VFXModel> m_Roots;

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializedRoots;

        public virtual void OnBeforeSerialize()
        {
            m_SerializedRoots = SerializationHelper.Serialize<VFXModel>(m_Roots);
        }

        public virtual void OnAfterDeserialize()
        {
            m_Roots = SerializationHelper.Deserialize<VFXModel>(m_SerializedRoots, null);
            m_SerializedRoots = null; // No need to keep it
        }

        void OnEnable()
        {
            if (m_Roots == null)
                m_Roots = new List<VFXModel>();
        }
    }
}
