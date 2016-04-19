using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.VFX
{
    static class VFXPropertyConverter
    {
        static VFXValueType ConvertType(VFXParam.Type type)
        {
            switch(type)
            {
                case kTypeFloat:        return VFXValueType.kFloat;
                case kTypeFloat2:       return VFXValueType.kFloat2;
                case kTypeFloat3:       return VFXValueType.kFloat3;
                case kTypeFloat4:       return VFXValueType.kFloat4;
                case kTypeInt:          return VFXValueType.kInt;
                case kTypeUint:         return VFXValueType.kUint;
                case kTypeTexture2D:    return VFXValueType.kTexture2D;
                case kTypeTexture3D:    return VFXValueType.kTexture3D;
                default:                return VFXValueType.kNone;
            }
        }

        static VFXPropertyTypeSemantics CreateSemantics(VFXValueType type)
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

        static VFXProperty CreateProperty(VFXParam param)
        {
            return new VFXProperty(CreateSemantics(ConvertType(param.m_Type)), param.m_Name);
        }

        static VFXProperty[] CreateProperties(VFXParam[] parameters)
        {
            if (parameters == null)
                return null;

            int nbProperties = parameters.Length;
            var properties = new VFXProperty[nbProperties];
            for (int i = 0; i < nbProperties; ++i)
                properties[i] = CreateProperty(parameters[i]);

            return properties;
        }
    }
}