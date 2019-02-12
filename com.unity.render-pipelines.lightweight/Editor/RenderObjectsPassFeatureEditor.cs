using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Rendering.LWRP 
{
	[CustomPropertyDrawer(typeof(RenderObjectsPassFeature.RenderObjectsSettings), true)]
    public class RenderObjectsPassFeatureEditor : PropertyDrawer
    {
	    internal class Styles
	    {
		    public static GUIContent callback = new GUIContent("Callback", "Chose the Callback position for this render pass object.");
		    public static GUIContent filtersHeader = new GUIContent("Filters", "Filters.");
		    public static GUIContent renderQueueFilter = new GUIContent("Queue", "Filter the render queue range you want to render.");
		    public static GUIContent layerMask = new GUIContent("Layer Mask", "Chose the Callback position for this render pass object.");
		    public static GUIContent shaderPassFilter = new GUIContent("Shader Passes", "Chose the Callback position for this render pass object.");
		}

	    //SavedBool m_FiltersFoldout;
	    private bool firstTime = true;
	    
	    private SerializedProperty m_Callback;
	    private SerializedProperty m_RenderQueue;
	    private SerializedProperty m_LayerMask;
	    private SerializedProperty m_ShaderPasses;

	    private ReorderableList m_shaderPassesList;

	    private void Init(SerializedProperty property)
	    {
		    //m_FiltersFoldout = new SavedBool($"{target.GetType()}.FiltersFoldout", true);
		    
		    m_Callback = property.FindPropertyRelative("callback");
		    m_RenderQueue = property.FindPropertyRelative("renderQueueType");
		    m_LayerMask = property.FindPropertyRelative("layerMask");
		    m_ShaderPasses = property.FindPropertyRelative("passNames");
		    
		    m_shaderPassesList = new ReorderableList(null, m_ShaderPasses, true, true, true, true);

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
			    EditorGUI.LabelField(testHeaderRect, Styles.shaderPassFilter);
		    };
	    }

	    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
	    {
			rect.xMin -= EditorStyles.inspectorDefaultMargins.padding.left;
		    
		    if(firstTime)
			    Init(property);
		    EditorGUI.BeginProperty(rect, label, property);

		    //EditorGUIUtility.labelWidth = 80;
		    rect.height = EditorGUIUtility.singleLineHeight;
		    EditorGUI.PropertyField(rect, m_Callback, Styles.callback);
		    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		    
		    //m_FiltersFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_FiltersFoldout.value, Styles.filtersHeader);
		    //if (m_FiltersFoldout.value)
		    {
			    EditorGUI.PropertyField(rect, m_RenderQueue, Styles.renderQueueFilter);
			    
			    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			    EditorGUI.PropertyField(rect, m_LayerMask, Styles.layerMask);

			    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			    //rect.height = 10f;
			    m_shaderPassesList.DoList(rect);
		    }
		    //EditorGUILayout.EndFoldoutHeaderGroup();

		    property.serializedObject.ApplyModifiedProperties();
		    EditorGUI.EndProperty();
		    firstTime = false;
	    }

	    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	    {
		    var shaderPasses = property.FindPropertyRelative("passNames");
		    float shaderPassListHeight = EditorGUIUtility.standardVerticalSpacing;
		    if(m_shaderPassesList != null)
				shaderPassListHeight += m_shaderPassesList.GetHeight();
		    int lineCount = 4;
		    return ((EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * lineCount) + shaderPassListHeight;
	    }
    }
}
