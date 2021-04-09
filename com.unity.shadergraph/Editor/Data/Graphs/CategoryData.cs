using System;
using System.Collections.Generic;
using System.Linq;
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
        public string categoryGuid => this.objectId;

        [SerializeField]
        List<JsonRef<ShaderInput>> m_ChildObjectList = new List<JsonRef<ShaderInput>>();


        public RefValueEnumerable<ShaderInput> Children => m_ChildObjectList.SelectValue();

        HashSet<string> m_ChildObjectIDSet = new HashSet<string>();
        // We expose the object list as a HashSet of their objectIDs for faster existence checks
        public HashSet<string> childObjectIDSet
        {
            get => m_ChildObjectIDSet;
            set => m_ChildObjectIDSet = value;
        }
        public int childCount => m_ChildObjectIDSet.Count;

        public void AddItemToCategory(ShaderInput itemToAdd)
        {
            m_ChildObjectList.Add(itemToAdd);
            m_ChildObjectIDSet.Add(itemToAdd.objectId);
        }

        public void RemoveItemFromCategory(ShaderInput itemToRemove)
        {
            m_ChildObjectList.Remove(itemToRemove);
            if (m_ChildObjectIDSet.Contains(itemToRemove.objectId))
                m_ChildObjectIDSet.Remove(itemToRemove.objectId);
        }

        public void MoveItemInCategory(ShaderInput itemToMove, int newIndex)
        {
            m_ChildObjectList.Remove(itemToMove);
            m_ChildObjectList.Insert(newIndex,itemToMove);
            // Recreate the hash-set as the data structure does not allow for moving/inserting elements
            m_ChildObjectIDSet.Clear();
            foreach (var childObject in m_ChildObjectList)
            {
                m_ChildObjectIDSet.Add(childObject.value.objectId);
            }
        }

        public bool IsItemInCategory(ShaderInput itemToCheck)
        {
            return m_ChildObjectIDSet.Contains(itemToCheck.objectId);
        }

        public bool IsNamedCategory()
        {
            return name != String.Empty;
        }

        public override void OnAfterDeserialize()
        {
            if (m_ChildObjectList != null)
            {
                foreach (var childObject in m_ChildObjectList.ToList())
                {
                    if (childObject.value != null)
                        m_ChildObjectIDSet.Add(childObject.value.objectId);
                    else
                        m_ChildObjectList.Remove(childObject);
                }
            }

            base.OnAfterDeserialize();
        }

        public CategoryData()
        {
            foreach (var childObject in m_ChildObjectList)
            {
                m_ChildObjectIDSet.Add(childObject.value.objectId);
            }
        }

        public CategoryData(string inName, List<ShaderInput> inChildObjectList = null)
        {
            name = inName;
            if (inChildObjectList != null)
            {
                foreach (var childObject in inChildObjectList)
                {
                    m_ChildObjectList.Add(childObject);
                    m_ChildObjectIDSet.Add(childObject.objectId);
                }
            }
        }
    }
}
