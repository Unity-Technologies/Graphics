using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Vector 1")]
    public class Vector1Node : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private float m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        /*[SerializeField]
        private FloatPropertyChunk.FloatType m_floatType;*/

        // [SerializeField]
        //private Vector3 m_rangeValues = new Vector3(0f, 1f, 2f);

        public Vector1Node()
        {
            name = "Vector 1";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [MultiFloatControl("")]
        public float value
        {
            get { return m_Value; }
            set
            {
                if (m_Value == value)
                    return;

                m_Value = value;

                Dirty(ModificationScope.Node);
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

                 Dirty(ModificationScope.Node);
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

                  Dirty(ModificationScope.Node);
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
            properties.Add(new PreviewProperty(PropertyType.Float)
            {
                name = GetVariableNameForNode(),
                floatValue = m_Value
            });
        }

        public IShaderProperty AsShaderProperty()
        {
            return new FloatShaderProperty { value = value };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
