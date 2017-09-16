using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class MaterialGraph : AbstractMaterialGraph
    {
        public MasterNode masterNode
        {
            get { return GetNodes<MasterNode>().FirstOrDefault(); }
        }

        public string GetFullShader(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            // figure out what kind of preview we want!
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode);
            bool requiresBitangent = activeNodeList.OfType<IMayRequireBitangent>().Any(x => x.RequiresBitangent());
            bool requiresTangent = activeNodeList.OfType<IMayRequireTangent>().Any(x => x.RequiresTangent());
            bool requiresViewDirTangentSpace = activeNodeList.OfType<IMayRequireViewDirectionTangentSpace>().Any(x => x.RequiresViewDirectionTangentSpace());
            bool requiresViewDir = activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection());
            bool requiresWorldPos = activeNodeList.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition());
            bool requiresNormal = activeNodeList.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal());
            bool requiresScreenPosition = activeNodeList.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
            bool requiresVertexColor = activeNodeList.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor());

            var shaderInterpolators = new ShaderGenerator();
            var vertexShader = new ShaderGenerator();
            var pixelShader = new ShaderGenerator();
            var surfaceDescription = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var surfaceInputs = new ShaderGenerator();

            // always add color because why not.
            shaderInterpolators.AddShaderChunk(@"
struct GraphVertexInput
{
     float4 vertex : POSITION;
     float3 normal : NORMAL;
     float4 tangent : TANGENT;
     float2 texcoord : TEXCOORD0;
     float2 lightmapUV : TEXCOORD1;
     UNITY_VERTEX_INPUT_INSTANCE_ID
};", false);

            shaderInterpolators.AddShaderChunk("struct GraphVertexOutput{", false);
            shaderInterpolators.Indent();
            shaderInterpolators.AddShaderChunk("float4 position : POSITION;", false);

            vertexShader.AddShaderChunk("GraphVertexInput PopulateVertexData(GraphVertexInput v){", false);
            vertexShader.Indent();
            vertexShader.AddShaderChunk("return v;", false);
            vertexShader.Deindent();
            vertexShader.AddShaderChunk("}", false);

            surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
            surfaceInputs.Indent();

            surfaceDescription.AddShaderChunk(@"struct SurfaceDescription{", false);
            surfaceDescription.Indent();
            foreach (var input in masterNode.GetInputSlots<MaterialSlot>())
            {
                surfaceDescription.AddShaderChunk(AbstractMaterialNode.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, input.concreteValueType) + " " + input.shaderOutputName + ";", false);
            }
            surfaceDescription.Deindent();
            surfaceDescription.AddShaderChunk("};", false);

            pixelShader.AddShaderChunk("SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {", false);
            pixelShader.Indent();

            // view directions calculated from world position
            if (requiresWorldPos || requiresViewDir || requiresViewDirTangentSpace)
            {
                shaderInterpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD0;", ShaderGeneratorNames.WorldSpacePosition), false);
                surfaceInputs.AddShaderChunk(string.Format("float3 {0}", ShaderGeneratorNames.WorldSpacePosition), false);

                //vertexShader.AddShaderChunk("o.worldPos = worldPos;", false);
            }

           /* if (requiresBitangent || requiresNormal || requiresViewDirTangentSpace)
            {
                shaderInterpolators.AddShaderChunk("float3 worldNormal : TEXCOORD1;", false);

                //vertexShader.AddShaderChunk("o.worldNormal = worldNormal;", false);
                //pixelShader.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceNormal + " = normalize(IN.worldNormal);", false);
            }

            for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
            {
                var channel = (UVChannel)uvIndex;
                if (activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
                {
                    shaderInterpolators.AddShaderChunk(string.Format("half4 meshUV{0} : TEXCOORD{1};", uvIndex, (uvIndex + 5)), false);
                    vertexShader.AddShaderChunk(string.Format("o.meshUV{0} = v.texcoord{1};", uvIndex, uvIndex == 0 ? "" : uvIndex.ToString()), false);
                    pixelShader.AddShaderChunk(string.Format("half4 {0} = IN.meshUV{1};", channel.GetUVName(), uvIndex), false);
                }
            }

            if (requiresViewDir || requiresViewDirTangentSpace)
            {
                pixelShader.AddShaderChunk(
                    "float3 "
                    + ShaderGeneratorNames.WorldSpaceViewDirection
                    + " = normalize(UnityWorldSpaceViewDir("
                    + ShaderGeneratorNames.WorldSpacePosition
                    + "));", false);
            }

            if (requiresScreenPosition)
            {
                shaderInterpolators.AddShaderChunk("float4 screenPos : TEXCOORD3;", false);
                vertexShader.AddShaderChunk("o.screenPos = screenPos;", false);
                pixelShader.AddShaderChunk("half4 " + ShaderGeneratorNames.ScreenPosition + " = IN.screenPos;", false);
            }

            if (requiresBitangent || requiresViewDirTangentSpace || requiresTangent)
            {
                shaderInterpolators.AddShaderChunk("float4 worldTangent : TEXCOORD4;", false);
                vertexShader.AddShaderChunk("o.worldTangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);", false);
                pixelShader.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceTangent + " = normalize(IN.worldTangent.xyz);", false);
            }

            if (requiresBitangent || requiresViewDirTangentSpace)
            {
                pixelShader.AddShaderChunk(string.Format("float3 {0} = cross({1}, {2}) * IN.worldTangent.w;", ShaderGeneratorNames.WorldSpaceBitangent, ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.WorldSpaceTangent), false);
            }

            if (requiresViewDirTangentSpace)
            {
                pixelShader.AddShaderChunk(
                    "float3 " + ShaderGeneratorNames.TangentSpaceViewDirection + ";", false);

                pixelShader.AddShaderChunk(
                    ShaderGeneratorNames.TangentSpaceViewDirection + ".x = dot(" +
                    ShaderGeneratorNames.WorldSpaceViewDirection + "," +
                    ShaderGeneratorNames.WorldSpaceTangent + ");", false);

                pixelShader.AddShaderChunk(
                    ShaderGeneratorNames.TangentSpaceViewDirection + ".y = dot(" +
                    ShaderGeneratorNames.WorldSpaceViewDirection + "," +
                    ShaderGeneratorNames.WorldSpaceBitangent + ");", false);

                pixelShader.AddShaderChunk(
                    ShaderGeneratorNames.TangentSpaceViewDirection + ".z = dot(" +
                    ShaderGeneratorNames.WorldSpaceViewDirection + "," +
                    ShaderGeneratorNames.WorldSpaceNormal + ");", false);
            }

            if (requiresVertexColor)
            {
                vertexShader.AddShaderChunk("o.color = v.color;", false);
                pixelShader.AddShaderChunk("float4 " + ShaderGeneratorNames.VertexColor + " = IN.color;", false);
            }*/

            shaderInterpolators.Deindent();
            shaderInterpolators.AddShaderChunk("};", false);

            var generationMode = GenerationMode.ForReals;
            var shaderProperties = new PropertyCollector();
            CollectShaderProperties(shaderProperties, generationMode);

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (activeNode is IGeneratesFunction)
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(shaderFunctionVisitor, generationMode);
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(pixelShader, generationMode);

                activeNode.CollectShaderProperties(shaderProperties, generationMode);
            }

            pixelShader.AddShaderChunk("SurfaceDescription surface;", false);
            foreach (var input in masterNode.GetInputSlots<MaterialSlot>())
            {
                foreach (var edge in GetEdges(input.slotReference))
                {
                    var outputRef = edge.outputSlot;
                    var fromNode = GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                    if (fromNode == null)
                        continue;

                    var remapper = fromNode as INodeGroupRemapper;
                    if (remapper != null && !remapper.IsValidSlotConnection(outputRef.slotId))
                        continue;


                    pixelShader.AddShaderChunk(string.Format("surface.{0} = {1};", input.shaderOutputName, fromNode.GetVariableNameForSlot(outputRef.slotId)), true);
                }
            }
            pixelShader.AddShaderChunk("return surface;", false);
            pixelShader.Deindent();
            pixelShader.AddShaderChunk("}", false);
            ListPool<INode>.Release(activeNodeList);

            surfaceInputs.Deindent();
            surfaceInputs.AddShaderChunk("};", false);

            var finalShader = new ShaderGenerator();
            finalShader.AddShaderChunk(string.Format(@"Shader ""{0}""", name), false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();

            finalShader.AddShaderChunk("Properties", false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();
            finalShader.AddShaderChunk(shaderProperties.GetPropertiesBlock(2), false);
            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            finalShader.AddShaderChunk("CGINCLUDE", false);
            finalShader.AddShaderChunk("#include \"UnityCG.cginc\"", false);
            finalShader.AddShaderChunk(shaderFunctionVisitor.GetShaderString(2), false);
            finalShader.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);
            finalShader.AddShaderChunk(shaderInterpolators.GetShaderString(2), false);
            finalShader.AddShaderChunk(surfaceInputs.GetShaderString(2), false);
            finalShader.AddShaderChunk(surfaceDescription.GetShaderString(2), false);
            finalShader.AddShaderChunk(vertexShader.GetShaderString(2), false);
            finalShader.AddShaderChunk(pixelShader.GetShaderString(2), false);
            finalShader.AddShaderChunk("ENDCG", false);

            finalShader.AddShaderChunk(masterNode.GetSubShader(requiresNormal), false);

            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);
            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.GetShaderString(0);
        }
    }
}
