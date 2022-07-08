
using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphTypeConstant : BaseShaderGraphConstant
    {
        internal GraphType.Length GetLength() => IsInitialized ? GraphTypeHelpers.GetLength(GetField()) : GraphType.Length.Four;
        private GraphType.Primitive GetPrimitive() => IsInitialized ? GraphTypeHelpers.GetPrimitive(GetField()) : GraphType.Primitive.Float;

        // TODO: Not this
        protected override void StoreValueForCopy()
        {
            if (!IsInitialized)
                return;
            switch (GetLength())
            {
                case GraphType.Length.One:
                    switch (GetPrimitive())
                    {
                        case GraphType.Primitive.Int: m_StoredValue.x = GraphTypeHelpers.GetAsInt(GetField());
                            break;
                        case GraphType.Primitive.Bool: m_StoredValue.x = Convert.ToSingle(GraphTypeHelpers.GetAsBool(GetField()));
                            break;
                        case GraphType.Primitive.Float:
                        default: m_StoredValue.x = GraphTypeHelpers.GetAsFloat(GetField());
                            break;
                    }
                    break;
                case GraphType.Length.Two: m_StoredValue = GraphTypeHelpers.GetAsVec2(GetField());
                    break;
                case GraphType.Length.Three: m_StoredValue = GraphTypeHelpers.GetAsVec3(GetField());
                    break;
                case GraphType.Length.Four: m_StoredValue = GraphTypeHelpers.GetAsVec4(GetField());
                    break;
                default: m_StoredValue = (Vector4)DefaultValue;
                    break;
            }

            m_StoredLength = GetLength();
            m_StoredPrimitive = GetPrimitive();
        }


        // TODO: Not this
        public override object GetStoredValueForCopy()
        {
            switch (m_StoredLength)
            {
                case GraphType.Length.One:
                    switch (m_StoredPrimitive)
                    {
                        case GraphType.Primitive.Int: return (int)m_StoredValue.x;
                        case GraphType.Primitive.Bool: return Convert.ToBoolean(m_StoredValue.x);
                        case GraphType.Primitive.Float:
                        default: return m_StoredValue.x;
                    }
                case GraphType.Length.Two: return new Vector2(m_StoredValue.x, m_StoredValue.y);
                case GraphType.Length.Three: return new Vector3(m_StoredValue.x, m_StoredValue.y, m_StoredValue.z);
                case GraphType.Length.Four: return m_StoredValue;
                default: return DefaultValue;
            }
        }

        // This is a hack needed to support how GTF intends for constant values to duplicated
        // The value needs to be stored in a serializable field, and then applied to the new constant
        [SerializeField]
        Vector4 m_StoredValue;

        [SerializeField]
        GraphType.Length m_StoredLength;

        [SerializeField]
        GraphType.Primitive m_StoredPrimitive;

        protected override object GetValue()
        {
            switch (GetLength())
            {
                case GraphType.Length.One:
                    switch (GetPrimitive())
                    {
                        case GraphType.Primitive.Int: return GraphTypeHelpers.GetAsInt(GetField());
                        case GraphType.Primitive.Bool: return GraphTypeHelpers.GetAsBool(GetField());
                        case GraphType.Primitive.Float:
                        default: return GraphTypeHelpers.GetAsFloat(GetField());
                    }
                case GraphType.Length.Two: return GraphTypeHelpers.GetAsVec2(GetField());
                case GraphType.Length.Three: return GraphTypeHelpers.GetAsVec3(GetField());
                case GraphType.Length.Four when Temp_DisplayAsColor: return (Color)GraphTypeHelpers.GetAsVec4(GetField());
                case GraphType.Length.Four: return GraphTypeHelpers.GetAsVec4(GetField());
                default: return DefaultValue;
            }
        }

        protected override void SetValue(object value)
        {
            if (value is Color color)
            {
                value = (Vector4)color;
            }

            switch (GetLength())
            {
                case GraphType.Length.One:
                    switch (GetPrimitive())
                    {
                        case GraphType.Primitive.Int: GraphTypeHelpers.SetAsInt(GetField(), Convert.ToInt32(value)); break;
                        case GraphType.Primitive.Bool: GraphTypeHelpers.SetAsBool(GetField(), Convert.ToBoolean(value)); break;
                        case GraphType.Primitive.Float:
                        default: GraphTypeHelpers.SetAsFloat(GetField(), Convert.ToSingle(value)); break;
                    }
                    break;

                case GraphType.Length.Two: GraphTypeHelpers.SetAsVec2(GetField(), (Vector2)value); break;
                case GraphType.Length.Three: GraphTypeHelpers.SetAsVec3(GetField(), (Vector3)value); break;
                case GraphType.Length.Four: GraphTypeHelpers.SetAsVec4(GetField(), (Vector4)value); break;
            }
        }
        public override object DefaultValue => Activator.CreateInstance(Type);
        public override Type Type
        {
            get
            {
                switch (GetLength())
                {
                    case GraphType.Length.Two: return typeof(Vector2);
                    case GraphType.Length.Three: return typeof(Vector3);
                    case GraphType.Length.Four when Temp_DisplayAsColor: return typeof(Color);
                    case GraphType.Length.Four: return typeof(Vector4);
                    default:
                        switch (GetPrimitive())
                        {
                            case GraphType.Primitive.Int: return typeof(int);
                            case GraphType.Primitive.Bool: return typeof(bool);
                            default: return typeof(float);
                        }
                }
            }
        }
        public override TypeHandle GetTypeHandle()
        {
            try
            {
                switch (GetLength())
                {
                    case GraphType.Length.Two: return TypeHandle.Vector2;
                    case GraphType.Length.Three: return TypeHandle.Vector3;
                    case GraphType.Length.Four when Temp_DisplayAsColor: return ShaderGraphExampleTypes.Color;
                    case GraphType.Length.Four: return TypeHandle.Vector4;
                    default:
                        switch (GetPrimitive())
                        {
                            case GraphType.Primitive.Int: return TypeHandle.Int;
                            case GraphType.Primitive.Bool: return TypeHandle.Bool;
                            default: return TypeHandle.Float;
                        }
                }
            }
            catch(Exception e)
            {
                // TODO: (Sai) Currently, when a blackboard item is deleted, the inspector still tries to draw it after
                // this causes exceptions due to missing graph data, need to investigate more
                Debug.LogException(e);
                return TypeHandle.Unknown;
            }
        }

        // TODO: Proper abstraction - just experimenting rn.
        public bool Temp_DisplayAsColor { get; set; }
    }
}
