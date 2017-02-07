using UnityEngine;

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

        public object GetCurrentProperties()
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

        public object m_PropertyBuffer;
    }
}
