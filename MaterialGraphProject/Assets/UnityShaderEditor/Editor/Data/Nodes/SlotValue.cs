using System;

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
        Vector1
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
        Vector1
    }

    public static class SlotValueHelper
    {
        public enum ChannelCount
        {
            Zero = 0,
            One = 1,
            Two = 2,
            Three = 3,
            Four = 4,
        }

        public static ChannelCount GetChannelCount(ConcreteSlotValueType type)
        {
            switch (type)
            {
                case ConcreteSlotValueType.Vector4:
                    return ChannelCount.Four;
                case ConcreteSlotValueType.Vector3:
                    return ChannelCount.Three;
                case ConcreteSlotValueType.Vector2:
                    return ChannelCount.Two;
                case ConcreteSlotValueType.Vector1:
                    return ChannelCount.One;
                default:
                    return ChannelCount.Zero;
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
            "typeFloat1"
        };

        public static string ToClassName(this ConcreteSlotValueType type)
        {
            return k_ConcreteSlotValueTypeClassNames[(int)type];
        }
    }
}
