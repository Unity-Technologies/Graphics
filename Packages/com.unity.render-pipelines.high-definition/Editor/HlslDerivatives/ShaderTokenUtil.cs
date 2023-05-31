using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition
{
    class ShaderTokenUtil
    {
        static internal int GetTypeNumRows(ConcreteSlotValueType slotValue)
        {
            int ret = -1;
            switch (slotValue)
            {
                case ConcreteSlotValueType.Matrix4:
                    ret = 4;
                    break;
                case ConcreteSlotValueType.Matrix3:
                    ret = 3;
                    break;
                case ConcreteSlotValueType.Matrix2:
                    ret = 2;
                    break;
                case ConcreteSlotValueType.Vector4:
                case ConcreteSlotValueType.Vector3:
                case ConcreteSlotValueType.Vector2:
                case ConcreteSlotValueType.Vector1:
                case ConcreteSlotValueType.Boolean:
                    ret = 1;
                    break;
                case ConcreteSlotValueType.SamplerState:
                case ConcreteSlotValueType.Texture2D:
                case ConcreteSlotValueType.Texture2DArray:
                case ConcreteSlotValueType.Texture3D:
                case ConcreteSlotValueType.Cubemap:
                case ConcreteSlotValueType.Gradient:
                case ConcreteSlotValueType.VirtualTexture:
                case ConcreteSlotValueType.PropertyConnectionState:
                    ret = -1;
                    break;

            }
            return ret;
        }

        static internal ConcreteSlotValueType GetVectorTypeFromNumCols(int numCols)
        {
            ConcreteSlotValueType ret = ConcreteSlotValueType.Vector1;

            switch (numCols)
            {
                case 1:
                    ret = ConcreteSlotValueType.Vector1;
                    break;
                case 2:
                    ret = ConcreteSlotValueType.Vector2;
                    break;
                case 3:
                    ret = ConcreteSlotValueType.Vector3;
                    break;
                case 4:
                    ret = ConcreteSlotValueType.Vector4;
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return ret;
        }

        static internal int GetTypeNumCols(ConcreteSlotValueType slotValue)
        {
            int ret = -1;
            switch (slotValue)
            {
                case ConcreteSlotValueType.Matrix4:
                    ret = 4;
                    break;
                case ConcreteSlotValueType.Matrix3:
                    ret = 3;
                    break;
                case ConcreteSlotValueType.Matrix2:
                    ret = 2;
                    break;
                case ConcreteSlotValueType.Vector4:
                    ret = 4;
                    break;
                case ConcreteSlotValueType.Vector3:
                    ret = 3;
                    break;
                case ConcreteSlotValueType.Vector2:
                    ret = 2;
                    break;
                case ConcreteSlotValueType.Vector1:
                    ret = 1;
                    break;
                case ConcreteSlotValueType.Boolean:
                    ret = 1;
                    break;
                case ConcreteSlotValueType.SamplerState:
                case ConcreteSlotValueType.Texture2D:
                case ConcreteSlotValueType.Texture2DArray:
                case ConcreteSlotValueType.Texture3D:
                case ConcreteSlotValueType.Cubemap:
                case ConcreteSlotValueType.Gradient:
                case ConcreteSlotValueType.VirtualTexture:
                case ConcreteSlotValueType.PropertyConnectionState:
                    ret = -1;
                    break;

            }
            return ret;
        }

        static internal int GetTypeNumElems(ConcreteSlotValueType slotValue)
        {
            int ret = -1;
            int numRows = GetTypeNumRows(slotValue);
            int numCols = GetTypeNumCols(slotValue);
            if (numRows >= 1 && numCols >= 1)
            {
                ret = numRows * numCols;
            }
            return ret;
        }

        static internal bool IsVectorType(ConcreteSlotValueType slotValue)
        {
            bool ret;

            switch (slotValue)
            {
                case ConcreteSlotValueType.Vector1:
                case ConcreteSlotValueType.Vector2:
                case ConcreteSlotValueType.Vector3:
                case ConcreteSlotValueType.Vector4:
                case ConcreteSlotValueType.Boolean: // bools are treated as scalars, more or less
                    ret = true;
                    break;
                default:
                    ret = false;
                    break;
            }

            return ret;
        }

        static internal bool IsMatrixType(ConcreteSlotValueType slotValue)
        {
            bool ret;

            switch (slotValue)
            {
                case ConcreteSlotValueType.Matrix2:
                case ConcreteSlotValueType.Matrix3:
                case ConcreteSlotValueType.Matrix4:
                    ret = true;
                    break;
                default:
                    ret = false;
                    break;
            }

            return ret;
        }

        static internal int FindSlotWithId(MaterialSlot[] slots, int slotId)
        {
            int foundIndex = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].id == slotId)
                {
                    foundIndex = i;
                    break;
                }
            }
            return foundIndex;
        }

        static internal string SlotTypeToString(ConcreteSlotValueType slotType, ApdStatus apdStatus, ConcretePrecision precision)
        {
            string precLower = (precision == ConcretePrecision.Single) ? "float" : "half";

            string ret = "";
            switch (slotType)
            {
                case ConcreteSlotValueType.SamplerState:
                    ret = "UnitySamplerState";
                    break;
                case ConcreteSlotValueType.Matrix4:
                    ret = precLower + "4x4";
                    break;
                case ConcreteSlotValueType.Matrix3:
                    ret = precLower + "3x3";
                    break;
                case ConcreteSlotValueType.Matrix2:
                    ret = precLower + "2x2";
                    break;
                case ConcreteSlotValueType.Texture2D:
                    ret = "UnityTexture2D";
                    break;
                case ConcreteSlotValueType.Texture2DArray:
                    ret = "UnityTexture2DArray";
                    break;
                case ConcreteSlotValueType.Texture3D:
                    ret = "UnityTexture3D";
                    break;
                case ConcreteSlotValueType.Cubemap:
                    ret = "UnityTextureCube";
                    break;
                case ConcreteSlotValueType.Gradient:
                    ret = "<gradient>";
                    break;
                case ConcreteSlotValueType.Vector4:
                    ret = PartialDerivUtilWriter.GetDeclName(3, apdStatus, precision);
                    break;
                case ConcreteSlotValueType.Vector3:
                    ret = PartialDerivUtilWriter.GetDeclName(2, apdStatus, precision);
                    break;
                case ConcreteSlotValueType.Vector2:
                    ret = PartialDerivUtilWriter.GetDeclName(1, apdStatus, precision);
                    break;
                case ConcreteSlotValueType.Vector1:
                    ret = PartialDerivUtilWriter.GetDeclName(0, apdStatus, precision);
                    break;
                case ConcreteSlotValueType.Boolean:
                    ret = precLower;
                    break;
                case ConcreteSlotValueType.VirtualTexture:
                    ret = "<virtual texture>";
                    break;
                case ConcreteSlotValueType.PropertyConnectionState:
                    ret = "<property connection>";
                    break;
            }

            return ret;
        }

    }
}
