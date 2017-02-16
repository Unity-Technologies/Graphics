using UnityEngine;
using System.Collections.Generic;
using Type = System.Type;
using System.Reflection;

namespace UnityEditor.VFX
{
    abstract class VFXBlock : VFXModel<VFXContext, VFXModel>
    {
        public abstract string name { get; }
        public abstract VFXContextType compatibleContexts { get; }

        public VFXBlock()
        {
            var propertyType = GetPropertiesType();
            if (propertyType != null)
                m_PropertyBuffer = System.Activator.CreateInstance(propertyType);
        }

        public System.Type GetPropertiesType()
        {
            return GetType().GetNestedType("Properties");
        }

        public void ExpandPath(string fieldPath)
        {
            m_expandedPaths.Add(fieldPath);
        }

        public void RetractPath(string fieldPath)
        {
            m_expandedPaths.Remove(fieldPath);
        }

        public bool IsPathExpanded(string fieldPath)
        {
            return m_expandedPaths.Contains(fieldPath);
        }


        public object GetCurrentPropertiesValue()
        {
            return m_PropertyBuffer;
        }

        [SerializeField]
        HashSet<string> m_expandedPaths = new HashSet<string>();

        public object m_PropertyBuffer;
    }
}
