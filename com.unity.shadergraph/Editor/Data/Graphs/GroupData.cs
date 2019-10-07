using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class GroupData : IJsonObject
    {
        // TODO: Get rid of this
        [JsonProperty]
        [JsonUpgrade("m_GuidSerialized")]
        public Guid legacyGuid { get; private set; }

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

        public GroupData() { }

        public GroupData(string title, Vector2 position)
        {
            m_Title = title;
            m_Position = position;
        }
    }
}

