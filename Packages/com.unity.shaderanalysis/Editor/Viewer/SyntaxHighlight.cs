using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class SyntaxHighlight
{
    static string[] hlslKeywords =
    {
        "AppendStructuredBuffer", "break", "Buffer", "ByteAddressBuffer", "false", "for", "groupshared", "if", "in", "inout", "out",
        "case", "cbuffer", "const", "continue", "ConsumeStructuredBuffer", "default", "discard", "do", "else",
        "return", "register", "RWBuffer", "RWByteAddressBuffer", "RWStructuredBuffer", "RWTexture1D",
        "RWTexture1DArray", "RWTexture2D", "RWTexture2DArray", "RWTexture3D",
        "sample", "sampler", "SamplerState", "SamplerComparisonState", "static", "struct", "switch", "StructuredBuffer",
        "tbuffer", "texture", "Texture2D", "Texture2DArray", "Texture2DMS", "Texture2DMSArray", "Texture3D",
        "TextureCube", "TextureCubeArray", "true", "typedef", "uniform", "unsigned", "void", "while"
    };

    static string[] hlslFunctions =
    {
        "abort", "abs", "acos", "all", "AllMemoryBarrier", "AllMemoryBarrierWithGroupSync", "any", "asdouble", "asfloat", "asin", "asint", "asuint",
        "asuint", "atan", "atan2", "ceil", "CheckAccessFullyMapped", "clamp", "clip", "cos", "cosh", "countbits", "cross", "ddx", "ddx_coarse",
        "ddx_fine", "ddy", "ddy_coarse", "ddy_fine", "degrees", "determinant", "DeviceMemoryBarrier", "DeviceMemoryBarrierWithGroupSync", "distance",
        "dot", "dst", "errorf", "EvaluateAttributeCentroid", "EvaluateAttributeAtSample", "EvaluateAttributeSnapped", "exp", "exp2", "f16tof32",
        "f32tof16", "faceforward", "firstbithigh", "firstbitlow", "floor", "fma", "fmod", "frac", "frexp", "fwidth", "GetRenderTargetSampleCount",
        "GetRenderTargetSamplePosition", "GroupMemoryBarrier", "GroupMemoryBarrierWithGroupSync", "InterlockedAdd", "InterlockedAnd",
        "InterlockedCompareExchange", "InterlockedCompareStore", "InterlockedExchange", "InterlockedMax", "InterlockedMin", "InterlockedOr",
        "InterlockedXor", "isfinite", "isinf", "isnan", "ldexp", "length", "lerp", "lit", "log", "log10", "log2", "mad", "max", "min", "modf",
        "msad4", "mul", "noise", "normalize", "pow", "printf", "Process2DQuadTessFactorsAvg", "Process2DQuadTessFactorsMax",
        "Process2DQuadTessFactorsMin", "ProcessIsolineTessFactors", "ProcessQuadTessFactorsAvg", "ProcessQuadTessFactorsMax",
        "ProcessQuadTessFactorsMin", "ProcessTriTessFactorsAvg", "ProcessTriTessFactorsMax", "ProcessTriTessFactorsMin", "radians", "rcp", "reflect",
        "refract", "reversebits", "round", "rsqrt", "saturate", "sign", "sin", "sincos", "sinh", "smoothstep", "sqrt", "step", "tan", "tanh", "tex1D",
        "tex1D", "tex1Dbias", "tex1Dgrad", "tex1Dlod", "tex1Dproj", "tex2D", "tex2D", "tex2Dbias", "tex2Dgrad", "tex2Dlod", "tex2Dproj", "tex3D",
        "tex3D", "tex3Dbias", "tex3Dgrad", "tex3Dlod", "tex3Dproj", "texCUBE", "texCUBE", "texCUBEbias", "texCUBEgrad", "texCUBElod", "texCUBEproj", "transpose", "trunc"
    };

    public static string Highlight(string code)
    {
        var opt = RegexOptions.Multiline;
        // numbers
        code = Regex.Replace(code, @"(\s|[^\w#])([+-]?(?:[0-9]*[.])?[0-9]+f?)", m => m.Groups[1].Value + "<color=#8877f7>" + m.Groups[2].Value + "</color>", opt);
        // regular types
        code = Regex.Replace(code, @"\b(?:float|half|real|bool|int|uint|min16int|min16float)(?:[\d]?|(?:\dx\d)?)\b", m => "<color=#db8f56>" + m.Groups[0].Value + "</color>", opt);
        // // function calls
        // code = Regex.Replace(code, @"\b[\w_]{4,}\b(\()", m => "<color=#e6f542>" + m.Groups[1].Value + "</color>" + m.Groups[2].Value);
        // data access
        code = Regex.Replace(code, @"([a-zA-Z][\w_]+\.[\w_]+\.)([\w_]+)", m => m.Groups[1].Value + "<color=#22c7c9>" + m.Groups[2].Value + "</color>", opt);
        code = Regex.Replace(code, @"([a-zA-Z][\w_]+\.)([\w_]+)", m => m.Groups[1].Value + "<color=#22c7c9>" + m.Groups[2].Value + "</color>", opt);
        foreach (var keyword in hlslKeywords)
            code = Regex.Replace(code, @"\b" + keyword + @"\b", "<color=#cf472d>" + keyword + "</color>", opt | RegexOptions.Compiled);

        // HLSL builtin Functions
        foreach (var keyword in hlslFunctions)
            code = Regex.Replace(code, @"\b" + keyword + @"\b", "<color=#6dedbe>" + keyword + "</color>", opt | RegexOptions.Compiled);

        // defines
        code = Regex.Replace(code, @"\b[A-Z_][A-Z_0-9]{3,}\b", m => "<color=#a76ef5>" + m.Groups[0].Value + "</color>", opt);
        // comments
        code = Regex.Replace(code, "(//.*)", m => "<color=#269143>" + m.Groups[0].Value + "</color>", opt);
        return code;
    }
}
