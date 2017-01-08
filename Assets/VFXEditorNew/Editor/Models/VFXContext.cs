using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXContext : VFXModel<VFXSystem, VFXSubContext>
    {
        private VFXContext() {} // Used by serialization

        public VFXContext(VFXContextDesc desc)
        {
            m_Desc = desc;
        }

        public VFXContextDesc Desc              { get { return m_Desc; } }
        public VFXContextDesc.Type ContextType  { get { return Desc.ContextType; } }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableDesc = m_Desc.GetType().FullName;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            // TODO Construct desc based on its type
            m_Desc = VFXLibrary.GetContext(m_SerializableDesc);
            m_SerializableDesc = null;
        }

        private VFXContextDesc m_Desc;

        [SerializeField]
        private string m_SerializableDesc;
    }

    class VFXSubContext : VFXModel<VFXContext, VFXModel>
    {
        // In and out sub context, if null directly connected to the context input/output
        private VFXSubContext m_In;
        private VFXSubContext m_Out;
    }
}
