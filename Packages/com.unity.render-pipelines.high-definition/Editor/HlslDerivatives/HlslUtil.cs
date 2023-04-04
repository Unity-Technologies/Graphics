using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    internal enum BinaryFunc
    {
        Add,
        Sub,
        Mul,
        Div,
        Min,
        Max,
        Pow,
        Reflect,
        Num
    };

    internal enum SingleFunc
    {
        Saturate,
        Rcp,
        Sqr,
        Log,
        Log2,
        Log10,
        Exp,
        Exp2,
        Sqrt,
        Rsqrt,
        Normalize,
        Frac,
        Cos,
        CosH,
        Sin,
        SinH,
        Tan,
        TanH,
        Abs,
        Negate,
        Floor,
        Ceil,
        Num
    };

    // These ops have different rules than the BinaryFunc/SingleFunc ops above. For examples,
    // length always returns a scalar type (even if the input is a vector).
    internal enum Func1
    {
        Len,
        LenSqr,
        InvLen,
        InvLenSqr,
        Num
    };

    internal enum Func2
    {
        Dot,
        Cross,
        Num
    };

    internal enum Func3
    {
        Lerp,
        Num
    };

    internal enum TexSampleFunc
    {
        Tex2D,
        Tex2DArray,
        TexCube,
        Tex3D,
    }

    internal enum TexSampleType
    {
        Lod0,
        Fpd,
        Apd,
        Apd_3x,
        Num
    };

    // APD = Analytic Partial Derivative
    enum ApdStatus
    {
        Unknown, // not known yet, used for cases when we haven't traversed this node yet
        Zero, // known to be zero, such as uniforms or values derived from uniforms
        NotNeeded, // known to not be needed by anything, so are not going to keep track of it. treated as zero.
        Valid, // valid. we need to evaluate this node's apd because we need it elsewhere
        Invalid, // invalid, we need this derivative, but we don't have it. used for complicated funcs that aren't implemented yet.
        Num
    };

    enum AttributeType
    {
        UV,
        Color
    }

    internal class HlslUtil
    {
        // for a given parameter in a function, what kinds of apd status can we give it?
        // OnlyApd: it MUST be apd (such as a texture sample)
        // OnlyFpd: never is apd, such as the lod bias
        // AllowApdVariation: there is an fpd/apd version of this function, but if
        //      one type upgrades than they all have to upgrade.
        // Any: the parameter can be apd/fpd independently of any other parameter.

        static internal bool IsValidSwizzleType(HlslNativeType type)
        {
            bool ret = false;
            if (HlslNativeType._float <= type && type <= HlslNativeType._bool4)
            {
                ret = true;
            }
            return ret;
        }

        static internal int GetNumRows(HlslNativeType type)
        {
            int ret = -1;
            switch (type)
            {
                case HlslNativeType._unknown: // default: unset
                case HlslNativeType._invalid: // known to be invalid because something went wrong with parsing
                case HlslNativeType._void:
                    ret = 0;
                    break;

                case HlslNativeType._float:
                case HlslNativeType._float1:
                case HlslNativeType._float2:
                case HlslNativeType._float3:
                case HlslNativeType._float4:

                case HlslNativeType._int:
                case HlslNativeType._int1:
                case HlslNativeType._int2:
                case HlslNativeType._int3:
                case HlslNativeType._int4:

                case HlslNativeType._half:
                case HlslNativeType._half1:
                case HlslNativeType._half2:
                case HlslNativeType._half3:
                case HlslNativeType._half4:

                case HlslNativeType._uint:
                case HlslNativeType._uint1:
                case HlslNativeType._uint2:
                case HlslNativeType._uint3:
                case HlslNativeType._uint4:

                case HlslNativeType._bool:
                case HlslNativeType._bool1:
                case HlslNativeType._bool2:
                case HlslNativeType._bool3:
                case HlslNativeType._bool4:

                case HlslNativeType._float1x2:
                case HlslNativeType._int1x2:
                case HlslNativeType._half1x2:
                case HlslNativeType._uint1x2:
                case HlslNativeType._bool1x2:
                case HlslNativeType._float1x3:
                case HlslNativeType._int1x3:
                case HlslNativeType._half1x3:
                case HlslNativeType._uint1x3:
                case HlslNativeType._bool1x3:
                case HlslNativeType._float1x4:
                case HlslNativeType._int1x4:
                case HlslNativeType._half1x4:
                case HlslNativeType._uint1x4:
                case HlslNativeType._bool1x4:
                    return 1;

                case HlslNativeType._float2x2:
                case HlslNativeType._int2x2:
                case HlslNativeType._half2x2:
                case HlslNativeType._uint2x2:
                case HlslNativeType._bool2x2:
                case HlslNativeType._float2x3:
                case HlslNativeType._int2x3:
                case HlslNativeType._half2x3:
                case HlslNativeType._uint2x3:
                case HlslNativeType._bool2x3:
                case HlslNativeType._float2x4:
                case HlslNativeType._int2x4:
                case HlslNativeType._half2x4:
                case HlslNativeType._uint2x4:
                case HlslNativeType._bool2x4:
                    return 2;

                case HlslNativeType._float3x2:
                case HlslNativeType._int3x2:
                case HlslNativeType._half3x2:
                case HlslNativeType._uint3x2:
                case HlslNativeType._bool3x2:
                case HlslNativeType._float3x3:
                case HlslNativeType._int3x3:
                case HlslNativeType._half3x3:
                case HlslNativeType._uint3x3:
                case HlslNativeType._bool3x3:
                case HlslNativeType._float3x4:
                case HlslNativeType._int3x4:
                case HlslNativeType._half3x4:
                case HlslNativeType._uint3x4:
                case HlslNativeType._bool3x4:
                    return 3;

                case HlslNativeType._float4x2:
                case HlslNativeType._int4x2:
                case HlslNativeType._half4x2:
                case HlslNativeType._uint4x2:
                case HlslNativeType._bool4x2:
                case HlslNativeType._float4x3:
                case HlslNativeType._int4x3:
                case HlslNativeType._half4x3:
                case HlslNativeType._uint4x3:
                case HlslNativeType._bool4x3:
                case HlslNativeType._float4x4:
                case HlslNativeType._int4x4:
                case HlslNativeType._half4x4:
                case HlslNativeType._uint4x4:
                case HlslNativeType._bool4x4:
                    return 4;

                case HlslNativeType._Texture:
                case HlslNativeType._Texture1D:
                case HlslNativeType._Texture1DArray:
                case HlslNativeType._Texture2D:
                case HlslNativeType._Texture2DArray:
                case HlslNativeType._Texture3D:
                case HlslNativeType._TextureCUBE:
                case HlslNativeType._TextureCUBEArray:
                case HlslNativeType._SamplerState:
                case HlslNativeType._SamplerComparisonState:

                case HlslNativeType._RWTexture2D:
                case HlslNativeType._RWTexture2DArray:
                case HlslNativeType._RWTexture3D:

                case HlslNativeType._struct: // user defined: of course
                default:
                    ret = 0;
                    break;

            }

            return ret;
        }

        static internal int GetNumCols(HlslNativeType type)
        {
            int ret = -1;
            switch (type)
            {
                case HlslNativeType._unknown: // default: unset
                case HlslNativeType._invalid: // known to be invalid because something went wrong with parsing
                case HlslNativeType._void:
                    ret = 0;
                    break;

                case HlslNativeType._float:
                case HlslNativeType._float1:
                case HlslNativeType._int:
                case HlslNativeType._int1:
                case HlslNativeType._half:
                case HlslNativeType._half1:
                case HlslNativeType._uint:
                case HlslNativeType._uint1:
                case HlslNativeType._bool:
                case HlslNativeType._bool1:
                    ret = 1;
                    break;

                case HlslNativeType._float2:
                case HlslNativeType._int2:
                case HlslNativeType._half2:
                case HlslNativeType._uint2:
                case HlslNativeType._bool2:
                case HlslNativeType._float1x2:
                case HlslNativeType._float2x2:
                case HlslNativeType._float3x2:
                case HlslNativeType._float4x2:

                case HlslNativeType._int1x2:
                case HlslNativeType._int2x2:
                case HlslNativeType._int3x2:
                case HlslNativeType._int4x2:

                case HlslNativeType._half1x2:
                case HlslNativeType._half2x2:
                case HlslNativeType._half3x2:
                case HlslNativeType._half4x2:

                case HlslNativeType._uint1x2:
                case HlslNativeType._uint2x2:
                case HlslNativeType._uint3x2:
                case HlslNativeType._uint4x2:

                case HlslNativeType._bool1x2:
                case HlslNativeType._bool2x2:
                case HlslNativeType._bool3x2:
                case HlslNativeType._bool4x2:

                    ret = 2;
                    break;

                case HlslNativeType._float3:
                case HlslNativeType._int3:
                case HlslNativeType._half3:
                case HlslNativeType._uint3:
                case HlslNativeType._bool3:
                case HlslNativeType._float1x3:
                case HlslNativeType._float2x3:
                case HlslNativeType._float3x3:
                case HlslNativeType._float4x3:

                case HlslNativeType._int1x3:
                case HlslNativeType._int2x3:
                case HlslNativeType._int3x3:
                case HlslNativeType._int4x3:

                case HlslNativeType._half1x3:
                case HlslNativeType._half2x3:
                case HlslNativeType._half3x3:
                case HlslNativeType._half4x3:

                case HlslNativeType._uint1x3:
                case HlslNativeType._uint2x3:
                case HlslNativeType._uint3x3:
                case HlslNativeType._uint4x3:

                case HlslNativeType._bool1x3:
                case HlslNativeType._bool2x3:
                case HlslNativeType._bool3x3:
                case HlslNativeType._bool4x3:

                    ret = 3;
                    break;

                case HlslNativeType._float4:
                case HlslNativeType._int4:
                case HlslNativeType._half4:
                case HlslNativeType._uint4:
                case HlslNativeType._bool4:

                case HlslNativeType._float1x4:
                case HlslNativeType._float2x4:
                case HlslNativeType._float3x4:
                case HlslNativeType._float4x4:

                case HlslNativeType._int1x4:
                case HlslNativeType._int2x4:
                case HlslNativeType._int3x4:
                case HlslNativeType._int4x4:

                case HlslNativeType._half1x4:
                case HlslNativeType._half2x4:
                case HlslNativeType._half3x4:
                case HlslNativeType._half4x4:

                case HlslNativeType._uint1x4:
                case HlslNativeType._uint2x4:
                case HlslNativeType._uint3x4:
                case HlslNativeType._uint4x4:

                case HlslNativeType._bool1x4:
                case HlslNativeType._bool2x4:
                case HlslNativeType._bool3x4:
                case HlslNativeType._bool4x4:
                    ret = 4;
                    break;

                case HlslNativeType._Texture:
                case HlslNativeType._Texture1D:
                case HlslNativeType._Texture1DArray:
                case HlslNativeType._Texture2D:
                case HlslNativeType._Texture2DArray:
                case HlslNativeType._Texture3D:
                case HlslNativeType._TextureCUBE:
                case HlslNativeType._TextureCUBEArray:
                case HlslNativeType._SamplerState:
                case HlslNativeType._SamplerComparisonState:

                case HlslNativeType._RWTexture2D:
                case HlslNativeType._RWTexture2DArray:
                case HlslNativeType._RWTexture3D:

                case HlslNativeType._struct: // user defined: of course
                default:
                    ret = 0;
                    break;

            }

            return ret;
        }

        // a "scalar" type is really anything that we can reasonably cast to a float
        static internal bool IsNativeTypeScalar(HlslNativeType type)
        {
            HlslNativeType baseType = GetNativeBaseType(type);

            bool ret = false;
            switch (baseType)
            {
                case HlslNativeType._float:
                case HlslNativeType._half:
                case HlslNativeType._int:
                case HlslNativeType._uint:
                    ret = true;
                    break;
                default:
                    ret = false;
                    break;
            }

            return ret;
        }

        static internal HlslNativeType GetNativeBaseType(HlslNativeType type)
        {
            HlslNativeType ret = HlslNativeType._invalid;
            switch (type)
            {
                case HlslNativeType._float:
                case HlslNativeType._float1:
                case HlslNativeType._float2:
                case HlslNativeType._float1x2:
                case HlslNativeType._float2x2:
                case HlslNativeType._float3x2:
                case HlslNativeType._float4x2:
                case HlslNativeType._float3:
                case HlslNativeType._float1x3:
                case HlslNativeType._float2x3:
                case HlslNativeType._float3x3:
                case HlslNativeType._float4x3:
                case HlslNativeType._float4:
                case HlslNativeType._float1x4:
                case HlslNativeType._float2x4:
                case HlslNativeType._float3x4:
                case HlslNativeType._float4x4:
                    ret = HlslNativeType._float;
                    break;

                case HlslNativeType._int:
                case HlslNativeType._int1:
                case HlslNativeType._int2:
                case HlslNativeType._int1x2:
                case HlslNativeType._int2x2:
                case HlslNativeType._int3x2:
                case HlslNativeType._int4x2:
                case HlslNativeType._int3:
                case HlslNativeType._int1x3:
                case HlslNativeType._int2x3:
                case HlslNativeType._int3x3:
                case HlslNativeType._int4x3:
                case HlslNativeType._int4:
                case HlslNativeType._int1x4:
                case HlslNativeType._int2x4:
                case HlslNativeType._int3x4:
                case HlslNativeType._int4x4:
                    ret = HlslNativeType._int;
                    break;
                case HlslNativeType._half:
                case HlslNativeType._half1:
                case HlslNativeType._half2:
                case HlslNativeType._half1x2:
                case HlslNativeType._half2x2:
                case HlslNativeType._half3x2:
                case HlslNativeType._half4x2:
                case HlslNativeType._half3:
                case HlslNativeType._half1x3:
                case HlslNativeType._half2x3:
                case HlslNativeType._half3x3:
                case HlslNativeType._half4x3:
                case HlslNativeType._half4:
                case HlslNativeType._half1x4:
                case HlslNativeType._half2x4:
                case HlslNativeType._half3x4:
                case HlslNativeType._half4x4:
                    ret = HlslNativeType._half;
                    break;

                case HlslNativeType._uint:
                case HlslNativeType._uint1:
                case HlslNativeType._uint2:
                case HlslNativeType._uint1x2:
                case HlslNativeType._uint2x2:
                case HlslNativeType._uint3x2:
                case HlslNativeType._uint4x2:
                case HlslNativeType._uint3:
                case HlslNativeType._uint1x3:
                case HlslNativeType._uint2x3:
                case HlslNativeType._uint3x3:
                case HlslNativeType._uint4x3:
                case HlslNativeType._uint4:
                case HlslNativeType._uint1x4:
                case HlslNativeType._uint2x4:
                case HlslNativeType._uint3x4:
                case HlslNativeType._uint4x4:
                    ret = HlslNativeType._uint;
                    break;

                case HlslNativeType._bool:
                case HlslNativeType._bool1:
                case HlslNativeType._bool2:
                case HlslNativeType._bool1x2:
                case HlslNativeType._bool2x2:
                case HlslNativeType._bool3x2:
                case HlslNativeType._bool4x2:
                case HlslNativeType._bool3:
                case HlslNativeType._bool1x3:
                case HlslNativeType._bool2x3:
                case HlslNativeType._bool3x3:
                case HlslNativeType._bool4x3:
                case HlslNativeType._bool4:
                case HlslNativeType._bool1x4:
                case HlslNativeType._bool2x4:
                case HlslNativeType._bool3x4:
                case HlslNativeType._bool4x4:
                    ret = HlslNativeType._bool;
                    break;

                case HlslNativeType._struct: // user defined: of course
                default:
                    ret = type;
                    break;
            }

            return ret;
        }

        // Only support float and half vectors. We could support matrices later.
        static internal bool IsNativeTypeLegalForApd(HlslNativeType type)
        {
            bool ret = false;
            switch (type)
            {
                case HlslNativeType._float:
                case HlslNativeType._float1:
                case HlslNativeType._float2:
                case HlslNativeType._float3:
                case HlslNativeType._float4:
                case HlslNativeType._half:
                case HlslNativeType._half1:
                case HlslNativeType._half2:
                case HlslNativeType._half3:
                case HlslNativeType._half4:
                    ret = true;
                    break;
                default:
                    ret = false;
                    break;
            }

            return ret;
        }

        static internal HlslNativeType GetVectorFromBaseType(HlslNativeType baseType, int vecLen)
        {
            HlslNativeType ret = HlslNativeType._invalid;
            HlslUtil.ParserAssert(1 <= vecLen && vecLen <= 4);
            switch (baseType)
            {
                case HlslNativeType._float:
                    {
                        switch (vecLen)
                        {
                            case 1:
                                ret = HlslNativeType._float;
                                break;
                            case 2:
                                ret = HlslNativeType._float2;
                                break;
                            case 3:
                                ret = HlslNativeType._float3;
                                break;
                            case 4:
                                ret = HlslNativeType._float4;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case HlslNativeType._half:
                    {
                        switch (vecLen)
                        {
                            case 1:
                                ret = HlslNativeType._half;
                                break;
                            case 2:
                                ret = HlslNativeType._half2;
                                break;
                            case 3:
                                ret = HlslNativeType._half3;
                                break;
                            case 4:
                                ret = HlslNativeType._half4;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case HlslNativeType._int:
                    {
                        switch (vecLen)
                        {
                            case 1:
                                ret = HlslNativeType._int;
                                break;
                            case 2:
                                ret = HlslNativeType._int2;
                                break;
                            case 3:
                                ret = HlslNativeType._int3;
                                break;
                            case 4:
                                ret = HlslNativeType._int4;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case HlslNativeType._uint:
                    {
                        switch (vecLen)
                        {
                            case 1:
                                ret = HlslNativeType._uint;
                                break;
                            case 2:
                                ret = HlslNativeType._uint2;
                                break;
                            case 3:
                                ret = HlslNativeType._uint3;
                                break;
                            case 4:
                                ret = HlslNativeType._uint4;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case HlslNativeType._bool:
                    {
                        switch (vecLen)
                        {
                            case 1:
                                ret = HlslNativeType._bool;
                                break;
                            case 2:
                                ret = HlslNativeType._bool2;
                                break;
                            case 3:
                                ret = HlslNativeType._bool3;
                                break;
                            case 4:
                                ret = HlslNativeType._bool4;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    // no op, returning _invalid seems fine.
                    break;
            }

            return ret;
        }

        // helper function to convert each type into a 0-4 index
        internal static int GetIndexHelperForBaseType(HlslNativeType type)
        {
            int ret = -1;
            switch (type)
            {
                case HlslNativeType._bool:
                    ret = 0;
                    break;
                case HlslNativeType._uint:
                    ret = 1;
                    break;
                case HlslNativeType._int:
                    ret = 2;
                    break;
                case HlslNativeType._half:
                    ret = 3;
                    break;
                case HlslNativeType._float:
                    ret = 4;
                    break;
                default:
                    break;
            }
            return ret;
        }

        static HlslNativeType GetBaseTypeForIndexHelper(int index)
        {
            HlslNativeType ret = HlslNativeType._invalid;
            switch (index)
            {
                case 0:
                    ret = HlslNativeType._bool;
                    break;
                case 1:
                    ret = HlslNativeType._uint;
                    break;
                case 2:
                    ret = HlslNativeType._int;
                    break;
                case 3:
                    ret = HlslNativeType._half;
                    break;
                case 4:
                    ret = HlslNativeType._float;
                    break;
                default:
                    break;
            }
            return ret;
        }

        internal static HlslNativeType GetBaseTypeResultFromBinaryOp(HlslNativeType lhsType, HlslNativeType rhsType)
        {
            int lhsIndex = GetIndexHelperForBaseType(lhsType);
            int rhsIndex = GetIndexHelperForBaseType(rhsType);

            // The casting ruls are, AFAICT undocumented. Best answer I could find was this one:
            //   https://github.com/KhronosGroup/glslang/issues/449
            // The main idea is:
            //   bool < uint < int < float < double
            // That being said, it's unclear how a half fits into this. So we'll go with this for now:
            //   bool < uint < int < half < float < double
            int dstIndex = Math.Max(lhsIndex, rhsIndex);

            HlslNativeType ret = GetBaseTypeForIndexHelper(dstIndex);
            return ret;
        }

        internal static HlslNativeType GetBaseTypeWithDims(HlslNativeType baseType, int numRows, int numCols)
        {
            HlslNativeType ret = HlslNativeType._invalid;

            HlslNativeType[] floatTypes = new HlslNativeType[16]
            {
                HlslNativeType._float,  HlslNativeType._float2,HlslNativeType._float3,HlslNativeType._float4,
                HlslNativeType._float2, HlslNativeType._float2x2,HlslNativeType._float2x3,HlslNativeType._float2x4,
                HlslNativeType._float3, HlslNativeType._float3x2,HlslNativeType._float3x3,HlslNativeType._float3x4,
                HlslNativeType._float4, HlslNativeType._float4x2,HlslNativeType._float4x3,HlslNativeType._float4x4,
            };

            HlslNativeType[] halfTypes = new HlslNativeType[16]
            {
                HlslNativeType._half,  HlslNativeType._half2,HlslNativeType._half3,HlslNativeType._half4,
                HlslNativeType._half2, HlslNativeType._half2x2,HlslNativeType._half2x3,HlslNativeType._half2x4,
                HlslNativeType._half3, HlslNativeType._half3x2,HlslNativeType._half3x3,HlslNativeType._half3x4,
                HlslNativeType._half4, HlslNativeType._half4x2,HlslNativeType._half4x3,HlslNativeType._half4x4,
            };

            HlslNativeType[] uintTypes = new HlslNativeType[16]
            {
                HlslNativeType._uint,  HlslNativeType._uint2,HlslNativeType._uint3,HlslNativeType._uint4,
                HlslNativeType._uint2, HlslNativeType._uint2x2,HlslNativeType._uint2x3,HlslNativeType._uint2x4,
                HlslNativeType._uint3, HlslNativeType._uint3x2,HlslNativeType._uint3x3,HlslNativeType._uint3x4,
                HlslNativeType._uint4, HlslNativeType._uint4x2,HlslNativeType._uint4x3,HlslNativeType._uint4x4,
            };

            HlslNativeType[] intTypes = new HlslNativeType[16]
            {
                HlslNativeType._int,  HlslNativeType._int2,HlslNativeType._int3,HlslNativeType._int4,
                HlslNativeType._int2, HlslNativeType._int2x2,HlslNativeType._int2x3,HlslNativeType._int2x4,
                HlslNativeType._int3, HlslNativeType._int3x2,HlslNativeType._int3x3,HlslNativeType._int3x4,
                HlslNativeType._int4, HlslNativeType._int4x2,HlslNativeType._int4x3,HlslNativeType._int4x4,
            };

            HlslNativeType[] boolTypes = new HlslNativeType[16]
            {
                HlslNativeType._bool,  HlslNativeType._bool2,HlslNativeType._bool3,HlslNativeType._bool4,
                HlslNativeType._bool2, HlslNativeType._bool2x2,HlslNativeType._bool2x3,HlslNativeType._bool2x4,
                HlslNativeType._bool3, HlslNativeType._bool3x2,HlslNativeType._bool3x3,HlslNativeType._bool3x4,
                HlslNativeType._bool4, HlslNativeType._bool4x2,HlslNativeType._bool4x3,HlslNativeType._bool4x4,
            };

            int colIndex = numCols - 1;
            int rowIndex = numRows - 1;

            if (colIndex >= 0 && colIndex < 4 && rowIndex >= 0 && rowIndex < 4)
            {
                switch (baseType)
                {
                    case HlslNativeType._float:
                        ret = floatTypes[rowIndex * 4 + colIndex];
                        break;
                    case HlslNativeType._half:
                        ret = halfTypes[rowIndex * 4 + colIndex];
                        break;
                    case HlslNativeType._int:
                        ret = floatTypes[rowIndex * 4 + colIndex];
                        break;
                    case HlslNativeType._uint:
                        ret = floatTypes[rowIndex * 4 + colIndex];
                        break;
                    case HlslNativeType._bool:
                        ret = floatTypes[rowIndex * 4 + colIndex];
                        break;
                    default:
                        break;
                }
            }
            return ret;
        }

        internal static HlslNativeType GetBinaryOpReturnType(HlslNativeType lhsType, HlslNativeType rhsType, bool isMul)
        {
            int lhsRows = GetNumRows(lhsType);
            int lhsCols = GetNumCols(lhsType);
            int rhsRows = GetNumRows(rhsType);
            int rhsCols = GetNumCols(rhsType);

            HlslNativeType lhsBase = GetNativeBaseType(lhsType);
            HlslNativeType rhsBase = GetNativeBaseType(rhsType);
            HlslNativeType dstBase = GetBaseTypeResultFromBinaryOp(lhsBase, rhsBase);

            HlslNativeType dstType = HlslNativeType._invalid;
            if (isMul)
            {
                // is either a matrix type?
                bool isLhsMatrix = (lhsRows >= 2);
                bool isRhsMatrix = (rhsRows >= 2);

                // if either type is a scalar, simply promote to the larger type
                if (lhsRows == 1 && lhsCols == 1)
                {
                    // if lhs is scalar, then promote to rhs
                    dstType = GetBaseTypeWithDims(dstBase, rhsRows, rhsCols);
                }
                else if (rhsRows == 1 && rhsCols == 1)
                {
                    // if rhs is scalar, then promote to lhs
                    dstType = GetBaseTypeWithDims(dstBase, lhsRows, lhsCols);
                }
                else
                {
                    // Gets pretty messy becuase a float4 can be either a 4x1 or 1x4 vector. So if one is a matrix
                    // and the other is a vector, we might have to transpose the vector.
                    if (isLhsMatrix && isRhsMatrix)
                    {
                        // lhsCols should be the same as rhsCols, but we'll let the compiler handle that.
                        int dstRows = lhsRows;
                        int dstCols = rhsCols;
                        dstType = GetBaseTypeWithDims(dstBase, dstRows, dstCols);
                    }
                    else if (isLhsMatrix)
                    {
                        HlslUtil.ParserAssert(!isRhsMatrix); // should be handled by case above
                        // we'll assume that the rhs number of rows is the same as the lhs number of cols, so the resulting
                        // size would be the number of lhs rows.
                        int dstVecLen = lhsRows;
                        dstType = GetBaseTypeWithDims(dstBase, 1, dstVecLen);
                    }
                    else if (isRhsMatrix)
                    {
                        HlslUtil.ParserAssert(!isLhsMatrix); // should be handled by case above
                        // we'll assume that the rhs number of rows is the same as the lhs number of cols, so the resulting
                        // size would be the number of rhs cols.
                        int dstVecLen = rhsCols;
                        dstType = GetBaseTypeWithDims(dstBase, 1, dstVecLen);
                    }
                    else
                    {
                        // neither is a matrix
                        HlslUtil.ParserAssert(!isLhsMatrix);
                        HlslUtil.ParserAssert(!isRhsMatrix);

                        int dstVecLen = -1;

                        if (lhsCols == 1 || rhsCols == 1)
                        {
                            // If one is a sclara and the other is a vector, then promote to a vector
                            dstVecLen = Math.Max(lhsCols, rhsCols);
                        }
                        else
                        {
                            // Was unsure if we should use the min or max type, but it seems that hlsl defaults to smaller type.
                            // void Unity_GradientNoise_LegacyMod_float (float2 UV, float3 Scale, out float Out)
                            //{
                            //    float2 p = UV * Scale;
                            //
                            //
                            dstVecLen = Math.Min(lhsCols, rhsCols);
                        }
                        dstType = GetBaseTypeWithDims(dstBase, 1, dstVecLen);
                    }
                }
            }
            else
            {
                // For non-mul ops, just expand to the higher size. It's more permissive than the compiler, which should be fine.
                int dstRows = Math.Max(lhsRows, rhsRows);
                int dstCols = Math.Max(lhsCols, rhsCols);
                dstType = GetBaseTypeWithDims(dstBase, dstRows, dstCols);
            }
            return dstType;
        }

        // returns -1 if invalid
        static internal int GetSwizzleIndex(char ch)
        {
            int ret = -1;
            switch (ch)
            {
                case 'x':
                case 'r':
                    ret = 0;
                    break;
                case 'y':
                case 'g':
                    ret = 1;
                    break;
                case 'z':
                case 'b':
                    ret = 2;
                    break;
                case 'w':
                case 'a':
                    ret = 3;
                    break;
            }
            return ret;
        }

        // returns -1 if invalid
        static internal int GetSwizzleLength(string swizzle, HlslNativeType nativeType)
        {
            // todo: verify that the swizzle
            int numRows = GetNumRows(nativeType);
            int numCols = GetNumCols(nativeType);
            bool valid = true;
            if (numRows != 1)
            {
                valid = false;
            }
            if (swizzle.Length == 0 || swizzle.Length > 4)
            {
                valid = false;
            }
            for (int i = 0; i < swizzle.Length; i++)
            {
                int val = GetSwizzleIndex(swizzle[i]);
                if (val < 0 || val >= numCols)
                {
                    valid = false;
                }
            }

            return valid ? swizzle.Length : -1;
        }

        internal static string GetNativeTypeString(HlslNativeType type)
        {
            // skip the _
            return type.ToString().Substring(1);
        }

        internal static string GetNativeTypeStringApd(HlslNativeType type)
        {
            string ret = "";
            switch (type)
            {
                case HlslNativeType._float:
                case HlslNativeType._float1:
                    ret = "FloatApd";
                    break;
                case HlslNativeType._float2:
                    ret = "FloatApd2";
                    break;
                case HlslNativeType._float3:
                    ret = "FloatApd3";
                    break;
                case HlslNativeType._float4:
                    ret = "FloatApd4";
                    break;
                case HlslNativeType._half:
                case HlslNativeType._half1:
                    ret = "HalfApd";
                    break;
                case HlslNativeType._half2:
                    ret = "HalfApd2";
                    break;
                case HlslNativeType._half3:
                    ret = "HalfApd3";
                    break;
                case HlslNativeType._half4:
                    ret = "HalfApd4";
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return ret;
        }

        internal struct PrototypeInfo
        {
            internal static PrototypeInfo MakePrototypeInfo(HlslParser.TypeInfo returnType, string identifier, HlslParser.TypeInfo[] paramInfoVec, int uniqueId)
            {
                PrototypeInfo ret = new PrototypeInfo();
                ret.returnType = returnType;
                ret.identifier = identifier;
                ret.paramInfoVec = paramInfoVec;
                ret.uniqueId = uniqueId;
                return ret;
            }

            // return type
            internal HlslParser.TypeInfo returnType;

            // name of the function
            internal string identifier;

            // parameters
            internal HlslParser.TypeInfo[] paramInfoVec;

            internal int uniqueId;
        }

        internal struct OverloadInfo
        {
            // name of the identifier
            internal string identifier;

            // list of all functions that havet he same struct/identifer pair but differnt overloads
            internal List<int> prototypeList;
        }

        // TODO: Replace the hash function
        struct StringPair
        {
            string lhs;
            string rhs;
        }

        // give then struct and identifier, get the index into the overloads
        Dictionary<StringPair, int> overloadInfoFromStructFuncPair;

        internal struct FieldInfo
        {
            internal string identifier;

            internal int arrayDims;
            internal HlslParser.TypeInfo typeInfo;

            internal string[] semantics; // do we need semantics for builtin types?

            // to avoid allocating an array for every single parameter, we can reuse these one for all non-array variables
            //static int[] emptyDims = new int[0];
            static string[] emptySemantics = new string[0];

            static internal FieldInfo MakeNativeType(HlslNativeType nativeType, string fieldName, int dims, ApdAllowedState allowedState)
            {
                FieldInfo ret = new FieldInfo();
                ret.identifier = fieldName;
                ret.typeInfo = HlslParser.TypeInfo.MakeNativeType(nativeType, dims, allowedState);
                ret.semantics = emptySemantics;
                return ret;
            }

            static internal FieldInfo MakeStruct(string structName, string fieldName, int dims)
            {
                FieldInfo ret = new FieldInfo();
                ret.identifier = fieldName;
                ret.typeInfo = HlslParser.TypeInfo.MakeStruct(structName, dims);
                ret.semantics = emptySemantics;
                return ret;
            }
        }

        internal struct StructInfo
        {
            internal string identifier;

            internal FieldInfo[] fields;
            internal PrototypeInfo[] prototypes;

        }

        // maintains all the information for parsed structs and prototypes
        internal class ParsedFuncStructData
        {
            internal List<HlslUtil.PrototypeInfo> allPrototypes;

            // for each identifier, a list of all the prototypes that overload can have
            internal List<HlslUtil.OverloadInfo> allOverloads;

            // for each unique identifier, the index of allOverload.
            internal Dictionary<string, int> overloadFromIdentifer;

            // for each custom struct, our description of it
            internal List<HlslUtil.StructInfo> allStructs;

            // for each unique identifier, the index of allStructs
            internal Dictionary<string, int> structFromIdentifer;


            internal ParsedFuncStructData()
            {
                allPrototypes = new List<HlslUtil.PrototypeInfo>();
                allOverloads = new List<HlslUtil.OverloadInfo>();
                overloadFromIdentifer = new Dictionary<string, int>();

                // for each custom struct, our description of it
                allStructs = new List<HlslUtil.StructInfo>();

                // for each unique identifier, the index of allStructs
                structFromIdentifer = new Dictionary<string, int>();
            }

            internal void AddStruct(HlslUtil.StructInfo structInfo)
            {
                int dstIndex = allStructs.Count;
                allStructs.Add(structInfo);

                structFromIdentifer.Add(structInfo.identifier, dstIndex);
            }

            internal int AddPrototype(HlslUtil.PrototypeInfo protoInfo)
            {
                int dstProtoIndex = allPrototypes.Count;
                allPrototypes.Add(protoInfo);

                int foundOverloadIndex = -1;
                if (overloadFromIdentifer.ContainsKey(protoInfo.identifier))
                {
                    foundOverloadIndex = overloadFromIdentifer[protoInfo.identifier];
                }
                else
                {
                    foundOverloadIndex = allOverloads.Count;

                    HlslUtil.OverloadInfo overloadInfo = new HlslUtil.OverloadInfo();
                    overloadInfo.identifier = protoInfo.identifier;
                    overloadInfo.prototypeList = new List<int>();

                    allOverloads.Add(overloadInfo);

                    overloadFromIdentifer.Add(protoInfo.identifier, foundOverloadIndex);
                }

                allOverloads[foundOverloadIndex].prototypeList.Add(dstProtoIndex);

                return dstProtoIndex;
            }

        }

        internal static void ParserAssert(bool condition)
        {
            // note that we are doing the if(!condition) so that we have a place to set a breakpoint
            if (!condition)
            {
                Assert.IsTrue(condition);
            }
        }
    }
}
