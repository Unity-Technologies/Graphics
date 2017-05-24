using System;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public enum SlotValueType
    {
        Matrix4,
        Matrix3,
        Matrix2,
        sampler2D,
        Dynamic,
        Vector4,
        Vector3,
        Vector2,
        Vector1
    }

    public enum ConcreteSlotValueType
    {
        Matrix4 = 8,
        Matrix3 = 7,
        Matrix2 = 6,
        sampler2D = 5,
        Vector4 = 4,
        Vector3 = 3,
        Vector2 = 2,
        Vector1 = 1,
        Error = 0
    }
}
