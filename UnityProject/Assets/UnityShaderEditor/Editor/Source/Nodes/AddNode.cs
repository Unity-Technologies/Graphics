using UnityEngine;

namespace UnityEditor.Graphs.Material
{
	[Title("Math/Add Node")]
	class AddNode : FunctionMultiInput, IGeneratesFunction
	{
		[SerializeField]
		private float m_DefaultValue = 0.0f;

		public override void Init()
		{
			name = "AddNode";
			base.Init();
		}

		protected override string GetFunctionName () {return "unity_add_"+precision;}

		public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
		{
			var outputString = new ShaderGenerator ();
			foreach (var precision in m_PrecisionNames)
			{
				outputString.AddShaderChunk("inline " + precision + "4 unity_add_" + precision + " (" + precision + "4 arg1, " + precision + "4 arg2)", false);
				outputString.AddShaderChunk("{", false);
				outputString.Indent();
				outputString.AddShaderChunk("return arg1 + arg2;", false);
				outputString.Deindent();
				outputString.AddShaderChunk("}", false);
			}

			visitor.AddShaderChunk (outputString.GetShaderString (0), true);
		}

		public override Vector4 GetNewSlotDefaultValue ()
		{
			return Vector4.zero;
		}

		public override void NodeUI (Graphs.GraphGUI host)
		{
			base.NodeUI(host);
			
			EditorGUI.BeginChangeCheck ();
			m_DefaultValue = EditorGUILayout.FloatField("", m_DefaultValue, GUILayout.Width(64));
			if (EditorGUI.EndChangeCheck ())
				RegeneratePreviewShaders ();
		}
	}
}
