using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.VFX
{
    // TODO remove me when not needed anymore
    static class VFXPropertyConverter
    {
        internal static VFXPropertyTypeSemantics CreateSemantics(VFXValueType type)
        {
            switch(type)
            {
                case VFXValueType.kFloat:       return new VFXFloatType();
                case VFXValueType.kFloat2:      return new VFXFloat2Type();
                case VFXValueType.kFloat3:      return new VFXFloat3Type();
                case VFXValueType.kFloat4:      return new VFXFloat4Type();
                case VFXValueType.kInt:         return new VFXIntType();
                case VFXValueType.kUint:        return new VFXUintType();
                case VFXValueType.kTexture2D:   return new VFXTexture2DType();
                case VFXValueType.kTexture3D:   return new VFXTexture3DType();
                default:                        return null; 
            }
        }
    }
}