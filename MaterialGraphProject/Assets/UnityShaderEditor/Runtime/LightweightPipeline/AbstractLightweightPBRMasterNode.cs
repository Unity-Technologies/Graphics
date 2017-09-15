using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class AbstractLightweightPBRMasterNode : AbstractMasterNode
    {
        public const string AlbedoSlotName = "Albedo";
        public const string NormalSlotName = "Normal";
        public const string EmissionSlotName = "Emission";
        public const string SmoothnessSlotName = "Smoothness";
        public const string OcclusionSlotName = "Occlusion";
        public const string AlphaSlotName = "Alpha";
        public const string VertexOffsetName = "VertexPosition";

        public const int AlbedoSlotId = 0;
        public const int NormalSlotId = 1;
        public const int EmissionSlotId = 3;
        public const int SmoothnessSlotId = 4;
        public const int OcclusionSlotId = 5;
        public const int AlphaSlotId = 6;
        public const int VertexOffsetId = 7;

        [SerializeField]
        private SurfaceMaterialOptions m_MaterialOptions = new SurfaceMaterialOptions();

        public SurfaceMaterialOptions options
        {
            get { return m_MaterialOptions; }
        }

        public abstract string GetWorkflowName();
        //public abstract string GetSurfaceOutputName();
        //public abstract string GetLightFunction();


        void GenerateNodeFunctionsAndPropertyUsages(
            ShaderGenerator shaderBody,
            ShaderGenerator propertyUsages,
            ShaderGenerator nodeFunction,
            GenerationMode mode,
            int[] validNodeIds)
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, NodeUtils.IncludeSelf.Include,
                new List<int>(validNodeIds));

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (node is IGeneratesFunction)
                    (node as IGeneratesFunction).GenerateNodeFunction(nodeFunction, mode);

                node.GeneratePropertyUsages(propertyUsages, mode);
            }

            var nodes = ListPool<INode>.Get();
            //Get the rest of the nodes for all the other slots
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, NodeUtils.IncludeSelf.Exclude, new List<int>(vertexInputs));
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, mode);
            }
            ListPool<INode>.Release(nodes);
        }

        void GenerateVertexShaderInternal(
            ShaderGenerator propertyUsages,
            ShaderGenerator shaderBody,
            ShaderGenerator nodeFunction,
            ShaderGenerator vertexShaderBlock,
            GenerationMode mode)
        {
            GenerateNodeFunctionsAndPropertyUsages(vertexShaderBlock, propertyUsages, nodeFunction, mode, vertexInputs);

            var slot = FindInputSlot<MaterialSlot>(VertexOffsetId);
            foreach (var edge in owner.GetEdges(slot.slotReference))
            {
                var outputRef = edge.outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                if (fromNode == null)
                    continue;

                var remapper = fromNode as INodeGroupRemapper;
                if (remapper != null && !remapper.IsValidSlotConnection(outputRef.slotId))
                    continue;

                vertexShaderBlock.AddShaderChunk("v.vertex.xyz += " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
            }
        }

        public override string GetSubShader(GenerationMode mode, PropertyGenerator shaderPropertiesVisitor)
        {
            var templateLocation = ShaderGenerator.GetTemplatePath("lightweightSubshaderPBR.template");

            if (!File.Exists(templateLocation))
                return string.Empty;

            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);
            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
                node.GeneratePropertyBlock(shaderPropertiesVisitor, mode);

            var templateText = File.ReadAllText(templateLocation);
            var shaderBodyVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var shaderPropertyUsagesVisitor = new ShaderGenerator();
            var shaderInputVisitor = new ShaderGenerator();
            var shaderOutputVisitor = new ShaderGenerator();
            var vertexShaderBlock = new ShaderGenerator();

            GenerateSurfaceShaderInternal(
                shaderPropertyUsagesVisitor,
                shaderBodyVisitor,
                shaderFunctionVisitor,
                shaderInputVisitor,
                shaderOutputVisitor,
                vertexShaderBlock,
                mode);

            GenerateVertexShaderInternal(
                shaderPropertyUsagesVisitor,
                shaderBodyVisitor,
                shaderFunctionVisitor,
                vertexShaderBlock,
                mode);

            var tagsVisitor = new ShaderGenerator();
            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();
            var definesVisitor = new ShaderGenerator();

            m_MaterialOptions.GetTags(tagsVisitor);
            m_MaterialOptions.GetBlend(blendingVisitor);
            m_MaterialOptions.GetCull(cullingVisitor);
            m_MaterialOptions.GetDepthTest(zTestVisitor);
            m_MaterialOptions.GetDepthWrite(zWriteVisitor);

            GetDefines(definesVisitor);

            var resultShader = templateText.Replace("${ShaderPropertyUsages}", shaderPropertyUsagesVisitor.GetShaderString(2));
            //resultShader = resultShader.Replace("${LightingFunctionName}", GetLightFunction());
            //resultShader = resultShader.Replace("${SurfaceOutputStructureName}", GetSurfaceOutputName());
            resultShader = resultShader.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${VertexInputs}", shaderInputVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${VertexOutputs}", shaderOutputVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${PixelShaderBody}", shaderBodyVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${LOD}", "" + m_MaterialOptions.lod);

            resultShader = resultShader.Replace("${Defines}", definesVisitor.GetShaderString(2));

            //resultShader = resultShader.Replace("${VertexShaderDecl}", "vertex:vert");
            resultShader = resultShader.Replace("${VertexShaderBody}", vertexShaderBlock.GetShaderString(3));
            
            return resultShader;
        }

        public void GetDefines(ShaderGenerator visitor)
        {
            if(GetWorkflowName() == "Metallic")
                visitor.AddShaderChunk("#define _METALLIC_SETUP 1", true);
            else
                visitor.AddShaderChunk("", false);
            visitor.AddShaderChunk("#define _GLOSSYREFLECTIONS_ON", true);
            //visitor.AddShaderChunk("#define _SOFT_SHADOWS", true);
            visitor.AddShaderChunk("#define _SPECULARHIGHLIGHTS_ON", true);
        }

        public override string GetFullShader(GenerationMode mode, string name, out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            var templateLocation = ShaderGenerator.GetTemplatePath("shader.template");

            if (!File.Exists(templateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }

            var templateText = File.ReadAllText(templateLocation);

            var shaderPropertiesVisitor = new PropertyGenerator();
            var resultShader = templateText.Replace("${ShaderName}", name);
            resultShader = resultShader.Replace("${SubShader}", GetSubShader(mode, shaderPropertiesVisitor));
            resultShader = resultShader.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            //resultShader = templateText.Replace("${Fallback}", "Diffuse");
            configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();

            Debug.Log(resultShader);

            return Regex.Replace(resultShader, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }

        protected abstract int[] surfaceInputs
        {
            get;
        }

        protected abstract int[] vertexInputs
        {
            get;
        }

        private void GenerateSurfaceShaderInternal(
            ShaderGenerator propertyUsages,
            ShaderGenerator shaderBody,
            ShaderGenerator nodeFunction,
            ShaderGenerator shaderInputVisitor,
            ShaderGenerator shaderOutputVisitor,
            ShaderGenerator vertexShaderBlock,
            GenerationMode mode)
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, NodeUtils.IncludeSelf.Include,
                new List<int>(surfaceInputs));

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (node is IGeneratesFunction)
                {
                    ((IGeneratesFunction)node).GenerateNodeFunction(nodeFunction, mode);
                }

                node.GeneratePropertyUsages(propertyUsages, mode);
            }

            int vertInputIndex = 1; // DIRTY
            int vertOutputIndex = 4; // DIRTY

            // Need these for lighting
            shaderInputVisitor.AddShaderChunk("float4 vertex : POSITION;", true);
            shaderInputVisitor.AddShaderChunk("float4 normal : NORMAL;", true);
            shaderInputVisitor.AddShaderChunk("float2 lightmapUV : TEXCOORD0;", true);
            // always add color because why not. 
            shaderInputVisitor.AddShaderChunk("float4 color : COLOR;", true);

            // Need these for lighting
            shaderOutputVisitor.AddShaderChunk("float4 posWS : TEXCOORD0;", true);
            shaderOutputVisitor.AddShaderChunk("half4 viewDir : TEXCOORD1;", true);
            shaderOutputVisitor.AddShaderChunk("half4 fogCoord : TEXCOORD2;", true);
            shaderOutputVisitor.AddShaderChunk("half3 normal : TEXCOORD3;", true);
            shaderOutputVisitor.AddShaderChunk("float4 hpos : SV_POSITION;", true);
            
            bool requiresBitangent = activeNodeList.OfType<IMayRequireBitangent>().Any(x => x.RequiresBitangent());
            bool requiresTangent = activeNodeList.OfType<IMayRequireTangent>().Any(x => x.RequiresTangent());
            bool requiresViewDirTangentSpace = activeNodeList.OfType<IMayRequireViewDirectionTangentSpace>().Any(x => x.RequiresViewDirectionTangentSpace());
            bool requiresViewDir = activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection());
            bool requiresWorldPos = activeNodeList.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition());
            bool requiresNormal = activeNodeList.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal());
            bool requiresScreenPosition = activeNodeList.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
            bool requiresVertexColor = activeNodeList.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor());
            
            for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
            {
                var channel = (UVChannel)uvIndex;
                if (activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
                {
                    shaderInputVisitor.AddShaderChunk(string.Format("half4 texcoord{0} : TEXCOORD{1};", uvIndex, vertInputIndex), true);
                    shaderOutputVisitor.AddShaderChunk(string.Format("half4 meshUV{0} : TEXCOORD{1};", uvIndex, vertOutputIndex), true);
                    vertexShaderBlock.AddShaderChunk(string.Format("o.meshUV{0} = v.texcoord{1};", uvIndex, uvIndex/*uvIndex == 0 ? "" : uvIndex.ToString()*/), true);
                    shaderBody.AddShaderChunk(string.Format("half4 {0} = i.meshUV{1};", channel.GetUVName(), uvIndex), true);
                    vertInputIndex++;
                    vertOutputIndex++;
                    //shaderInputVisitor.AddShaderChunk(string.Format("half4 meshUV{0};", uvIndex), true);
                    //vertexShaderBlock.AddShaderChunk(string.Format("o.meshUV{0} = v.texcoord{1};", uvIndex, uvIndex == 0 ? "" : uvIndex.ToString()), true);
                    //shaderBody.AddShaderChunk(string.Format("half4 {0} = IN.meshUV{1};", channel.GetUVName(), uvIndex), true);
                }
            }

            if (requiresViewDir || requiresViewDirTangentSpace)
            {
                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceViewDirection + " = i.viewDir;", true);
                //shaderInputVisitor.AddShaderChunk("float3 worldViewDir;", true);
                //shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceViewDirection  + " = IN.worldViewDir;", true);
            }

            if (requiresWorldPos)
            {
                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpacePosition + " = i.posWS;", true);
                //shaderInputVisitor.AddShaderChunk("float3 worldPos;", true);
                //shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpacePosition + " = IN.worldPos;", true);
            }

            if (requiresScreenPosition)
            {
                shaderInputVisitor.AddShaderChunk(string.Format("half4 screenPos : TEXCOORD{0};", vertInputIndex), true);
                shaderOutputVisitor.AddShaderChunk(string.Format("half4 screenPos : TEXCOORD{0};", vertOutputIndex), true);
                vertexShaderBlock.AddShaderChunk("o.screenPos = v.screenPos;", true);
                shaderBody.AddShaderChunk("float4 " + ShaderGeneratorNames.ScreenPosition + " = i.screenPos;", true);
                vertInputIndex++;
                vertOutputIndex++;
                //shaderInputVisitor.AddShaderChunk("float4 screenPos;", true);
                //shaderBody.AddShaderChunk("float4 " + ShaderGeneratorNames.ScreenPosition + " = IN.screenPos;", true);
            }

            if (requiresBitangent || requiresTangent || requiresViewDirTangentSpace)
            {
                shaderInputVisitor.AddShaderChunk(string.Format("half4 tangent : TEXCOORD{0};", vertInputIndex), true);
                shaderOutputVisitor.AddShaderChunk(string.Format("half4 tangentWS : TEXCOORD{0};", vertOutputIndex), true);
                vertexShaderBlock.AddShaderChunk("o.tangentWS = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);", true);
                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceTangent + " = normalize(i.worldTangent.xyz);", true);
                vertInputIndex++;
                vertOutputIndex++;

                //shaderInputVisitor.AddShaderChunk("float4 worldTangent;", true);
                //vertexShaderBlock.AddShaderChunk("o.worldTangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);", true);
                //shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceTangent + " = normalize(IN.worldTangent.xyz);", true);
            }

            if (requiresBitangent || requiresNormal || requiresViewDirTangentSpace)
            {
                // is the normal connected?
                var normalSlot = FindInputSlot<MaterialSlot>(NormalSlotId);
                var edges = owner.GetEdges(normalSlot.slotReference);

                shaderInputVisitor.AddShaderChunk(string.Format("half4 normalWS : TEXCOORD{0};", vertInputIndex), true);
                shaderOutputVisitor.AddShaderChunk(string.Format("half4 normalWS : TEXCOORD{0};", vertOutputIndex), true);
                vertexShaderBlock.AddShaderChunk("o.normalWS = unity_ObjectToWorld(v.normal);", true);
                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceNormal + " = normalize(i.normalWS);", true);
                vertInputIndex++;
                vertOutputIndex++;

                //shaderInputVisitor.AddShaderChunk("float3 worldNormal;", true);
                if (edges.Any())
                    shaderInputVisitor.AddShaderChunk("INTERNAL_DATA", true);
                
                //shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceNormal + " = normalize(IN.worldNormal);", true);
            }

            if (requiresBitangent || requiresViewDirTangentSpace)
            {
                shaderBody.AddShaderChunk(string.Format("float3 {0} = cross({1}, {2}) * i.tangentWS.w;", ShaderGeneratorNames.WorldSpaceBitangent, ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.WorldSpaceTangent), true);
                //shaderBody.AddShaderChunk(string.Format("float3 {0} = cross({1}, {2}) * IN.worldTangent.w;", ShaderGeneratorNames.WorldSpaceBitangent, ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.WorldSpaceTangent), true);
            }

            if (requiresViewDirTangentSpace)
            {
                //                shaderInputVisitor.AddShaderChunk("float3 tangentViewDir;", true);
                //                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.TangentSpaceViewDirection + " = IN.tangentViewDir;", true);
                shaderBody.AddShaderChunk(
                    "float3 " + ShaderGeneratorNames.TangentSpaceViewDirection + ";", true);

                shaderBody.AddShaderChunk(
                    ShaderGeneratorNames.TangentSpaceViewDirection + ".x = dot(" +
                    ShaderGeneratorNames.WorldSpaceViewDirection + "," +
                    ShaderGeneratorNames.WorldSpaceTangent + ");", true);

                shaderBody.AddShaderChunk(
                    ShaderGeneratorNames.TangentSpaceViewDirection + ".y = dot(" +
                    ShaderGeneratorNames.WorldSpaceViewDirection + "," +
                    ShaderGeneratorNames.WorldSpaceBitangent + ");", true);

                shaderBody.AddShaderChunk(
                    ShaderGeneratorNames.TangentSpaceViewDirection + ".z = dot(" +
                    ShaderGeneratorNames.WorldSpaceViewDirection + "," +
                    ShaderGeneratorNames.WorldSpaceNormal + ");", true);
            }


            if (requiresVertexColor)
            {
                shaderOutputVisitor.AddShaderChunk(string.Format("half4 color : TEXCOORD{0};", vertOutputIndex), true);
                shaderBody.AddShaderChunk("float4 " + ShaderGeneratorNames.VertexColor + " = i.color;", true);
                vertInputIndex++;
                vertOutputIndex++;

                //shaderBody.AddShaderChunk("float4 " + ShaderGeneratorNames.VertexColor + " = IN.color;", true);
            }



            GenerateNodeCode(shaderBody, mode);
        }

        public void GenerateNodeCode(ShaderGenerator shaderBody, GenerationMode generationMode)
        {
            var nodes = ListPool<INode>.Get();

            //Get the rest of the nodes for all the other slots
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, NodeUtils.IncludeSelf.Exclude, new List<int>(surfaceInputs));
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }
            ListPool<INode>.Release(nodes);

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                if (surfaceInputs.Contains(slot.id))
                {
                    foreach (var edge in owner.GetEdges(slot.slotReference))
                    {
                        var outputRef = edge.outputSlot;
                        var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                        if (fromNode == null)
                            continue;

                        var remapper = fromNode as INodeGroupRemapper;
                        if (remapper != null && !remapper.IsValidSlotConnection(outputRef.slotId))
                            continue;

                        shaderBody.AddShaderChunk("o." + slot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);

                        if (slot.id == NormalSlotId)
                            shaderBody.AddShaderChunk("o." + slot.shaderOutputName + " += 1e-6;", true);
                    }
                }
            }
        }
    }
}
