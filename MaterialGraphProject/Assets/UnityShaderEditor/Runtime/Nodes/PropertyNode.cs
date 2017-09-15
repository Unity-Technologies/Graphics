using System;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Property Node")]
    public class PropertyNode : AbstractMaterialNode
    {
        private Guid m_PropertyGuid;

        [SerializeField]
        private string m_PropertyGuidSerialized;

        public const int OutputSlotId = 0;

        public const int ROutputSlotId = 1;
        public const int GOutputSlotId = 2;
        public const int BOutputSlotId = 3;
        public const int AOutputSlotId = 4;
        public const int TOutputSlotId = 5;

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
                AddSlot(new MaterialSlot(OutputSlotId, "float", "float", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is Vector2ShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "V2", "V2", SlotType.Output, SlotValueType.Vector2, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is Vector3ShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "V3", "V3", SlotType.Output, SlotValueType.Vector3, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is Vector4ShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "V4", "V4", SlotType.Output, SlotValueType.Vector4, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is ColorShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "Color", "Color", SlotType.Output, SlotValueType.Vector4, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is TextureShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "RGBA", "RGBA", SlotType.Output, SlotValueType.Vector4, Vector4.zero));
                AddSlot(new MaterialSlot(ROutputSlotId, "R", "R", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(GOutputSlotId, "G", "G", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(BOutputSlotId, "B", "B", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(AOutputSlotId, "A", "A", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(TOutputSlotId, "T", "T", SlotType.Output, SlotValueType.Texture2D, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId, ROutputSlotId, GOutputSlotId, BOutputSlotId, AOutputSlotId, TOutputSlotId});
            }
        }

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

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            var graph = owner as AbstractMaterialGraph;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);
            return property.name;
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

    }
}
