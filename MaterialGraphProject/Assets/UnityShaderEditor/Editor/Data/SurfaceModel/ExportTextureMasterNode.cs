using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    /*   [Serializable]
       [Title("Master/Custom Texture")]
       public class ExportTextureMasterNode : MasterNode
       {
           public const string ColorSlotName = "Color";
           public const int ColorSlotId = 0;

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

           public override string GetSubShader(GenerationMode mode, PropertyCollector shaderPropertiesVisitor)
           {
               return "";
           }

           public override bool has3DPreview()
           {
               return false;
           }

           public override string GetFullShader(GenerationMode generationMode, string name, out List<PropertyCollector.TextureInfo> configuredTextures)
           {
               // figure out what kind of preview we want!
               var activeNodeList = ListPool<INode>.Get();
               NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

               string templateLocation = ShaderGenerator.GetTemplatePath("ExportTexture.template");
               if (!File.Exists(templateLocation))
               {
                   configuredTextures = new List<PropertyCollector.TextureInfo>();
                   return string.Empty;
               }

               string template = File.ReadAllText(templateLocation);

               var shaderBodyVisitor = new ShaderGenerator();
               var shaderFunctionVisitor = new ShaderGenerator();
               var shaderPropertiesVisitor = new PropertyCollector();
               var shaderPropertyUsagesVisitor = new ShaderGenerator();
               var shaderInputVisitor = new ShaderGenerator();

               // always add color because why not.
               shaderInputVisitor.AddShaderChunk("float4 color : COLOR;", true);

               bool needBitangent = activeNodeList.OfType<IMayRequireBitangent>().Any(x => x.RequiresBitangent());
               bool needsWorldPos = activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection());
               if (needsWorldPos || activeNodeList.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition()))
               {
                   shaderBodyVisitor.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpacePosition + " = float3(0.0, 0.0, 0.0);", true);
               }

               if (needBitangent || activeNodeList.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal()))
               {
                   shaderBodyVisitor.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceNormal + " = float3(0.0, 0.0, 1.0);", true);
               }

               for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
               {
                   var channel = (UVChannel)uvIndex;
                   if (activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
                   {
                       shaderBodyVisitor.AddShaderChunk("half4 " + channel.GetUVName() + " = float4(IN.localTexcoord.xyz,1.0);", true);
                   }
               }

               if (activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection()))
               {
                   shaderBodyVisitor.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceViewDirection + " = float3(0.0, 0.0, -1.0);", true);
               }

               if (activeNodeList.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition()))
               {
                   shaderBodyVisitor.AddShaderChunk("half4 " + ShaderGeneratorNames.ScreenPosition + " = float4(IN.globalTexcoord.xyz, 1.0);", true);
               }
               if (needBitangent || activeNodeList.OfType<IMayRequireTangent>().Any(x => x.RequiresTangent()))
               {
                   shaderBodyVisitor.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceTangent + " = float3(1.0,0.0,0.0);", true);
               }
               if (needBitangent)
               {
                   shaderBodyVisitor.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceBitangent + " = float3(0.0,1.0,0.0);", true);
               }

               if (activeNodeList.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor()))
               {
                   shaderBodyVisitor.AddShaderChunk("float4 " + ShaderGeneratorNames.VertexColor + " = float4(1.0,1.0,1.0,1.0);", true);
               }

               foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
               {
                   if (activeNode is IGeneratesFunction)
                       (activeNode as IGeneratesFunction).GenerateNodeFunction(shaderFunctionVisitor, generationMode);
                   if (activeNode is IGeneratesBodyCode)
                       (activeNode as IGeneratesBodyCode).GenerateNodeCode(shaderBodyVisitor, generationMode);

                   activeNode.GeneratePropertyBlock(shaderPropertiesVisitor, generationMode);
                   activeNode.GeneratePropertyUsages(shaderPropertyUsagesVisitor, generationMode);
               }

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
                   shaderBodyVisitor.AddShaderChunk("return float4(0.5, 0.5, 0.5, 0.5);", true);
               }

               ListPool<INode>.Release(activeNodeList);

               template = template.Replace("${ShaderName}", name);
               template = template.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetPropertiesBlock(2));
               template = template.Replace("${ShaderPropertyUsages}", shaderPropertyUsagesVisitor.GetShaderString(3));
               template = template.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(3));
               template = template.Replace("${PixelShaderBody}", shaderBodyVisitor.GetShaderString(4));

               //In preview mode we use a different vertex shader, as the custom texture shaders are customized and not preview compatible.
               template = template.Replace("${ShaderIsUsingPreview}", generationMode == GenerationMode.Preview ? "_Preview" : "");

               configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();
               return Regex.Replace(template, @"\r\n|\n\r|\n|\r", Environment.NewLine);
           }
       }*/
}
