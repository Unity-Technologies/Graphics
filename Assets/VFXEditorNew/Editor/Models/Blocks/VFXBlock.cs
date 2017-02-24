using UnityEngine;
using System.Collections.Generic;
using Type = System.Type;
using System.Reflection;

namespace UnityEditor.VFX
{
    abstract class VFXBlock : VFXModel<VFXContext, VFXModel>
    {
        public abstract VFXContextType compatibleContexts { get; }

        public VFXBlock()
        {
            System.Type type = GetType().GetNestedType("Properties");

            var fields = type != null ? type.GetFields() : new FieldInfo[0];

            m_Properties = new Property[fields.Length];
            m_Buffers = new object[fields.Length];


            var defaultBuffer = System.Activator.CreateInstance(type);


            for (int i = 0; i < fields.Length; ++i)
            {
                m_Properties[i] = new Property() { type = fields[i].FieldType, name = fields[i].Name };
                
                m_Buffers[i] = fields[i].GetValue(defaultBuffer);
            }
        }


        Property[] m_Properties;
        object[] m_Buffers;


        public struct Property
        {
            public System.Type type;
            public string name;
        }

        public Property[] GetProperties()
        {
            return m_Properties;
        }

        public void ExpandPath(string fieldPath)
        {
            m_expandedPaths.Add(fieldPath);
            Invalidate(InvalidationCause.kParamChanged);
        }

        public void RetractPath(string fieldPath)
        {
            m_expandedPaths.Remove(fieldPath);
            Invalidate(InvalidationCause.kParamChanged);
        }

        public bool IsPathExpanded(string fieldPath)
        {
            return m_expandedPaths.Contains(fieldPath);
        }


        public object[] GetCurrentPropertiesValues()
        {
            return m_Buffers;
        }

        [SerializeField]
        HashSet<string> m_expandedPaths = new HashSet<string>();
    }
}
