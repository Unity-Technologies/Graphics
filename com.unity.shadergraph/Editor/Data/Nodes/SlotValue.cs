using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    enum SlotValueType
    {
        SamplerState,
        DynamicMatrix,
        Matrix4,
        Matrix3,
        Matrix2,
        Texture2D,
        Texture2DArray,
        Texture3D,
        Cubemap,
        Gradient,
        DynamicVector,
        Vector4,
        Vector3,
        Vector2,
        Vector1,
        Dynamic,
        Boolean
    }

    enum ConcreteSlotValueType
    {
        SamplerState,
        Matrix4,
        Matrix3,
        Matrix2,
        Texture2D,
        Texture2DArray,
        Texture3D,
        Cubemap,
        Gradient,
        Vector4,
        Vector3,
        Vector2,
        Vector1,
        Boolean
    }

    static class SlotValueHelper
    {
        public static int GetChannelCount(this ConcreteSlotValueType type)
        {
            switch (type)
            {
                case ConcreteSlotValueType.Vector4:
                    return 4;
                case ConcreteSlotValueType.Vector3:
                    return 3;
                case ConcreteSlotValueType.Vector2:
                    return 2;
                case ConcreteSlotValueType.Vector1:
                    return 1;
                default:
                    return 0;
            }
        }

        public static int GetMatrixDimension(ConcreteSlotValueType type)
        {
            switch (type)
            {
                case ConcreteSlotValueType.Matrix4:
                    return 4;
                case ConcreteSlotValueType.Matrix3:
                    return 3;
                case ConcreteSlotValueType.Matrix2:
                    return 2;
                default:
                    return 0;
            }
        }

        public static ConcreteSlotValueType ConvertMatrixToVectorType(ConcreteSlotValueType matrixType)
        {
            switch (matrixType)
            {
                case ConcreteSlotValueType.Matrix4:
                    return ConcreteSlotValueType.Vector4;
                case ConcreteSlotValueType.Matrix3:
                    return ConcreteSlotValueType.Vector3;
                default:
                    return ConcreteSlotValueType.Vector2;
            }
        }

        static readonly string[] k_ConcreteSlotValueTypeClassNames =
        {
            null,
            "typeMatrix",
            "typeMatrix",
            "typeMatrix",
            "typeTexture2D",
            "typeTexture2DArray",
            "typeTexture3D",
            "typeCubemap",
            "typeGradient",
            "typeFloat4",
            "typeFloat3",
            "typeFloat2",
            "typeFloat1",
            "typeBoolean"
        };

        public static string ToClassName(this ConcreteSlotValueType type)
        {
            return k_ConcreteSlotValueTypeClassNames[(int)type];
        }

        public static string ToShaderString(this ConcreteSlotValueType type, ConcretePrecision concretePrecision)
        {
            string precisionString = concretePrecision.ToShaderString();
            return type.ToShaderString(precisionString);
        }

        public static string ToShaderString(this ConcreteSlotValueType type, string precisionToken = PrecisionUtil.Token)
        {
            switch (type)
            {
                case ConcreteSlotValueType.Boolean:
                    return precisionToken;
                case ConcreteSlotValueType.Vector1:
                    return precisionToken;
                case ConcreteSlotValueType.Vector2:
                    return precisionToken + "2";
                case ConcreteSlotValueType.Vector3:
                    return precisionToken + "3";
                case ConcreteSlotValueType.Vector4:
                    return precisionToken + "4";
                case ConcreteSlotValueType.Texture2D:
                    return "Texture2D";
                case ConcreteSlotValueType.Texture2DArray:
                    return "Texture2DArray";
                case ConcreteSlotValueType.Texture3D:
                    return "Texture3D";
                case ConcreteSlotValueType.Cubemap:
                    return "TextureCube";
                case ConcreteSlotValueType.Gradient:
                    return "Gradient";
                case ConcreteSlotValueType.Matrix2:
                    return precisionToken + "2x2";
                case ConcreteSlotValueType.Matrix3:
                    return precisionToken + "3x3";
                case ConcreteSlotValueType.Matrix4:
                    return precisionToken + "4x4";
                case ConcreteSlotValueType.SamplerState:
                    return "SamplerState";
                default:
                    return "Error";
            }
        }
    }
}
