using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class CategoryData : JsonObject
    {
        [SerializeField]
        string m_Name;

        public string name
        {
            get => m_Name;
            set => m_Name = value;
        }

        [SerializeField]
        Guid m_CategoryGuid;

        public Guid categoryGuid
        {
            get => m_CategoryGuid;
            set => m_CategoryGuid = value;
        }

        [SerializeField]
        List<Guid> m_ChildItemIDList;

        public List<Guid> childItemIDList
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

        public CategoryData(string inName,  List<Guid> inChildItemIDList = null, Guid inCategoryGuid = new Guid())
        {
            name = inName;
            childItemIDList = inChildItemIDList;
            categoryGuid = inCategoryGuid;
        }
    }
}
