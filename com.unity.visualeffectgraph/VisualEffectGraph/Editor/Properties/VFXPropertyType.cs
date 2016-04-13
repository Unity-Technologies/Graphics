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



    public abstract class VFXPropertyTypeSemantics
    {
        public abstract bool CanLink();

        public virtual void Constrain(VFXPropertyValue value)       {}
        public virtual void Default(VFXPropertyValue value)         {}
        public virtual void CreateUIGizmo(VFXPropertyValue value)   {}
        public virtual void CreateUIField(VFXPropertyValue value)   {}

        public abstract VFXShaderValueType GetShaderValueType();
        public abstract void ExtractShaderValues(List<VFXShaderValue> dst);

        protected void Check(VFXPropertyValue value)
        {
            if (value.m_Type != this)
                throw new InvalidOperationException("VFXPropertyValue does not hold the correct semantic type");
        }
    }


    public abstract class VFXCompositeTypeSemantics : VFXPropertyTypeSemantics
    {
        public override bool CanLink(VFXPropertyTypeSemantics other)
        {
            return GetType() == other.GetType() || ChildrenCanLink(other as VFXCompositeTypeSemantics);
        }

        public sealed override VFXShaderValueType GetShaderValueType()
        {
            return VFXShaderValueType.kNone; 
        }

        // By default no transformation on values, only gather sub values
        public override bool ExtractShaderValues(List<VFXShaderValue> values,VFXPropertyValue value)
        {
            for (int i = 0; i < m_Children.Length; ++i)
                value.m_ChildrenValue(i);
        }

        protected bool ChildrenCanLink(VFXCompositeTypeSemantics other)
        {
            if (other == null)
                return false;

            int nbChildren = m_Children.Length;
            if (nbChildren != other.m_Children.Length)
                return false;

            for (int i = 0; i < nbChildren; ++i)
                if (!m_Children[i].m_Type.CanLink(other.m_Children[i].m_Type))
                    return false;

            return true;
        }

        protected VFXProperty[] m_Children;
    }

    public class VFXConcreteTypeSemantics : VFXPropertyTypeSemantics
    {
        public override bool CanLink(VFXPropertyTypeSemantics other)
        {
            return GetShaderValueType() == other.GetShaderValueType();
        }
    }

    public class VFXFloat3TypeSemantics : VFXCompositeTypeSemantics
    {
        VFXFloat3TypeSemantics()
        {
            m_Children = new VFXProperty[3];
            m_Children[0] = new VFXProperty(new VFXFloatTypeSemantics,"X");
            m_Children[1] = new VFXProperty(new VFXFloatTypeSemantics,"Y");
            m_Children[2] = new VFXProperty(new VFXFloatTypeSemantics,"Z");
        }

        public override bool ExtractShaderValues(List<VFXShaderValue> values, VFXPropertyValue value)
        {
            Check(value);

            var shaderValue = VFXShaderValue.Create(VFXShaderValueType.kFloat3);
            shaderValue.Set<Vector3>(GetVector3(value));
            values.Add(shaderValue);
        }

        protected Vector3 GetVector3(VFXPropertyValue value)
        {
            Vector3 val = new Vector3();
            for (int i = 0; i < 3; ++i)
                val[i] = value.GetChildren(i).GetValue().GetValue<float>();
            return val;
        }

        protected SetVector3(Vector3 vec,VFXPropertyValue value)
        {
            for (int i = 0; i < 3; ++i)
                value.GetChildren(i).GetValue.SetValue<float>(vec[i]);
        }
    }

    public class VFXNormal3TypeSemantics : VFXFloat3TypeSemantics
    {
        public virtual void Constrain(VFXPropertyValue value)
        {
            Check(value);
            Vector3 vec = GetVector3(value);
            vec.Normalize();
            SetVector3(vec,value);
        }

        public virtual void Default(VFXPropertyValue value)
        {
            SetVector3(Vector3.up);
        }
    }

    /*public class VFXPropertyTypeSemantics
    {
        // Can this semantic type be transformed to another semantic type ?
        public virtual bool CanTransform(VFXPropertyTypeSemantics other);
        
        // Transform source value with this semantic type to dst
        public virtual void Transform(VFXPropertyValue dst,VFXPropertyValue src)
        {
            Check(src);
        }

        // Set semantic type default value to the passed value
        public virtual void Default(VFXPropertyValue value) 
        { 
            Check(value);
            value.SetDefault(); 
        }

        // Ensure passed value meets the semantic type requirements
        public virtual void Constrain(VFXPropertyValue value) 
        {
            Check(value);
            for (int i = 0; i < nb; ++i)
                value.GetChild(i).Constrain();
        }

        // Create a preview gizmo to manipulate value
        public virtual void CreateUIGizmo(VFXPropertyValue value) {}
        
        // Create the block UI for this value
        public virtual void CreateUIBlock(VFXPropertyValue value) {}

        public virtual VFXShaderValueType GetShaderValueType() { return VFXShaderValueType.kNone; }
        // Used by the compiler to retrieve the shader values
        public virtual void ExtractShaderValues(List<VFXShaderValue> values,VFXPropertyValue value)
        {
            Check(value);
            var shaderValue = VFXShaderValue.Create(GetShaderValueType());
            if (!shaderValue)
                values.Add(shaderValue)
            else

            else
                // transform;
            return null;
        }

        protected void Check(VFXPropertyValue value)
        {
            if (value.GetSemanticType() != this)
                throw Exception();
        }
    }

    public class VFXFloat3Semantics
    {

    }



    public class VFXPropertyTypeSemantics
    {
        // 
        public VFXPropertyValue CreateValue()
        {
            return new VFXPropertyValue(this);
        }

        public bool CanTransformTo(VFXPropertyTypeSemantics other)
        {
            return GetType() == other.GetType() || 
               
        }

        public bool TransformTo(VFXPropertyValue dest,VFXPropertyValue src)
        {
            if (src.m_Type != CanTransformTo() || )
            for (int i = 0;)
        }

        void CreateUIGizmo();
        void CreateUIBlock();



        bool SetDefaultValue(VFXPropertyValue value)
        {
            for (int i = 0; i < properties.Length; ++i)
                properties[i].m_Type.SetDefaultValue(value.GetChild(i));
        }

        public virtual VFXShaderValue[] GetShaderValues {  return null; }

        void Constrain(VFXPropertyValue value) {}

        // Shader value type for that semantic type. (If none, no value is used)
        VFXShaderValueType getValueType() { return VFXShaderValueType.kNone; }
          
        private VFXProperty[] properties = null;     
    }*/
}

