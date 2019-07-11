using System;
using System.Collections.Generic;
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
        public static SlotValueType ToSlotValueType(this ConcreteSlotValueType concreteValueType)
        {
            switch(concreteValueType)
            {
                case ConcreteSlotValueType.SamplerState:
                    return SlotValueType.SamplerState;
                case ConcreteSlotValueType.Matrix2:
                    return SlotValueType.Matrix2;
                case ConcreteSlotValueType.Matrix3:
                    return SlotValueType.Matrix3;
                case ConcreteSlotValueType.Matrix4:
                    return SlotValueType.Matrix4;
                case ConcreteSlotValueType.Texture2D:
                    return SlotValueType.Texture2D;
                case ConcreteSlotValueType.Texture2DArray:
                    return SlotValueType.Texture2DArray;
                case ConcreteSlotValueType.Texture3D:
                    return SlotValueType.Texture3D;
                case ConcreteSlotValueType.Cubemap:
                    return SlotValueType.Cubemap;
                case ConcreteSlotValueType.Gradient:
                    return SlotValueType.Gradient;
                case ConcreteSlotValueType.Vector4:
                    return SlotValueType.Vector4;
                case ConcreteSlotValueType.Vector3:
                    return SlotValueType.Vector3;
                case ConcreteSlotValueType.Vector2:
                    return SlotValueType.Vector2;
                case ConcreteSlotValueType.Vector1:
                    return SlotValueType.Vector1;
                case ConcreteSlotValueType.Boolean:
                    return SlotValueType.Boolean;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public static ConcreteSlotValueType ToConcreteSlotValueType(this SlotValueType slotValueType)
        {
            switch (slotValueType)
            {
                case SlotValueType.SamplerState:
                    return ConcreteSlotValueType.SamplerState;
                case SlotValueType.Matrix2:
                    return ConcreteSlotValueType.Matrix2;
                case SlotValueType.Matrix3:
                    return ConcreteSlotValueType.Matrix3;
                case SlotValueType.Matrix4:
                    return ConcreteSlotValueType.Matrix4;
                case SlotValueType.Texture2D:
                    return ConcreteSlotValueType.Texture2D;
                case SlotValueType.Texture2DArray:
                    return ConcreteSlotValueType.Texture2DArray;
                case SlotValueType.Texture3D:
                    return ConcreteSlotValueType.Texture3D;
                case SlotValueType.Cubemap:
                    return ConcreteSlotValueType.Cubemap;
                case SlotValueType.Gradient:
                    return ConcreteSlotValueType.Gradient;
                case SlotValueType.Vector4:
                    return ConcreteSlotValueType.Vector4;
                case SlotValueType.Vector3:
                    return ConcreteSlotValueType.Vector3;
                case SlotValueType.Vector2:
                    return ConcreteSlotValueType.Vector2;
                case SlotValueType.Vector1:
                    return ConcreteSlotValueType.Vector1;
                case SlotValueType.Boolean:
                    return ConcreteSlotValueType.Boolean;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }



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

        static Dictionary<ConcreteSlotValueType, List<SlotValueType>> s_ValidConversions;
        static List<SlotValueType> s_ValidSlotTypes;
        public static bool AreCompatible(SlotValueType inputType, ConcreteSlotValueType outputType)
        {
            if (s_ValidConversions == null)
            {
                var validVectors = new List<SlotValueType>()
                {
                    SlotValueType.Dynamic, SlotValueType.DynamicVector,
                    SlotValueType.Vector1, SlotValueType.Vector2, SlotValueType.Vector3, SlotValueType.Vector4
                };

                s_ValidConversions = new Dictionary<ConcreteSlotValueType, List<SlotValueType>>()
                {
                    {ConcreteSlotValueType.Boolean, new List<SlotValueType>() {SlotValueType.Boolean}},
                    {ConcreteSlotValueType.Vector1, validVectors},
                    {ConcreteSlotValueType.Vector2, validVectors},
                    {ConcreteSlotValueType.Vector3, validVectors},
                    {ConcreteSlotValueType.Vector4, validVectors},
                    {ConcreteSlotValueType.Matrix2, new List<SlotValueType>()
                        {SlotValueType.Dynamic, SlotValueType.DynamicMatrix, SlotValueType.Matrix2}},
                    {ConcreteSlotValueType.Matrix3, new List<SlotValueType>()
                        {SlotValueType.Dynamic, SlotValueType.DynamicMatrix, SlotValueType.Matrix2, SlotValueType.Matrix3}},
                    {ConcreteSlotValueType.Matrix4, new List<SlotValueType>()
                        {SlotValueType.Dynamic, SlotValueType.DynamicMatrix, SlotValueType.Matrix2, SlotValueType.Matrix3, SlotValueType.Matrix4}},
                    {ConcreteSlotValueType.Texture2D, new List<SlotValueType>() {SlotValueType.Texture2D}},
                    {ConcreteSlotValueType.Texture3D, new List<SlotValueType>() {SlotValueType.Texture3D}},
                    {ConcreteSlotValueType.Texture2DArray, new List<SlotValueType>() {SlotValueType.Texture2DArray}},
                    {ConcreteSlotValueType.Cubemap, new List<SlotValueType>() {SlotValueType.Cubemap}},
                    {ConcreteSlotValueType.SamplerState, new List<SlotValueType>() {SlotValueType.SamplerState}},
                    {ConcreteSlotValueType.Gradient, new List<SlotValueType>() {SlotValueType.Gradient}},
                };
            }

            if(s_ValidConversions.TryGetValue(outputType, out s_ValidSlotTypes))
            {
                return s_ValidSlotTypes.Contains(inputType);
            }
            throw new ArgumentOutOfRangeException("Unknown Concrete Slot Type: " + outputType);
        }

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
