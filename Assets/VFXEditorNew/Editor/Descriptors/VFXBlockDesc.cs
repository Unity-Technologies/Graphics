
namespace UnityEditor.VFX
{
    abstract class VFXBlockDesc
    {
        public string Name { get { return m_Name; }}
        public VFXContextType CompatibleContexts { get { return m_CompatibleContexts; }}

        protected VFXBlockDesc(string name, VFXContextType compatibleContexts)
        {
            m_Name = name;
            m_CompatibleContexts = compatibleContexts;
        }

        private string m_Name;
        private VFXContextType m_CompatibleContexts;
    }

    // Test blocks only !
    // TODO Remove that!

    class VFXInitBlockTest : VFXBlockDesc
    {
        public VFXInitBlockTest() : base("Init Block", VFXContextType.kInit) { }
    }

    class VFXUpdateBlockTest : VFXBlockDesc
    {
        public VFXUpdateBlockTest() : base("Update Block", VFXContextType.kUpdate) { }
    }

    class VFXOutputBlockTest : VFXBlockDesc
    {
        public VFXOutputBlockTest() : base("Output Block", VFXContextType.kOutput) { }
    }

    class VFXInitAndUpdateTest : VFXBlockDesc
    {
        public VFXInitAndUpdateTest() : base("Init And Update Block", VFXContextType.kInitAndUpdate) { }
    }
}
