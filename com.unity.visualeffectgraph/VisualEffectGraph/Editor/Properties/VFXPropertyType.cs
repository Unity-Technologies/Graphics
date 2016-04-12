using UnityEngine;

namespace UnityEngine.Experimental.VFX
{
    public class VFXPropertyType
    {
        public virtual void SpawnWidget() {}
        public virtual VFXProperty[] CreateSubProperties() { return null; }

        public VFXProperty[] CreateSubProperties(string prefix)
        {
            var properties = CreateSubProperties();
            if (properties != null)
                foreach (var prop in properties)
                    prop.m_Name = prefix + "_" + prop.m_Name;
            return properties;
        }

        public virtual VFXShaderValueType ValueType { get { return VFXShaderValueType.kNone; } }
        public virtual void SetDefaultValue(VFXShaderValue value) { value.SetDefault(); }
        public virtual void ConstraintValue(VFXShaderValue value) {}
    }

    public class VFXProperty
    {
        public VFXProperty(VFXPropertyType type,string name)
        {
            m_Type = type;
            m_Name = name;
        }

        public VFXPropertyType m_Type;
        public string m_Name;
    }

    // Base concrete type
    public class VFXFloatType : VFXPropertyType         { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat; }}}
    public class VFXFloat2Type : VFXPropertyType        { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat2; }}}
    public class VFXFloat3Type : VFXPropertyType        { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat3; }}}
    public class VFXFloat4Type : VFXPropertyType        { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat4; }}}
    public class VFXIntType : VFXPropertyType           { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kInt; }}}
    public class VFXUintType : VFXPropertyType          { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kUint; }}}
    public class VFXTexture2DType : VFXPropertyType     { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kTexture2D; }}}
    public class VFXTexture3DType : VFXPropertyType     { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kTexture3D; }}}

    // Concrete type with custom editing widget
    public class VFXPositionType : VFXPropertyType      { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat3; }}}
    public class VFXDirectionType : VFXPropertyType     { public override VFXShaderValueType ValueType { get { return VFXShaderValueType.kFloat3; }}}

    // Composite types
    public class VFXSphereType : VFXPropertyType
    {
        public override VFXProperty[] CreateSubProperties() 
        {
            VFXProperty[] children = new VFXProperty[2];
            children[0] = new VFXProperty(new VFXPositionType(), "center");
            children[1] = new VFXProperty(new VFXFloatType(), "radius");
            return children;
        }
    }

    public class VFXPlaneType : VFXPropertyType
    {
        public override VFXProperty[] CreateSubProperties() 
        {
            VFXProperty[] children = new VFXProperty[2];
            children[0] = new VFXProperty(new VFXPositionType(), "position");
            children[1] = new VFXProperty(new VFXDirectionType(), "normal");
            return children;
        }
    }
}

