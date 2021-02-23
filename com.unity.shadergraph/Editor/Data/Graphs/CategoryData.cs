using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class CategoryData : JsonObject
    {
        [SerializeField]
        string m_CategoryName;

        public string categoryName
        {
            get => m_CategoryName;
            set => m_CategoryName = value;
        }

        [SerializeField]
        GUID m_CategoryID;

        public GUID categoryID
        {
            get => m_CategoryID;
            set => m_CategoryID = value;
        }

        [SerializeField]
        List<GUID> m_ChildItemIDList;

        public List<GUID> childItemIDList
        {
            get => m_ChildItemIDList;
            set => m_ChildItemIDList = value;
        }

        [SerializeField]
        bool m_IsExpanded;

        public bool isExpanded
        {
            get => m_IsExpanded;
            set => m_IsExpanded = value;
        }
    }
}
