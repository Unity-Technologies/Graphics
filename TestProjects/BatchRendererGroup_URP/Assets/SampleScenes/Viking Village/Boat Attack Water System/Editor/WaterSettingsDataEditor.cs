using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace WaterSystem.Data
{
    [CustomEditor(typeof(WaterSettingsData))]
    public class WaterSettingsDataEditor : Editor
    {
		public override void OnInspectorGUI()
        {
			var geomType = serializedObject.FindProperty("waterGeomType");
			EditorGUILayout.PropertyField(geomType);

			var depthMapCullingMask = serializedObject.FindProperty("depthMapCullingMask");
			EditorGUILayout.PropertyField(depthMapCullingMask);

			var refType = serializedObject.FindProperty("refType");
			refType.enumValueIndex = GUILayout.Toolbar(refType.enumValueIndex, refType.enumDisplayNames);
			
			switch(refType.enumValueIndex)
			{
				case 0:
				{
					// cubemap
					var cube = serializedObject.FindProperty("cubemapRefType");
					EditorGUILayout.PropertyField(cube, new GUIContent("Cubemap Texture"));
				}
				break;
				case 1:
				{
					// probe
					EditorGUILayout.HelpBox("Reflection Probe setting has no options, it automatically uses the nearest reflection probe to the main camera", MessageType.Info);
				}
				break;
				case 2:
				{
					// planar
					var planarSettings = serializedObject.FindProperty("planarSettings");
					EditorGUILayout.PropertyField(planarSettings, true);
				}
				break;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
