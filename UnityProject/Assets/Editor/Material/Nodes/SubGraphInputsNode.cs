using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	public class SubGraphInputsNode : SubGraphIOBaseNode, IGenerateProperties
	{
		public override void Init()
		{
			name = "SubGraphInputs";
			title = "Inputs";
			position = new Rect(BaseMaterialGraphGUI.kDefaultNodeWidth * 4, BaseMaterialGraphGUI.kDefaultNodeHeight * 2, Mathf.Max(300, position.width), position.height);
			base.Init();
		}

		public override void AddSlot ()
		{
			AddSlot (new Slot (SlotType.OutputSlot, GenerateSlotName (SlotType.OutputSlot)));
		}

		public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
		{
			base.GeneratePropertyBlock (visitor, generationMode);

			if (!generationMode.IsPreview ())
				return;

			foreach (var slot in outputSlots)
			{
				if (slot.edges.Count == 0)
					continue;

				var defaultValue = GetSlotDefaultValue (slot.name);
				if (defaultValue != null)
					defaultValue.GeneratePropertyBlock (visitor, generationMode);
			}
		}

		public override void GeneratePropertyUsages (ShaderGenerator visitor, GenerationMode generationMode)
		{
			base.GeneratePropertyUsages (visitor, generationMode);

			if (!generationMode.IsPreview ())
				return;

			foreach (var slot in outputSlots) 
			{
				if (slot.edges.Count == 0)
					continue;

				var defaultValue = GetSlotDefaultValue(slot.name);
				if (defaultValue != null)
					defaultValue.GeneratePropertyUsages(visitor, generationMode);
			}
		}

		public override void UpdatePreviewProperties ()
		{
			base.UpdatePreviewProperties ();
			foreach (var slot in outputSlots)
			{
				if (slot.edges.Count == 0)
					continue;

				var defaultOutput = GetSlotDefaultValue (slot.name);
				if (defaultOutput == null)
					continue;

				var pp = new PreviewProperty
				{
					m_Name = defaultOutput.inputName,
					m_PropType = PropertyType.Vector4,
					m_Vector4 = defaultOutput.defaultValue
				};
				SetDependentPreviewMaterialProperty(pp);
			}
		}
	}
}
