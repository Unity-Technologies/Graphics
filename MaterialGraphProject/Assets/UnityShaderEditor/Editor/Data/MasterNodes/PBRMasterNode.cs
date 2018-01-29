using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum AlphaMode
    {
        Opaque,
        AlphaBlend,
        AdditiveBlend
    }

    [Serializable]
    [Title("Master", "PBR")]
    public class PBRMasterNode : MasterNode
    {
        public const string AlbedoSlotName = "Albedo";
        public const string NormalSlotName = "Normal";
        public const string EmissionSlotName = "Emission";
        public const string MetallicSlotName = "Metallic";
        public const string SpecularSlotName = "Specular";
        public const string SmoothnessSlotName = "Smoothness";
        public const string OcclusionSlotName = "Occlusion";
        public const string AlphaSlotName = "Alpha";
        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const string VertexOffsetName = "VertexPosition";

        public const int AlbedoSlotId = 0;
        public const int NormalSlotId = 1;
        public const int MetallicSlotId = 2;
        public const int SpecularSlotId = 3;
        public const int EmissionSlotId = 4;
        public const int SmoothnessSlotId = 5;
        public const int OcclusionSlotId = 6;
        public const int AlphaSlotId = 7;
        public const int AlphaThresholdSlotId = 8;

        public enum Model
        {
            Specular,
            Metallic
        }

        [SerializeField]
        private Model m_Model = Model.Metallic;

        [EnumControl("")]
        public Model model
        {
            get { return m_Model; }
            set
            {
                if (m_Model == value)
                    return;

                m_Model = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        private AlphaMode m_AlphaMode;

        [EnumControl("")]
        public AlphaMode alphaMode
        {
            get { return m_AlphaMode; }
            set
            {
                if (m_AlphaMode == value)
                    return;

                m_AlphaMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public PBRMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            name = "PBR Master";
            AddSlot(new ColorRGBMaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, Color.grey, ShaderStage.Fragment));
            AddSlot(new Vector3MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, new Vector3(0, 0, 1), ShaderStage.Fragment));
            AddSlot(new ColorRGBMaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, Color.black, ShaderStage.Fragment));
            if (model == Model.Metallic)
                AddSlot(new Vector1MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0, ShaderStage.Fragment));
            else
                AddSlot(new ColorRGBMaterialSlot(SpecularSlotId, SpecularSlotName, SpecularSlotName, SlotType.Input, Color.grey, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, 0.5f, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, 1f, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1f, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0f, ShaderStage.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
            {
                AlbedoSlotId,
                NormalSlotId,
                EmissionSlotId,
                model == Model.Metallic ? MetallicSlotId : SpecularSlotId,
                SmoothnessSlotId,
                OcclusionSlotId,
                AlphaSlotId,
                AlphaThresholdSlotId
            }, true);
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

            var lwSub = new LightWeightPBRSubShader();
            foreach (var subshader in lwSub.GetSubshader(this, mode))
                finalShader.AddShaderChunk(subshader, true);

            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.GetShaderString(0);
        }
    }
}
