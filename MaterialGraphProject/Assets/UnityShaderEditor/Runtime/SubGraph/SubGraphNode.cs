using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [Title("Sub-graph/Sub-graph Node")]
    public class SubGraphNode : AbstractMaterialNode
        , IGeneratesBodyCode
        , IGeneratesFunction
        , IGeneratesVertexShaderBlock
        , IGeneratesVertexToFragmentBlock
        , IOnAssetEnabled
    {

        [SerializeField]
        private string m_SubGraphAssetGuid;

#if UNITY_EDITOR
        public MaterialSubGraphAsset subGraphAsset
        {
            get
            {
                if (string.IsNullOrEmpty(m_SubGraphAssetGuid))
                    return null;

                var path = AssetDatabase.GUIDToAssetPath(m_SubGraphAssetGuid);
                if (string.IsNullOrEmpty(path))
                    return null;

                return AssetDatabase.LoadAssetAtPath<MaterialSubGraphAsset>(path);
            }
            set
            {
                var assetPath = AssetDatabase.GetAssetPath(value);
                if (string.IsNullOrEmpty(assetPath))
                    return;

                m_SubGraphAssetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                OnEnable();
            }
        }
#else
        public MaterialSubGraphAsset subGraphAsset
        {
            get
            {
                return null;
            }
            set
            {}
        }
#endif

        private SubGraph subGraph
        {
            get
            {
                if (subGraphAsset == null)
                    return null;

                return subGraphAsset.subGraph;
            }
        }

        public override bool hasPreview
        {
            get { return subGraphAsset != null; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                if (subGraphAsset == null)
                    return PreviewMode.Preview2D;

                return subGraph.previewMode;
            }
        }

        public SubGraphNode()
        {
            name = "SubGraph";
        }

        public void OnEnable()
        {
            List<string> validNames = new List<string>();
            if (subGraphAsset == null)
            {
                RemoveSlotsNameNotMatching(validNames);
                return;
            }

            var subGraphInputNode = subGraphAsset.subGraph.inputNode;
            foreach (var slot in subGraphInputNode.GetOutputSlots<MaterialSlot>())
            {
                AddSlot(new MaterialSlot(slot.name, slot.displayName, SlotType.Input, slot.priority, slot.valueType, slot.defaultValue));
                validNames.Add(slot.name);
            }

            var subGraphOutputNode = subGraphAsset.subGraph.outputNode;
            foreach (var slot in subGraphOutputNode.GetInputSlots<MaterialSlot>())
            {
                AddSlot(new MaterialSlot(slot.name, slot.displayName, SlotType.Output, slot.priority, slot.valueType, slot.defaultValue));
                validNames.Add(slot.name);
            }

            RemoveSlotsNameNotMatching(validNames);
        }

        public void GenerateNodeCode(ShaderGenerator shaderBodyVisitor, GenerationMode generationMode)
        {
            if (subGraphAsset == null)
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
                    + GetVariableNameForSlot(slot)
                    + " = 0;", false);
            }

            // Step 2...
            // Go into the subgraph
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            // Step 3...
            // For each input that is used and connects through we want to generate code.
            // First we assign the input variables to the subgraph
            var subGraphInputNode = subGraphAsset.subGraph.inputNode;

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                var varName = subGraphInputNode.GetVariableNameForSlot(subGraphInputNode.FindOutputSlot<MaterialSlot>(slot.name));
                var varValue = GetSlotValue(slot, GenerationMode.SurfaceShader);

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
            subGraph.GenerateNodeCode(bodyGenerator, GenerationMode.SurfaceShader);
            var subGraphOutputNode = subGraphAsset.subGraph.outputNode;
            outputString.AddShaderChunk(bodyGenerator.GetShaderString(0), false);

            // Step 5...
            // Copy the outputs to the parent context name);
            foreach (var slot in GetOutputSlots<MaterialSlot>())
            {
                var inputSlot = subGraphOutputNode.FindInputSlot<MaterialSlot>(slot.name);
                var inputValue = subGraphOutputNode.GetSlotValue(inputSlot, GenerationMode.SurfaceShader);

                outputString.AddShaderChunk(
                    GetVariableNameForSlot(slot)
                    + " = "
                    + inputValue 
                    + ";", false);
            }

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.AddShaderChunk("// Subgraph ends", false);

            shaderBodyVisitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyBlock(visitor, generationMode);

            if (subGraph == null)
                return;

            subGraph.GeneratePropertyBlock(visitor, GenerationMode.SurfaceShader);
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyUsages(visitor, generationMode);

            if (subGraph == null)
                return;

            subGraph.GeneratePropertyUsages(visitor, GenerationMode.SurfaceShader);
        }

        public override void CollectPreviewMaterialProperties (List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            if (subGraph == null)
                return;

            properties.AddRange(subGraph.GetPreviewProperties());
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (subGraph == null)
                return;

            subGraph.GenerateNodeFunction(visitor, GenerationMode.SurfaceShader);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (subGraph == null)
                return;

            subGraph.GenerateVertexShaderBlock(visitor, GenerationMode.SurfaceShader);
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (subGraph == null)
                return;

            subGraph.GenerateVertexToFragmentBlock(visitor, GenerationMode.SurfaceShader);
        }
    }
}
