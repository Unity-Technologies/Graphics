using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Range/Remap")]
	public class RemapNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
	{
		protected const string kInputSlot1ShaderName = "Input1";
		protected const string kInputSlot2ShaderName = "InMin";
		protected const string kInputSlot3ShaderName = "InMax";
		protected const string kOutputSlotShaderName = "Output";

		public const int InputSlot1Id = 0;
		public const int InputSlot2Id = 1;
		public const int InputSlot3Id = 2;
		public const int OutputSlotId = 3;

		[SerializeField]
		private Vector4 m_Value;

		public override bool hasPreview
		{
			get { return true; }
		}

		public RemapNode ()
		{
			name = "Remap";
			//UpdateNodeAfterDeserialization ();
		}

		public string GetFunctionName ()
		{
			return "unity_remap_" + precision;
		}
		
		public sealed override void UpdateNodeAfterDeserialization()
		{
			AddSlot(GetInputSlot1());
			AddSlot(GetInputSlot2());
			AddSlot(GetInputSlot3());
			AddSlot(GetOutputSlot());
			RemoveSlotsNameNotMatching(validSlots);
		}

		protected int[] validSlots
		{
			get { return new[] {InputSlot1Id, InputSlot2Id, InputSlot3Id, OutputSlotId}; }
		}

		protected virtual MaterialSlot GetInputSlot1()
		{
			return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
		}
		
		protected virtual MaterialSlot GetInputSlot2()
		{
			return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector2, Vector2.zero);
		}
		
		protected virtual MaterialSlot GetInputSlot3()
		{
			return new MaterialSlot(InputSlot3Id, GetInputSlot3Name(), kInputSlot3ShaderName, SlotType.Input, SlotValueType.Vector2, Vector2.zero);
		}
		
		protected virtual MaterialSlot GetOutputSlot()
		{
			return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
		}
		
		protected virtual string GetInputSlot1Name()
		{
			return "Input";
		}
		
		protected virtual string GetInputSlot2Name()
		{
			return "InMin/Max";
		}
		
		protected virtual string GetInputSlot3Name()
		{
			return "OutMin/Max";
		}
		
		protected virtual string GetOutputSlotName()
		{
			return "Output";
		}

		protected virtual string GetFunctionPrototype(string arg1Name, string arg2Name, string arg3Name)
		{
			return "inline " + precision + outputDimension + " " + GetFunctionName() + " ("
				+ precision + input1Dimension + " " + arg1Name + ", "
					+ precision + input2Dimension + " " + arg2Name + ", "
					+ precision + input3Dimension + " " + arg3Name + ")";
		}

		public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
		{
			NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlot1Id, InputSlot2Id, InputSlot3Id }, new[] { OutputSlotId });
			string input1Value = GetSlotValue(InputSlot1Id, generationMode);
			string input2Value = GetSlotValue(InputSlot2Id, generationMode);
			string input3Value = GetSlotValue(InputSlot3Id, generationMode);
			
			visitor.AddShaderChunk(precision + outputDimension + " " + GetVariableNameForSlot(OutputSlotId) + " = " + GetFunctionCallBody(input1Value, input2Value, input3Value) + ";", true);
		}
		
		public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1", "arg2", "arg3"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
			outputString.AddShaderChunk ("return arg1 * ((arg3.y - arg3.x) / (arg2.y - arg2.x)) + arg3.x;", false);
			outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);
			
			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}
		
		protected virtual string GetFunctionCallBody(string inputValue1, string inputValue2, string inputValue3)
		{
			return GetFunctionName() + " (" + inputValue1 + ", " + inputValue2 + ", " + inputValue3 + ")";
		}
		
		public string outputDimension
		{
			get { return ConvertConcreteSlotValueTypeToString(FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType); }
		}
		private string input1Dimension
		{
			get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot1Id).concreteValueType); }
		}
		
		private string input2Dimension
		{
			get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot2Id).concreteValueType); }
		}
		
		public string input3Dimension
		{
			get { return ConvertConcreteSlotValueTypeToString(FindInputSlot<MaterialSlot>(InputSlot3Id).concreteValueType); }
		}
	}
}
