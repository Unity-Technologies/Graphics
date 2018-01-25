using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public enum SlotValueType
    {
        SamplerState,
        Matrix4,
        Matrix3,
        Matrix2,
        Texture2D,
        Cubemap,
        Dynamic,
        Vector4,
        Vector3,
        Vector2,
        Vector1,
        Boolean
    }

    public enum ConcreteSlotValueType
    {
        SamplerState,
        Matrix4,
        Matrix3,
        Matrix2,
        Texture2D,
        Cubemap,
        Vector4,
        Vector3,
        Vector2,
        Vector1,
        Boolean
    }

    public static class SlotValueHelper
    {
        public static int GetChannelCount(ConcreteSlotValueType type)
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

        static readonly string[] k_ConcreteSlotValueTypeClassNames =
        {
            null,
            "typeMatrix",
            "typeMatrix",
            "typeMatrix",
            "typeTexture2D",
            "typeCubemap",
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

        public static string ToString(this ConcreteSlotValueType type, AbstractMaterialNode.OutputPrecision precision)
        {
            return NodeUtils.ConvertConcreteSlotValueTypeToString(precision, type);
        }
    }
}
