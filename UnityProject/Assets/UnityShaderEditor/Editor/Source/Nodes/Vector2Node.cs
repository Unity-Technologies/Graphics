using System;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Input/Vector 2 Node")]
    class Vector2Node : PropertyNode, IGeneratesBodyCode
    {
        private const string kOutputSlotName = "Value";

        [SerializeField]
        private Vector2 m_Value;

        public override void OnCreate()
        {
            base.OnCreate();
            name = "V2Node"; 
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), SlotValueType.Vector2));
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector2; }
        }
        
        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType valueType)
        {
            if (exposed || generationMode.IsPreview())
                visitor.AddShaderChunk("float2 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposed || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "2 " +  propertyName + " = " + precision + "2 (" + m_Value.x + ", " + m_Value.y + ");", true);
        }
        
        public override GUIModificationType NodeUI(Rect drawArea)
        {
            base.NodeUI(drawArea);

            EditorGUI.BeginChangeCheck();
            m_Value = EditorGUI.Vector2Field(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), "Value", m_Value);
            if (EditorGUI.EndChangeCheck())
            {
                return GUIModificationType.Repaint;
            }
            return GUIModificationType.None;
        }
        
        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
                   {
                       m_Name = propertyName,
                       m_PropType = PropertyType.Vector2,
                       m_Vector4 = m_Value
                   };
        }
    }
}
