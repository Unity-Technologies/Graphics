using System;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public enum SlotValueType
    {
		SamplerState,
        Matrix4,
        Matrix3,
        Matrix2,
        Texture2D,
        Dynamic,
        Vector4,
        Vector3,
        Vector2,
        Vector1
    }

    public enum ConcreteSlotValueType
    {
		SamplerState = 9,
        Matrix4 = 8,
        Matrix3 = 7,
        Matrix2 = 6,
        Texture2D = 5,
        Vector4 = 4,
        Vector3 = 3,
        Vector2 = 2,
        Vector1 = 1,
        Error = 0
    }
}
