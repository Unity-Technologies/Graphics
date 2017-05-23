using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Export Texture")]
    public class ExportTextureMasterNode : AbstractMasterNode
    {
        public const string ColorSlotName = "Color";
        public const int ColorSlotId = 0;

        [SerializeField]
        private SurfaceMaterialOptions m_MaterialOptions = new SurfaceMaterialOptions();

        public SurfaceMaterialOptions options
        {
            get { return m_MaterialOptions; }
        }

        public ExportTextureMasterNode()
        {
            name = "ExportTextureMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, SlotValueType.Vector4, Vector4.zero));

            // clear out slot names that do not match the slots we support
            RemoveSlotsNameNotMatching(new[] { ColorSlotId });
        }

        public override PreviewMode previewMode
        {
            get
            {
                return PreviewMode.Preview2D;
            }
        }

        public override string GetSubShader(GenerationMode mode, PropertyGenerator shaderPropertiesVisitor)
        {
            return "";
        }

        public override bool has3DPreview()
        {
            return false;
        }

        public override string GetFullShader(GenerationMode mode, out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            
            // figure out what kind of preview we want!
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            string templateLocation = ShaderGenerator.GetTemplatePath("ExportTexture.template");
            if (!File.Exists(templateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }

            string template = File.ReadAllText(templateLocation);

            var shaderBodyVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var shaderPropertiesVisitor = new PropertyGenerator();
            var shaderPropertyUsagesVisitor = new ShaderGenerator();


            var shaderName = "Hidden/PreviewShader/" + GetType() + guid.ToString();
            //var shaderName = "Hidden/PreviewShader/" + this.GetVariableNameForSlot(this.GetOutputSlots<MaterialSlot>().First().id);


            var shaderInputVisitor = new ShaderGenerator();
            var vertexShaderBlock = new ShaderGenerator();

            // always add color because why not.
            shaderInputVisitor.AddShaderChunk("float4 color : COLOR;", true);

            vertexShaderBlock.AddShaderChunk("float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;", true);
            vertexShaderBlock.AddShaderChunk("float3 viewDir = UnityWorldSpaceViewDir(worldPos);", true);
            vertexShaderBlock.AddShaderChunk("float4 screenPos = ComputeScreenPos(UnityObjectToClipPos(v.vertex));", true);
            vertexShaderBlock.AddShaderChunk("float3 worldNormal = UnityObjectToWorldNormal(v.normal);", true);

            bool needBitangent = activeNodeList.OfType<IMayRequireBitangent>().Any(x => x.RequiresBitangent());
            bool needsWorldPos = activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection());
            if (needsWorldPos || activeNodeList.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition()))
            {
                shaderInputVisitor.AddShaderChunk("float3 worldPos : TEXCOORD0;", true);
                vertexShaderBlock.AddShaderChunk("o.worldPos = worldPos;", true);
                shaderBodyVisitor.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpacePosition + " = IN.worldPos;", true);
            }

            if (needBitangent || activeNodeList.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal()))
            {
                shaderInputVisitor.AddShaderChunk("float3 worldNormal : TEXCOORD1;", true);
                vertexShaderBlock.AddShaderChunk("o.worldNormal = worldNormal;", true);
                shaderBodyVisitor.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceNormal + " = normalize(IN.worldNormal);", true);
            }

            for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
            {
                var channel = (UVChannel)uvIndex;
                if (activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
                {
                    shaderInputVisitor.AddShaderChunk(string.Format("half4 meshUV{0} : TEXCOORD{1};", uvIndex, (uvIndex + 5)), true);
                    vertexShaderBlock.AddShaderChunk(string.Format("o.meshUV{0} = v.texcoord{1};", uvIndex, uvIndex == 0 ? "" : uvIndex.ToString()), true);
                    shaderBodyVisitor.AddShaderChunk(string.Format("half4 {0} = IN.meshUV{1};", channel.GetUVName(), uvIndex), true);
                }
            }

            if (activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection()))
            {
                shaderBodyVisitor.AddShaderChunk(
                    "float3 "
                    + ShaderGeneratorNames.WorldSpaceViewDirection
                    + " = normalize(UnityWorldSpaceViewDir("
                    + ShaderGeneratorNames.WorldSpacePosition
                    + "));", true);
            }

            if (activeNodeList.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition()))
            {
                shaderInputVisitor.AddShaderChunk("float4 screenPos : TEXCOORD3;", true);
                vertexShaderBlock.AddShaderChunk("o.screenPos = screenPos;", true);
                shaderBodyVisitor.AddShaderChunk("half4 " + ShaderGeneratorNames.ScreenPosition + " = IN.screenPos;", true);
            }

            if (needBitangent || activeNodeList.OfType<IMayRequireTangent>().Any(x => x.RequiresTangent()))
            {
                shaderInputVisitor.AddShaderChunk("float4 worldTangent : TEXCOORD4;", true);
                vertexShaderBlock.AddShaderChunk("o.worldTangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);", true);
                shaderBodyVisitor.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceTangent + " = normalize(IN.worldTangent.xyz);", true);
            }

            if (needBitangent)
            {
                shaderBodyVisitor.AddShaderChunk(string.Format("float3 {0} = cross({1}, {2}) * IN.worldTangent.w;", ShaderGeneratorNames.WorldSpaceBitangent, ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.WorldSpaceTangent), true);
            }

            if (activeNodeList.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor()))
            {
                vertexShaderBlock.AddShaderChunk("o.color = v.color;", true);
                shaderBodyVisitor.AddShaderChunk("float4 " + ShaderGeneratorNames.VertexColor + " = IN.color;", true);
            }

            var generationMode = GenerationMode.Preview;
            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (activeNode is IGeneratesFunction)
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(shaderFunctionVisitor, generationMode);
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(shaderBodyVisitor, generationMode);

                activeNode.GeneratePropertyBlock(shaderPropertiesVisitor, generationMode);
                activeNode.GeneratePropertyUsages(shaderPropertyUsagesVisitor, generationMode);
            }

            //shaderBodyVisitor.AddShaderChunk("return " + ShaderGenerator.AdaptNodeOutputForPreview(this, GetOutputSlots<MaterialSlot>().First().id) + ";", true);
            //shaderBodyVisitor.AddShaderChunk("return " + AdaptNodeInputForPreview(this, ColorSlotId) + ";", true);
            //shaderBodyVisitor.AddShaderChunk("return " + GetVariableNameForSlot(GetSlotReference(ColorSlotId).slotId) + ";", true);

            var inputSlot = GetInputSlots<MaterialSlot>().First();
            var edges = owner.GetEdges(inputSlot.slotReference);
            if (edges.Count() > 0)
            {
                var outputRef = edges.First().outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                shaderBodyVisitor.AddShaderChunk("return " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
            }
            else
            {
                shaderBodyVisitor.AddShaderChunk("return float4(1.0, 1.0, 1.0, 1.0);", true);
            }
            
            ListPool<INode>.Release(activeNodeList);

            template = template.Replace("${ShaderName}", shaderName);
            template = template.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            template = template.Replace("${ShaderPropertyUsages}", shaderPropertyUsagesVisitor.GetShaderString(3));
            template = template.Replace("${ShaderInputs}", shaderInputVisitor.GetShaderString(4));
            template = template.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(3));
            template = template.Replace("${VertexShaderBody}", vertexShaderBlock.GetShaderString(4));
            template = template.Replace("${PixelShaderBody}", shaderBodyVisitor.GetShaderString(4));

            string vertexShaderBody = vertexShaderBlock.GetShaderString(4);
            if (vertexShaderBody.Length > 0)
            {
                template = template.Replace("${VertexShaderDecl}", "vertex:vert");
                template = template.Replace("${VertexShaderBody}", vertexShaderBody);
            }
            else
            {
                template = template.Replace("${VertexShaderDecl}", "");
                template = template.Replace("${VertexShaderBody}", vertexShaderBody);
            }

            configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();
            return Regex.Replace(template, @"\r\n|\n\r|\n|\r", Environment.NewLine);
            
            //=============================================================================================

            /*
            string templateLocation = ShaderGenerator.GetTemplatePath("ExportTexture.template");

            if (!File.Exists(templateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }

            var shaderPropertiesVisitor = new PropertyGenerator();

            // figure out what kind of preview we want!
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);
            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
            {
                //if (node is IGeneratesFunction)
                   // (node as IGeneratesFunction).GenerateNodeFunction(nodeFunction, mode);
                node.GeneratePropertyBlock(shaderPropertiesVisitor, mode);
            }

            string texName = "_ExportTexture";

            if (shaderPropertiesVisitor.GetConfiguredTexutres().Count > 0)
            texName = shaderPropertiesVisitor.GetConfiguredTexutres()[0].name;


            string template = File.ReadAllText(templateLocation);
            var shaderName = "Hidden/ExportTexture/" + this.GetVariableNameForSlot(this.GetInputSlots<MaterialSlot>().First().id);
            template = template.Replace("${ShaderName}", shaderName);
            template = template.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(0));
            template = template.Replace("${ShaderTextureName}", texName);

            configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();

            ListPool<INode>.Release(activeNodeList);
            
            return Regex.Replace(template, @"\r\n|\n\r|\n|\r", Environment.NewLine);
            */
        }
    }
}
