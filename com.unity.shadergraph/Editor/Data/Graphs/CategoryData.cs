using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class CategoryData : JsonObject
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

        // We store Guids as a list out to the graph asset
        [SerializeField]
        List<Guid> m_ChildItemIDList;

        HashSet<Guid> m_ChildItemIDSet;
        // We expose Guids as a HashSet for faster existence checks
        public HashSet<Guid> childItemIDSet
        {
            get => m_ChildItemIDSet;
            set => m_ChildItemIDSet = value;
        }

        public CategoryData(string inName,  List<Guid> inChildItemIDList = null, Guid inCategoryGuid = new Guid())
        {
            name = inName;
            m_ChildItemIDList = inChildItemIDList;
            if (m_ChildItemIDList != null)
                m_ChildItemIDSet = new HashSet<Guid>(m_ChildItemIDList);
            else
                AssertHelpers.Fail("Category data provided invalid data for construction.");
            categoryGuid = inCategoryGuid;
        }
    }
}
