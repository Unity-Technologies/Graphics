using System.Collections.Generic;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Vector/Vector 1")]
    public class Vector1Node : AbstractMaterialNode, IGeneratesBodyCode
    {
        [SerializeField]
        private float m_Value;

        /*[SerializeField]
        private FloatPropertyChunk.FloatType m_floatType;*/

       // [SerializeField]
        //private Vector3 m_rangeValues = new Vector3(0f, 1f, 2f);

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Value";

        public Vector1Node()
        {
            name = "Vector1";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public float value
        {
            get { return m_Value; }
            set
            {
                if (m_Value == value)
                    return;

                m_Value = value;

                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

       /* public FloatPropertyChunk.FloatType floatType
        {
            get { return m_floatType; }
            set
            {
                if (m_floatType == value)
                    return;

                m_floatType = value;

                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }*/

      /*  public Vector3 rangeValues
        {
            get { return m_rangeValues; }
            set
            {
                if (m_rangeValues == value)
                    return;

                m_rangeValues = value;

                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }
*/
        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddShaderProperty(new FloatShaderProperty()
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false,
                value = value
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + " " + GetVariableNameForNode() + " = " + m_Value + ";", true);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty()
            {
                m_Name = GetVariableNameForNode(),
                m_PropType = PropertyType.Float,
                m_Float = m_Value
            });
        }
    }
}
