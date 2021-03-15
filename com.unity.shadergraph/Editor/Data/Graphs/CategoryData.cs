using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
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

        [SerializeField]
        List<JsonRef<ShaderInput>> m_children;

        public RefValueEnumerable<ShaderInput> Children => m_children.SelectValue(); 

        public void AddItemToCategory(ShaderInput shaderInput)
        {
            m_children.Add(shaderInput);
        }

        public void RemoveItemFromCategory(ShaderInput shaderInput)
        {
            foreach(var child in Children)
            {
                if(child == shaderInput)
                {
                    m_children.Remove(shaderInput);
                }
            }
        }

        public CategoryData()
        {
            name = String.Empty;
            m_children = new List<JsonRef<ShaderInput>>();
            categoryGuid = new Guid();
        }

        public CategoryData(string inName, List<ShaderInput> inChildItems)
        {
            name = inName;
            m_children = new List<JsonRef<ShaderInput>>();
            foreach(var child in inChildItems)
            {
                m_children.Add(child);
            }
        }
    }
}
