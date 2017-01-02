using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXContext : VFXModel<VFXSystem, VFXModel>
    {
        private VFXContext() {} // Used by serialization

        public VFXContext(VFXContextDesc desc)
        {
            m_Desc = desc;
        }

        public VFXContextDesc Desc              { get { return m_Desc; } }
        public VFXContextDesc.Type ContextType  { get { return Desc.ContextType; } }

        public virtual void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableDesc = m_Desc.GetType().FullName;
        }

        public virtual void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            // TODO Construct desc based on its type
            m_SerializableDesc = null;
        }

        private VFXContextDesc m_Desc;

        [SerializeField]
        private string m_SerializableDesc;
    }
}
