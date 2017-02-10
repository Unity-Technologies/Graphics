using UnityEngine;
using System.Collections.Generic;
using Type = System.Type;
using System.Reflection;

namespace UnityEditor.VFX
{
    class VFXBlock : VFXModel<VFXContext, VFXModel>
    {
        private VFXBlock() {} // Used by serialization

        public VFXBlockDesc Desc { get { return m_Desc; } }

        public VFXBlock(VFXBlockDesc desc)
        {
            m_Desc = desc;


            System.Type propertyType = Desc.GetPropertiesType();
            if (propertyType != null)
                m_PropertyBuffer = System.Activator.CreateInstance(propertyType);
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableDesc = m_Desc.GetType().FullName;
            

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

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_Desc = VFXLibrary.GetBlock(m_SerializableDesc);
            m_SerializableDesc = null;
            System.Type propertyType = Desc.GetPropertiesType();
            if (propertyType != null)
                m_PropertyBuffer = System.Activator.CreateInstance(propertyType);
        }

        private VFXBlockDesc m_Desc;

        [SerializeField]
        private string m_SerializableDesc;

        [SerializeField]
        HashSet<string> m_expandedPaths = new HashSet<string>();

        public object m_PropertyBuffer;
    }
}
