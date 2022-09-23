using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class GraphTypeConstant : BaseShaderGraphConstant
    {
        internal GraphType.Length GetLength() => IsInitialized ? GraphTypeHelpers.GetLength(GetField()) : GraphType.Length.Four;
        internal GraphType.Height GetHeight() => IsInitialized ? GraphTypeHelpers.GetHeight(GetField()) : GraphType.Height.One;

        private GraphType.Primitive GetPrimitive() => IsInitialized ? GraphTypeHelpers.GetPrimitive(GetField()) : GraphType.Primitive.Float;

        // TODO: Not this
        protected override void StoreValueForCopy()
        {
            if (!IsInitialized)
                return;

            m_StoredLength = GetLength();
            m_StoredHeight = GetHeight();
            m_StoredPrimitive = GetPrimitive();

            switch (GetHeight())
            {
                case GraphType.Height.Four:
                    m_StoredValue = GraphTypeHelpers.GetAsMat4(GetField());
                    return;
                case GraphType.Height.Three:
                    m_StoredValue = GraphTypeHelpers.GetAsMat3(GetField());
                    return;
                case GraphType.Height.Two:
                    m_StoredValue = GraphTypeHelpers.GetAsMat2(GetField());
                    return;
            }

            // Height is 1 at this point
            m_StoredValue = new Matrix4x4();
            switch (GetLength())
            {
                case GraphType.Length.One:
                    switch (GetPrimitive())
                    {
                        case GraphType.Primitive.Int:
                            m_StoredValue.m00 = GraphTypeHelpers.GetAsInt(GetField());
                            break;
                        case GraphType.Primitive.Bool:
                            m_StoredValue.m00 = Convert.ToSingle(GraphTypeHelpers.GetAsBool(GetField()));
                            break;
                        case GraphType.Primitive.Float:
                        default:
                            m_StoredValue.m00 = GraphTypeHelpers.GetAsFloat(GetField());
                            break;
                    }

                    break;
                case GraphType.Length.Two:
                    m_StoredValue.SetColumn(0, GraphTypeHelpers.GetAsVec2(GetField()));
                    break;
                case GraphType.Length.Three:
                    m_StoredValue.SetColumn(0, GraphTypeHelpers.GetAsVec3(GetField()));
                    break;
                case GraphType.Length.Four:
                    m_StoredValue.SetColumn(0, GraphTypeHelpers.GetAsVec4(GetField()));
                    break;
                default:
                    m_StoredValue.SetColumn(0, (Vector4)DefaultValue);
                    break;
            }
        }

        // TODO: Not this
        public override object GetStoredValueForCopy()
        {
            if (m_StoredHeight > GraphType.Height.One)
            {
                // 2x2, 3x3, and 4x4 matrices are all using a Matrix4x4.
                return m_StoredValue;
            }

            switch (m_StoredLength)
            {
                case GraphType.Length.One:
                    switch (m_StoredPrimitive)
                    {
                        case GraphType.Primitive.Int: return (int)m_StoredValue.m00;
                        case GraphType.Primitive.Bool: return Convert.ToBoolean(m_StoredValue.m00);
                        case GraphType.Primitive.Float:
                        default: return m_StoredValue.m00;
                    }
                case GraphType.Length.Two: return (Vector2)m_StoredValue.GetColumn(0);
                case GraphType.Length.Three: return (Vector3)m_StoredValue.GetColumn(0);
                case GraphType.Length.Four: return m_StoredValue.GetColumn(0);
                default: return DefaultValue;
            }
        }

        // This is a hack needed to support how GTF intends for constant values to duplicated
        // The value needs to be stored in a serializable field, and then applied to the new constant
        // - Int/float/bool are stored in m00
        // - Vectors are stored in column 0
        // - 2x2 and 3x3 matrices are stored in the top-left, the same format used by GraphType helper methods
        [SerializeField]
        Matrix4x4 m_StoredValue;

        [SerializeField]
        GraphType.Length m_StoredLength;

        [SerializeField]
        GraphType.Height m_StoredHeight;

        [SerializeField]
        GraphType.Primitive m_StoredPrimitive;

        protected override object GetValue()
        {
            switch (GetHeight())
            {
                case GraphType.Height.Four: return GraphTypeHelpers.GetAsMat4(GetField());
                case GraphType.Height.Three: return GraphTypeHelpers.GetAsMat3(GetField());
                case GraphType.Height.Two: return GraphTypeHelpers.GetAsMat2(GetField());
            }

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
                case GraphType.Length.Four: return GraphTypeHelpers.GetAsVec4(GetField());
                default: return DefaultValue;
            }
        }

        protected override void SetValue(object value)
        {
            switch (GetHeight())
            {
                case GraphType.Height.Four:
                    GraphTypeHelpers.SetAsMat4(GetField(), (Matrix4x4)value);
                    return;
                case GraphType.Height.Three:
                    GraphTypeHelpers.SetAsMat3(GetField(), (Matrix4x4)value);
                    return;
                case GraphType.Height.Two:
                    GraphTypeHelpers.SetAsMat2(GetField(), (Matrix4x4)value);
                    return;
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
                if (GetHeight() > GraphType.Height.One)
                {
                    return typeof(Matrix4x4);
                }

                switch (GetLength())
                {
                    case GraphType.Length.Two: return typeof(Vector2);
                    case GraphType.Length.Three: return typeof(Vector3);
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
                switch (GetHeight())
                {
                    case GraphType.Height.Four: return ShaderGraphExampleTypes.Matrix4;
                    case GraphType.Height.Three: return ShaderGraphExampleTypes.Matrix3;
                    case GraphType.Height.Two: return ShaderGraphExampleTypes.Matrix2;
                }

                switch (GetLength())
                {
                    case GraphType.Length.Two: return TypeHandle.Vector2;
                    case GraphType.Length.Three: return TypeHandle.Vector3;
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
    }
}
