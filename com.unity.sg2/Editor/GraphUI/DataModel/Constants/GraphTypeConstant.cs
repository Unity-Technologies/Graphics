
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

        protected override void StoreValue()
        {
            if (!IsInitialized)
                return;
            switch (GetLength())
            {
                case GraphType.Length.One:
                    switch (GetPrimitive())
                    {
                        case GraphType.Primitive.Int: storedValue.x = GraphTypeHelpers.GetAsInt(GetField());
                            break;
                        case GraphType.Primitive.Bool: storedValue.x = Convert.ToSingle(GraphTypeHelpers.GetAsBool(GetField()));
                            break;
                        case GraphType.Primitive.Float:
                        default: storedValue.x = GraphTypeHelpers.GetAsFloat(GetField());
                            break;
                    }
                    break;
                case GraphType.Length.Two: storedValue = GraphTypeHelpers.GetAsVec2(GetField());
                    break;
                case GraphType.Length.Three: storedValue = GraphTypeHelpers.GetAsVec3(GetField());
                    break;
                case GraphType.Length.Four: storedValue = GraphTypeHelpers.GetAsVec4(GetField());
                    break;
                default: storedValue = (Vector4)DefaultValue;
                    break;
            }
        }

        public override object GetStoredValue()
        {
            return storedValue;
        }

        override protected object GetValue()
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
                case GraphType.Length.Four: return GraphTypeHelpers.GetAsVec4(GetField());
                default: return DefaultValue;
            }
        }

        [SerializeField]
        Vector4 storedValue;

        override protected void SetValue(object value)
        {
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
        override public object DefaultValue => Activator.CreateInstance(Type);
        override public Type Type
        {
            get
            {
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
        override public TypeHandle GetTypeHandle()
        {
            try
            {
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
