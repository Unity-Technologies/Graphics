using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Sub-graph/Sub-graph Node")]
    public class SubGraphNode : AbstractMaterialNode
        , IGeneratesBodyCode
        , IGeneratesFunction
        , IOnAssetEnabled
        , IMayRequireNormal
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequireWorldPosition
    {
        [SerializeField]
        private string m_SerializedSubGraph = string.Empty;

        [Serializable]
        private class SubGraphHelper
        {
            public MaterialSubGraphAsset subGraph;
        }


#if UNITY_EDITOR
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
                var helper = new SubGraphHelper();
                helper.subGraph = value;
                m_SerializedSubGraph = EditorJsonUtility.ToJson(helper, true);
                OnEnable();

                if (onModified != null)
                    onModified(this, ModificationScope.Graph);
            }
        }
        /*
       // SAVED FOR LATER
        if (serializedVersion<kCurrentSerializedVersion)
                    DoUpgrade();
        [SerializeField]
        private string m_SubGraphAssetGuid;

        [SerializeField]
        private int serializedVersion = 0;
        const int kCurrentSerializedVersion = 1;

        private void DoUpgrade()
        {
            var helper = new SubGraphHelper();
            if (string.IsNullOrEmpty(m_SubGraphAssetGuid))
                helper.subGraph = null;

            var path = AssetDatabase.GUIDToAssetPath(m_SubGraphAssetGuid);
            if (string.IsNullOrEmpty(path))
                helper.subGraph = null;

            helper.subGraph = AssetDatabase.LoadAssetAtPath<MaterialSubGraphAsset>(path);

            m_SerializedSubGraph = EditorJsonUtility.ToJson(helper, true);
            serializedVersion = kCurrentSerializedVersion;
            m_SubGraphAssetGuid = string.Empty;
            mark dirty damn
        }*/
#else
        public MaterialSubGraphAsset subGraphAsset {get; set; }
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

                return PreviewMode.Preview3D;
            }
        }

        public SubGraphNode()
        {
            name = "SubGraph";
        }

        public void OnEnable()
        {
            var validNames = new List<int>();
            if (subGraphAsset == null)
            {
                RemoveSlotsNameNotMatching(validNames);
                return;
            }

            var subGraphInputNode = subGraphAsset.subGraph.inputNode;
            foreach (var slot in subGraphInputNode.GetOutputSlots<MaterialSlot>())
            {
                AddSlot(new MaterialSlot(slot.id, slot.displayName, slot.shaderOutputName, SlotType.Input, slot.valueType, slot.defaultValue));
                validNames.Add(slot.id);
            }

            var subGraphOutputNode = subGraphAsset.subGraph.outputNode;
            foreach (var slot in subGraphOutputNode.GetInputSlots<MaterialSlot>())
            {
                AddSlot(new MaterialSlot(slot.id, slot.displayName, slot.shaderOutputName, SlotType.Output, slot.valueType, slot.defaultValue));
                validNames.Add(slot.id);
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
                    + GetVariableNameForSlot(slot.id)
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
                var varName = subGraphInputNode.GetVariableNameForSlot(slot.id);
                var varValue = GetSlotValue(slot.id, GenerationMode.ForReals);

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
            subGraph.GenerateNodeCode(bodyGenerator, GenerationMode.ForReals);
            var subGraphOutputNode = subGraphAsset.subGraph.outputNode;
            outputString.AddShaderChunk(bodyGenerator.GetShaderString(0), false);

            // Step 5...
            // Copy the outputs to the parent context name);
            foreach (var slot in GetOutputSlots<MaterialSlot>())
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

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyBlock(visitor, generationMode);

            if (subGraph == null)
                return;

            subGraph.GeneratePropertyBlock(visitor, GenerationMode.ForReals);
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyUsages(visitor, generationMode);

            if (subGraph == null)
                return;

            subGraph.GeneratePropertyUsages(visitor, GenerationMode.ForReals);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
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

            subGraph.GenerateNodeFunction(visitor, GenerationMode.ForReals);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (subGraph == null)
                return;

            subGraph.GenerateVertexShaderBlock(visitor, GenerationMode.ForReals);
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (subGraph == null)
                return;

            subGraph.GenerateVertexToFragmentBlock(visitor, GenerationMode.ForReals);
        }

        public bool RequiresNormal()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal());
        }

        public bool RequiresMeshUV()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV());
        }

        public bool RequiresScreenPosition()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
        }

        public bool RequiresViewDirection()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection());
        }

        public bool RequiresWorldPosition()
        {
            if (subGraph == null)
                return false;

            return subGraph.activeNodes.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition());
        }
    }
}
