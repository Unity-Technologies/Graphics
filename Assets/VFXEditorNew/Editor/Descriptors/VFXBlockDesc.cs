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
    class VFXInitBlockTest : VFXBlock
    {
        public override string name                         { get { return "Init Block"; }}
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInit; } }
    }

    [VFXInfo]
    class VFXUpdateBlockTest : VFXBlock
    {
        public override string name                         { get { return "Update Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kUpdate; } }
    }

    [VFXInfo]
    class VFXOutputBlockTest : VFXBlock
    {
        public override string name                         { get { return "Output Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kOutput; } }
    }

    [VFXInfo(category = "test")]
    class VFXInitAndUpdateTest : VFXBlock
    {
        public override string name                         { get { return "Init And Update Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInitAndUpdate; } }
    }

    [VFXInfo]
    class VFXRotate : VFXBlock
    {
        public override string name                         { get { return "Rotate"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInitAndUpdate; } }

        public class Properties
        {
            public float angle = 30;
            public Vector3 axis = Vector3.forward;
        }
    }

    [VFXInfo]
    class VFXAllType : VFXBlock
    {
        public override string name                         { get { return "Test"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kAll; } }

        public class Properties
        {
            public float aFloat = 123.456f;
            public Vector2 aVector2 = Vector2.left;
            public Vector3 aVector3 = Vector3.forward;
            public Vector4 aVector4 = Vector4.one;
            public Color aColor = Color.gray;
            public Texture2D aTexture2D = null;
            public Texture3D aTexture3D = null;
            public Sphere aSphere;
        }
    }
}
