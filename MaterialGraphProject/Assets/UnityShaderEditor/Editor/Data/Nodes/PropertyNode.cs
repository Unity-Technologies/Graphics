using System;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Property Node")]
    public class PropertyNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        private Guid m_PropertyGuid;

        [SerializeField] private string m_PropertyGuidSerialized;

        public const int OutputSlotId = 0;

        public const int ROutputSlotId = 1;
        public const int GOutputSlotId = 2;
        public const int BOutputSlotId = 3;
        public const int AOutputSlotId = 4;
        public const int TOutputSlotId = 5;

        public const int UVInput = 6;

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
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Vector2ShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "V2", "V2", SlotType.Output, SlotValueType.Vector2, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Vector3ShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "V3", "V3", SlotType.Output, SlotValueType.Vector3, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is Vector4ShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "V4", "V4", SlotType.Output, SlotValueType.Vector4, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is ColorShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "Color", "Color", SlotType.Output, SlotValueType.Vector4, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId});
            }
            else if (property is TextureShaderProperty)
            {
                AddSlot(new MaterialSlot(OutputSlotId, "RGBA", "RGBA", SlotType.Output, SlotValueType.Vector4, Vector4.zero));
                AddSlot(new MaterialSlot(ROutputSlotId, "R", "R", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(GOutputSlotId, "G", "G", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(BOutputSlotId, "B", "B", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(AOutputSlotId, "A", "A", SlotType.Output, SlotValueType.Vector1, Vector4.zero));
                AddSlot(new MaterialSlot(TOutputSlotId, "T", "T", SlotType.Output, SlotValueType.Texture2D, Vector4.zero));
                AddSlot(new MaterialSlot(UVInput, "UV", "UV", SlotType.Input, SlotValueType.Vector2, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] {OutputSlotId, ROutputSlotId, GOutputSlotId, BOutputSlotId, AOutputSlotId, TOutputSlotId,UVInput});
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
            else if (property is TextureShaderProperty)
            {
                //UV input slot
                var uvSlot = FindInputSlot<MaterialSlot>(UVInput);
                var uvName = string.Format("{0}.xy", UVChannel.uv0.GetUVName());
                var edgesUV = owner.GetEdges(uvSlot.slotReference);
                if (edgesUV.Any())
                    uvName = GetSlotValue(UVInput, generationMode);

                //Sampler input slot
                var result = string.Format("{0}4 {1} = UNITY_SAMPLE_TEX2D({2},{3});"
                        , precision
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName
                        , uvName);
                visitor.AddShaderChunk(result, true);
                visitor.AddShaderChunk(string.Format("{0} {1} = {2}.r;", precision, GetVariableNameForSlot(ROutputSlotId), GetVariableNameForSlot(OutputSlotId)), true);
                visitor.AddShaderChunk(string.Format("{0} {1} = {2}.g;", precision, GetVariableNameForSlot(GOutputSlotId), GetVariableNameForSlot(OutputSlotId)), true);
                visitor.AddShaderChunk(string.Format("{0} {1} = {2}.b;", precision, GetVariableNameForSlot(BOutputSlotId), GetVariableNameForSlot(OutputSlotId)), true);
                visitor.AddShaderChunk(string.Format("{0} {1} = {2}.a;", precision, GetVariableNameForSlot(AOutputSlotId), GetVariableNameForSlot(OutputSlotId)), true);
            }
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            if (channel != UVChannel.uv0)
                return false;

            var uvSlot = FindInputSlot<MaterialSlot>(UVInput);
            if (uvSlot == null)
                return true;

            var edges = owner.GetEdges(uvSlot.slotReference).ToList();
            return edges.Count == 0;
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
            if (slotId != TOutputSlotId)
                return base.GetVariableNameForSlot(slotId);

            var graph = owner as AbstractMaterialGraph;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);
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
    }
}
