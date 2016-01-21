using System;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Generate/Color Node")]
    class ColorNode : PropertyNode, IGeneratesBodyCode
    {
        [SerializeField]
        private Color m_Color;

        private const string kOutputSlotName = "Color";

        public override void OnCreate()
        {
            base.OnCreate();
            name = "ColorNode";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), SlotValueType.Vector4));
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Color; }
        }
        
        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            return GetPropertyName();
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            if (exposed || generationMode.IsPreview())
                visitor.AddShaderChunk("float4 " + GetPropertyName() + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            // we only need to generate node code if we are using a constant... otherwise we can just refer to the property :)
            if (exposed || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "4 " + GetPropertyName() + " = " + precision + "4 (" + m_Color.r + ", " + m_Color.g + ", " + m_Color.b + ", " + m_Color.a + ");", true);
        }
        

        public override bool NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);
            
            EditorGUI.BeginChangeCheck();
            m_Color = EditorGUI.ColorField(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), m_Color);
            if (EditorGUI.EndChangeCheck())
                return true;

            return false;
        }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
                   {
                       m_Name = GetPropertyName(),
                       m_PropType = PropertyType.Color,
                       m_Color = m_Color
                   };
        }
    }
}
