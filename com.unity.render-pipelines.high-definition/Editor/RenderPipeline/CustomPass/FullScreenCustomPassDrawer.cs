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
    public class FullScreenCustomPassDrawer : CustomPassDrawer
    {
	    private class Styles
	    {
		    public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            public static GUIContent fullScreenPassMaterial = new GUIContent("FullScreen Material", "FullScreen Material used for the full screen DrawProcedural.");
            public static GUIContent materialPassIndex = new GUIContent("Pass Name", "The shader pass to use for your fullscreen pass.");
			public static GUIContent fetchColorBuffer = new GUIContent("Fetch Color Buffer", "Tick this if your effect sample/fetch the camera color buffer");

			public readonly static string writeAndFetchColorBufferWarning = "Fetching and Writing to the camera color buffer at the same time is not supported on most platforms.";
	    }

		// Fullscreen pass
		SerializedProperty		m_FullScreenPassMaterial;
		SerializedProperty		m_MaterialPassIndex;
		SerializedProperty      m_FetchColorBuffer;
		SerializedProperty      m_TargetColorBuffer;

		CustomPass.TargetBuffer	targetColorBuffer => (CustomPass.TargetBuffer)m_TargetColorBuffer.intValue;

	    protected override void Initialize(SerializedProperty customPass)
	    {
			m_FullScreenPassMaterial = customPass.FindPropertyRelative("fullscreenPassMaterial");
			m_MaterialPassIndex = customPass.FindPropertyRelative("materialPassIndex");
			m_FetchColorBuffer = customPass.FindPropertyRelative("fetchColorBuffer");
			m_TargetColorBuffer = customPass.FindPropertyRelative("targetColorBuffer");
	    }

        GUIContent[] GetMaterialPassNames(Material mat)
        {
            GUIContent[] passNames = new GUIContent[mat.passCount];

            for (int i = 0; i < mat.passCount; i++)
            {
                string passName = mat.GetPassName(i);
                passNames[i] = new GUIContent(string.IsNullOrEmpty(passName) ? i.ToString() : passName);
            }
            
            return passNames;
        }

		protected override void DoPassGUI(SerializedProperty customPass, Rect rect)
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
					m_MaterialPassIndex.intValue = EditorGUI.IntPopup(rect, Styles.materialPassIndex, m_MaterialPassIndex.intValue, GetMaterialPassNames(mat), Enumerable.Range(0, mat.passCount).ToArray());
					// EditorGUI.PropertyField(rect, m_MaterialPassIndex, Styles.materialPassIndex);
				}
			}
        }

		protected override float GetPassHeight(SerializedProperty customPass)
		{
			int lineCount = (m_FullScreenPassMaterial.objectReferenceValue is Material ? 3 : 2);
			lineCount += (m_FetchColorBuffer.boolValue && targetColorBuffer == CustomPass.TargetBuffer.Camera) ? 1 : 0;

			return Styles.defaultLineSpace * lineCount;
		}
    }
}