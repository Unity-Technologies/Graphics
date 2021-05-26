using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class StickyNoteData : JsonObject, IGroupItem, IRectInterface
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

        Rect IRectInterface.rect
        {
            get => position;
            set
            {
                position = value;
            }
        }

        [SerializeField]
        JsonRef<GroupData> m_Group = null;

        public GroupData group
        {
            get => m_Group;
            set
            {
                if (m_Group == value)
                    return;

                m_Group = value;
            }
        }


        public StickyNoteData() : base() {}
        public StickyNoteData(string title, string content, Rect position)
        {
            m_Title = title;
            m_Position = position;
            m_Content = content;
        }
    }
}
