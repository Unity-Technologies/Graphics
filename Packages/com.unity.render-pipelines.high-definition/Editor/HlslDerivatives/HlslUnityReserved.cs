using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HlslUnityReserved
    {
        internal Dictionary<string, HlslParser.TypeInfo> allGlobals;
        internal HlslUtil.ParsedFuncStructData allReservedData;

        // The HlslUnityReserved class will build a list of all valid prototypes once, and since it can be pretty huge
        // we only want to store it once. Then each compiled shader will use this struct to keep track of which ones
        // actually used.
        internal struct PrototypeActiveSet
        {
            // for each prototype, an int of the id which we gave to the caller
            int[] reversePrototypeLookup;

            // given the id, fetch the index of allPrototypes.
            Dictionary<int, int> activePrototypeIdMapping;

            // for each struct, an int of the id we gave to the caller
            int[] reverseStructLookup;

            // given the id, fetch the index of allPrototypes.
            Dictionary<int, int> allStructs;
        }

        struct UnityMacroTypeDecl
        {
            static internal UnityMacroTypeDecl Make(HlslParser.TypeInfo baseTypeInfo, HlslParser.TypeInfo subTypeInfo)
            {
                UnityMacroTypeDecl ret = new UnityMacroTypeDecl();
                ret.baseTypeInfo = baseTypeInfo;
                ret.subTypeInfo = subTypeInfo;
                return ret;
            }

            internal HlslParser.TypeInfo baseTypeInfo;
            internal HlslParser.TypeInfo subTypeInfo;
        }

        internal HlslUnityReserved()
        {
            {
                allGlobals = new Dictionary<string, HlslParser.TypeInfo>();

                HlslParser.TypeInfo topLevelFloat1 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float1, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo topLevelFloat2 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo topLevelFloat3 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float3, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo topLevelFloat4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo topLevelFloat4x4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4x4, 0, ApdAllowedState.OnlyFpd);

                allGlobals.Add("PI", topLevelFloat1);
                allGlobals.Add("HALF_PI", topLevelFloat1);

                allGlobals.Add("unity_OrthoParams", topLevelFloat4);
                allGlobals.Add("unity_DeltaTime", topLevelFloat4);
                allGlobals.Add("_ProjectionParams", topLevelFloat4);
                allGlobals.Add("_ScreenParams", topLevelFloat4);
                
                allGlobals.Add("SHADERGRAPH_OBJECT_POSITION", topLevelFloat3);

                allGlobals.Add("UNITY_MATRIX_M", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_I_M", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_V", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_I_V", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_P", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_I_P", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_VP", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_I_VP", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_MV", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_T_MV", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_IT_MV", topLevelFloat4x4);
                allGlobals.Add("UNITY_MATRIX_MVP", topLevelFloat4x4);
                allGlobals.Add("UNITY_PREV_MATRIX_M", topLevelFloat4x4);
                allGlobals.Add("UNITY_PREV_MATRIX_I_M", topLevelFloat4x4);
            }

            {
                macroTypeDecl = new Dictionary<string, UnityMacroTypeDecl>();

                HlslParser.TypeInfo nativeTex2d = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._Texture2D, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo nativeTex2dArray = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._Texture2DArray, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo nativeTexCube = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._TextureCUBE, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo nativeTexCubeArray = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._TextureCUBEArray, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo nativeTex3d = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._Texture3D, 0, ApdAllowedState.OnlyFpd);

                HlslParser.TypeInfo nativeRwTex2d = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._RWTexture2D, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo nativeRwTex2dArray = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._RWTexture2DArray, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo nativeRwTex3d = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._RWTexture3D, 0, ApdAllowedState.OnlyFpd);


                HlslParser.TypeInfo nativeSamplerState = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerState, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo nativeSamplerComparisonState = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerComparisonState, 0, ApdAllowedState.OnlyFpd);

                // any for these?
                HlslParser.TypeInfo nativeFloat4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.Any);
                HlslParser.TypeInfo nativeHalf4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._half4, 0, ApdAllowedState.Any);
                HlslParser.TypeInfo nativeUnknown = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._unknown, 0, ApdAllowedState.Any);
                HlslParser.TypeInfo nativeInvalid = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._invalid, 0, ApdAllowedState.Any);

                macroTypeDecl.Add("TEXTURE2D", UnityMacroTypeDecl.Make(nativeTex2d, nativeUnknown));
                macroTypeDecl.Add("TEXTURE2D_ARRAY", UnityMacroTypeDecl.Make(nativeTex2dArray, nativeUnknown));
                macroTypeDecl.Add("TEXTURECUBE", UnityMacroTypeDecl.Make(nativeTexCube, nativeUnknown));
                macroTypeDecl.Add("TEXTURECUBE_ARRAY", UnityMacroTypeDecl.Make(nativeTexCubeArray, nativeUnknown));
                macroTypeDecl.Add("TEXTURE3D", UnityMacroTypeDecl.Make(nativeTex3d, nativeUnknown));

                macroTypeDecl.Add("TEXTURE2D_FLOAT", UnityMacroTypeDecl.Make(nativeTex2d, nativeFloat4));
                macroTypeDecl.Add("TEXTURE2D_ARRAY_FLOAT", UnityMacroTypeDecl.Make(nativeTex2dArray, nativeFloat4));
                macroTypeDecl.Add("TEXTURECUBE_FLOAT", UnityMacroTypeDecl.Make(nativeTexCube, nativeFloat4));
                macroTypeDecl.Add("TEXTURECUBE_ARRAY_FLOAT", UnityMacroTypeDecl.Make(nativeTexCubeArray, nativeFloat4));
                macroTypeDecl.Add("TEXTURE3D_FLOAT", UnityMacroTypeDecl.Make(nativeTex3d, nativeFloat4));

                macroTypeDecl.Add("TEXTURE2D_HALF", UnityMacroTypeDecl.Make(nativeTex2d, nativeHalf4));
                macroTypeDecl.Add("TEXTURE2D_ARRAY_HALF", UnityMacroTypeDecl.Make(nativeTex2dArray, nativeHalf4));
                macroTypeDecl.Add("TEXTURECUBE_HALF", UnityMacroTypeDecl.Make(nativeTexCube, nativeHalf4));
                macroTypeDecl.Add("TEXTURECUBE_ARRAY_HALF", UnityMacroTypeDecl.Make(nativeTexCubeArray, nativeHalf4));
                macroTypeDecl.Add("TEXTURE3D_HALF", UnityMacroTypeDecl.Make(nativeTex3d, nativeHalf4));

                macroTypeDecl.Add("TEXTURE2D_SHADOW", UnityMacroTypeDecl.Make(nativeTex2d, nativeUnknown));
                macroTypeDecl.Add("TEXTURE2D_ARRAY_SHADOW", UnityMacroTypeDecl.Make(nativeTex2dArray, nativeUnknown));
                macroTypeDecl.Add("TEXTURECUBE_SHADOW", UnityMacroTypeDecl.Make(nativeTexCube, nativeUnknown));
                macroTypeDecl.Add("TEXTURECUBE_ARRAY_SHADOW", UnityMacroTypeDecl.Make(nativeTexCubeArray, nativeUnknown));

                // this is a little janky, but we'll use invalid to denote that the user needs to specify a type, and thus
                // these three macros require 2 params instead of one.
                macroTypeDecl.Add("RW_TEXTURE2D", UnityMacroTypeDecl.Make(nativeRwTex2d, nativeInvalid));
                macroTypeDecl.Add("RW_TEXTURE2D_ARRAY", UnityMacroTypeDecl.Make(nativeRwTex2d, nativeInvalid));
                macroTypeDecl.Add("RW_TEXTURE3D", UnityMacroTypeDecl.Make(nativeRwTex3d, nativeInvalid));

                macroTypeDecl.Add("SAMPLER", UnityMacroTypeDecl.Make(nativeSamplerState, nativeUnknown));
                macroTypeDecl.Add("SAMPLER_CMP", UnityMacroTypeDecl.Make(nativeSamplerComparisonState, nativeUnknown));
            }

            parsedFuncStructData = new HlslUtil.ParsedFuncStructData();

            // SamplerState
            {
                HlslUtil.StructInfo sampler2dInfo = new HlslUtil.StructInfo();
                sampler2dInfo.identifier = "UnitySampler2D";

                {
                    // fields for the struct
                    List<HlslUtil.FieldInfo> fields = new List<HlslUtil.FieldInfo>();

                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._SamplerState, "samplerstate", 0, ApdAllowedState.OnlyFpd));

                    sampler2dInfo.fields = fields.ToArray();
                }

                parsedFuncStructData.AddStruct(sampler2dInfo);
            }

            // Texture2D
            {
                HlslUtil.StructInfo texture2dInfo = new HlslUtil.StructInfo();
                texture2dInfo.identifier = "UnityTexture2D";

                {
                    // fields for the struct
                    List<HlslUtil.FieldInfo> fields = new List<HlslUtil.FieldInfo>();

                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._Texture2D, "tex", 0, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._SamplerState, "samplerstate", 0, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._float4, "texelSize", 0, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._float4, "scaleTranslate", 0, ApdAllowedState.OnlyFpd));

                    texture2dInfo.fields = fields.ToArray();
                }

                {
                    HlslParser.TypeInfo unity_s = HlslParser.TypeInfo.MakeStruct("UnitySamplerState", 0);
                    HlslParser.TypeInfo unity_sc = HlslParser.TypeInfo.MakeStruct("UnitySamplerComparisonState", 0);

                    HlslParser.TypeInfo native_s = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerState, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo native_sc = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerComparisonState, 0, ApdAllowedState.OnlyFpd);

                    HlslParser.TypeInfo uv = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyApd);
                    HlslParser.TypeInfo lod = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo bias = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo dpdx = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo dpdy = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo cmp = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo pixel = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._int3, 0, ApdAllowedState.OnlyFpd);


                    HlslParser.TypeInfo retFloat4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.AllowApdVariation);
                    HlslParser.TypeInfo retFloat = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.AllowApdVariation);
                    HlslParser.TypeInfo retUv2 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyApd);

                    int ignoredId = -1;
                    List<HlslUtil.PrototypeInfo> protoInfo = new List<HlslUtil.PrototypeInfo>();
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Sample", new HlslParser.TypeInfo[] { unity_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleLevel", new HlslParser.TypeInfo[] { unity_s, uv, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleBias", new HlslParser.TypeInfo[] { unity_s, uv, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleGrad", new HlslParser.TypeInfo[] { unity_s, uv, dpdx, dpdy }, ignoredId));

                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retUv2, "GetTransformedUV", new HlslParser.TypeInfo[] { uv }, ignoredId));

                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "CalculateLevelOfDetail", new HlslParser.TypeInfo[] { unity_s, uv }, ignoredId));

                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Sample", new HlslParser.TypeInfo[] { native_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleLevel", new HlslParser.TypeInfo[] { native_s, uv, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleBias", new HlslParser.TypeInfo[] { native_s, uv, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleGrad", new HlslParser.TypeInfo[] { native_s, uv, dpdx, dpdy }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleCmpLevelZero", new HlslParser.TypeInfo[] { native_s, uv, cmp }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Load", new HlslParser.TypeInfo[] { pixel }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "CalculateLevelOfDetail", new HlslParser.TypeInfo[] { native_s, uv }, ignoredId));

                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Gather", new HlslParser.TypeInfo[] { unity_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "GatherRed", new HlslParser.TypeInfo[] { unity_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "GatherGreen", new HlslParser.TypeInfo[] { unity_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "GatherBlue", new HlslParser.TypeInfo[] { unity_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "GatherAlpha", new HlslParser.TypeInfo[] { unity_s, uv }, ignoredId));

                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Gather", new HlslParser.TypeInfo[] { native_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "GatherRed", new HlslParser.TypeInfo[] { native_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "GatherGreen", new HlslParser.TypeInfo[] { native_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "GatherBlue", new HlslParser.TypeInfo[] { native_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "GatherAlpha", new HlslParser.TypeInfo[] { native_s, uv }, ignoredId));

                    texture2dInfo.prototypes = protoInfo.ToArray();
                }

                parsedFuncStructData.AddStruct(texture2dInfo);
            }

            // TextureCube
            {
                HlslUtil.StructInfo textureCubeInfo = new HlslUtil.StructInfo();
                textureCubeInfo.identifier = "UnityTextureCube";

                {
                    // fields for the struct
                    List<HlslUtil.FieldInfo> fields = new List<HlslUtil.FieldInfo>();

                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._TextureCUBE, "tex", 0, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._SamplerState, "samplerstate", 0, ApdAllowedState.OnlyFpd));

                    textureCubeInfo.fields = fields.ToArray();
                }

                {
                    HlslParser.TypeInfo unity_s = HlslParser.TypeInfo.MakeStruct("UnitySamplerState", 0);
                    HlslParser.TypeInfo unity_sc = HlslParser.TypeInfo.MakeStruct("UnitySamplerComparisonState", 0);

                    HlslParser.TypeInfo native_s = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerState, 0, ApdAllowedState.OnlyFpd);

                    HlslParser.TypeInfo dir = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float3, 0, ApdAllowedState.OnlyApd);
                    HlslParser.TypeInfo lod = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo bias = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);

                    HlslParser.TypeInfo retFloat4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.AllowApdVariation);

                    int ignoredId = -1;
                    List<HlslUtil.PrototypeInfo> protoInfo = new List<HlslUtil.PrototypeInfo>();
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Sample", new HlslParser.TypeInfo[] { unity_s, dir }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleLevel", new HlslParser.TypeInfo[] { unity_s, dir, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleBias", new HlslParser.TypeInfo[] { unity_s, dir, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Sample", new HlslParser.TypeInfo[] { native_s, dir }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleLevel", new HlslParser.TypeInfo[] { native_s, dir, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleBias", new HlslParser.TypeInfo[] { native_s, dir, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Gather", new HlslParser.TypeInfo[] { unity_s, dir }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Gather", new HlslParser.TypeInfo[] { native_s, dir }, ignoredId));

                    textureCubeInfo.prototypes = protoInfo.ToArray();
                }

                parsedFuncStructData.AddStruct(textureCubeInfo);
            }

            // Texture2DArray
            {
                HlslUtil.StructInfo textureCubeInfo = new HlslUtil.StructInfo();
                textureCubeInfo.identifier = "UnityTexture2DArray";

                {
                    // fields for the struct
                    List<HlslUtil.FieldInfo> fields = new List<HlslUtil.FieldInfo>();

                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._TextureCUBE, "tex", 0, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._SamplerState, "samplerstate", 0, ApdAllowedState.OnlyFpd));

                    textureCubeInfo.fields = fields.ToArray();
                }

                {
                    HlslParser.TypeInfo unity_s = HlslParser.TypeInfo.MakeStruct("UnitySamplerState", 0);
                    HlslParser.TypeInfo unity_sc = HlslParser.TypeInfo.MakeStruct("UnitySamplerComparisonState", 0);

                    HlslParser.TypeInfo native_s = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerState, 0, ApdAllowedState.OnlyFpd);

                    HlslParser.TypeInfo uv = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float3, 0, ApdAllowedState.OnlyApd);
                    HlslParser.TypeInfo lod = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo bias = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);

                    HlslParser.TypeInfo dpdx = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo dpdy = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyFpd);

                    HlslParser.TypeInfo retFloat4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.AllowApdVariation);
                    HlslParser.TypeInfo cmp = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo pixel = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._int3, 0, ApdAllowedState.OnlyFpd);

                    int ignoredId = -1;
                    List<HlslUtil.PrototypeInfo> protoInfo = new List<HlslUtil.PrototypeInfo>();
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Sample", new HlslParser.TypeInfo[] { unity_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleLevel", new HlslParser.TypeInfo[] { unity_s, uv, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleBias", new HlslParser.TypeInfo[] { unity_s, uv, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleGrad", new HlslParser.TypeInfo[] { unity_s, uv, dpdx, dpdy }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Sample", new HlslParser.TypeInfo[] { native_s, uv }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleLevel", new HlslParser.TypeInfo[] { native_s, uv, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleBias", new HlslParser.TypeInfo[] { native_s, uv, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleGrad", new HlslParser.TypeInfo[] { native_s, uv, dpdx, dpdy }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleCmpLevelZero", new HlslParser.TypeInfo[] { native_s, uv, cmp }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Load", new HlslParser.TypeInfo[] { pixel }, ignoredId));

                    textureCubeInfo.prototypes = protoInfo.ToArray();
                }

                parsedFuncStructData.AddStruct(textureCubeInfo);
            }

            // Texture3d
            {
                HlslUtil.StructInfo texture3dInfo = new HlslUtil.StructInfo();
                texture3dInfo.identifier = "UnityTexture3D";

                {
                    // fields for the struct
                    List<HlslUtil.FieldInfo> fields = new List<HlslUtil.FieldInfo>();

                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._Texture3D, "tex", 0, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._SamplerState, "samplerstate", 0, ApdAllowedState.OnlyFpd));

                    texture3dInfo.fields = fields.ToArray();
                }

                {
                    HlslParser.TypeInfo unity_s = HlslParser.TypeInfo.MakeStruct("UnitySamplerState", 0);

                    HlslParser.TypeInfo native_s = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerState, 0, ApdAllowedState.OnlyFpd);

                    HlslParser.TypeInfo uvw = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float3, 0, ApdAllowedState.OnlyApd);
                    HlslParser.TypeInfo lod = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo bias = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo pixel = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._int4, 0, ApdAllowedState.OnlyFpd);

                    HlslParser.TypeInfo retFloat4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.AllowApdVariation);

                    int ignoredId = -1;
                    List<HlslUtil.PrototypeInfo> protoInfo = new List<HlslUtil.PrototypeInfo>();
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Sample", new HlslParser.TypeInfo[] { unity_s, uvw }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleLevel", new HlslParser.TypeInfo[] { unity_s, uvw, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Sample", new HlslParser.TypeInfo[] { native_s, uvw }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "SampleLevel", new HlslParser.TypeInfo[] { native_s, uvw, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, "Load", new HlslParser.TypeInfo[] { pixel }, ignoredId));

                    texture3dInfo.prototypes = protoInfo.ToArray();
                }

                parsedFuncStructData.AddStruct(texture3dInfo);
            }

            // UnitySamplerState
            {
                HlslUtil.StructInfo samplerStateInfo = new HlslUtil.StructInfo();
                samplerStateInfo.identifier = "UnitySamplerState";

                {
                    // fields for the struct
                    List<HlslUtil.FieldInfo> fields = new List<HlslUtil.FieldInfo>();

                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._SamplerState, "samplerstate", 0, ApdAllowedState.OnlyFpd));
                    samplerStateInfo.fields = fields.ToArray();
                }

                samplerStateInfo.prototypes = new HlslUtil.PrototypeInfo[0];

                parsedFuncStructData.AddStruct(samplerStateInfo);
            }

            // Gradient
            {

                HlslUtil.StructInfo gradientInfo = new HlslUtil.StructInfo();
                gradientInfo.identifier = "Gradient";

                {
                    HlslParser.TypeInfo int1 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._int, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo vec2 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyFpd);
                    HlslParser.TypeInfo vec4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.OnlyFpd);

                    // fields for the struct
                    List<HlslUtil.FieldInfo> fields = new List<HlslUtil.FieldInfo>();

                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._int, "type", 0, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._int, "colorsLength", 0, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._int, "alphasLength", 0, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._float2, "colors", 1, ApdAllowedState.OnlyFpd));
                    fields.Add(HlslUtil.FieldInfo.MakeNativeType(HlslNativeType._float4, "alphas", 1, ApdAllowedState.OnlyFpd));
                    gradientInfo.fields = fields.ToArray();
                }

                gradientInfo.prototypes = new HlslUtil.PrototypeInfo[0];
                parsedFuncStructData.AddStruct(gradientInfo);
            }

            {
                HlslParser.TypeInfo tex2d = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._Texture2D, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo tex2dArray = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._Texture2DArray, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo texCube = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._TextureCUBE, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo texCubeArray = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._TextureCUBE, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo tex3d = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._Texture3D, 0, ApdAllowedState.OnlyFpd);

                HlslParser.TypeInfo native_s = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerState, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo native_sc = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerComparisonState, 0, ApdAllowedState.OnlyFpd);

                HlslParser.TypeInfo lod = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo bias = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo dpdx = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo dpdy = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo cmp = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo pixel = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._int3, 0, ApdAllowedState.OnlyFpd);

                HlslParser.TypeInfo index = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._int, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo sampleIndex = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._int, 0, ApdAllowedState.OnlyFpd);

                HlslParser.TypeInfo coord2Apd = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyApd);
                HlslParser.TypeInfo coord3Apd = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float3, 0, ApdAllowedState.OnlyApd);
                HlslParser.TypeInfo coord4Apd = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.OnlyApd);

                HlslParser.TypeInfo coord2Fpd = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo coord3Fpd = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float3, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo coord4Fpd = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.OnlyFpd);

                HlslParser.TypeInfo retFloat4 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float4, 0, ApdAllowedState.Any);
                HlslParser.TypeInfo retFloat = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.Any);
                HlslParser.TypeInfo retFloat2 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float2, 0, ApdAllowedState.Any);

                List<HlslUtil.PrototypeInfo> protoInfo = new List<HlslUtil.PrototypeInfo>();

                int ignoredId = -1;

                for (int prefixIter = 0; prefixIter < 2; prefixIter++)
                {
                    string prefix = (prefixIter == 0) ? "" : "PLATFORM_";

                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE2D", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Apd }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE2D_LOD", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE2D_BIAS", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Apd, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE2D_GRAD", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd, dpdx, dpdy }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE2D_ARRAY", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Apd, index }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE2D_ARRAY_LOD", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd, index, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE2D_ARRAY_BIAS", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Apd, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE2D_ARRAY_GRAD", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd, dpdx, dpdy }, ignoredId));

                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURECUBE", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Apd }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURECUBE_LOD", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Fpd, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURECUBE_BIAS", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Apd, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURECUBE_ARRAY", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Fpd, index }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURECUBE_ARRAY_LOD", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Apd, index, lod }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURECUBE_ARRAY_BIAS", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Fpd, index, bias }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE3D", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Apd }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat4, prefix + "SAMPLE_TEXTURE3D_LOD", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Fpd, lod }, ignoredId));
                }

                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "SAMPLE_TEXTURE2D_SHADOW", new HlslParser.TypeInfo[] { tex2d, native_sc, coord3Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "SAMPLE_TEXTURE2D_ARRAY_SHADOW", new HlslParser.TypeInfo[] { tex2d, native_sc, coord3Fpd, index }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "SAMPLE_TEXTURECUBE_SHADOW", new HlslParser.TypeInfo[] { tex2d, native_sc, coord4Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "SAMPLE_TEXTURECUBE_ARRAY_SHADOW", new HlslParser.TypeInfo[] { tex2d, native_sc, coord4Fpd, index }, ignoredId));

                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "SAMPLE_DEPTH_TEXTURE", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "SAMPLE_DEPTH_TEXTURE_LOD", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd, lod }, ignoredId));

                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "LOAD_TEXTURE2D", new HlslParser.TypeInfo[] { tex2d, coord2Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "LOAD_TEXTURE2D_LOD", new HlslParser.TypeInfo[] { tex2d, coord2Fpd, lod }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "LOAD_TEXTURE2D_MSAA", new HlslParser.TypeInfo[] { tex2d, coord2Fpd, sampleIndex }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "LOAD_TEXTURE2D_ARRAY", new HlslParser.TypeInfo[] { tex2d, coord2Fpd, index }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "LOAD_TEXTURE2D_ARRAY_MSAA", new HlslParser.TypeInfo[] { tex2d, coord2Fpd, index, sampleIndex }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "LOAD_TEXTURE2D_ARRAY_LOD", new HlslParser.TypeInfo[] { tex2d, coord2Fpd, lod, index }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "LOAD_TEXTURE3D", new HlslParser.TypeInfo[] { tex2d, coord3Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "LOAD_TEXTURE3D_LOD", new HlslParser.TypeInfo[] { tex2d, coord3Fpd, lod }, ignoredId));

                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "GATHER_TEXTURE2D", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "GATHER_TEXTURE2D_ARRAY", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd, index }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "GATHER_TEXTURECUBE", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "GATHER_TEXTURECUBE_ARRAY", new HlslParser.TypeInfo[] { tex2d, native_s, coord3Fpd, index }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "GATHER_RED_TEXTURE2D", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "GATHER_GREEN_TEXTURE2D", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "GATHER_BLUE_TEXTURE2D", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(retFloat, "GATHER_ALPHA_TEXTURE2D", new HlslParser.TypeInfo[] { tex2d, native_s, coord2Fpd }, ignoredId));

                for (int i = 0; i < protoInfo.Count; i++)
                {
                    HlslUtil.PrototypeInfo currProto = protoInfo[i];
                    parsedFuncStructData.AddPrototype(currProto);
                }
            }

            int prec = 0;
            {
                HlslParser.TypeInfo vec1 = HlslParser.TypeInfo.MakeNativeType(prec == 0 ? HlslNativeType._float : HlslNativeType._half, 0, ApdAllowedState.AllowApdVariation);
                HlslParser.TypeInfo vec2 = HlslParser.TypeInfo.MakeNativeType(prec == 0 ? HlslNativeType._float2 : HlslNativeType._half2, 0, ApdAllowedState.AllowApdVariation);
                HlslParser.TypeInfo vec3 = HlslParser.TypeInfo.MakeNativeType(prec == 0 ? HlslNativeType._float3 : HlslNativeType._half3, 0, ApdAllowedState.AllowApdVariation);
                HlslParser.TypeInfo vec4 = HlslParser.TypeInfo.MakeNativeType(prec == 0 ? HlslNativeType._float4 : HlslNativeType._half4, 0, ApdAllowedState.AllowApdVariation);

                HlslParser.TypeInfo[] vecInOrder = new HlslParser.TypeInfo[4] { vec1, vec2, vec3, vec4 };

                List<HlslUtil.PrototypeInfo> protoInfo = new List<HlslUtil.PrototypeInfo>();

                int ignoredId = -1;

                for (int iter = 0; iter < 4; iter++)
                {
                    HlslParser.TypeInfo baseVec = vecInOrder[iter];
                    HlslParser.TypeInfo baseVecFpd = vecInOrder[iter];
                    baseVecFpd.allowedState = ApdAllowedState.OnlyFpd;
                    HlslParser.TypeInfo baseVecApd = vecInOrder[iter];
                    baseVecApd.allowedState = ApdAllowedState.OnlyApd;

                    //  BinaryFunc, these are valid with any vec1/2/3/4, but require the same to be passed to both sides
                    //min,
                    //max,
                    //pow,
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "min", new HlslParser.TypeInfo[] { baseVec, baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "max", new HlslParser.TypeInfo[] { baseVec, baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "pow", new HlslParser.TypeInfo[] { baseVec, baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "reflect", new HlslParser.TypeInfo[] { baseVec, baseVec }, ignoredId));

                    // SingleFunc, these are valid with any vec1/2/3/4
                    //saturate,
                    //rcp,
                    //log,
                    //log2,
                    //log10
                    //exp,
                    //exp2,
                    //sqrt,
                    //rsqrt,
                    //normalize,
                    //frac,
                    //cos,
                    //sin,
                    //abs,
                    //ddx
                    //ddy
                    //floor
                    //ceil
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "saturate", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "rcp", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "log", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "log2", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "log10", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "exp", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "exp2", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "sqrt", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "rsqrt", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "normalize", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "frac", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "cos", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "sin", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "tan", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "cosh", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "sinh", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "tanh", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "abs", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVecFpd, "ddx", new HlslParser.TypeInfo[] { baseVecApd }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVecFpd, "ddy", new HlslParser.TypeInfo[] { baseVecApd }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "floor", new HlslParser.TypeInfo[] { baseVec }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "ceil", new HlslParser.TypeInfo[] { baseVec }, ignoredId));

                    // Func1, in this case valid for every type, but always returns a vec1
                    // length
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(vec1, "length", new HlslParser.TypeInfo[] { baseVec }, ignoredId));

                    // Func2, a few cases
                    // dot, valid for all 4, but returns 1
                    // cross, only valid for vec3
                    // reflect, only valid for vec3
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(vec1, "dot", new HlslParser.TypeInfo[] { baseVec, baseVec }, ignoredId));
                    if (iter == 2)
                    {
                        protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(vec3, "cross", new HlslParser.TypeInfo[] { vec3, vec3 }, ignoredId));
                    }

                    // Func3
                    // lerp, valid for all 4
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVec, "lerp", new HlslParser.TypeInfo[] { baseVec, baseVec, baseVec }, ignoredId));

                    // these functions don't have an apd version, so are only legal for fpd
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVecFpd, "fmod", new HlslParser.TypeInfo[] { baseVecFpd, baseVecFpd }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVecFpd, "step", new HlslParser.TypeInfo[] { baseVecFpd, baseVecFpd }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVecFpd, "clamp", new HlslParser.TypeInfo[] { baseVecFpd, baseVecFpd, baseVecFpd }, ignoredId));
                    protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVecFpd, "round", new HlslParser.TypeInfo[] { baseVecFpd }, ignoredId));

                    {
                        string safePosPowName = "SafePositivePow_" + (prec == 0 ? "float" : "half");
                        protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(baseVecFpd, safePosPowName, new HlslParser.TypeInfo[] { baseVecFpd, baseVecFpd }, ignoredId));
                    }
                }

                for (int i = 0; i < protoInfo.Count; i++)
                {
                    HlslUtil.PrototypeInfo currProto = protoInfo[i];
                    parsedFuncStructData.AddPrototype(currProto);
                }
            }

            // add the helper function prototyps
            {
                List<HlslUtil.PrototypeInfo> protoInfo = new List<HlslUtil.PrototypeInfo>();

                HlslParser.TypeInfo unityTexture2D = HlslParser.TypeInfo.MakeStruct("UnityTexture2D", 0);
                HlslParser.TypeInfo unityTextureCube = HlslParser.TypeInfo.MakeStruct("UnityTextureCube", 0);
                HlslParser.TypeInfo unityTexture3D = HlslParser.TypeInfo.MakeStruct("UnityTexture3D", 0);
                HlslParser.TypeInfo unityTexture2DArray = HlslParser.TypeInfo.MakeStruct("UnityTexture2DArray", 0);

                HlslParser.TypeInfo tex2d = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._Texture2D, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo texCube = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._TextureCUBE, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo tex3d = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._Texture3D, 0, ApdAllowedState.OnlyFpd);

                HlslParser.TypeInfo unitySamplerState = HlslParser.TypeInfo.MakeStruct("UnitySamplerState", 0);
                HlslParser.TypeInfo samplerState = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._SamplerState, 0, ApdAllowedState.OnlyFpd);

                HlslParser.TypeInfo float1 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._float, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo half1 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._half, 0, ApdAllowedState.OnlyFpd);
                HlslParser.TypeInfo bool1 = HlslParser.TypeInfo.MakeNativeType(HlslNativeType._bool, 0, ApdAllowedState.OnlyFpd);

                int ignoredId = -1;
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(unityTexture2D, "UnityBuildTexture2DStructNoScale", new HlslParser.TypeInfo[] { tex2d }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(unityTexture2D, "UnityBuildTexture2DStruct", new HlslParser.TypeInfo[] { tex2d }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(unityTexture2DArray, "UnityBuildTexture2DArrayStruct", new HlslParser.TypeInfo[] { tex2d }, ignoredId));

                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(unityTextureCube, "UnityBuildTextureCubeStruct", new HlslParser.TypeInfo[] { texCube }, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(unityTexture3D, "UnityBuildTexture3DStruct", new HlslParser.TypeInfo[] { tex3d }, ignoredId));

                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(unitySamplerState, "UnityBuildSamplerStateStruct", new HlslParser.TypeInfo[] { samplerState }, ignoredId));

                // epsilon funcs and consts
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(float1, "Eps_float", new HlslParser.TypeInfo[] {}, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(float1, "Min_float", new HlslParser.TypeInfo[] {}, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(float1, "Max_float", new HlslParser.TypeInfo[] {}, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(half1, "Eps_half", new HlslParser.TypeInfo[] {}, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(half1, "Min_half", new HlslParser.TypeInfo[] {}, ignoredId));
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(half1, "Max_half", new HlslParser.TypeInfo[] { }, ignoredId));

                // color space
                protoInfo.Add(HlslUtil.PrototypeInfo.MakePrototypeInfo(bool1, "IsGammaSpace", new HlslParser.TypeInfo[] { }, ignoredId));


                for (int i = 0; i < protoInfo.Count; i++)
                {
                    HlslUtil.PrototypeInfo currProto = protoInfo[i];
                    parsedFuncStructData.AddPrototype(currProto);
                }
            }
        }

        internal bool IsIdentifierMacroDecl(string name)
        {
            return macroTypeDecl.ContainsKey(name);
        }

        internal void GetMacroDeclBaseAndSubType(out HlslParser.TypeInfo baseType, out HlslParser.TypeInfo subType, string name)
        {
            UnityMacroTypeDecl decl = macroTypeDecl[name];
            baseType = decl.baseTypeInfo;
            subType = decl.subTypeInfo;
        }

        internal bool IsIdentifierUnityStruct(string name)
        {
            return parsedFuncStructData.structFromIdentifer.ContainsKey(name);
        }

        internal bool IsIdentifierUnityFunction(string name)
        {
            return parsedFuncStructData.overloadFromIdentifer.ContainsKey(name);
        }

        internal bool FindUnityGlobal(out HlslParser.TypeInfo typeInfo, string name)
        {
            bool ret = false;
            typeInfo = new HlslParser.TypeInfo();
            if (allGlobals.ContainsKey(name))
            {
                ret = true;
                typeInfo = allGlobals[name];
            }

            return ret;
        }

        // these ids are the index in parsedFuncStructData.allPrototypes
        internal int[] GetPrototypeIdsForFunction(string name)
        {
            int overloadGroup = parsedFuncStructData.overloadFromIdentifer[name];
            HlslUtil.OverloadInfo info = parsedFuncStructData.allOverloads[overloadGroup];
            return info.prototypeList.ToArray();
        }

        internal PrototypeActiveSet MakeEmptyActiveSet()
        {
            PrototypeActiveSet activeSet = new PrototypeActiveSet();
            return activeSet;

        }

        internal int FindOrAddPrototype(ref HlslUtil.ParsedFuncStructData parsedData, ref PrototypeActiveSet activeSet)
        {
            return -1;
        }

        internal int AddPrototype(ref HlslUtil.ParsedFuncStructData parsedData, ref PrototypeActiveSet activeSet, string identifier, HlslParser.TypeInfo[] paramInfoVec)
        {
            return -1;
        }

        Dictionary<string, UnityMacroTypeDecl> macroTypeDecl;

        // Ideally we would preprocess only the macros that change code paths, but since that isn't
        // feasible just hardcode a few macros here during the tokenizing step.
        internal HlslUtil.ParsedFuncStructData parsedFuncStructData;

    }

}
