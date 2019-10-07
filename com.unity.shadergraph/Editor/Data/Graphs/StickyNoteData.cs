using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class StickyNoteData : IJsonObject, IGroupItem
    {
        // TODO: Get rid of this
        [JsonProperty]
        [JsonUpgrade("m_GuidSerialized")]
        Guid m_Guid;

        public Guid guid => m_Guid;

        public Guid RewriteGuid()
        {
            m_Guid = Guid.NewGuid();
            return m_Guid;
        }

        [SerializeField]
        string m_Title;

        public string title
        {
            get => m_Title;
            set => m_Title = value;
        }

        [SerializeField]
        string m_Content;

        public string content
        {
            get => m_Content;
            set => m_Content = value;
        }

        [SerializeField]
        int m_TextSize;

        public int textSize
        {
            get => m_TextSize;
            set => m_TextSize = value;
        }

        [SerializeField]
        int m_Theme;

        public int theme
        {
            get => m_Theme;
            set => m_Theme = value;
        }

        [SerializeField]
        Rect m_Position;

        public Rect position
        {
            get => m_Position;
            set => m_Position = value;
        }

        [JsonProperty]
        public GroupData group { get; set; }

        public StickyNoteData(string title, string content, Rect position)
        {
            m_Guid = Guid.NewGuid();
            m_Title = title;
            m_Position = position;
            m_Content = content;
        }
    }
}
