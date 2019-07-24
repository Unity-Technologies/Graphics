using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class PropertyUtil
    {
        public static ConcreteSlotValueType ToConcreteShaderValueType(this PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.SamplerState:
                    return ConcreteSlotValueType.SamplerState;
                case PropertyType.Matrix4:
                    return ConcreteSlotValueType.Matrix4;
                case PropertyType.Matrix3:
                    return ConcreteSlotValueType.Matrix3;
                case PropertyType.Matrix2:
                    return ConcreteSlotValueType.Matrix2;
                case PropertyType.Texture2D:
                    return ConcreteSlotValueType.Texture2D;
                case PropertyType.Texture2DArray:
                    return ConcreteSlotValueType.Texture2DArray;
                case PropertyType.Texture3D:
                    return ConcreteSlotValueType.Texture3D;
                case PropertyType.Cubemap:
                    return ConcreteSlotValueType.Cubemap;
                case PropertyType.Gradient:
                    return ConcreteSlotValueType.Gradient;
                case PropertyType.Vector4:
                    return ConcreteSlotValueType.Vector4;
                case PropertyType.Vector3:
                    return ConcreteSlotValueType.Vector3;
                case PropertyType.Vector2:
                    return ConcreteSlotValueType.Vector2;
                case PropertyType.Vector1:
                    return ConcreteSlotValueType.Vector1;
                case PropertyType.Boolean:
                    return ConcreteSlotValueType.Boolean;
                case PropertyType.Color:
                    return ConcreteSlotValueType.Vector4;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
