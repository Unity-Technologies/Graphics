using System;

namespace UnityEditor.VFX
{
    public abstract class VFXBlockDesc
    {
        public string Name { get { return m_Name; } }
        //public string Category { get { return m_Category; } }
        public VFXContextType CompatibleContexts { get { return m_CompatibleContexts; }}

        protected VFXBlockDesc(string name, /*string category,*/ VFXContextType compatibleContexts)
        {
            m_Name = name;
            //m_Category = category;
            m_CompatibleContexts = compatibleContexts;
        }
        /*protected VFXBlockDesc(string name, VFXContextType compatibleContexts):this(name,"Default",compatibleContexts)
        {
        }*/

        private string m_Name;
        //private string m_Category;
        private VFXContextType m_CompatibleContexts;
    }

    // Test blocks only !
    // TODO Remove that!

    [VFXInfo]
    class VFXInitBlockTest : VFXBlockDesc
    {
        public VFXInitBlockTest() : base("Init Block", VFXContextType.kInit) { }
    }

    [VFXInfo]
    class VFXUpdateBlockTest : VFXBlockDesc
    {
        public VFXUpdateBlockTest() : base("Update Block", VFXContextType.kUpdate) { }
    }

    [VFXInfo]
    class VFXOutputBlockTest : VFXBlockDesc
    {
        public VFXOutputBlockTest() : base("Output Block",/*"Other",*/ VFXContextType.kOutput) { }
    }

    [VFXInfo(category = "test")]
    class VFXInitAndUpdateTest : VFXBlockDesc
    {
        public VFXInitAndUpdateTest() : base("Init And Update Block", VFXContextType.kInitAndUpdate) { }
    }
}
