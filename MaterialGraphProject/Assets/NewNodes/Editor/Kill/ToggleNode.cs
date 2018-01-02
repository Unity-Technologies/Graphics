using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    /* [Title("Input", "Toggle")]
     public class ToggleNode : PropertyNode, IGeneratesBodyCode
     {
         [SerializeField]
         //private float m_Float;
         private bool m_ToggleState;

         private const int kOutputSlotId = 0;
         private const string kOutputSlotName = "Ouput";

         public ToggleNode()
         {
             name = "Toggle";
             UpdateNodeAfterDeserialization();
         }

         public sealed override void UpdateNodeAfterDeserialization()
         {
             AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector1, Vector2.zero));
             RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
         }

         public override PropertyType propertyType
         {
             get { return PropertyType.Float; }
         }

         public bool value
         {
             get { return m_ToggleState; }
             set
             {
                 if (m_ToggleState == value)
                     return;

                 m_ToggleState = value;
                 Dirty(ModificationScope.Node);
             }
         }

         public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
         {
             if (exposedState == ExposedState.Exposed)
                 visitor.AddShaderProperty(new FloatPropertyChunk(propertyName, description, 0f, FloatPropertyChunk.FloatType.Toggle, PropertyChunk.HideState.Visible));
         }

         public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
         {
             if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
                 visitor.AddShaderChunk(precision + " " + propertyName + ";", true);
         }

         public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
         {
             if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
                 return;

             visitor.AddShaderChunk(value.ToString(), true);
         }

         public override PreviewProperty GetPreviewProperty()
         {
             return new PreviewProperty
             {
                 m_Name = propertyName,
                 m_PropType = PropertyType.Float,
                 m_Float = value ? 1f : 0f
             };
         }
     }*/
}
