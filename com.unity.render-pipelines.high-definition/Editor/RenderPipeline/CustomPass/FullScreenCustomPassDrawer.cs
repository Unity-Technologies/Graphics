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
	    }

		// Fullscreen pass
		SerializedProperty		m_FullScreenPassMaterial;

	    protected override void Initialize(SerializedProperty customPass)
	    {
			m_FullScreenPassMaterial = customPass.FindPropertyRelative("fullscreenPassMaterial");
	    }

		protected override void DoPassGUI(SerializedProperty customPass, Rect rect)
        {
			// TODO: remove all this code when the fix for SerializedReference lands
			m_FullScreenPassMaterial.objectReferenceValue = EditorGUI.ObjectField(rect, Styles.fullScreenPassMaterial, m_FullScreenPassMaterial.objectReferenceValue, typeof(Material), false);
			// EditorGUI.PropertyField(rect, m_FullScreenPassMaterial, Styles.fullScreenPassMaterial);
        }

		protected override float GetPassHeight(SerializedProperty customPass) => Styles.defaultLineSpace;
    }
}