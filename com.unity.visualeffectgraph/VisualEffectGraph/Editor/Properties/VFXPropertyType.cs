using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

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
        
        public virtual void CreateValue(VFXPropertySlot slot)
        {
            Check(slot);
            slot.Value = VFXValue.Create(ValueType);
        }

        public virtual bool Default(VFXPropertySlot slot)       { return false; }

        public virtual void CreateUIWidget(VFXPropertySlot value)                   {}
        public virtual void RenderUIController(VFXPropertySlot value,Rect area)     {}

        public virtual VFXValueType ValueType { get { return VFXValueType.kNone; } }

        public virtual bool UpdateProxy(VFXPropertySlot slot) { return false; }  // Set Proxy value from underlying values

        public int GetNbChildren() { return m_Children == null ? 0 : m_Children.Length; }
        public VFXProperty[] GetChildren() { return m_Children; }

        protected void Check(VFXPropertySlot value)
        {
            if (value.Semantics != this)
                throw new InvalidOperationException("VFXPropertyValue does not hold the correct semantic type");
        }
        
        protected VFXProperty[] m_Children;
    }

    // Base primitive types
    public class VFXFloatType : VFXPropertyTypeSemantics         
    {
        public VFXFloatType(): this(0.0f) {}
        public VFXFloatType(float defaultValue)             { m_Default = defaultValue; }
        public override VFXValueType ValueType              { get { return VFXValueType.kFloat; } }
        
        public override bool Default(VFXPropertySlot slot)  
        {
            slot.SetValue(m_Default); 
            return true; 
        }
        
        public override void RenderUIController(VFXPropertySlot slot, Rect area)
        {
            slot.SetValue(EditorGUI.FloatField(area, "", slot.GetValue<float>()));
        }

        private float m_Default;
    }

    public class VFXIntType : VFXPropertyTypeSemantics
    {
        public VFXIntType(): this(0) {}
        VFXIntType(int defaultValue)                        { m_Default = defaultValue; }
        public override VFXValueType ValueType              { get { return VFXValueType.kInt; } }
        
        public override bool Default(VFXPropertySlot slot)  
        {
            slot.SetValue(m_Default);
            return true; 
        }

        public override void RenderUIController(VFXPropertySlot slot, Rect area)
        {
            slot.SetValue(EditorGUI.IntField(area, "", slot.GetValue<int>()));
        }

        private int m_Default;
    }

    public class VFXUintType : VFXPropertyTypeSemantics
    {
        public VFXUintType() : this(0u) {}
        VFXUintType(uint defaultValue)                      { m_Default = defaultValue; }
        public override VFXValueType ValueType              { get { return VFXValueType.kUint; } }
        
        public override bool Default(VFXPropertySlot slot)  
        {
            slot.SetValue(m_Default);
            return true; 
        }

        public override void RenderUIController(VFXPropertySlot slot, Rect area)
        {
            slot.SetValue<uint>((uint)EditorGUI.IntField(area, "", (int)slot.GetValue<uint>()));
        }

        private uint m_Default;
    }

    public class VFXTexture2DType : VFXPropertyTypeSemantics
    {
        public override VFXValueType ValueType { get { return VFXValueType.kTexture2D; } }

        public override void RenderUIController(VFXPropertySlot slot, Rect area)
        {
            slot.SetValue<Texture2D>((Texture2D)EditorGUI.ObjectField(area, slot.GetValue<Texture2D>(), typeof(Texture2D)));
        }
    }

    public class VFXTexture3DType : VFXPropertyTypeSemantics
    {
        public override VFXValueType ValueType { get { return VFXValueType.kTexture3D; } }

        public override void RenderUIController(VFXPropertySlot slot, Rect area)
        {
            slot.SetValue<Texture3D>((Texture3D)EditorGUI.ObjectField(area, slot.GetValue<Texture3D>(), typeof(Texture3D)));
        }
    }

    // Proxy types
    public class VFXFloat2Type : VFXPropertyTypeSemantics
    {
        public VFXFloat2Type() : this(Vector2.zero) { }
        public VFXFloat2Type(Vector3 defaultValue)
        {
            m_Children = new VFXProperty[2];
            m_Children[0] = new VFXProperty(new VFXFloatType(defaultValue.x), "x");
            m_Children[1] = new VFXProperty(new VFXFloatType(defaultValue.y), "y");
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat2; } }

        public override void CreateValue(VFXPropertySlot slot)
        {
            UpdateProxy(slot);
        }

        public override bool UpdateProxy(VFXPropertySlot slot)
        {
            Check(slot);

            slot.Value = new VFXExpressionCombineFloat2(
                slot.GetChild(0).ValueRef,
                slot.GetChild(1).ValueRef);

            return true;
        }

        public override void RenderUIController(VFXPropertySlot slot, Rect area)
        {
            Check(slot);

            var xSlot = slot.GetChild(0);
            var ySlot = slot.GetChild(1);

            Vector3 v = new Vector2(
                xSlot.GetValue<float>(),
                ySlot.GetValue<float>());

            v = EditorGUI.Vector2Field(area, "", v);

            xSlot.SetValue(v.x);
            ySlot.SetValue(v.y);
        }
    }

    public class VFXFloat3Type : VFXPropertyTypeSemantics
    {
        public VFXFloat3Type() : this(Vector3.zero) {}
        public VFXFloat3Type(Vector3 defaultValue) 
        {
            m_Children = new VFXProperty[3];
            m_Children[0] = new VFXProperty(new VFXFloatType(defaultValue.x), "x");
            m_Children[1] = new VFXProperty(new VFXFloatType(defaultValue.y), "y");
            m_Children[2] = new VFXProperty(new VFXFloatType(defaultValue.z), "z");
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } }

        public override void CreateValue(VFXPropertySlot slot)
        {
            UpdateProxy(slot);
        }

        public override bool UpdateProxy(VFXPropertySlot slot)
        {
            Check(slot);

            slot.Value = new VFXExpressionCombineFloat3(
                slot.GetChild(0).ValueRef,
                slot.GetChild(1).ValueRef,
                slot.GetChild(2).ValueRef);

            return true;
        }

        public override void RenderUIController(VFXPropertySlot slot, Rect area)
        {
            Check(slot);

            var xSlot = slot.GetChild(0);
            var ySlot = slot.GetChild(1);
            var zSlot = slot.GetChild(2);

            Vector3 v = new Vector3(
                xSlot.GetValue<float>(),
                ySlot.GetValue<float>(),
                zSlot.GetValue<float>());

            v = EditorGUI.Vector3Field(area, "", v);

            xSlot.SetValue(v.x);
            ySlot.SetValue(v.y);
            zSlot.SetValue(v.z);
        }
    }

    public class VFXFloat4Type : VFXPropertyTypeSemantics
    {
        public VFXFloat4Type() : this(Vector4.zero) { }
        public VFXFloat4Type(Vector4 defaultValue)
        {
            m_Children = new VFXProperty[4];
            m_Children[0] = new VFXProperty(new VFXFloatType(defaultValue.x), "x");
            m_Children[1] = new VFXProperty(new VFXFloatType(defaultValue.y), "y");
            m_Children[2] = new VFXProperty(new VFXFloatType(defaultValue.y), "z");
            m_Children[3] = new VFXProperty(new VFXFloatType(defaultValue.y), "w");
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat4; } }

        public override void CreateValue(VFXPropertySlot slot)
        {
            UpdateProxy(slot);
        }

        public override bool UpdateProxy(VFXPropertySlot slot)
        {
            Check(slot);

            slot.Value = new VFXExpressionCombineFloat4(
                slot.GetChild(0).ValueRef,
                slot.GetChild(1).ValueRef,
                slot.GetChild(2).ValueRef,
                slot.GetChild(3).ValueRef);

            return true;
        }

        public override void RenderUIController(VFXPropertySlot slot, Rect area)
        {
            Check(slot);

            var xSlot = slot.GetChild(0);
            var ySlot = slot.GetChild(1);
            var zSlot = slot.GetChild(2);
            var wSlot = slot.GetChild(3);

            Vector3 v = new Vector4(
                xSlot.GetValue<float>(),
                ySlot.GetValue<float>(),
                zSlot.GetValue<float>(),
                wSlot.GetValue<float>());

            v = EditorGUI.Vector4Field(area, "", v);

            xSlot.SetValue(v.x);
            ySlot.SetValue(v.y);
            zSlot.SetValue(v.y);
            wSlot.SetValue(v.y);
        }
    }

    // Composite types
    public class VFXSphereType : VFXPropertyTypeSemantics
    {
        public VFXSphereType() 
        {
            m_Children = new VFXProperty[2];
            m_Children[0] = new VFXProperty(new VFXFloat3Type(), "center");
            m_Children[1] = new VFXProperty(new VFXFloatType(1.0f), "radius");
        }
    }

    public class VFXAABoxType : VFXPropertyTypeSemantics
    {
        public VFXAABoxType()
        {
            m_Children = new VFXProperty[2];
            m_Children[0] = new VFXProperty(new VFXFloat3Type(), "center");
            m_Children[1] = new VFXProperty(new VFXFloat3Type(Vector3.one), "size");
        }
    }

    public class VFXPlaneType : VFXPropertyTypeSemantics
    {
        public VFXPlaneType()
        {
            m_Children = new VFXProperty[2];
            m_Children[0] = new VFXProperty(new VFXFloat3Type(), "position");
            m_Children[1] = new VFXProperty(new VFXFloat3Type(Vector3.up), "normal");
        }
    }
}

