
namespace UnityEditor.VFX
{
    abstract class VFXBlockDesc
    {
        public string Name { get { return m_Name; }}
        public VFXContextDesc.Type CompatibleContexts { get { return m_CompatibleContexts; }}

        protected VFXBlockDesc(string name, VFXContextDesc.Type compatibleContexts)
        {
            m_Name = name;
            m_CompatibleContexts = compatibleContexts;
        }

        private string m_Name;
        private VFXContextDesc.Type m_CompatibleContexts;
    }

    // Test blocks only !
    // TODO Rmeove that!

    class VFXInitBlockTest : VFXBlockDesc
    {
        public VFXInitBlockTest() : base("Init Block", VFXContextDesc.Type.kTypeInit) { }
    }

    class VFXUpdateBlockTest : VFXBlockDesc
    {
        public VFXUpdateBlockTest() : base("Update Block", VFXContextDesc.Type.kTypeUpdate) { }
    }

    class VFXOutputBlockTest : VFXBlockDesc
    {
        public VFXOutputBlockTest() : base("Output Block", VFXContextDesc.Type.kTypeOutput) { }
    }

    class VFXInitAndUpdateTest : VFXBlockDesc
    {
        public VFXInitAndUpdateTest() : base("Init And Update Block", VFXContextDesc.Type.kInitAndUpdate) { }
    }
}
