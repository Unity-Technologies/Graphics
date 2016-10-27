using System;
using System.Collections.Generic;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable][Title("Math/Add Node")]
    public abstract class AbstractSurfaceMasterNode : AbstractMasterNode
    {
        public const string AlbedoSlotName = "Albedo";
        public const string NormalSlotName = "Normal";
        public const string EmissionSlotName = "Emission";
        public const string SmoothnessSlotName = "Smoothness";
        public const string OcclusionSlotName = "Occlusion";
        public const string AlphaSlotName = "Alpha";

        public const int AlbedoSlotId = 0;
        public const int NormalSlotId = 1;
        public const int EmissionSlotId = 3;
        public const int SmoothnessSlotId = 4;
        public const int OcclusionSlotId = 5;
        public const int AlphaSlotId = 6;

        public override void GenerateNodeCode(ShaderGenerator shaderBody, GenerationMode generationMode)
        {
            var firstPassSlotId = NormalSlotId;
            // do the normal slot first so that it can be used later in the shader :)
            var firstPassSlot = FindInputSlot<MaterialSlot>(firstPassSlotId);
            var nodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, firstPassSlotId, NodeUtils.IncludeSelf.Exclude);

            for (int index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }

            foreach (var edge in owner.GetEdges(firstPassSlot.slotReference))
            {
                var outputRef = edge.outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                if (fromNode == null)
                    continue;

                shaderBody.AddShaderChunk("o." + firstPassSlot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
            }

            // track the last index of nodes... they have already been processed :)
            int pass2StartIndex = nodes.Count;

            //Get the rest of the nodes for all the other slots
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, null, NodeUtils.IncludeSelf.Exclude);
            for (var i = pass2StartIndex; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }

            ListPool<INode>.Release(nodes);

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                foreach (var edge in owner.GetEdges(slot.slotReference))
                {
                    var outputRef = edge.outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                    if (fromNode == null)
                        continue;

                    shaderBody.AddShaderChunk("o." + slot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
                }
            }
        }
    }

    [Serializable]
    [Title("Master/Metallic")]
    public class MetallicMasterNode : AbstractSurfaceMasterNode
    {
        public const string MetallicSlotName = "Metallic";
        public const int MetallicSlotId = 2;

        public const string LightFunctionName = "Standard";
        public const string SurfaceOutputStructureName = "SurfaceOutputStandard";

        public MetallicMasterNode()
        {
            name = "MetallicMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                                       new[]
                                       {
                                           AlbedoSlotId,
                                           NormalSlotId,
                                           EmissionSlotId,
                                           MetallicSlotId,
                                           SmoothnessSlotId,
                                           OcclusionSlotId,
                                           AlphaSlotId
                                       });
        }
        
        public override void GenerateSurfaceOutput(ShaderGenerator surfaceOutput)
        {
            surfaceOutput.AddPragmaChunk(SurfaceOutputStructureName);
        }

        public override void GenerateLightFunction(ShaderGenerator lightFunction)
        {
            lightFunction.AddPragmaChunk(LightFunctionName);
        }
    }

    [Serializable]
    [Title("Master/Specular")]
    public class SpecularMasterNode : AbstractSurfaceMasterNode
    {
        public const string SpecularSlotName = "Specular";
        public const int SpecularSlotId = 2;

        public const string LightFunctionName = "StandardSpecular";
        public const string SurfaceOutputStructureName = "SurfaceOutputStandardSpecular";

        public SpecularMasterNode()
        {
            name = "SpecularMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(SpecularSlotId, SpecularSlotName, SpecularSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(SmoothnessSlotId, SmoothnessSlotName, SmoothnessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OcclusionSlotId, OcclusionSlotName, OcclusionSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                                       new[]
                                       {
                                           AlbedoSlotId,
                                           NormalSlotId,
                                           EmissionSlotId,
                                           SpecularSlotId,
                                           SmoothnessSlotId,
                                           OcclusionSlotId,
                                           AlphaSlotId
                                       });
        }

        public override void GenerateSurfaceOutput(ShaderGenerator surfaceOutput)
        {
            surfaceOutput.AddPragmaChunk(SurfaceOutputStructureName);
        }

        public override void GenerateLightFunction(ShaderGenerator lightFunction)
        {
            lightFunction.AddPragmaChunk(LightFunctionName);
        }
    }

    [Serializable]
    public abstract class AbstractMasterNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        protected override bool generateDefaultInputs { get { return false; } }

        public override IEnumerable<ISlot> GetInputsWithNoConnection()
        {
            return new List<ISlot>();
        }

        public override bool hasPreview
        {
            get { return true; }
        }
        
        public abstract void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode);
        public abstract void GenerateSurfaceOutput  (ShaderGenerator surfaceOutput);
        public abstract void GenerateLightFunction(ShaderGenerator lightFunction);
    }
}
