using System.Collections.Generic;
using UnityEngine.MaterialGraph;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Sampler State")]
    public class SamplerStateNode : AbstractMaterialNode, IGeneratesBodyCode
    {

        public enum FilterMode
        {
            Linear,
            Point
        }

        public enum WrapMode
        {
            Repeat,
            Clamp
        }

        static Dictionary<FilterMode, string> filterMode = new Dictionary<FilterMode, string>
        {
            {FilterMode.Linear, "_linear"},
            {FilterMode.Point, "_point"},
        };

        static Dictionary<WrapMode, string> wrapMode = new Dictionary<WrapMode, string>
        {
            {WrapMode.Repeat, "_repeat"},
            {WrapMode.Clamp, "_clamp"},
        };


        [SerializeField]
        private FilterMode m_filter = FilterMode.Linear;

 
        public FilterMode filter
        {
            get { return m_filter; }
            set
            {
                if (m_filter == value)
                    return;

                m_filter = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        [SerializeField]
        private WrapMode m_wrap = WrapMode.Repeat;


        public WrapMode wrap
        {
            get { return m_wrap; }
            set
            {
                if (m_wrap == value)
                    return;

                m_wrap = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        public SamplerStateNode()
        {
            name = "SamplerState";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Sampler Output";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.SamplerState, Vector4.zero, false));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        /*
        public override PropertyType propertyType
        {
            get { return PropertyType.SamplerState; }
        }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName + "HEEEEEEYYYYY",
                m_PropType = PropertyType.Float,
               // m_Float = filterMode[filter];
            };
        }

        */



        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
           // GetVariableNameForSlot();
        }

        public override string GetVariableNameForSlot(int slotId)
        {
           // var slot = FindSlot<MaterialSlot>(slotId);
            //if (slot == null)
              //  throw new ArgumentException(string.Format("Attempting to use MaterialSlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");

            return GetVariableNameForNode();
        }

        //my_linear_repeat_sampler
        public override string GetVariableNameForNode()
        {
            return "my" + filterMode[filter] + wrapMode[wrap] + "_sampler";           
        }
    }
}
