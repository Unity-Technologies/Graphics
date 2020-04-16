using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class StickyNoteData : JsonObject, IGroupItem
    {

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

        [SerializeField]
        string m_GroupId;

        public string groupId
        {
            get { return m_GroupId; }
            set { m_GroupId = value; }
        }

        public StickyNoteData() : base() {}
        public bool groupIdIsEmpty => string.IsNullOrEmpty(m_GroupId) || m_GroupId.Equals(emptyObjectId);
        public StickyNoteData(string title, string content, Rect position)
        {
            m_Title = title;
            m_Position = position;
            m_Content = content;
            m_GroupId = emptyObjectId;
        }

    }
}

