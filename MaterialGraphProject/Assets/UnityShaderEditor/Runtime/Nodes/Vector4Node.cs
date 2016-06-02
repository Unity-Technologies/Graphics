using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Vector 4 Node")]
    class Vector4Node : PropertyNode, IGeneratesBodyCode
    {
        [SerializeField]
        public Vector4 m_Value;
       
        private const string kOutputSlotName = "Value";
        
        public Vector4Node(IGraph owner) : base(owner)
        {
            name = "V4Node";
            UpdateSlots();
        }

        private void UpdateSlots()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType valueType)
        {
            if (exposed || generationMode.IsPreview())
                visitor.AddShaderChunk("float4 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposed || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "4 " +  propertyName + " = " + precision + "4 (" + m_Value.x + ", " + m_Value.y + ", " + m_Value.z + ", " + m_Value.w + ");", true);
        }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
                   {
                       m_Name = propertyName,
                       m_PropType = PropertyType.Vector4,
                       m_Vector4 = m_Value
                   };
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            UpdateSlots(); 
        }


        /*
 public override GUIModificationType NodeUI(Rect drawArea)
 {
     base.NodeUI(drawArea);

     EditorGUI.BeginChangeCheck();
     m_Value = EditorGUI.Vector4Field(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), "Value", m_Value);
     if (EditorGUI.EndChangeCheck())
     {
         //TODO:tidy this shit.
         //EditorUtility.SetDirty(materialGraphOwner.owner);
         return GUIModificationType.Repaint;
     }
     return GUIModificationType.None;
 }
 */
    }
}
