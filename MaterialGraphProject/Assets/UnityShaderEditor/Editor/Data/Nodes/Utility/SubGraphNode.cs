using System;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEditor.Graphing;
using UnityEditor.Graphs;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Sub-graph")]
    public class SubGraphNode : AbstractSubGraphNode
        , IGeneratesBodyCode
        , IOnAssetEnabled
    {
        [SerializeField]
        private string m_SerializedSubGraph = string.Empty;

        [Serializable]
        private class SubGraphHelper
        {
            public MaterialSubGraphAsset subGraph;
        }

        protected override AbstractSubGraph referencedGraph
        {
            get
            {
                if (subGraphAsset == null)
                    return null;

                return subGraphAsset.subGraph;
            }
        }

#if UNITY_EDITOR
        [ObjectControl("")]
        public MaterialSubGraphAsset subGraphAsset
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedSubGraph))
                    return null;

                var helper = new SubGraphHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerializedSubGraph, helper);
                return helper.subGraph;
            }
            set
            {
                if (subGraphAsset == value)
                    return;

                var helper = new SubGraphHelper();
                helper.subGraph = value;
                m_SerializedSubGraph = EditorJsonUtility.ToJson(helper, true);
                UpdateSlots();

                Dirty(ModificationScope.Topological);
            }
        }
#else
        public MaterialSubGraphAsset subGraphAsset {get; set; }
#endif

        public override INode outputNode
        {
            get
            {
                if (subGraphAsset != null && subGraphAsset.subGraph != null)
                    return subGraphAsset.subGraph.outputNode;
                return null;
            }
        }

        public SubGraphNode()
        {
            name = "Sub-graph";
        }

        public void GenerateNodeCode(ShaderGenerator shaderBodyVisitor, GenerationMode generationMode)
        {
            if (referencedGraph == null)
                return;

            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("// Subgraph for node " + GetVariableNameForNode(), false);

            // Step 1...
            // find out which output slots are actually used
            //var validOutputSlots = NodeUtils.GetSlotsThatOutputToNodeRecurse(this, (graph as BaseMaterialGraph).masterNode);
            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                var outDimension = NodeUtils.ConvertConcreteSlotValueTypeToString(precision, slot.concreteValueType);
                outputString.AddShaderChunk(string.Format("{0} {1} = 0;", outDimension, GetVariableNameForSlot(slot.id)), false);
            }

            // Step 2...
            // Go into the subgraph
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            // Step 3...
            // For each input that is used and connects through we want to generate code.
            // First we assign the input variables to the subgraph
            // we do this by renaming the properties to be the names of where the variables come from
            // weird, but works.
            var sSubGraph = SerializationHelper.Serialize(subGraphAsset.subGraph);
            var dSubGraph = SerializationHelper.Deserialize<SubGraph>(sSubGraph, null);

            var subGraphInputs = dSubGraph.properties;

            var propertyGen = new PropertyCollector();
            dSubGraph.CollectShaderProperties(propertyGen, GenerationMode.ForReals);

            foreach (var prop in subGraphInputs)
            {
                var inSlotId = prop.guid.GetHashCode();
                var inSlot = FindInputSlot<MaterialSlot>(inSlotId);

                var edges = owner.GetEdges(inSlot.slotReference).ToArray();

                string varValue = inSlot.GetDefaultValue(generationMode);
                if (edges.Any())
                {
                    var fromSocketRef = edges[0].outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(fromSocketRef.nodeGuid);
                    if (fromNode != null)
                    {
                        var slot = fromNode.FindOutputSlot<MaterialSlot>(fromSocketRef.slotId);
                        if (slot != null)
                            prop.overrideReferenceName = fromNode.GetSlotValue(slot.id, generationMode);
                    }
                }
                else if (inSlot is Texture2DInputMaterialSlot)
                {
                    prop.overrideReferenceName =  ((Texture2DInputMaterialSlot)inSlot).GetDefaultValue(generationMode);
                }
                else
                {
                    var varName = prop.referenceName;
                    outputString.AddShaderChunk(NodeUtils.ConvertConcreteSlotValueTypeToString(precision, inSlot.concreteValueType)
                        + " "
                        + varName
                        + " = "
                        + varValue
                        + ";", false);
                }
            }

            // Step 4...
            // Using the inputs we can now generate the shader body :)
            var bodyGenerator = new ShaderGenerator();
            dSubGraph.GenerateNodeCode(bodyGenerator, GenerationMode.ForReals);
            var subGraphOutputNode = dSubGraph.outputNode;
            outputString.AddShaderChunk(bodyGenerator.GetShaderString(0), false);

            // Step 5...
            // Copy the outputs to the parent context name);
            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                var inputValue = subGraphOutputNode.GetSlotValue(slot.id, GenerationMode.ForReals);

                outputString.AddShaderChunk(
                    GetVariableNameForSlot(slot.id)
                    + " = "
                    + inputValue
                    + ";", false);
            }

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.AddShaderChunk("// Subgraph ends", false);

            shaderBodyVisitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public void OnEnable()
        {
            UpdateSlots();
        }
    }
}
