using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.VFX
{
    public class VFXProperty
    {
        public static VFXProperty Create<T>(string name) where T : VFXPropertyTypeSemantics, new()
        {
            return new VFXProperty(new T(),name);
        }

        public VFXProperty(VFXPropertyTypeSemantics type,string name)
        {
            m_Type = type;
            m_Name = name;
        }

        public VFXPropertyTypeSemantics m_Type;
        public string m_Name;
    }

    public abstract class VFXPropertyTypeSemantics
    {
        public virtual bool CanLink(VFXPropertyTypeSemantics other)
        {
            return GetType() == other.GetType() || ChildrenCanLink(other);
        }

        protected bool ChildrenCanLink(VFXPropertyTypeSemantics other)
        {
            if (other == null)
                return false;

            int nbChildren = GetNbChildren();
            if (nbChildren != other.GetNbChildren())
                return false;

            for (int i = 0; i < nbChildren; ++i)
                if (!m_Children[i].m_Type.CanLink(other.m_Children[i].m_Type))
                    return false;

            return true;
        }
        
        //public abstract bool CanTransform(VFXPropertyTypeSemantics other);
        //public abstract void Transform(VFXPropertySlot dst,VFXPropertySlot src);

        //public virtual void Constrain(VFXPropertySlot value)       {}
        public virtual void CreateValue(VFXPropertySlot slot)
        {
            Check(slot);
            slot.Value = VFXValue.Create(ValueType);
        }

        public virtual bool Default(VFXPropertySlot slot)       { return false; }

        public virtual void CreateUIGizmo(VFXPropertySlot value)   {}
        public virtual void CreateUIField(VFXPropertySlot value)   {}

        public virtual VFXValueType ValueType { get{ return VFXValueType.kNone; }}
        //public abstract void ExtracValues(List<VFXValue> dst);

        public int GetNbChildren() { return m_Children == null ? 0 : m_Children.Length; }
        public VFXProperty[] GetChildren() { return m_Children; }

        protected void Check(VFXPropertySlot value)
        {
            if (value.Semantics != this)
                throw new InvalidOperationException("VFXPropertyValue does not hold the correct semantic type");
        }
        
        protected VFXProperty[] m_Children;
    }

    // Base concrete type
    public class VFXFloatType : VFXPropertyTypeSemantics         { public override VFXValueType ValueType { get { return VFXValueType.kFloat; }}}
    public class VFXIntType : VFXPropertyTypeSemantics           { public override VFXValueType ValueType { get { return VFXValueType.kInt; }}}
    public class VFXUintType : VFXPropertyTypeSemantics          { public override VFXValueType ValueType { get { return VFXValueType.kUint; }}}
    public class VFXTexture2DType : VFXPropertyTypeSemantics     { public override VFXValueType ValueType { get { return VFXValueType.kTexture2D; }}}
    public class VFXTexture3DType : VFXPropertyTypeSemantics     { public override VFXValueType ValueType { get { return VFXValueType.kTexture3D; }}}

    // Composite types
    /*public class VFXFloat2Type : VFXPropertyTypeSemantics 
    {
        public override bool Default(VFXPropertySlot slot)
        {
            Check(slot);

            slot.GetChild(1).SetValue(0.0f);
            slot.GetChild(2).SetValue(0.0f);
            slot.GetChild(3).SetValue(0.0f);

            return true;
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat2; } } 
    }*/

    public class VFXFloat2Type : VFXPropertyTypeSemantics { public override VFXValueType ValueType { get { return VFXValueType.kFloat2; } } }
    public class VFXFloat3Type : VFXPropertyTypeSemantics { public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } } }
    public class VFXFloat4Type : VFXPropertyTypeSemantics { public override VFXValueType ValueType { get { return VFXValueType.kFloat4; } } }

    // Concrete type with custom editing widget
    /*public class VFXPositionType : VFXFloat3Type 
    {

    }

    public class VFXDirectionType : VFXFloat3Type 
    {
        public override bool Default(VFXPropertySlot slot) 
        {
            Check(slot);

            slot.GetChild(1).SetValue(0.0f);
            slot.GetChild(2).SetValue(0.0f);
            slot.GetChild(3).SetValue(1.0f);

            return true;
        }
    }*/

    public class VFXPositionType : VFXFloat3Type { }
    public class VFXDirectionType : VFXFloat3Type { }


    // Composite types
    public class VFXSphereType : VFXPropertyTypeSemantics
    {
        public VFXSphereType() 
        {
            m_Children = new VFXProperty[2];
            m_Children[0] = new VFXProperty(new VFXPositionType(), "center");
            m_Children[1] = new VFXProperty(new VFXFloatType(), "radius");
        }
    }

    public class VFXPlaneType : VFXPropertyTypeSemantics
    {
        public VFXPlaneType() 
        {
            m_Children = new VFXProperty[2];
            m_Children[0] = new VFXProperty(new VFXPositionType(), "position");
            m_Children[1] = new VFXProperty(new VFXDirectionType(), "normal");
        }
    }
}

