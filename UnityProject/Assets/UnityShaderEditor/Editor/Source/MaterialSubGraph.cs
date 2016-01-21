using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class MaterialSubGraph : BaseMaterialGraph, IGeneratesVertexToFragmentBlock, IGeneratesFunction, IGeneratesVertexShaderBlock, IGenerateProperties
    {
        [SerializeField]
        private SubGraphInputsNode m_InputsNode;

        public SubGraphNode processingOwner { get; set; }

        [SerializeField]
        private SubGraphOutputsNode m_OutputsNode;

        public SubGraphInputsNode inputsNode { get { return m_InputsNode; } }

        public SubGraphOutputsNode outputsNode { get { return m_OutputsNode; } }

        protected override void RecacheActiveNodes()
        {
        }

        public new void OnEnable()
        {
            base.OnEnable();
            if (m_InputsNode == null)
            {
                m_InputsNode = CreateInstance<SubGraphInputsNode>();
                m_InputsNode.hideFlags = HideFlags.HideInHierarchy;
                m_InputsNode.OnCreate();
                AddMasterNodeNoAddToAsset(m_InputsNode);
            }

            if (m_OutputsNode == null)
            {
                m_OutputsNode = CreateInstance<SubGraphOutputsNode>();
                m_OutputsNode.hideFlags = HideFlags.HideInHierarchy;
                m_OutputsNode.OnCreate();
                AddMasterNodeNoAddToAsset(m_OutputsNode);
            }
        }

        public void CreateSubAssets()
        {
            AssetDatabase.AddObjectToAsset(m_InputsNode, this);
            AssetDatabase.AddObjectToAsset(m_OutputsNode, this);
        }

        // Return if the given input slot is wired all the way to an output slot
        // and if that output slot is connected on the SubGraphNode
        public bool InputInternallyWired(string slotName, SubGraphNode subGraphNode)
        {
            var outputSlot = inputsNode.slots.FirstOrDefault(x => x.name == slotName);

            if (outputSlot == null || outputSlot.edges.Count == 0)
                return false;

            var usedInputSlots = new List<Slot>();
            foreach (var edge in outputSlot.edges)
            {
                if (edge.toSlot.node == outputsNode)
                    usedInputSlots.Add(edge.toSlot);

                FindValidInputsToNodeFromNode(outputsNode, edge.toSlot.node, usedInputSlots);
            }

            //var inputWiredToSlots = new List<Slot> ();
            foreach (var foundUsedInputSlot in usedInputSlots)
            {
                Slot slot = foundUsedInputSlot;
                var onExternalNodeSlot = subGraphNode.outputSlots.FirstOrDefault(x => x.name == slot.name);

                if (onExternalNodeSlot != null && onExternalNodeSlot.edges.Count > 0)
                    return true;
                //inputWiredToSlots.Add (onExternalNodeSlot);
            }
            return false;

            //return inputWiredToSlots;
        }

        private static void FindValidInputsToNodeFromNode(Node toNode, Node currentNode, ICollection<Slot> foundUsedInputSlots)
        {
            if (currentNode == null || toNode == null)
            {
                Debug.LogError("Recursing to find valid inputs on NULL node");
                return;
            }

            foreach (var outputSlot in currentNode.outputSlots)
            {
                foreach (var edge in outputSlot.edges)
                {
                    if (edge.toSlot.node == toNode && !foundUsedInputSlots.Contains(edge.toSlot))
                    {
                        var validSlots = ListPool<Slot>.Get();
                        (edge.toSlot.node as BaseMaterialNode).GetValidInputSlots(validSlots);
                        
                        if (validSlots.Contains(edge.toSlot))
                            foundUsedInputSlots.Add(edge.toSlot);

                        ListPool<Slot>.Release(validSlots);
                    }
                    else
                        FindValidInputsToNodeFromNode(toNode, edge.toSlot.node, foundUsedInputSlots);
                }
            }
        }

        public string GetInputVariableNameForSlotByName(string slotName, BaseMaterialNode usageNode, GenerationMode generationMode)
        {
            var outputSlot = inputsNode.slots.FirstOrDefault(x => x.name == slotName);
            if (outputSlot == null)
                return "Could Not Find Slot";
            return inputsNode.GetOutputVariableNameForSlot(outputSlot, generationMode);
        }

        public bool OutputInternallyWired(string slotName)
        {
            var inputSlot = outputsNode.slots.FirstOrDefault(x => x.name == slotName);

            if (inputSlot == null)
                return false;

            return inputSlot.edges.Count > 0;
        }

        public string GetOutputVariableNameForSlotByName(string slotName, BaseMaterialNode usageNode, GenerationMode generationMode)
        {
            var inputSlot = outputsNode.slots.FirstOrDefault(x => x.name == slotName);
            if (inputSlot == null || inputSlot.edges.Count == 0)
                return "Slot Error";

            var bmn = inputSlot.edges[0].fromSlot.node as BaseMaterialNode;
            return bmn == null ? "Slot Error" : bmn.GetOutputVariableNameForSlot(inputSlot.edges[0].fromSlot, generationMode);
        }

        private IEnumerable<BaseMaterialNode> GetCollectedNodes()
        {
            return null;
            //return outputsNode.CollectChildNodesByExecutionOrder();
        }

        public void GenerateNodeCode(ShaderGenerator visitor, SubGraphNode generatingFor)
        {
            // First find which outputs are connected externally
            var externallyConnected = new List<Slot>();
            foreach (var slot in outputsNode.inputSlots)
            {
                var externalSlot = generatingFor.outputSlots.FirstOrDefault(x => x.name == slot.name);
                if (externalSlot == null)
                    continue;

                if (externalSlot.edges.Count > 0)
                    externallyConnected.Add(slot);
            }

            // then collect the valid nodes
            var collectedNodes = new List<BaseMaterialNode>();
            foreach (var s in externallyConnected)
                outputsNode.CollectChildNodesByExecutionOrder(collectedNodes, s, false);

            // then generate code for the connected nodes
            foreach (var n in collectedNodes.OfType<IGeneratesBodyCode>())
                n.GenerateNodeCode(visitor, GenerationMode.SurfaceShader);
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var n in GetCollectedNodes().OfType<IGeneratesVertexToFragmentBlock>())
                n.GenerateVertexToFragmentBlock(visitor, generationMode);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var n in GetCollectedNodes().OfType<IGeneratesFunction>())
                n.GenerateNodeFunction(visitor, generationMode);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var n in GetCollectedNodes().OfType<IGeneratesVertexShaderBlock>())
                n.GenerateVertexShaderBlock(visitor, generationMode);
        }

        public void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            foreach (var n in GetCollectedNodes().OfType<IGenerateProperties>())
                n.GeneratePropertyBlock(visitor, generationMode);
        }

        public void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            foreach (var n in GetCollectedNodes().OfType<IGenerateProperties>())
                n.GeneratePropertyUsages(visitor, generationMode, slotValueType);
        }

        // Returns a list of preview properties that need to be set
        public IEnumerable<PreviewProperty> GetPreviewProperties()
        {
            var properties = new List<PreviewProperty>();
            foreach (var tnode in nodes.Where(x => x is TextureNode).Cast<TextureNode>())
            {
                properties.Add(tnode.GetPreviewProperty());
            }

            foreach (var subNode in nodes.Where(x => x is SubGraphNode).Cast<SubGraphNode>())
            {
                if (subNode.subGraphAsset != null)
                    properties.AddRange(subNode.subGraphAsset.GetPreviewProperties());
            }
            return properties.Distinct();
        }
    }
}
