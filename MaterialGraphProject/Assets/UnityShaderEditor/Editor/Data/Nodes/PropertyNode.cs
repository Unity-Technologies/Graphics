using System;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Property Node")]
    public class PropertyNode : AbstractMaterialNode, IGeneratesBodyCode, IOnAssetEnabled
    {
        private Guid m_PropertyGuid;

        [SerializeField]
        private string m_PropertyGuidSerialized;

        public const int OutputSlotId = 0;

        public PropertyNode()
        {
            name = "Property";
            UpdateNodeAfterDeserialization();
        }

        private void UpdateNode()
        {
            var graph = owner as AbstractMaterialGraph;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
                return;

            if (property is FloatShaderProperty)
            {
                AddSlot(new Vector1MaterialSlot(OutputSlotId, "float", "float", SlotType.Output, 0));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Vector2ShaderProperty)
            {
                AddSlot(new Vector2MaterialSlot(OutputSlotId, "V2", "V2", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Vector3ShaderProperty)
            {
                AddSlot(new Vector3MaterialSlot(OutputSlotId, "V3", "V3", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Vector4ShaderProperty)
            {
                AddSlot(new Vector4MaterialSlot(OutputSlotId, "V4", "V4", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is ColorShaderProperty)
            {
                AddSlot(new Vector4MaterialSlot(OutputSlotId, "Color", "Color", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is TextureShaderProperty)
            {
                AddSlot(new Texture2DMaterialSlot(OutputSlotId, "T", "T", SlotType.Output));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var graph = owner as AbstractMaterialGraph;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
                return;

            if (property is FloatShaderProperty)
            {
                var result = string.Format("{0} {1} = {2};"
                    , precision
                    , GetVariableNameForSlot(OutputSlotId)
                    , property.referenceName);
                visitor.AddShaderChunk(result, true);
            }
            else if (property is Vector2ShaderProperty)
            {
                var result = string.Format("{0}2 {1} = {2};"
                    , precision
                    , GetVariableNameForSlot(OutputSlotId)
                    , property.referenceName);
                visitor.AddShaderChunk(result, true);
            }
            else if (property is Vector3ShaderProperty)
            {
                var result = string.Format("{0}3 {1} = {2};"
                    , precision
                    , GetVariableNameForSlot(OutputSlotId)
                    , property.referenceName);
                visitor.AddShaderChunk(result, true);
            }
            else if (property is Vector4ShaderProperty)
            {
                var result = string.Format("{0}4 {1} = {2};"
                    , precision
                    , GetVariableNameForSlot(OutputSlotId)
                    , property.referenceName);
                visitor.AddShaderChunk(result, true);
            }
            else if (property is ColorShaderProperty)
            {
                var result = string.Format("{0}4 {1} = {2};"
                    , precision
                    , GetVariableNameForSlot(OutputSlotId)
                    , property.referenceName);
                visitor.AddShaderChunk(result, true);
            }
        }

        [PropertyControl]
        public Guid propertyGuid
        {
            get { return m_PropertyGuid; }
            set
            {
                if (m_PropertyGuid == value)
                    return;

                var graph = owner as AbstractMaterialGraph;
                var property = graph.properties.FirstOrDefault(x => x.guid == value);
                if (property == null)
                    return;
                m_PropertyGuid = value;

                UpdateNode();

                if (onModified != null)
                {
                    onModified(this, ModificationScope.Topological);
                }
            }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            var graph = owner as AbstractMaterialGraph;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);

            if (!(property is TextureShaderProperty))
                return base.GetVariableNameForSlot(slotId);

            return property.referenceName;
        }

        protected override bool CalculateNodeHasError()
        {
            var graph = owner as AbstractMaterialGraph;

            if (!graph.properties.Any(x => x.guid == propertyGuid))
                return true;

            return false;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_PropertyGuidSerialized = m_PropertyGuid.ToString();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!string.IsNullOrEmpty(m_PropertyGuidSerialized))
                m_PropertyGuid = new Guid(m_PropertyGuidSerialized);
        }

        public void OnEnable()
        {
            UpdateNode();
        }

        public void ReplaceWithConcreteNode()
        {
            var matGraph = owner as MaterialGraph;
            if (matGraph == null)
                return;

            var property = matGraph.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property != null)
            {
                AbstractMaterialNode node = null;
                int slotId = -1;
                if (property is FloatShaderProperty)
                {
                    var createdNode = new Vector1Node();
                    createdNode.value = ((FloatShaderProperty) property).value;
                    slotId = Vector1Node.OutputSlotId;
                    node = createdNode;
                }
                else if (property is Vector2ShaderProperty)
                {
                    var createdNode = new Vector2Node();
                    createdNode.value = ((Vector2ShaderProperty) property).value;
                    slotId = Vector2Node.OutputSlotId;
                    node = createdNode;
                }
                else if (property is Vector3ShaderProperty)
                {
                    var createdNode = new Vector3Node();
                    createdNode.value = ((Vector3ShaderProperty) property).value;
                    slotId = Vector3Node.OutputSlotId;
                    node = createdNode;
                }
                else if (property is Vector4ShaderProperty)
                {
                    var createdNode = new Vector4Node();
                    createdNode.value = ((Vector4ShaderProperty) property).value;
                    slotId = Vector4Node.OutputSlotId;
                    node = createdNode;
                }
                else if (property is ColorShaderProperty)
                {
                    var createdNode = new ColorNode();
                    createdNode.color = ((ColorShaderProperty) property).value;
                    slotId = ColorNode.OutputSlotId;
                    node = createdNode;
                }

                if (node == null)
                    return;

                var slot = FindOutputSlot<MaterialSlot>(OutputSlotId);
                node.drawState = drawState;
                owner.AddNode(node);

                foreach (var edge in owner.GetEdges(slot.slotReference).ToArray())
                    owner.Connect(node.GetSlotReference(slotId), edge.inputSlot);

                owner.RemoveNode(this);
            }
        }
    }
}
