using System;
using UnityEngine;

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

        public System.Type GetPropertiesType()
        {
            return GetType().GetNestedType("Properties");
        }

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
    class VFXRotate : VFXBlockDesc
    {
        public VFXRotate() : base("Rotate Block", VFXContextType.kInit | VFXContextType.kUpdate) { }


        public class Properties
        {
            public float angle = 30;
            public Vector3 axis = Vector3.forward;
        }
    }

    [VFXInfo]
    class VFXAllType : VFXBlockDesc
    {
        public VFXAllType() : base("Test Block", VFXContextType.kAll) { }


        public class Properties
        {
            public float aFloat = 123.456f;
            public Vector2 aVector2 = Vector2.left;
            public Vector3 aVector3 = Vector3.forward;
            public Vector4 aVector4 = Vector4.one;
            public Color aColor = Color.gray;
            public Texture2D aTexture2D = null;
            public Texture3D aTexture3D = null;
        }
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
