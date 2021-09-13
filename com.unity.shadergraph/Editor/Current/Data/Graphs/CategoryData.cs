using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
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

        // We expose the object list as a HashSet of their objectIDs for faster existence checks
        HashSet<string> m_ChildObjectIDSet = new HashSet<string>();

        public int childCount => m_ChildObjectIDSet.Count;

        public void InsertItemIntoCategory(ShaderInput itemToAdd, int insertionIndex = -1)
        {
            if (itemToAdd == null)
            {
                AssertHelpers.Fail("Tried to insert item into category that was null.");
                return;
            }

            if (insertionIndex == -1)
            {
                m_ChildObjectList.Add(itemToAdd);
                m_ChildObjectIDSet.Add(itemToAdd.objectId);
            }
            else
            {
                m_ChildObjectList.Insert(insertionIndex, itemToAdd);
                m_ChildObjectIDSet.Add(itemToAdd.objectId);
            }
        }

        public void RemoveItemFromCategory(ShaderInput itemToRemove)
        {
            if (IsItemInCategory(itemToRemove))
            {
                m_ChildObjectList.Remove(itemToRemove);
                m_ChildObjectIDSet.Remove(itemToRemove.objectId);
            }
        }

        public void MoveItemInCategory(ShaderInput itemToMove, int newIndex)
        {
            int oldIndex = m_ChildObjectList.IndexOf(itemToMove);
            if (newIndex == oldIndex)
                return;
            m_ChildObjectList.RemoveAt(oldIndex);
            m_ChildObjectIDSet.Remove(itemToMove.objectId);
            // The actual index could have shifted due to the removal
            if (newIndex > oldIndex)
                newIndex--;
            InsertItemIntoCategory(itemToMove, newIndex);
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
                for (int index = 0; index < m_ChildObjectList.Count; ++index)
                {
                    var childObject = m_ChildObjectList[index];
                    if (childObject.value != null)
                        m_ChildObjectIDSet.Add(childObject.value.objectId);
                    else
                        m_ChildObjectList.RemoveAt(index);
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

        public CategoryData(string inName, List<ShaderInput> categoryChildren = null)
        {
            name = inName;
            if (categoryChildren != null)
            {
                foreach (var childObject in categoryChildren)
                {
                    m_ChildObjectList.Add(childObject);
                    m_ChildObjectIDSet.Add(childObject.objectId);
                }
            }
        }

        public CategoryData(CategoryData categoryToCopy)
        {
            this.name = categoryToCopy.name;
        }

        public static CategoryData DefaultCategory(List<ShaderInput> categoryChildren = null)
        {
            return new CategoryData(String.Empty, categoryChildren);
        }
    }
}
