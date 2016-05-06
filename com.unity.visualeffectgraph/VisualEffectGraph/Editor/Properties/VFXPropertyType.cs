using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental;

namespace UnityEngine.Experimental.VFX
{
    public struct VFXProperty
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
        // Description of the semantics in order to be able to spawn data node block from them
        // Semantics that dont implement static method Desc Description() wont be associated to a data node block
        public struct Desc
        {
            public Desc(string name, string icon, string category)
            {
                m_Name = name;
                m_Icon = icon;
                m_Category = category;
            }

            public string m_Name;
            public string m_Icon;
            public string m_Category;
        }

        protected static VFXPropertySlot Slot(VFXPropertySlot slot, bool linked)
        {
            return linked ? slot.CurrentValueRef : slot;
        }

        protected static bool CanSet(VFXPropertySlot slot, bool linked)
        {
            return !linked || slot.IsValueUsed();
        }

        public virtual bool CanLink(VFXPropertyTypeSemantics other)
        {
            return GetType() == other.GetType() || (GetNbChildren() != 0 && ChildrenCanLink(other));
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

        public virtual VFXValueType ValueType { get { return VFXValueType.kNone; } }
        
        public virtual void CreateValue(VFXPropertySlot slot)
        {
            slot.Value = VFXValue.Create(ValueType);
        }

        public T Get<T>(VFXPropertySlot slot, bool linked = false)                      { return (T)InnerGet(slot, linked); }
        public void Set<T>(VFXPropertySlot slot, T t, bool linked = false)              { InnerSet(slot, (object)t, linked); }

        protected virtual object InnerGet(VFXPropertySlot slot,bool linked)             { throw new InvalidOperationException(); }
        protected virtual void InnerSet(VFXPropertySlot slot, object value,bool linked) { throw new InvalidOperationException(); }

        public virtual bool Default(VFXPropertySlot slot)       { return false; }

        // UI stuff
        public virtual VFXUIWidget CreateUIWidget(VFXPropertySlot value)        { return null; }    
        public virtual void OnCanvas2DGUI(VFXPropertySlot value, Rect area)     {}
        public virtual void OnInspectorGUI(VFXPropertySlot value)               {}

        public virtual bool UpdateProxy(VFXPropertySlot slot) { return false; }  // Set Proxy value from underlying values

        public int GetNbChildren() { return m_Children == null ? 0 : m_Children.Length; }
        public VFXProperty[] GetChildren() { return m_Children; }
        
        protected VFXProperty[] m_Children;
    }

    // Base primitive types
    public abstract class VFXPrimitiveType<T> : VFXPropertyTypeSemantics
    {
        protected VFXPrimitiveType(T defaultValue)
        {
            m_Type = VFXValue.ToValueType<T>();
            m_Default = defaultValue;
        }

        public override VFXValueType ValueType { get { return m_Type; } }
        
        public override bool Default(VFXPropertySlot slot)
        {
            slot.SetInnerValue(m_Default);
            return true;
        }

        protected override object InnerGet(VFXPropertySlot slot, bool linked)             
        {
            return Slot(slot,linked).GetInnerValue<T>(); 
        }

        protected override void InnerSet(VFXPropertySlot slot, object value, bool linked) 
        {
            if (CanSet(slot,linked))
                Slot(slot,linked).SetInnerValue((T)value); 
        }

        protected VFXValueType m_Type;
        protected T m_Default;
    }

    public partial class VFXFloatType : VFXPrimitiveType<float>
    {
        public static Desc Description() { return new Desc("Float", "type_float", "Basic"); }

        public VFXFloatType() : this(0.0f) { }
        public VFXFloatType(float defaultValue) : base(defaultValue) { }
    }

    public partial class VFXIntType : VFXPrimitiveType<int>
    {
        public static Desc Description() { return new Desc("Int", "type_integer", "Basic"); }

        public VFXIntType() : this(0) { }
        VFXIntType(int defaultValue) : base(defaultValue) { }
    }

    public partial class VFXUintType : VFXPrimitiveType<uint>
    {
        public static Desc Description() { return new Desc("Uint", "type_integer", "Basic"); }

        public VFXUintType() : this(0u) {}
        VFXUintType(uint defaultValue) : base(defaultValue) { }
    }

    public partial class VFXTexture2DType : VFXPrimitiveType<Texture2D>
    {
        public static Desc Description() { return new Desc("Texture2D", "Texture", "Texture"); }

        public VFXTexture2DType() : this(null) {}
        VFXTexture2DType(Texture2D defaultValue) : base(defaultValue) { }
    }

    public partial class VFXTexture3DType : VFXPrimitiveType<Texture3D>
    {
        public static Desc Description() { return new Desc("Texture3D", "Volume", "Texture"); }

        public VFXTexture3DType() : this(null) {}
        VFXTexture3DType(Texture3D defaultValue) : base(defaultValue) { }
    }

    public partial class VFXCurveType : VFXPrimitiveType<AnimationCurve>
    {
        public static Desc Description() { return new Desc("Curve", "Curve", "Curve"); }

        public VFXCurveType() : base(null) {}

        public override bool Default(VFXPropertySlot slot)
        {
            slot.SetInnerValue(AnimationCurve.EaseInOut(0.0f,0.0f,1.0f,1.0f));
            return true;
        }
    }

    public partial class VFXColorGradientType : VFXPrimitiveType<Gradient>
    {
        public static Desc Description() { return new Desc("Color Gradient", "Gradient", "Color"); }

        public VFXColorGradientType() : base(null) {}

        public override bool Default(VFXPropertySlot slot)
        {
            slot.SetInnerValue(new Gradient());
            return true;
        }
    }

    // Proxy types
    // TODO
    public abstract class VFXProxyVectorType : VFXPropertyTypeSemantics
    {
        protected VFXProxyVectorType(int nbComponents,Vector4 defaultValue)
        {
            kNbComponents = nbComponents;
            m_Default = defaultValue;

            m_Children = new VFXProperty[kNbComponents];
            for (int i = 0; i < kNbComponents; ++i )
                m_Children[i] = new VFXProperty(new VFXFloatType(m_Default[i]), kComponentNames[i]);
        }

        public override bool CanLink(VFXPropertyTypeSemantics other)
        {
            if (base.CanLink(other))
                return true;

            var v = other as VFXProxyVectorType;
            return v != null && v.kNbComponents >= kNbComponents;
        }

        public override void CreateValue(VFXPropertySlot slot)
        {
            UpdateProxy(slot);
        }

        public override bool Default(VFXPropertySlot slot)
        {
            InnerSet(slot, BoxCast(m_Default), false);
            return true;
        }

        protected override object InnerGet(VFXPropertySlot slot, bool linked)
        {
            // TODO Should get the reduced value instead of recomputing it !
            Vector4 tmp = new Vector4();
            slot = Slot(slot, linked);
            for (int i = 0; i < kNbComponents; ++i)
                tmp[i] = Slot(slot.GetChild(i),linked).GetInnerValue<float>();
            return BoxCast(tmp);
        }

        protected override void InnerSet(VFXPropertySlot slot, object value, bool linked)
        {
            Vector4 tmp = UnboxCast(value);
            slot = Slot(slot, linked);
            for (int i = 0; i < kNbComponents; ++i)
            {
                var child = slot.GetChild(i);
                if (CanSet(child,linked))
                    Slot(child,linked).SetInnerValue<float>(tmp[i]);
            }
        }

        // Too bad, implicit conversion cannot be used with boxing/unboxing. So use this hack to explicitly cast between vectors
        protected abstract object BoxCast(Vector4 v);
        protected abstract Vector4 UnboxCast(object v);

        private readonly int kNbComponents;
        private static readonly string[] kComponentNames = new string[4] { "X", "Y", "Z", "W" };

        protected Vector4 m_Default;
    }

    public partial class VFXFloat2Type : VFXProxyVectorType
    {
        public static Desc Description() { return new Desc("Float2", "type_vector2", "Basic"); }

        public VFXFloat2Type() : this(Vector2.zero) { }
        public VFXFloat2Type(Vector2 defaultValue) : base(2, defaultValue) { }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat2; } }

        public override bool UpdateProxy(VFXPropertySlot slot)
        {
            slot.Value = new VFXExpressionCombineFloat2(
                slot.GetChild(0).ValueRef,
                slot.GetChild(1).ValueRef);

            return true;
        }

        protected override sealed object BoxCast(Vector4 v) { return (Vector2)v; }
        protected override sealed Vector4 UnboxCast(object v) { return (Vector2)v; }
    }

    public partial class VFXFloat3Type : VFXProxyVectorType
    {
        public static Desc Description() { return new Desc("Float3", "type_vector3", "Basic"); }

        public VFXFloat3Type() : this(Vector3.zero) {}
        public VFXFloat3Type(Vector3 defaultValue)  : base(3, defaultValue) {}

        public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } }

        public override bool UpdateProxy(VFXPropertySlot slot)
        {
            slot.Value = new VFXExpressionCombineFloat3(
                slot.GetChild(0).ValueRef,
                slot.GetChild(1).ValueRef,
                slot.GetChild(2).ValueRef);

            return true;
        }

        protected override sealed object BoxCast(Vector4 v) { return (Vector3)v; }
        protected override sealed Vector4 UnboxCast(object v) { return (Vector3)v; }
    }

    public partial class VFXFloat4Type : VFXProxyVectorType
    {
        public static Desc Description() { return new Desc("Float4", "type_vector4", "Basic"); }

        public VFXFloat4Type() : this(Vector4.zero) { }
        public VFXFloat4Type(Vector4 defaultValue) : base(4, defaultValue) { }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } }

        public override bool UpdateProxy(VFXPropertySlot slot)
        {
            slot.Value = new VFXExpressionCombineFloat4(
                slot.GetChild(0).ValueRef,
                slot.GetChild(1).ValueRef,
                slot.GetChild(2).ValueRef,
                slot.GetChild(3).ValueRef);

            return true;
        }

        protected override sealed object BoxCast(Vector4 v)     { return v; }
        protected override sealed Vector4 UnboxCast(object v)   { return (Vector4)v; }
    }

    public partial class VFXColorRGBType : VFXFloat3Type
    {
        public static Desc Description() { return new Desc("Color", "type_color", "Color"); }

        public VFXColorRGBType() : base(Vector3.one) {} // white as default color
    }

    public partial class VFXPositionType : VFXFloat3Type
    {
        public static Desc Description() { return new Desc("Float3 (Position)", "type_vector3", "Basic"); }

        public VFXPositionType() {}
    }

    public partial class VFXVectorType : VFXFloat3Type
    {
        public static Desc Description() { return new Desc("Float3 (Vector)", "type_vector3", "Basic"); }

        public VFXVectorType() : base(Vector3.up) { }
    }

    public partial class VFXDirectionType : VFXFloat3Type
    {
        public static Desc Description() { return new Desc("Float3 (Direction)", "type_vector3", "Basic"); }

        public VFXDirectionType() : base(Vector3.up) { }
    }

    public partial class VFXTransformType : VFXPropertyTypeSemantics
    {
        public static Desc Description() { return new Desc("Transform", "Position", "Basic"); }

        public VFXTransformType() : this(kComponentNames) {}
        protected VFXTransformType(string[] componentNames)
        {
            m_Children = new VFXProperty[3];
            m_Children[0] = new VFXProperty(new VFXFloat3Type(), componentNames[0]);
            m_Children[1] = new VFXProperty(new VFXFloat3Type(), componentNames[1]);
            m_Children[2] = new VFXProperty(new VFXFloat3Type(Vector3.one), componentNames[2]);
        }

        public override VFXValueType ValueType { get { return VFXValueType.kTransform; } }

        public override void CreateValue(VFXPropertySlot slot)
        {
            UpdateProxy(slot);
        }

        public override bool UpdateProxy(VFXPropertySlot slot)
        {
            slot.Value = new VFXExpressionTRSToMatrix(
                slot.GetChild(0).ValueRef,
                slot.GetChild(1).ValueRef,
                slot.GetChild(2).ValueRef);

            return true;
        }

        protected override object InnerGet(VFXPropertySlot slot, bool linked)
        {
            return Slot(slot, linked).GetInnerValue<Matrix4x4>();
            /*slot.Value.Reduce();
            slot = Slot(slot, linked);
            
            Vector3 position = Slot(slot.GetChild(0),linked).Get<Vector3>();
            Quaternion rotation = Quaternion.Euler(Slot(slot.GetChild(1), linked).Get<Vector3>());
            Vector3 scale = Slot(slot.GetChild(2),linked).Get<Vector3>();
            
            return Matrix4x4.TRS(position, rotation, scale);*/
        }

        protected override void InnerSet(VFXPropertySlot slot, object value, bool linked)
        {
            throw new NotImplementedException();
        }

        private static readonly string[] kComponentNames = new string[3] {"position","rotation","scale"};
    }

    // This is just an alias on VFXTransformType with custom widgets and component names
    public partial class VFXOrientedBoxType : VFXTransformType
    {
        public static Desc Description() { return new Desc("Box (Oriented)", "Box", "Primitive"); }

        public VFXOrientedBoxType() : base(kComponentNames) { }
        private static readonly string[] kComponentNames = new string[3] {"center","rotation","size"};
    }

    // Composite types
    public partial class VFXSphereType : VFXPropertyTypeSemantics
    {
        public static Desc Description() { return new Desc("Sphere", "Sphere", "Primitive"); }

        public VFXSphereType() 
        {
            m_Children = new VFXProperty[2];
            m_Children[0] = new VFXProperty(new VFXFloat3Type(), "center");
            m_Children[1] = new VFXProperty(new VFXFloatType(1.0f), "radius");
        }
    }

    public partial class VFXAABoxType : VFXPropertyTypeSemantics
    {
        public static Desc Description() { return new Desc("Box (Axis-Aligned)", "Box", "Primitive"); }

        public VFXAABoxType()
        {
            m_Children = new VFXProperty[2];
            m_Children[0] = new VFXProperty(new VFXFloat3Type(), "center");
            m_Children[1] = new VFXProperty(new VFXFloat3Type(Vector3.one), "size");
        }
    }

    public partial class VFXPlaneType : VFXPropertyTypeSemantics
    {
        public static Desc Description() { return new Desc("Plane", "Plane", "Primitive"); }

        public VFXPlaneType()
        {
            m_Children = new VFXProperty[2];
            m_Children[0] = new VFXProperty(new VFXFloat3Type(), "position");
            m_Children[1] = new VFXProperty(new VFXFloat3Type(Vector3.up), "normal");
        }
    }
}

