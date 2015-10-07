using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	abstract class Function2Input : BaseMaterialNode, IGeneratesBodyCode
	{
		public override bool hasPreview { get { return true; } }

		public override void Init()
		{
			base.Init();
			AddSlot(new Slot(SlotType.OutputSlot, GetOutputSlotName ()));
			AddSlot(new Slot(SlotType.InputSlot, GetInputSlot1Name ()));
			AddSlot(new Slot(SlotType.InputSlot, GetInputSlot2Name ()));
		}

		protected virtual string GetInputSlot1Name() {return "Input1";}
		protected virtual string GetInputSlot2Name() {return "Input2";}
		protected virtual string GetOutputSlotName() {return "Output";}

		protected abstract string GetFunctionName();

		public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputSlot = FindOutputSlot (GetOutputSlotName ());
			var inputSlot1 = FindInputSlot (GetInputSlot1Name ());
			var inputSlot2 = FindInputSlot (GetInputSlot2Name ());

			if (inputSlot1 == null || inputSlot2 == null || outputSlot == null)
			{
				Debug.LogError("Invalid slot configuration on node: " + name);
				return;
			}

			string input1Value = GetSlotValue (inputSlot1, generationMode);
			string input2Value = GetSlotValue (inputSlot2, generationMode);
			visitor.AddShaderChunk(precision + "4 " + GetOutputVariableNameForSlot(outputSlot, generationMode) + " = " + GetFunctionCallBody(input1Value, input2Value) + ";", true);
		}
		
		protected virtual string GetFunctionCallBody (string input1Value, string input2Value)
		{
			return GetFunctionName() + " (" + input1Value + ", " + input2Value +")";
		}
	}
}
