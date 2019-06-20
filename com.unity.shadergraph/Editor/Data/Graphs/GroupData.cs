using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class GroupData : ISerializationCallbackReceiver
    {
        [NonSerialized]
        Guid m_Guid;

        public Guid guid
        {
            get { return m_Guid; }
        }

        [SerializeField]
        string m_GuidSerialized;

        [SerializeField]
        string m_Title;

        public string title
        {
            get{ return m_Title; }
            set { m_Title = value; }
        }

        [SerializeField]
        Vector2 m_Position;

        public Vector2 position
        {
            get{ return m_Position; }
            set { m_Position = value; }
        }

        public GroupData(string title, Vector2 position)
        {
            m_Guid = Guid.NewGuid();
            m_Title = title;
            m_Position = position;
        }

        public void OnBeforeSerialize()
        {
            m_GuidSerialized = guid.ToString();
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_GuidSerialized))
            {
                m_Guid = new Guid(m_GuidSerialized);
            }
        }
    }
}

