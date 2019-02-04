using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEngine.Experimental.Rendering.LWRP 
{
	[CustomEditor(typeof(RenderObjectsPassFeature))]
    public class RenderObjectsPassFeatureEditor : Editor
    {
	    internal class Styles
	    {
		    public static GUIContent callback = new GUIContent("Callback", "Chose the Callback position for this render pass object.");
		    public static GUIContent renderQueueFilter = new GUIContent("Queue Filter", "Filter the render queue range you want to render.");
		    public static GUIContent layerMask = new GUIContent("Layer Mask", "Chose the Callback position for this render pass object.");
		    public static GUIContent shaderPassFilter = new GUIContent("Shader Passes", "Chose the Callback position for this render pass object.");
		}

	    private SerializedProperty m_callback;
	    private SerializedProperty m_renderQueue;
	    private SerializedProperty m_layerMask;
	    private SerializedProperty m_shaderPasses;

	    private ReorderableList m_shaderPassesList;

	    private void OnEnable()
	    {
		    m_callback = serializedObject.FindProperty("callback");
		    m_renderQueue = serializedObject.FindProperty("renderQueueType");
		    m_layerMask = serializedObject.FindProperty("layerMask");
		    m_shaderPasses = serializedObject.FindProperty("passNames");
		    
		    m_shaderPassesList = new ReorderableList(serializedObject, m_shaderPasses, true, true, true, true);

		    m_shaderPassesList.drawElementCallback =
		    (Rect rect, int index, bool isActive, bool isFocused) =>
		    {
			    var element = m_shaderPassesList.serializedProperty.GetArrayElementAtIndex(index);
			    var newRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
			    var labelWidth = EditorGUIUtility.labelWidth;
			    EditorGUIUtility.labelWidth = 50;
			    element.stringValue = EditorGUI.TextField(newRect, "Name", element.stringValue);
			    EditorGUIUtility.labelWidth = labelWidth;
		    };
		    
		    m_shaderPassesList.drawHeaderCallback = (Rect testHeaderRect) => {
			    EditorGUI.LabelField(testHeaderRect, "Shader Passes");
		    };
	    }

	    public override void OnInspectorGUI()
	    {
		    DrawDefaultInspector();
		    
		    // TODO:remove later
		    EditorGUILayout.Space(); 
		    EditorGUILayout.HelpBox("Testing Inspector Below", MessageType.Warning);
		    EditorGUILayout.Space();
			// End TODO
			
			serializedObject.Update();
			
		    EditorGUILayout.PropertyField(m_callback);
		    EditorGUILayout.PropertyField(m_renderQueue);
		    EditorGUILayout.PropertyField(m_layerMask);
		    
		    m_shaderPassesList.DoLayoutList();

		    serializedObject.ApplyModifiedProperties();
	    }
    }
}
