using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.VFX
{
    static class VFXOldShaderGraphHelpers
    {
        public class PassInfo
        {
            public int[] vertexPorts;
            public int[] pixelPorts;
        }

        public class RPInfo
        {
            public Dictionary<string, PassInfo> passInfos;
            HashSet<int> m_AllPorts;
            public IEnumerable<int> allPorts
            {
                get
                {
                    if (m_AllPorts == null)
                    {
                        m_AllPorts = new HashSet<int>();
                        foreach (var pass in passInfos.Values)
                        {
                            foreach (var port in pass.vertexPorts)
                                m_AllPorts.Add(port);
                            foreach (var port in pass.pixelPorts)
                                m_AllPorts.Add(port);
                        }
                    }

                    return m_AllPorts;
                }
            }
        }

        private const string kVFXShaderGraphFunctionsIncludeHDRP =
            "#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl\"" +
            "\n#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl\"" +
            "\n#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl\"" +
            "\n#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl\"" +
            "\n#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl\"" +
            "\n#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinGIUtilities.hlsl\"" +
            "\n#ifndef SHADERPASS" +
            "\n#error Shaderpass should be defined at this stage." +
            "\n#endif" +
            "\n#include \"Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl\"";

        private const string kVFXShaderGraphFunctionsIncludeURP =
            "#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl\"\n" +
            "#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl\"\n" +
            "#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl\"\n" +
            "#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl\"\n" +
            "#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl\"\n" +
            "#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl\"\n" +
            "#include_with_pragmas \"Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl\"\n" +
            "#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl\"";

        public static readonly RPInfo hdrpInfo = new RPInfo
        {
            passInfos = new Dictionary<string, PassInfo>()
            {
                { "Forward", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.ColorSlotId, ShaderGraphVfxAsset.EmissiveSlotId, ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "DepthOnly", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "DepthNormals", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId, ShaderGraphVfxAsset.NormalSlotId } } }
            }
        };

        public static readonly RPInfo hdrpLitInfo = new RPInfo
        {
            passInfos = new Dictionary<string, PassInfo>()
            {
                { "GBuffer", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.BaseColorSlotId, ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.MetallicSlotId, ShaderGraphVfxAsset.SmoothnessSlotId, ShaderGraphVfxAsset.EmissiveSlotId, ShaderGraphVfxAsset.NormalSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "Forward", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.BaseColorSlotId, ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.MetallicSlotId, ShaderGraphVfxAsset.SmoothnessSlotId, ShaderGraphVfxAsset.EmissiveSlotId, ShaderGraphVfxAsset.NormalSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "DepthOnly", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } }
            }
        };

        public static readonly RPInfo urpLitInfo = new RPInfo
        {
            passInfos = new Dictionary<string, PassInfo>()
            {
                { "GBuffer", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.BaseColorSlotId, ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.MetallicSlotId, ShaderGraphVfxAsset.SmoothnessSlotId, ShaderGraphVfxAsset.EmissiveSlotId, ShaderGraphVfxAsset.NormalSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "Forward", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.BaseColorSlotId, ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.MetallicSlotId, ShaderGraphVfxAsset.SmoothnessSlotId, ShaderGraphVfxAsset.EmissiveSlotId, ShaderGraphVfxAsset.NormalSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "DepthOnly", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "DepthNormals",  new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId, ShaderGraphVfxAsset.NormalSlotId } } }
            }
        };

        private static bool NeedsPositionWorldInterpolator(GraphCode graphCode)
        {
            return graphCode.requirements.requiresPosition != NeededCoordinateSpace.None
                   || graphCode.requirements.requiresViewDir != NeededCoordinateSpace.None
                   || graphCode.requirements.requiresScreenPosition;
        }

        public static Dictionary<string, GraphCode> BuildGraphCode(ShaderGraphVfxAsset shaderGraph, RPInfo info, bool isLitShader)
        {
            if (!isLitShader && shaderGraph.lit)
            {
                Debug.LogError("You must use an unlit vfx master node with an unlit output");
                return null;
            }
            if (isLitShader && !shaderGraph.lit)
            {
                Debug.LogError("You must use a lit vfx master node with a lit output");
                return null;
            }

            var graphCodes = new Dictionary<string, GraphCode>();
            var outputMetadata = new List<OutputMetadata>();
            foreach (var passInfo in info.passInfos)
            {
                outputMetadata.Clear();
                foreach (var pixelPort in passInfo.Value.pixelPorts)
                {
                    var output = shaderGraph.GetOutput(pixelPort);
                    if (!string.IsNullOrEmpty(output.referenceName))
                        outputMetadata.Add(output);
                }

                var code = shaderGraph.GetCode(outputMetadata.ToArray());
                graphCodes.Add(passInfo.Key, code);
            }

            return graphCodes;
        }

        public static IEnumerable<string> GetAdditionalDefinesGetAdditionalReplacement(ShaderGraphVfxAsset shaderGraph, RPInfo info, Dictionary<string, GraphCode> graphCodes, bool isMesh)
        {
            yield return "VFX_SHADERGRAPH";

            foreach (var port in info.allPorts)
            {
                var portInfo = shaderGraph.GetOutput(port);
                if (!string.IsNullOrEmpty(portInfo.referenceName))
                    yield return $"HAS_SHADERGRAPH_PARAM_{portInfo.referenceName.ToUpper(CultureInfo.InvariantCulture)}";
            }

            bool needsPosWS = false;

            // Per pass define
            foreach (var kvPass in graphCodes)
            {
                GraphCode graphCode = kvPass.Value;

                var pixelPorts = info.passInfos[kvPass.Key].pixelPorts;

                bool readsNormal = (graphCode.requirements.requiresNormal & ~NeededCoordinateSpace.Tangent) != 0;
                bool readsTangent = (graphCode.requirements.requiresTangent & ~NeededCoordinateSpace.Tangent) != 0 ||
                    (graphCode.requirements.requiresBitangent & ~NeededCoordinateSpace.Tangent) != 0 ||
                    (graphCode.requirements.requiresViewDir & NeededCoordinateSpace.Tangent) != 0;

                bool hasNormalPort = Array.IndexOf(pixelPorts, ShaderGraphVfxAsset.NormalSlotId) != -1 && shaderGraph.HasOutput(ShaderGraphVfxAsset.NormalSlotId);

                if (readsNormal || readsTangent || hasNormalPort) // needs normal
                    yield return $"SHADERGRAPH_NEEDS_NORMAL_{kvPass.Key.ToUpper(CultureInfo.InvariantCulture)}";

                if (readsTangent || hasNormalPort) // needs tangent
                    yield return $"SHADERGRAPH_NEEDS_TANGENT_{kvPass.Key.ToUpper(CultureInfo.InvariantCulture)}";

                needsPosWS |= NeedsPositionWorldInterpolator(graphCode);

                if (isMesh)
                {
                    for (UVChannel uv = UVChannel.UV1; uv <= UVChannel.UV3; ++uv)
                    {
                        if (graphCode.requirements.requiresMeshUVs.Contains(uv))
                        {
                            int uvi = (int)uv;
                            yield return $"VFX_SHADERGRAPH_HAS_UV{uvi}";
                        }
                    }

                    if (graphCode.requirements.requiresVertexColor)
                    {
                        yield return "VFX_SHADERGRAPH_HAS_COLOR";
                    }
                }
            }



            // TODO Put that per pass ?
            if (needsPosWS)
                yield return "VFX_NEEDS_POSWS_INTERPOLATOR";
        }

        public static IEnumerable<KeyValuePair<string, VFXShaderWriter>> GetAdditionalReplacement(ShaderGraphVfxAsset shaderGraph, RPInfo info, Dictionary<string, GraphCode> graphCodes, bool isMesh)
        {
            foreach (var port in info.allPorts)
            {
                var portInfo = shaderGraph.GetOutput(port);
                if (!string.IsNullOrEmpty(portInfo.referenceName))
                    yield return new KeyValuePair<string, VFXShaderWriter>($"${{SHADERGRAPH_PARAM_{portInfo.referenceName.ToUpper(CultureInfo.InvariantCulture)}}}", new VFXShaderWriter($"{portInfo.referenceName}_{portInfo.id}"));
            }

            foreach (var kvPass in graphCodes)
            {
                GraphCode graphCode = kvPass.Value;

                var preProcess = new VFXShaderWriter();
                if (graphCode.requirements.requiresCameraOpaqueTexture)
                    preProcess.WriteLine("#define REQUIRE_OPAQUE_TEXTURE");
                if (graphCode.requirements.requiresDepthTexture)
                    preProcess.WriteLine("#define REQUIRE_DEPTH_TEXTURE");
                string rpIncludes = VFXLibrary.currentSRPBinder.SRPAssetTypeStr == "HDRenderPipelineAsset"
                    ? kVFXShaderGraphFunctionsIncludeHDRP
                    : kVFXShaderGraphFunctionsIncludeURP;
                preProcess.WriteLine(rpIncludes);
                yield return new KeyValuePair<string, VFXShaderWriter>("${SHADERGRAPH_PIXEL_CODE_" + kvPass.Key.ToUpper(CultureInfo.InvariantCulture) + "}", new VFXShaderWriter(preProcess.ToString() + graphCode.code));

                var callSG = new VFXShaderWriter("//Call Shader Graph\n");
                callSG.builder.AppendLine($"{shaderGraph.inputStructName} INSG = ({shaderGraph.inputStructName})0;");

                if (graphCode.requirements.requiresNormal != NeededCoordinateSpace.None)
                {
                    callSG.builder.AppendLine("float3 WorldSpaceNormal = normalize(normalWS.xyz);");
                    if ((graphCode.requirements.requiresNormal & NeededCoordinateSpace.World) != 0)
                        callSG.builder.AppendLine("INSG.WorldSpaceNormal = WorldSpaceNormal;");
                    if ((graphCode.requirements.requiresNormal & NeededCoordinateSpace.Object) != 0)
                        callSG.builder.AppendLine("INSG.ObjectSpaceNormal = mul(WorldSpaceNormal, (float3x3)UNITY_MATRIX_M);");
                    if ((graphCode.requirements.requiresNormal & NeededCoordinateSpace.View) != 0)
                        callSG.builder.AppendLine("INSG.ViewSpaceNormal = mul(WorldSpaceNormal, (float3x3)UNITY_MATRIX_I_V);");
                    if ((graphCode.requirements.requiresNormal & NeededCoordinateSpace.Tangent) != 0)
                        callSG.builder.AppendLine("INSG.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);");
                }
                if (graphCode.requirements.requiresTangent != NeededCoordinateSpace.None)
                {
                    callSG.builder.AppendLine("float3 WorldSpaceTangent = normalize(tangentWS.xyz);");
                    if ((graphCode.requirements.requiresTangent & NeededCoordinateSpace.World) != 0)
                        callSG.builder.AppendLine("INSG.WorldSpaceTangent =  WorldSpaceTangent;");
                    if ((graphCode.requirements.requiresTangent & NeededCoordinateSpace.Object) != 0)
                        callSG.builder.AppendLine("INSG.ObjectSpaceTangent =  TransformWorldToObjectDir(WorldSpaceTangent);");
                    if ((graphCode.requirements.requiresTangent & NeededCoordinateSpace.View) != 0)
                        callSG.builder.AppendLine("INSG.ViewSpaceTangent = TransformWorldToViewDir(WorldSpaceTangent);");
                    if ((graphCode.requirements.requiresTangent & NeededCoordinateSpace.Tangent) != 0)
                        callSG.builder.AppendLine("INSG.TangentSpaceTangent = float3(1.0f, 0.0f, 0.0f);");
                }

                if (graphCode.requirements.requiresBitangent != NeededCoordinateSpace.None)
                {
                    callSG.builder.AppendLine("float3 WorldSpaceBiTangent =  normalize(bitangentWS.xyz);");
                    if ((graphCode.requirements.requiresBitangent & NeededCoordinateSpace.World) != 0)
                        callSG.builder.AppendLine("INSG.WorldSpaceBiTangent =  WorldSpaceBiTangent;");
                    if ((graphCode.requirements.requiresBitangent & NeededCoordinateSpace.Object) != 0)
                        callSG.builder.AppendLine("INSG.ObjectSpaceBiTangent =  TransformWorldToObjectDir(WorldSpaceBiTangent);");
                    if ((graphCode.requirements.requiresBitangent & NeededCoordinateSpace.View) != 0)
                        callSG.builder.AppendLine("INSG.ViewSpaceBiTangent = TransformWorldToViewDir(WorldSpaceBiTangent);");
                    if ((graphCode.requirements.requiresBitangent & NeededCoordinateSpace.Tangent) != 0)
                        callSG.builder.AppendLine("INSG.TangentSpaceBiTangent = float3(0.0f, 1.0f, 0.0f);");
                }

                if (NeedsPositionWorldInterpolator(graphCode))
                {
                    callSG.builder.AppendLine("float3 posRelativeWS = VFXGetPositionRWS(i.VFX_VARYING_POSWS);");
                    callSG.builder.AppendLine("float3 posAbsoluteWS = VFXGetPositionAWS(i.VFX_VARYING_POSWS);");

                    if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.World) != 0)
                        callSG.builder.AppendLine("INSG.WorldSpacePosition = posRelativeWS;");
                    if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.Object) != 0)
                        callSG.builder.AppendLine("INSG.ObjectSpacePosition = TransformWorldToObject(posRelativeWS);");
                    if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.View) != 0)
                        callSG.builder.AppendLine("INSG.ViewSpacePosition = VFXTransformPositionWorldToView(posRelativeWS);");
                    if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.Tangent) != 0)
                        callSG.builder.AppendLine("INSG.TangentSpacePosition = float3(0.0f, 0.0f, 0.0f);");
                    if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) != 0)
                        callSG.builder.AppendLine("INSG.AbsoluteWorldSpacePosition = posAbsoluteWS;");

                    if (graphCode.requirements.requiresPositionPredisplacement != NeededCoordinateSpace.None)
                    {
                        if ((graphCode.requirements.requiresPositionPredisplacement & NeededCoordinateSpace.World) != 0)
                            callSG.builder.AppendLine("INSG.WorldSpacePositionPredisplacement = posRelativeWS;");
                        if ((graphCode.requirements.requiresPositionPredisplacement & NeededCoordinateSpace.Object) != 0)
                            callSG.builder.AppendLine("INSG.ObjectSpacePositionPredisplacement = TransformWorldToObject(posRelativeWS);");
                        if ((graphCode.requirements.requiresPositionPredisplacement & NeededCoordinateSpace.View) != 0)
                            callSG.builder.AppendLine("INSG.ViewSpacePositionPredisplacement = VFXTransformPositionWorldToView(posRelativeWS);");
                        if ((graphCode.requirements.requiresPositionPredisplacement & NeededCoordinateSpace.Tangent) != 0)
                            callSG.builder.AppendLine("INSG.TangentSpacePositionPredisplacement = float3(0.0f, 0.0f, 0.0f);");
                        if ((graphCode.requirements.requiresPositionPredisplacement & NeededCoordinateSpace.AbsoluteWorld) != 0)
                            callSG.builder.AppendLine("INSG.AbsoluteWorldSpacePositionPredisplacement = posAbsoluteWS;");
                    }

                    if (graphCode.requirements.requiresViewDir != NeededCoordinateSpace.None)
                    {
                        callSG.builder.AppendLine("float3 V = GetWorldSpaceNormalizeViewDir(VFXGetPositionRWS(i.VFX_VARYING_POSWS));");
                        if ((graphCode.requirements.requiresViewDir & NeededCoordinateSpace.World) != 0)
                            callSG.builder.AppendLine("INSG.WorldSpaceViewDirection = V;");
                        if ((graphCode.requirements.requiresViewDir & NeededCoordinateSpace.Object) != 0)
                            callSG.builder.AppendLine("INSG.ObjectSpaceViewDirection =  TransformWorldToObjectDir(V);");
                        if ((graphCode.requirements.requiresViewDir & NeededCoordinateSpace.View) != 0)
                            callSG.builder.AppendLine("INSG.ViewSpaceViewDirection = TransformWorldToViewDir(V);");
                        if ((graphCode.requirements.requiresViewDir & NeededCoordinateSpace.Tangent) != 0)
                            callSG.builder.AppendLine("INSG.TangentSpaceViewDirection = mul(tbn, V);");
                    }

                    if (graphCode.requirements.requiresScreenPosition)
                    {
                        //ScreenPosition is expected to be the raw screen pos (float4) before the w division in pixel (SharedCode.template.hlsl)
                        callSG.builder.AppendLine("INSG.ScreenPosition = ComputeScreenPos(VFXTransformPositionWorldToClip(i.VFX_VARYING_POSWS), _ProjectionParams.x);");
                    }
                }

                if (graphCode.requirements.requiresNDCPosition || graphCode.requirements.requiresPixelPosition)
                {
                    callSG.builder.AppendLine("{");
                    if (graphCode.requirements.requiresPixelPosition || graphCode.requirements.requiresNDCPosition)
                    {
                        callSG.builder.AppendLine("#if UNITY_UV_STARTS_AT_TOP");
                        callSG.builder.AppendLine("    float2 PixelPosition = float2(i.VFX_VARYING_POSCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - i.VFX_VARYING_POSCS.y) : i.VFX_VARYING_POSCS.y);");
                        callSG.builder.AppendLine("#else");
                        callSG.builder.AppendLine("    float2 PixelPosition = float2(i.VFX_VARYING_POSCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - i.VFX_VARYING_POSCS.y) : i.VFX_VARYING_POSCS.y);");
                        callSG.builder.AppendLine("#endif");
                    }
                    if (graphCode.requirements.requiresPixelPosition)
                    {
                        callSG.builder.AppendLine("INSG.PixelPosition = PixelPosition;");
                    }
                    if (graphCode.requirements.requiresNDCPosition)
                    {
                        callSG.builder.AppendLine("INSG.NDCPosition = PixelPosition.xy / _ScreenParams.xy;");
                        callSG.builder.AppendLine("INSG.NDCPosition.y = 1.0f - INSG.NDCPosition.y;");
                    }
                    callSG.builder.AppendLine("}");
                }

                if (graphCode.requirements.requiresMeshUVs.Contains(UVChannel.UV0))
                {
                    callSG.builder.AppendLine("INSG.uv0.xy = i.uv;");
                }

                if (graphCode.requirements.requiresTime)
                {
                    callSG.builder.AppendLine("INSG.TimeParameters = _TimeParameters.xyz;");
                }

                if (graphCode.requirements.requiresFaceSign)
                {
                    callSG.builder.AppendLine("INSG.FaceSign = frontFace ? 1.0f : -1.0f;");
                }

                if (isMesh)
                {
                    for (UVChannel uv = UVChannel.UV1; uv <= UVChannel.UV3; ++uv)
                    {
                        if (graphCode.requirements.requiresMeshUVs.Contains(uv))
                        {
                            int uvi = (int)uv;
                            callSG.builder.AppendLine($"INSG.uv{uvi} = i.uv{uvi};");
                        }
                    }

                    if (graphCode.requirements.requiresVertexColor)
                    {
                        callSG.builder.AppendLine($"INSG.VertexColor = i.vertexColor;");
                    }
                }

                callSG.builder.Append($"\n{shaderGraph.outputStructName} OUTSG = {shaderGraph.evaluationFunctionName}(INSG");

                foreach (var property in graphCode.properties)
                {
                    var variableName = property.GetHLSLVariableName(true, GenerationMode.ForReals);
                    callSG.builder.Append(", ");
                    callSG.builder.Append(variableName);
                }

                callSG.builder.AppendLine(");");

                var pixelPorts = info.passInfos[kvPass.Key].pixelPorts;
                if (Array.IndexOf(pixelPorts, ShaderGraphVfxAsset.AlphaThresholdSlotId) != -1 && shaderGraph.alphaClipping)
                {
                    callSG.builder.AppendLine(
@"#if (USE_ALPHA_TEST || VFX_FEATURE_MOTION_VECTORS_FORWARD) && defined(VFX_VARYING_ALPHATHRESHOLD)
i.VFX_VARYING_ALPHATHRESHOLD = OUTSG.AlphaClipThreshold_7;
#endif");
                }

                yield return new KeyValuePair<string, VFXShaderWriter>("${SHADERGRAPH_PIXEL_CALL_" + kvPass.Key.ToUpper(CultureInfo.InvariantCulture) + "}", callSG);
            }
        }

        public static void ReplaceShaderGraphTag(VFXContext context, VFXNamedExpression[] namedExpressions, Dictionary<VFXExpression, string> expressionToName, VFXCodeGenerator.Cache codeGeneratorCache)
        {
            var shaderGraph = VFXShaderGraphHelpers.GetShaderGraph(context);
            if (shaderGraph == null || shaderGraph.generatesWithShaderGraph)
                return;

            int normSemantic = 0;
            var additionalInterpolantsGeneration = new VFXShaderWriter();
            var additionalInterpolantsDeclaration = new VFXShaderWriter();
            var additionalInterpolantsPreparation = new VFXShaderWriter();

            foreach (var fragmentProperty in shaderGraph.fragmentProperties)
            {
                if (VFXShaderGraphHelpers.IsTexture(fragmentProperty.propertyType))
                    continue;

                var fragmentParameter = fragmentProperty.referenceName;
                var filteredNamedExpressionIndex = Array.FindIndex(namedExpressions, o => fragmentParameter == o.name &&
                    !(expressionToName.ContainsKey(o.exp) && expressionToName[o.exp] == o.name)); // if parameter already in the global scope, there's nothing to do

                if (filteredNamedExpressionIndex != -1)
                {
                    var filteredNamedExpression = namedExpressions[filteredNamedExpressionIndex];
                    if (!filteredNamedExpression.exp.Is(VFXExpression.Flags.Constant))
                    {
                        additionalInterpolantsDeclaration.WriteDeclaration(filteredNamedExpression.exp.valueType, filteredNamedExpression.name, $"NORMAL{normSemantic++}");
                        additionalInterpolantsGeneration.WriteVariable(filteredNamedExpression.exp.valueType, filteredNamedExpression.name + "__", "0");
                        var expressionToNameLocal = new Dictionary<VFXExpression, string>(expressionToName);
                        additionalInterpolantsGeneration.EnterScope();
                        {
                            if (!expressionToNameLocal.ContainsKey(filteredNamedExpression.exp))
                            {
                                additionalInterpolantsGeneration.WriteVariable(filteredNamedExpression.exp, expressionToNameLocal);
                                additionalInterpolantsGeneration.WriteLine();
                            }
                            additionalInterpolantsGeneration.WriteAssignement(filteredNamedExpression.exp.valueType, filteredNamedExpression.name + "__", expressionToNameLocal[filteredNamedExpression.exp]);
                            additionalInterpolantsGeneration.WriteLine();
                        }
                        additionalInterpolantsGeneration.ExitScope();
                        additionalInterpolantsGeneration.WriteAssignement(filteredNamedExpression.exp.valueType, "o." + filteredNamedExpression.name, filteredNamedExpression.name + "__");
                        additionalInterpolantsPreparation.WriteVariable(filteredNamedExpression.exp.valueType, filteredNamedExpression.name, "i." + filteredNamedExpression.name);
                    }
                    else
                        additionalInterpolantsPreparation.WriteVariable(filteredNamedExpression.exp.valueType, filteredNamedExpression.name, filteredNamedExpression.exp.GetCodeString(null));
                }
            }

            codeGeneratorCache.TryAddSnippet("${VFXAdditionalInterpolantsGeneration}",  additionalInterpolantsGeneration.builder);
            codeGeneratorCache.TryAddSnippet("${VFXAdditionalInterpolantsDeclaration}", additionalInterpolantsDeclaration.builder);
            codeGeneratorCache.TryAddSnippet("${VFXAdditionalInterpolantsPreparation}", additionalInterpolantsPreparation.builder);
        }

    }
}
