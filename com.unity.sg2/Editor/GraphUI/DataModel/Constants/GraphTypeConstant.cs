using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    class GraphTypeConstant : BaseShaderGraphConstant
    {
        [SerializeField]
        Matrix4x4 m_CopyPasteData;

        internal GraphType.Length GetLength() => IsBound ? GraphTypeHelpers.GetLength(GetField()) : GraphType.Length.Four;
        internal GraphType.Height GetHeight() => IsBound ? GraphTypeHelpers.GetHeight(GetField()) : GraphType.Height.One;

        GraphType.Primitive GetPrimitive() => IsBound ? GraphTypeHelpers.GetPrimitive(GetField()) : GraphType.Primitive.Float;

        protected override object GetValue()
        {
            var field = GetField();
            if (field == null)
                return DefaultValue;

            return GraphTypeHelpers.GetFieldValue(field) ?? DefaultValue;
        }

        protected override void SetValue(object value)
        {
            var field = GetField();
            if (field == null)
                return;

            GraphTypeHelpers.SetFieldValue(field, value);
        }

        public override object DefaultValue
        {
            get
            {
                if (GetHeight() > GraphType.Height.One)
                {
                    return Matrix4x4.zero;
                }

                switch (GetLength())
                {
                    case GraphType.Length.Two: return Vector2.zero;
                    case GraphType.Length.Three: return Vector3.zero;
                    case GraphType.Length.Four: return Vector4.zero;
                    default:
                        switch (GetPrimitive())
                        {
                            case GraphType.Primitive.Int: return 0;
                            case GraphType.Primitive.Bool: return false;
                            default: return 0.0f;
                        }
                }
            }
        }

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

        /// <inheritdoc />
        public override void BindTo(string nodeName, string portName)
        {
            base.BindTo(nodeName, portName);

            if (OwnerModel is NodeModel nodeModel)
            {
                nodeModel.DefineNode();
            }
        }

        /// <inheritdoc />
        public override bool IsAssignableFrom(Type t)
        {
            return t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) || t == typeof(Matrix4x4) ||
                t == typeof(int) || t == typeof(bool) || t == typeof(float);
        }

        /// <inheritdoc />
        public override void OnBeforeCopy()
        {
            var typeHandle = GetTypeHandle();
            if (typeHandle == ShaderGraphExampleTypes.Matrix4 ||
                typeHandle == ShaderGraphExampleTypes.Matrix3 ||
                typeHandle == ShaderGraphExampleTypes.Matrix2)
            {
                m_CopyPasteData = (Matrix4x4)ObjectValue;
            }
            else if (typeHandle == TypeHandle.Int)
            {
                m_CopyPasteData[0] = (int)GetValue();
            }
            else if (typeHandle == TypeHandle.Bool)
            {
                m_CopyPasteData[0] = (bool)GetValue() ? 1 : 0;
            }
            else if (typeHandle == TypeHandle.Float)
            {
                m_CopyPasteData[0] = (float)GetValue();
            }
            else if (typeHandle == TypeHandle.Vector2)
            {
                var v = (Vector2)GetValue();
                m_CopyPasteData[0] = v.x;
                m_CopyPasteData[1] = v.y;
            }
            else if (typeHandle == TypeHandle.Vector3)
            {
                var v = (Vector3)GetValue();
                m_CopyPasteData[0] = v.x;
                m_CopyPasteData[1] = v.y;
                m_CopyPasteData[2] = v.z;
            }
            else if (typeHandle == TypeHandle.Vector4)
            {
                var v = (Vector4)GetValue();
                m_CopyPasteData[0] = v.x;
                m_CopyPasteData[1] = v.y;
                m_CopyPasteData[2] = v.z;
                m_CopyPasteData[3] = v.w;
            }
            else
            {
                Debug.LogError("Unexpected type: ${typeHandle}.");
            }
        }

        /// <inheritdoc />
        public override void OnAfterPaste()
        {
            var typeHandle = GetTypeHandle();
            if (typeHandle == ShaderGraphExampleTypes.Matrix4 ||
                typeHandle == ShaderGraphExampleTypes.Matrix3 ||
                typeHandle == ShaderGraphExampleTypes.Matrix2)
            {
                ObjectValue = m_CopyPasteData;
            }
            else if (typeHandle == TypeHandle.Int)
            {
                ObjectValue = m_CopyPasteData[0];
            }
            else if (typeHandle == TypeHandle.Bool)
            {
                ObjectValue = m_CopyPasteData[0] != 0;
            }
            else if (typeHandle == TypeHandle.Float)
            {
                ObjectValue = m_CopyPasteData[0];
            }
            else if (typeHandle == TypeHandle.Vector2)
            {
                var v = new Vector2(m_CopyPasteData[0], m_CopyPasteData[1]);
                ObjectValue = v;
            }
            else if (typeHandle == TypeHandle.Vector3)
            {
                var v = new Vector3(m_CopyPasteData[0], m_CopyPasteData[1], m_CopyPasteData[2]);
                ObjectValue = v;
            }
            else if (typeHandle == TypeHandle.Vector4)
            {
                ObjectValue = m_CopyPasteData.GetColumn(0);
            }
            else
            {
                Debug.LogError("Unexpected type: ${typeHandle}.");
            }
        }
    }
}
