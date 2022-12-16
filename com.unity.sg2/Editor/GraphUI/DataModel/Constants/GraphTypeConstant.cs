using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class GraphTypeConstant : BaseShaderGraphConstant
    {
        internal GraphType.Length GetLength() => IsBound ? GraphTypeHelpers.GetLength(GetField()) : GraphType.Length.Four;
        internal GraphType.Height GetHeight() => IsBound ? GraphTypeHelpers.GetHeight(GetField()) : GraphType.Height.One;

        GraphType.Primitive GetPrimitive() => IsBound ? GraphTypeHelpers.GetPrimitive(GetField()) : GraphType.Primitive.Float;

        protected override object GetValue()
        {
            var field = GetField();
            if (field == null)
                return DefaultValue;

            if (!IsBound)
                return GraphTypeHelpers.GetAsVec4(field);

            return GraphTypeHelpers.GetFieldValue(field, DefaultValue);
        }

        protected override void SetValue(object value)
        {
            var field = GetField();
            if (field == null)
                return;

            if (!IsBound)
                GraphTypeHelpers.SetAsVec4(field, (Vector4)value);

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

        /// <inheritdoc />
        public override bool IsAssignableFrom(Type t)
        {
            return t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) || t == typeof(Matrix4x4) ||
                t == typeof(int) || t == typeof(bool) || t == typeof(float);
        }
    }
}
