using System;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing
{
    public abstract class AbstractNodeInspector : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private string m_SerializedNodeGuid;

        private Guid m_NodeGuid;

        public Guid nodeGuid
        {
            get { return m_NodeGuid; }
        }

        public INode node { get; private set; }

        public virtual void OnInspectorGUI()
        {}

        public virtual void Initialize(SerializableNode node)
        {
            this.node = node;
            m_NodeGuid = node.guid;
        }

        public void OnBeforeSerialize()
        {
            m_SerializedNodeGuid = m_NodeGuid.ToString();
        }

        public void OnAfterDeserialize()
        {
            m_NodeGuid = new Guid(m_SerializedNodeGuid);
        }
    }
}
