using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	[Title("Time/Time Node")]
	public class TimeNode : BaseMaterialNode, IGenerateProperties, IRequiresTime
	{
		private const string kOutputSlotName = "Time";

		public override void Init()
		{
			base.Init ();
			name = "Time";
			AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
		}

		public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
		{
			return generationMode.IsPreview () ? "EDITOR_TIME" : "_Time";
		}

		public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
		{
			base.GeneratePropertyBlock (visitor, generationMode);

			if (!generationMode.IsPreview ())
				return;

			visitor.AddShaderProperty (new VectorPropertyChunk ("EDITOR_TIME", "EDITOR_TIME", Vector4.one, true));
		}

		public override void GeneratePropertyUsages (ShaderGenerator visitor, GenerationMode generationMode)
		{
			base.GeneratePropertyUsages (visitor, generationMode);

			if (!generationMode.IsPreview ())
				return;

			visitor.AddShaderChunk(precision + "4 " + GetPropertyName() + ";", true);
		}

		public string GetPropertyName () {return "EDITOR_TIME";}
	}
}
