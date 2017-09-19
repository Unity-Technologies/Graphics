using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Lightweight/Unlit")]
    public class LightweightUnlitMasterNode : MasterNode
    {
        public const string ColorSlotName = "Color";
        public const string AlphaSlotName = "Alpha";
        public const string VertexOffsetName = "VertexPosition";

        public const int ColorSlotId = 0;
        public const int AlphaSlotId = 1;
        public const int VertexOffsetId = 2;

        public LightweightUnlitMasterNode()
        {
            name = "LightweightUnlitMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(VertexOffsetId, VertexOffsetName, VertexOffsetName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Vertex));
            AddSlot(new MaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
            {
                ColorSlotId,
                AlphaSlotId,
                VertexOffsetId
            });
        }

        protected int[] masterSurfaceInputs
        {
            get
            {
                return new[]
                {
                    ColorSlotId,
                    AlphaSlotId,
                };
            }
        }

        protected int[] masterVertexInputs
        {
            get
            {
                return new[]
                {
                    VertexOffsetId
                };
            }
        }

        public override string GetSubShader(ShaderGraphRequirements shaderGraphRequirements)
        {
            var tagsVisitor = new ShaderGenerator();
            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();

            m_MaterialOptions.GetTags(tagsVisitor);
            m_MaterialOptions.GetBlend(blendingVisitor);
            m_MaterialOptions.GetCull(cullingVisitor);
            m_MaterialOptions.GetDepthTest(zTestVisitor);
            m_MaterialOptions.GetDepthWrite(zWriteVisitor);

            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            var interpolators = new ShaderGenerator();
            var vertexShader = new ShaderGenerator();
            var surfaceInput = new ShaderGenerator();

            // bitangent needs normal for x product
            if (shaderGraphRequirements.requiresNormal > 0 || shaderGraphRequirements.requiresBitangent > 0)
            {
                interpolators.AddShaderChunk(string.Format("float3 {0} : NORMAL;", ShaderGeneratorNames.ObjectSpaceNormal), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.normal;", ShaderGeneratorNames.ObjectSpaceNormal), false);
                surfaceInput.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", ShaderGeneratorNames.ObjectSpaceNormal), false);
            }

            if (shaderGraphRequirements.requiresTangent > 0 || shaderGraphRequirements.requiresBitangent > 0)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TANGENT;", ShaderGeneratorNames.ObjectSpaceTangent), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.tangent;", ShaderGeneratorNames.ObjectSpaceTangent), false);
                surfaceInput.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpaceTangent), false);
                surfaceInput.AddShaderChunk(string.Format("float4 {0} = normalize(cross(normalize(IN.{1}), normalize(IN.{2}.xyz)) * IN.{2}.w);",
                    ShaderGeneratorNames.ObjectSpaceBiTangent,
                    ShaderGeneratorNames.ObjectSpaceTangent,
                    ShaderGeneratorNames.ObjectSpaceNormal), false);
            }

            int interpolatorIndex = 0;
            if (shaderGraphRequirements.requiresViewDir > 0)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ObjectSpaceViewDirection, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ObjSpaceViewDir(v.vertex);", ShaderGeneratorNames.ObjectSpaceViewDirection), false);
                surfaceInput.AddShaderChunk(string.Format("float4 {0} = normalize(IN.{0});", ShaderGeneratorNames.ObjectSpaceViewDirection), false);
                interpolatorIndex++;
            }

            if (shaderGraphRequirements.requiresPosition > 0)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ObjectSpacePosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.vertex;", ShaderGeneratorNames.ObjectSpacePosition), false);
                surfaceInput.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpacePosition), false);
                interpolatorIndex++;
            }

            if (shaderGraphRequirements.NeedsTangentSpace())
            {
                surfaceInput.AddShaderChunk(string.Format("float3x3 tangentSpaceTransform = float3x3({0},{1},{2});",
                    ShaderGeneratorNames.ObjectSpaceTangent, ShaderGeneratorNames.ObjectSpaceBiTangent, ShaderGeneratorNames.ObjectSpaceNormal), false);
            }

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresNormal, surfaceInput,
                ShaderGeneratorNames.ObjectSpaceNormal, ShaderGeneratorNames.ViewSpaceNormal,
                ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.TangentSpaceNormal);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresTangent, surfaceInput,
                ShaderGeneratorNames.ObjectSpaceTangent, ShaderGeneratorNames.ViewSpaceTangent,
                ShaderGeneratorNames.WorldSpaceTangent, ShaderGeneratorNames.TangentSpaceTangent);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresBitangent, surfaceInput,
                ShaderGeneratorNames.ObjectSpaceBiTangent, ShaderGeneratorNames.ViewSpaceBiTangent,
                ShaderGeneratorNames.WorldSpaceSpaceBiTangent, ShaderGeneratorNames.TangentSpaceBiTangent);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresViewDir, surfaceInput,
                ShaderGeneratorNames.ObjectSpaceViewDirection, ShaderGeneratorNames.ViewSpaceViewDirection,
                ShaderGeneratorNames.WorldSpaceViewDirection, ShaderGeneratorNames.TangentSpaceViewDirection);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresPosition, surfaceInput,
                ShaderGeneratorNames.ObjectSpacePosition, ShaderGeneratorNames.ViewSpacePosition,
                ShaderGeneratorNames.WorldSpacePosition, ShaderGeneratorNames.TangentSpacePosition);

            if (shaderGraphRequirements.requiresVertexColor)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : COLOR;", ShaderGeneratorNames.VertexColor), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = color", ShaderGeneratorNames.VertexColor), false);
                surfaceInput.AddShaderChunk(string.Format("surfaceInput.{0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);
            }

            if (shaderGraphRequirements.requiresScreenPosition)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ScreenPosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ComputeScreenPos(UnityObjectToClipPos(v.vertex)", ShaderGeneratorNames.ScreenPosition), false);
                surfaceInput.AddShaderChunk(string.Format("surfaceInput.{0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
                interpolatorIndex++;
            }

            foreach (var channel in shaderGraphRequirements.requiresMeshUVs.Distinct())
            {
                interpolators.AddShaderChunk(string.Format("half4 {0} : TEXCOORD{1};", channel.GetUVName(), interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.texcoord{1};", channel.GetUVName(), interpolatorIndex == 0 ? "" : interpolatorIndex.ToString()), false);
                surfaceInput.AddShaderChunk(string.Format("surfaceInput.{0}  = IN.{0};", channel.GetUVName()), false);
                interpolatorIndex++;
            }

            ShaderGenerator defines = new ShaderGenerator();
            ShaderGenerator surfaceOutputRemap = new ShaderGenerator();


            var color = FindInputSlot<MaterialSlot>(ColorSlotId);
            var colorIn = owner.GetEdges(color.slotReference).FirstOrDefault();
            if (colorIn != null)
            {
                var outputRef = colorIn.outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);

                surfaceOutputRemap.AddShaderChunk(ConvertConcreteSlotValueTypeToString(precision, color.concreteValueType)
                                                  + " "
                                                  + color.shaderOutputName
                                                  + " = surf."
                                                  + color.shaderOutputName + ";", true);
            }
            else
            {
                surfaceOutputRemap.AddShaderChunk(ConvertConcreteSlotValueTypeToString(precision, color.concreteValueType)
                                                  + " " + color.shaderOutputName + " = 0;", false);
            }

            var alpha = FindInputSlot<MaterialSlot>(AlphaSlotId);
            var alphaIn = owner.GetEdges(alpha.slotReference).FirstOrDefault();
            if (alphaIn != null)
            {
                var outputRef = alphaIn.outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);

                surfaceOutputRemap.AddShaderChunk(ConvertConcreteSlotValueTypeToString(precision, alpha.concreteValueType)
                                                  + " "
                                                  + alpha.shaderOutputName
                                                  + " = surf."
                                                  + alpha.shaderOutputName + ";", true);
            }
            else
            {
                surfaceOutputRemap.AddShaderChunk(ConvertConcreteSlotValueTypeToString(precision, alpha.concreteValueType)
                                                  + " " + alpha.shaderOutputName + " = 0;", false);
            }

            var templateLocation = ShaderGenerator.GetTemplatePath("lightweightSubshaderUnlit.template");

            if (!File.Exists(templateLocation))
                return string.Empty;

            var subShaderTemplate = File.ReadAllText(templateLocation);
            var resultShader = subShaderTemplate.Replace("${Defines}", defines.GetShaderString(3));
            resultShader = resultShader.Replace("${Interpolators}", interpolators.GetShaderString(3));
            resultShader = resultShader.Replace("${VertexShader}", vertexShader.GetShaderString(3));
            resultShader = resultShader.Replace("${SurfaceInputs}", surfaceInput.GetShaderString(0));
            resultShader = resultShader.Replace("${SurfaceOutputRemap}", surfaceOutputRemap.GetShaderString(0));

            resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${LOD}", "" + m_MaterialOptions.lod);
            return resultShader;
        }
    }
}
