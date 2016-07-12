using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [Title("Sub-graph/Sub-graph Node")]
    public class SubGraphNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        [SerializeField]
        private MaterialSubGraphAsset m_SubGraphAsset;

        public MaterialSubGraphAsset subGraphAsset
        {
            get { return m_SubGraphAsset; }
            set { m_SubGraphAsset = value; }
        }

        public SubGraphNode()
        {
            name = "SubGraph";
            UpdateNodeAfterDeserialization();
        }
   
        public sealed override void UpdateNodeAfterDeserialization()
        {
            foreach (var slot in GetSlots<MaterialSlot>().Select(x => x.name).ToArray())
                RemoveSlot(slot);

            if (m_SubGraphAsset == null)
                return;

            var subGraphInputNode = m_SubGraphAsset.subGraph.inputNode;
            foreach (var slot in subGraphInputNode.GetOutputSlots<MaterialSlot>())
            {
                AddSlot(new MaterialSlot(slot.name, slot.displayName, SlotType.Input, slot.priority, slot.valueType, slot.defaultValue ));
            }

            var subGraphOutputNode = m_SubGraphAsset.subGraph.outputNode;
            foreach (var slot in subGraphOutputNode.GetInputSlots<MaterialSlot>())
            {
                AddSlot(new MaterialSlot(slot.name, slot.displayName, SlotType.Output, slot.priority, slot.valueType, slot.defaultValue));
            }
        }

        public void GenerateNodeCode(ShaderGenerator shaderBodyVisitor, GenerationMode generationMode)
        {
            if (m_SubGraphAsset == null)
                return;

            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("// Subgraph for node " + GetVariableNameForNode(), false);

            // Step 1...
            // find out which output slots are actually used
            //TODO: Be smarter about this and only output ones that are actually USED, not just connected
            //var validOutputSlots = NodeUtils.GetSlotsThatOutputToNodeRecurse(this, (graph as BaseMaterialGraph).masterNode);
            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                var outDimension = ConvertConcreteSlotValueTypeToString(slot.concreteValueType);

                outputString.AddShaderChunk(
                    "float"
                    + outDimension
                    + " "
                    + GetOutputVariableNameForSlot(slot)
                    + " = 0;", false);
            }

            // Step 2...
            // Go into the subgraph
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            // Step 3...
            // For each input that is used and connects through we want to generate code.
            // First we assign the input variables to the subgraph
            var subGraphInputNode = m_SubGraphAsset.subGraph.inputNode;

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                var varName = subGraphInputNode.GetOutputVariableNameForSlot(subGraphInputNode.FindOutputSlot<MaterialSlot>(slot.name));
                var varValue = GetSlotValue(slot, generationMode);

                var outDimension = ConvertConcreteSlotValueTypeToString(slot.concreteValueType);
                outputString.AddShaderChunk(
                    "float"
                    + outDimension 
                    + " " 
                    + varName 
                    + " = " 
                    + varValue 
                    + ";", false);
            }
           
            // Step 4...
            // Using the inputs we can now generate the shader body :)
            var bodyGenerator = new ShaderGenerator();
            var subGraphOutputNode = m_SubGraphAsset.subGraph.outputNode;

            var nodes = ListPool<INode>.Get();
            //Get the rest of the nodes for all the other slots
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, subGraphOutputNode, null, false);
            foreach (var node in nodes)
            {
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(bodyGenerator, generationMode);
            }
            ListPool<INode>.Release(nodes);
            outputString.AddShaderChunk(bodyGenerator.GetShaderString(0), false);

            // Step 5...
            // Copy the outputs to the parent context name);
            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                var inputSlot = subGraphOutputNode.FindInputSlot<MaterialSlot>(slot.name);
                var inputValue = subGraphOutputNode.GetSlotValue(inputSlot, generationMode);

                outputString.AddShaderChunk(
                    GetOutputVariableNameForSlot(slot)
                    + " = "
                    + inputValue 
                    + ";", false);
            }

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.AddShaderChunk("// Subgraph ends", false);

            shaderBodyVisitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

      /*  public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            m_SubGraphAsset.GenerateVertexToFragmentBlock(visitor, GenerationMode.SurfaceShader);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            m_SubGraphAsset.GenerateNodeFunction(visitor, GenerationMode.SurfaceShader);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            m_SubGraphAsset.GenerateVertexShaderBlock(visitor, GenerationMode.SurfaceShader);
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyBlock(visitor, generationMode);
            m_SubGraphAsset.GeneratePropertyBlock(visitor, GenerationMode.SurfaceShader);
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            base.GeneratePropertyUsages(visitor, generationMode, slotValueType);
            m_SubGraphAsset.GeneratePropertyUsages(visitor, GenerationMode.SurfaceShader, slotValueType);
        }

        protected override void CollectPreviewMaterialProperties (List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            properties.AddRange(m_SubGraphAsset.GetPreviewProperties());
        }*/
    }
}
