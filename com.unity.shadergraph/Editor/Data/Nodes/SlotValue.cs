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
        Boolean,
        VirtualTexture
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
        Boolean,
        VirtualTexture
    }

    // This enum must match ConcreteSlotValueType enum and is used to give friendly name in the enum popup used for custom function
    enum ConcreteSlotValueTypePopupName
    {
        SamplerState = ConcreteSlotValueType.SamplerState,
        Matrix4 = ConcreteSlotValueType.Matrix4,
        Matrix3 = ConcreteSlotValueType.Matrix3,
        Matrix2 = ConcreteSlotValueType.Matrix2,
        Texture2D = ConcreteSlotValueType.Texture2D,
        Texture2DArray = ConcreteSlotValueType.Texture2DArray,
        Texture3D = ConcreteSlotValueType.Texture3D,
        Cubemap = ConcreteSlotValueType.Cubemap,
        Gradient = ConcreteSlotValueType.Gradient,
        Vector4 = ConcreteSlotValueType.Vector4,
        Vector3 = ConcreteSlotValueType.Vector3,
        Vector2 = ConcreteSlotValueType.Vector2,
        Float = ConcreteSlotValueType.Vector1, // This is currently the only renaming we need - rename Vector1 to Float
        Boolean = ConcreteSlotValueType.Boolean,
        VirtualTexture = ConcreteSlotValueType.VirtualTexture,

        // These allow the user to choose 'bare' types for custom function nodes
        // they are treated specially in the conversion functions below
        BareSamplerState = 1000 + ConcreteSlotValueType.SamplerState,
        BareTexture2D = 1000 + ConcreteSlotValueType.Texture2D,
        BareTexture2DArray = 1000 + ConcreteSlotValueType.Texture2DArray,
        BareTexture3D = 1000 + ConcreteSlotValueType.Texture3D,
        BareCubemap = 1000 + ConcreteSlotValueType.Cubemap,
    }

    static class SlotValueHelper
    {
        public static ConcreteSlotValueTypePopupName ToConcreteSlotValueTypePopupName(this ConcreteSlotValueType slotType, bool isBareResource)
        {
            ConcreteSlotValueTypePopupName result = (ConcreteSlotValueTypePopupName)slotType;
            switch (slotType)
            {
                case ConcreteSlotValueType.SamplerState:
                    if (isBareResource)
                        result = ConcreteSlotValueTypePopupName.BareSamplerState;
                    break;
                case ConcreteSlotValueType.Texture2D:
                    if (isBareResource)
                        result = ConcreteSlotValueTypePopupName.BareTexture2D;
                    break;
                case ConcreteSlotValueType.Texture2DArray:
                    if (isBareResource)
                        result = ConcreteSlotValueTypePopupName.BareTexture2DArray;
                    break;
                case ConcreteSlotValueType.Texture3D:
                    if (isBareResource)
                        result = ConcreteSlotValueTypePopupName.BareTexture3D;
                    break;
                case ConcreteSlotValueType.Cubemap:
                    if (isBareResource)
                        result = ConcreteSlotValueTypePopupName.BareCubemap;
                    break;
            }
            return result;
        }

        public static ConcreteSlotValueType ToConcreteSlotValueType(this ConcreteSlotValueTypePopupName popup, out bool isBareResource)
        {
            switch (popup)
            {
                case ConcreteSlotValueTypePopupName.BareSamplerState:
                    isBareResource = true;
                    return ConcreteSlotValueType.SamplerState;
                case ConcreteSlotValueTypePopupName.BareTexture2D:
                    isBareResource = true;
                    return ConcreteSlotValueType.Texture2D;
                case ConcreteSlotValueTypePopupName.BareTexture2DArray:
                    isBareResource = true;
                    return ConcreteSlotValueType.Texture2DArray;
                case ConcreteSlotValueTypePopupName.BareTexture3D:
                    isBareResource = true;
                    return ConcreteSlotValueType.Texture3D;
                case ConcreteSlotValueTypePopupName.BareCubemap:
                    isBareResource = true;
                    return ConcreteSlotValueType.Cubemap;
            }
            ;

            isBareResource = false;
            return (ConcreteSlotValueType)popup;
        }

        public static bool AllowedAsSubgraphOutput(this ConcreteSlotValueTypePopupName type)
        {
            // virtual textures and bare types disallowed
            return (type < ConcreteSlotValueTypePopupName.VirtualTexture);
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
                    {ConcreteSlotValueType.VirtualTexture, new List<SlotValueType>() {SlotValueType.VirtualTexture}}
                };
            }

            if (s_ValidConversions.TryGetValue(outputType, out s_ValidSlotTypes))
            {
                return s_ValidSlotTypes.Contains(inputType);
            }
            throw new ArgumentOutOfRangeException("Unknown Concrete Slot Type: " + outputType);
        }
    }
}
