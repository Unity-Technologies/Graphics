using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master/Unlit")]
    public class UnlitMasterNode : MasterNode
    {
        public const string ColorSlotName = "Color";
        public const string AlphaSlotName = "Alpha";
        public const string VertexOffsetName = "VertexPosition";

        public const int ColorSlotId = 0;
        public const int AlphaSlotId = 7;

        public UnlitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            name = "Unlit Master";
            AddSlot(new Vector3MaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, new Vector3(0.5f, 0.5f, 0.5f), ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1, ShaderStage.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
            {
                ColorSlotId,
                AlphaSlotId
            });
        }

        public override string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            var shaderProperties = new PropertyCollector();

            var abstractMaterialGraph = owner as AbstractMaterialGraph;
            if (abstractMaterialGraph != null)
                abstractMaterialGraph.CollectShaderProperties(shaderProperties, mode);

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                activeNode.CollectShaderProperties(shaderProperties, mode);

            var finalShader = new ShaderGenerator();
            finalShader.AddShaderChunk(string.Format(@"Shader ""{0}""", outputName), false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();

            finalShader.AddShaderChunk("Properties", false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();
            finalShader.AddShaderChunk(shaderProperties.GetPropertiesBlock(2), false);
            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            var lwSub = new LightWeightUnlitSubShader();
            foreach (var subshader in lwSub.GetSubshader(this, mode))
                finalShader.AddShaderChunk(subshader, true);

            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.GetShaderString(0);
        }
    }
}
