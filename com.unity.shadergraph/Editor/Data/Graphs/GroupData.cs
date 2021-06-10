using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class GroupData : JsonObject
    {
        [SerializeField]
        string m_Title;

        public string title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        [SerializeField]
        Vector2 m_Position;

        public Vector2 position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public GroupData() : base() {}

        public GroupData(string title, Vector2 position)
        {
            m_Title = title;
            m_Position = position;
        }
    }
}
