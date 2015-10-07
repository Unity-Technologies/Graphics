using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    [Title("Sub-graph/Sub-graph Node")]
    public class SubGraphNode : BaseMaterialNode, IGeneratesBodyCode, IGeneratesVertexToFragmentBlock, IGeneratesFunction, IGeneratesVertexShaderBlock
    {
        [SerializeField]
        private MaterialSubGraph m_SubGraphAsset;

        public MaterialSubGraph subGraphAsset { get { return m_SubGraphAsset; } }

        public override PreviewMode previewMode
        {
            get
            {
                if (subGraphAsset == null)
                    return PreviewMode.Preview2D;

                var preview3D = subGraphAsset.outputsNode.CollectChildNodesByExecutionOrder().Any(x => x.previewMode == PreviewMode.Preview3D);
                return preview3D ? PreviewMode.Preview3D : PreviewMode.Preview2D;
            }
        }

        public const int kMaxSlots = 16;

        public override void Init()
        {
            base.Init();
            name = "SubGraph";
            position = new Rect(position.x, position.y, Mathf.Max(300, position.width), position.height);
        }

        static string GetInputSlotName(int n)
        {
            return string.Format("I{0:00}", n);
        }

        static string GetOutputSlotName(int n)
        {
            return string.Format("O{0:00}", n);
        }

        public override IEnumerable<Slot> GetValidInputSlots()
        {
            // We only want to return the input slots that are internally wired to an output slot
            return base.GetValidInputSlots().Where(slot => m_SubGraphAsset.InputInternallyWired(slot.name, this)).ToList();
        }

        public override void NodeUI(GraphGUI host)
        {
            EditorGUI.BeginChangeCheck();
            m_SubGraphAsset = (MaterialSubGraph)EditorGUILayout.ObjectField(GUIContent.none, m_SubGraphAsset, typeof(MaterialSubGraph), false);
            if (EditorGUI.EndChangeCheck() && m_SubGraphAsset != null)
            {
                SubGraphChanged(m_SubGraphAsset);
            }
        }

        private void SubGraphChanged(MaterialSubGraph sender)
        {
            RefreshSlots(SlotType.InputSlot, inputSlots.ToList(), sender.inputsNode.slots, (s) => true);
            RefreshSlots(SlotType.OutputSlot, outputSlots.ToList(), sender.outputsNode.slots, sender.OutputInternallyWired);
            RegeneratePreviewShaders();
        }

        private void RefreshSlots(SlotType type, IEnumerable<Slot> current, IEnumerable<Slot> updated, Func<string, bool> slotFilter)
        {
            var innerOutputSlots = updated.Where(n => slotFilter(n.name)).ToArray();
            foreach (var slot in innerOutputSlots)
            {
                var s = current.FirstOrDefault(n => n.name == slot.name);
                if (s == null)
                    AddSlot(new Slot(type, slot.name, slot.title));
                else
                    s.title = slot.title;
            }

            var danglingSlots = current.Except(innerOutputSlots, (ls, rs) =>  ls.name == rs.name).ToArray();
            foreach (var slot in danglingSlots)
                RemoveSlot(slot);
        }

        public void GenerateNodeCode(ShaderGenerator shaderBodyVisitor, GenerationMode generationMode)
        {
            if (m_SubGraphAsset == null)
                return;

            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("// Subgraph for node " + GetOutputVariableNameForNode(), false);

            // Step 1...
            // find out which output slots are actually used
            //TODO: Be smarter about this and only output ones that are actually USED, not just connected
            //var validOutputSlots = NodeUtils.GetSlotsThatOutputToNodeRecurse(this, (graph as BaseMaterialGraph).masterNode);
            var validOutputSlots = outputSlots.Where(x => x.edges.Count > 0);
            foreach (var slot in validOutputSlots)
            {
                outputString.AddShaderChunk(
                    "float4 "
                    + GetOutputVariableNameForSlot(slot, generationMode)
                    + " = float4(0, 0, 0, 0);", false);
            }

            // Step 2...
            // Go into the subgraph
            outputString.AddShaderChunk("{", false);
            outputString.Indent();

            // Step 3...
            // For each input that is used and connects through we want to generate code.
            // First we assign the input variables to the subgraph
            foreach (var slot in slots)
            {
                if (!slot.isInputSlot)
                    continue;

                // see if the input connects all the way though to the output
                // if it does allow generation
                var inputWired = m_SubGraphAsset.InputInternallyWired(slot.name, this);
                if (!inputWired)
                    continue;

                var varName = m_SubGraphAsset.GetInputVariableNameForSlotByName(slot.name, this, generationMode);
                var varValue = "float4(0, 0, 0, 0);";
                var slotDefaultValue = GetSlotDefaultValue(slot.name);
                if (slotDefaultValue != null)
                {
                    varValue = slotDefaultValue.GetDefaultValue(generationMode);
                }
                bool externallyWired = slot.edges.Count > 0;
                if (externallyWired)
                {
                    var fromSlot = slot.edges[0].fromSlot;
                    var fromNode = slot.edges[0].fromSlot.node as BaseMaterialNode;
                    varValue = fromNode.GetOutputVariableNameForSlot(fromSlot, generationMode);
                }

                outputString.AddShaderChunk("float4 " + varName + " = " + varValue + ";", false);
            }

            // Step 4...
            // Using the inputs we can now generate the shader body :)
            var bodyGenerator = new ShaderGenerator();
            m_SubGraphAsset.GenerateNodeCode(bodyGenerator, this);
            outputString.AddShaderChunk(bodyGenerator.GetShaderString(0), false);

            // Step 5...
            // Copy the outputs to the parent context name);
            foreach (var slot in validOutputSlots)
            {
                bool internallyWired = m_SubGraphAsset.OutputInternallyWired(slot.name);
                if (internallyWired)
                {
                    outputString.AddShaderChunk(
                        GetOutputVariableNameForSlot(slot, generationMode)
                        + " = "
                        + m_SubGraphAsset.GetOutputVariableNameForSlotByName(slot.name, this, generationMode)
                        + ";", false);
                }
            }

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.AddShaderChunk("// Subgraph ends", false);

            shaderBodyVisitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
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

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyUsages(visitor, generationMode);
            m_SubGraphAsset.GeneratePropertyUsages(visitor, GenerationMode.SurfaceShader);
        }

        public override void UpdatePreviewProperties()
        {
            base.UpdatePreviewProperties();

            var previewProperties = m_SubGraphAsset.GetPreviewProperties();

            foreach (var prop in previewProperties)
                SetDependentPreviewMaterialProperty(prop);
        }
    }
}
