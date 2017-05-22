using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
	[Title ("Math/Range/Remap")]
	public class RemapNode : Function2Input, IGeneratesFunction
	{
		/*protected const string kInputSlot1ShaderName = "Input1";
		protected const string kInputSlot2ShaderName = "Input2";
		protected const string kOutputSlotShaderName = "Output";

		public const int InputSlot1Id = 0;
		public const int InputSlot2Id = 1;
		public const int OutputSlotId = 2;

		[SerializeField]
		private Vector4 m_Value;
*/

		public RemapNode ()
		{
			name = "RemapNode";
			//UpdateNodeAfterDeserialization ();
		}

		protected override string GetFunctionName ()
		{
			return "unity_remap_" + precision;
		}

		//need to override the type of slot two somehow///////////////////////////////////////////////////
		/*override MaterialSlot GetInputSlot2 ()
		{
			return new MaterialSlot (InputSlot2Id, GetInputSlot2Name (), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector4, Vector4.zero);
		}*/

		public void GenerateNodeFunction (ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			outputString.AddShaderChunk (GetFunctionPrototype ("arg1", "arg2"), false);
			outputString.AddShaderChunk ("{", false);
			outputString.Indent ();
			outputString.AddShaderChunk ("return ((arg1 * (arg2.y - arg2.x)) * (arg2.w - arg2.z))+arg2.z;", false);
			outputString.Deindent ();
			outputString.AddShaderChunk ("}", false);

			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}

		/*
		public sealed override void UpdateNodeAfterDeserialization ()
		{
			AddSlot (GetInputSlot1 ());
			AddSlot (GetInputSlot2 ());
			AddSlot (GetOutputSlot ());
			RemoveSlotsNameNotMatching (validSlots);
		}

		protected int[] validSlots {
			get { return new[] { InputSlot1Id, InputSlot2Id, OutputSlotId }; }
		}

		protected virtual MaterialSlot GetInputSlot1 ()
		{
			return new MaterialSlot (InputSlot1Id, GetInputSlot1Name (), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
		}

		protected virtual MaterialSlot GetInputSlot2 ()
		{
			return new MaterialSlot (InputSlot2Id, GetInputSlot2Name (), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector4, Vector4.zero);
		}

		protected virtual MaterialSlot GetOutputSlot ()
		{
			return new MaterialSlot (OutputSlotId, GetOutputSlotName (), kOutputSlotShaderName, SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
		}

		protected virtual string GetInputSlot1Name ()
		{
			return "Input1";
		}

		protected virtual string GetInputSlot2Name ()
		{
			return "RemapVector";
		}

		protected virtual string GetOutputSlotName ()
		{
			return "Output";
		}

		//protected abstract string GetFunctionName ();

		/*protected virtual string GetFunctionPrototype (string arg1Name, string arg2Name)
		{
			return "inline " + precision + outputDimension + " " + GetFunctionName () + " ("
			+ precision + input1Dimension + " " + arg1Name + ", "
			+ precision + input2Dimension + " " + arg2Name + ")";
		}

		public void GenerateNodeCode (ShaderGenerator visitor, GenerationMode generationMode)
		{
			NodeUtils.SlotConfigurationExceptionIfBadConfiguration (this, new[] { InputSlot1Id, InputSlot2Id }, new[] { OutputSlotId });
			string input1Value = GetSlotValue (InputSlot1Id, generationMode);
			string input2Value = GetSlotValue (InputSlot2Id, generationMode);
			visitor.AddShaderChunk (precision + outputDimension + " " + GetVariableNameForSlot (OutputSlotId) + " = " + GetFunctionCallBody (input1Value, input2Value) + ";", true);
		}

		protected virtual string GetFunctionCallBody (string input1Value, string input2Value)
		{
			return GetFunctionName () + " (" + input1Value + ", " + input2Value + ")";
		}*/

		/*public string outputDimension {
			get { return ConvertConcreteSlotValueTypeToString (FindOutputSlot<MaterialSlot> (OutputSlotId).concreteValueType); }
		}

		private string input1Dimension {
			get { return ConvertConcreteSlotValueTypeToString (FindInputSlot<MaterialSlot> (InputSlot1Id).concreteValueType); }
		}

		private string input2Dimension {
			get { return ConvertConcreteSlotValueTypeToString (FindInputSlot<MaterialSlot> (InputSlot2Id).concreteValueType); }
		}*/

		/// <summary>
		/// Gets the type of the property.*/
		/// </summary>
		/// <value>The type of the property.</value>

		/*public override PropertyType propertyType {
			get { return PropertyType.Vector4; }
		}

		public Vector4 value {
			get { return m_Value; }
			set {
				if (m_Value == value)
					return;

				m_Value = value;

				if (onModified != null)
					onModified (this, ModificationScope.Node);
			}
		}*/

		/*public override void GeneratePropertyBlock (PropertyGenerator visitor, GenerationMode generationMode)
		{
			if (exposedState == ExposedState.Exposed)
				visitor.AddShaderProperty (new VectorPropertyChunk (propertyName, description, m_Value, PropertyChunk.HideState.Visible));
		}

		public override void GeneratePropertyUsages (ShaderGenerator visitor, GenerationMode generationMode)
		{
			if (exposedState == ExposedState.Exposed || generationMode.IsPreview ())
				visitor.AddShaderChunk (precision + "3 " + propertyName + ";", false);
		}*/

		/*public void GenerateNodeCode (ShaderGenerator visitor, GenerationMode generationMode)
		{
			if (exposedState == ExposedState.Exposed || generationMode.IsPreview ())
				return;
			var input1Value = GetSlotValue (InputSlot1Id, generationMode);
			var input2Value = GetSlotValue (InputSlot2Id, generationMode);
			visitor.AddShaderChunk (precision + "4 " + propertyName + " = " + input1Value + " * ((" + input2Value + ".w - " + input2Value + ".z) + " + input2Value + ".z);", false);
		}

		public override bool hasPreview {
			get { return true; }
		}

		public override PreviewProperty GetPreviewProperty ()
		{
			return new PreviewProperty {
				m_Name = propertyName,
				m_PropType = PropertyType.Vector4,
				m_Vector4 = m_Value
			};
		}*/
	}
}
