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

        /// <summary>
        /// TODO: Just get rid of this cause JsonObjects already have objectID
        /// </summary>

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

        // TODO: Make this be a list of JsonRefs<ShaderInput> that point at the actual blackboard Items
        // That handles save/load automagically
        [SerializeField]
        List<string> m_ChildItemIDStringList;

        HashSet<Guid> m_ChildItemIDSet;
        // We expose Guids as a HashSet for faster existence checks
        public HashSet<Guid> childItemIDSet
        {
            get => m_ChildItemIDSet;
            set => m_ChildItemIDSet = value;
        }

        public void AddItemToCategory(Guid itemGUID)
        {
            m_ChildItemIDList.Add(itemGUID);
            m_ChildItemIDSet.Add(itemGUID);
        }

        public void RemoveItemFromCategory(Guid itemGUID)
        {
            if (m_ChildItemIDSet.Contains(itemGUID))
            {
                m_ChildItemIDList.Remove(itemGUID);
                m_ChildItemIDSet.Remove(itemGUID);
            }
        }

        public CategoryData()
        {
            name = String.Empty;
            m_ChildItemIDList = new List<Guid>();
            m_ChildItemIDSet = new HashSet<Guid>();
            categoryGuid = new Guid();
        }

        public override void OnBeforeSerialize()
        {
            m_ChildItemIDStringList = new List<string>();
            foreach (var guid in m_ChildItemIDList)
            {
                m_ChildItemIDStringList.Add(guid.ToString());
            }
            base.OnBeforeSerialize();
        }

        public CategoryData(string inName, Guid inCategoryGuid, List<Guid> inChildItemIDList = null)
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
