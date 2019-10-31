using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class MasterNodeV0
    {
        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableSubShaders = default;

        public List<SerializationHelper.JSONSerializedElement> subShaders => m_SerializableSubShaders;
    }
}
