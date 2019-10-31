using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class StickyNoteDataV0
    {
        [SerializeField]
        string m_Title = default;

        [SerializeField]
        string m_Content = default;

        [SerializeField]
        int m_TextSize = default;

        [SerializeField]
        int m_Theme = default;

        [SerializeField]
        Rect m_Position = default;

        [SerializeField]
        string m_GroupGuidSerialized = default;

        public string title => m_Title;

        public string content => m_Content;

        public int textSize => m_TextSize;

        public int theme => m_Theme;

        public Rect position => m_Position;

        public string groupGuidSerialized => m_GroupGuidSerialized;
    }
}
