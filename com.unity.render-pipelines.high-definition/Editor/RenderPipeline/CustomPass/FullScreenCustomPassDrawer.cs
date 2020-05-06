using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
	/// <summary>
	/// FullScreen custom pass drawer
	/// </summary>
	[CustomPassDrawerAttribute(typeof(FullScreenCustomPass))]
    class FullScreenCustomPassDrawer : CustomPassDrawer
    {
	    private class Styles
	    {
		    public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            public static GUIContent fullScreenPassMaterial = new GUIContent("FullScreen Material", "FullScreen Material used for the full screen DrawProcedural.");
            public static GUIContent materialPassName = new GUIContent("Pass Name", "The shader pass to use for your fullscreen pass.");
			public static GUIContent fetchColorBuffer = new GUIContent("Fetch Color Buffer", "Tick this if your effect sample/fetch the camera color buffer");

			public static GUIContent overrideStencil = new GUIContent("Override Stencil", "Override the stencil test to allow you to mask your effect on a particular area of the screen.");
			public static GUIContent stencilReferenceValue = new GUIContent("Reference Value", "Value that will be compared to the value in the stencil buffer, using the comparison function.");
			public static GUIContent stencilComparisonFunction = new GUIContent("Comparison Function", "The function to use to compare the reference to the value in the buffer. Depending on result, either the pass operation or fail operation will be executed.");
			public static GUIContent stencilPassOperation = new GUIContent("Pass Operation", "The operation to execute when the comparision succeed.");
			public static GUIContent stencilFailOperation = new GUIContent("Fail Operation", "The operation to execute when the comparision fails.");

			public readonly static string writeAndFetchColorBufferWarning = "Fetching and Writing to the camera color buffer at the same time is not supported on most platforms.";
	    }

		// Fullscreen pass
		SerializedProperty		m_FullScreenPassMaterial;
        SerializedProperty      m_MaterialPassName;
		SerializedProperty      m_FetchColorBuffer;
		SerializedProperty      m_TargetColorBuffer;
		SerializedProperty      m_TargetDepthBuffer;

		// Stencil
		SerializedProperty 		m_OverrideStencil;
        SerializedProperty 		m_StencilReferenceValue;
        SerializedProperty 		m_StencilCompareFunction;
        SerializedProperty 		m_StencilPassOperation;
        SerializedProperty 		m_StencilFailOperation;

		CustomPass.TargetBuffer	targetColorBuffer => (CustomPass.TargetBuffer)m_TargetColorBuffer.intValue;
		CustomPass.TargetBuffer	targetDepthBuffer => (CustomPass.TargetBuffer)m_TargetDepthBuffer.intValue;

	    protected override void Initialize(SerializedProperty customPass)
	    {
			m_FullScreenPassMaterial = customPass.FindPropertyRelative("fullscreenPassMaterial");
            m_MaterialPassName = customPass.FindPropertyRelative("materialPassName");
			m_FetchColorBuffer = customPass.FindPropertyRelative("fetchColorBuffer");
			m_TargetColorBuffer = customPass.FindPropertyRelative("targetColorBuffer");
			m_TargetDepthBuffer = customPass.FindPropertyRelative("targetDepthBuffer");

			m_OverrideStencil = customPass.FindPropertyRelative(nameof(FullScreenCustomPass.overrideStencil));
			m_StencilReferenceValue = customPass.FindPropertyRelative(nameof(FullScreenCustomPass.stencilReferenceValue));
			m_StencilCompareFunction = customPass.FindPropertyRelative(nameof(FullScreenCustomPass.stencilCompareFunction));
			m_StencilPassOperation = customPass.FindPropertyRelative(nameof(FullScreenCustomPass.stencilPassOperation));
			m_StencilFailOperation = customPass.FindPropertyRelative(nameof(FullScreenCustomPass.stencilFailOperation));
	    }

		protected override void DoPassGUI(SerializedProperty customPass, Rect rect)
        {
			DoMaterialGUI(customPass, ref rect);
			DoStencilGUI(customPass, ref rect);
        }

		void DoMaterialGUI(SerializedProperty customPass, ref Rect rect)
		{
			EditorGUI.PropertyField(rect, m_FetchColorBuffer, Styles.fetchColorBuffer);
			rect.y += Styles.defaultLineSpace;

			if (m_FetchColorBuffer.boolValue && targetColorBuffer == CustomPass.TargetBuffer.Camera)
			{
				// We add a warning to prevent fetching and writing to the same render target
				EditorGUI.HelpBox(rect, Styles.writeAndFetchColorBufferWarning, MessageType.Warning);
				rect.y += Styles.defaultLineSpace;
			}

			// TODO: remove all this code when the fix for SerializedReference lands
			m_FullScreenPassMaterial.objectReferenceValue = EditorGUI.ObjectField(rect, Styles.fullScreenPassMaterial, m_FullScreenPassMaterial.objectReferenceValue, typeof(Material), false);
			// EditorGUI.PropertyField(rect, m_FullScreenPassMaterial, Styles.fullScreenPassMaterial);
			rect.y += Styles.defaultLineSpace;

			if (m_FullScreenPassMaterial.objectReferenceValue is Material mat)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUI.BeginChangeCheck();
					int index = mat.FindPass(m_MaterialPassName.stringValue);
					index = EditorGUI.IntPopup(rect, Styles.materialPassName, index, GetMaterialPassNames(mat), Enumerable.Range(0, mat.passCount).ToArray());
					rect.y += Styles.defaultLineSpace;
					if (EditorGUI.EndChangeCheck())
						m_MaterialPassName.stringValue = mat.GetPassName(index);
				}
			}
		}

		static bool ShaderHasProperty(Shader shader, string name)
		{
			for (int i = 0; i < shader.GetPropertyCount(); i++)
				if (shader.GetPropertyName(i) == name)
					return true;
			return false;
		}

		void DoStencilGUI(SerializedProperty customPass, ref Rect rect)
		{
			// TODO: remove all this code when the fix for SerializedReference lands
			m_OverrideStencil.boolValue = EditorGUI.Toggle(rect, Styles.overrideStencil, m_OverrideStencil.boolValue);
			// EditorGUI.PropertyField(rect, m_OverrideStencil, Styles.overrideStencil);
			rect.y += Styles.defaultLineSpace;

			if (m_OverrideStencil.boolValue)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					var referenceValue = targetDepthBuffer == CustomPass.TargetBuffer.Custom ? (Enum)(CustomPass.CustomStencilBits)m_StencilReferenceValue.intValue : (Enum)(UserStencilUsage)m_StencilReferenceValue.intValue;
					m_StencilReferenceValue.intValue = (int)(CustomPass.CustomStencilBits)EditorGUI.EnumFlagsField(rect, Styles.stencilReferenceValue, referenceValue);
					rect.y += Styles.defaultLineSpace;
					m_StencilCompareFunction.intValue = Convert.ToInt32(EditorGUI.EnumPopup(rect, Styles.stencilComparisonFunction, (CompareFunction)m_StencilCompareFunction.intValue));
					rect.y += Styles.defaultLineSpace;
					m_StencilPassOperation.intValue = Convert.ToInt32(EditorGUI.EnumPopup(rect, Styles.stencilPassOperation, (StencilOp)m_StencilPassOperation.intValue));
					rect.y += Styles.defaultLineSpace;
					m_StencilFailOperation.intValue = Convert.ToInt32(EditorGUI.EnumPopup(rect, Styles.stencilFailOperation, (StencilOp)m_StencilFailOperation.intValue));
					rect.y += Styles.defaultLineSpace;
				}
			}
		}

		protected override float GetPassHeight(SerializedProperty customPass)
		{
			int lineCount = (m_FullScreenPassMaterial.objectReferenceValue is Material ? 3 : 2);
			lineCount += (m_FetchColorBuffer.boolValue && targetColorBuffer == CustomPass.TargetBuffer.Camera) ? 1 : 0;
			lineCount += m_OverrideStencil.boolValue ? 5 : 1;

			return Styles.defaultLineSpace * lineCount;
		}
    }
}
