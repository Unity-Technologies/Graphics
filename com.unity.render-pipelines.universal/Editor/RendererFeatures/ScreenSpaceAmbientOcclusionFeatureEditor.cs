using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Experimental.Rendering.Universal;
using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.Universal
{
	[CustomPropertyDrawer(typeof(ScreenSpaceAmbientOcclusionFeature.Settings), true)]
    internal class ScreenSpaceAmbientOcclusionFeatureEditor : PropertyDrawer
    {
	    internal class Styles
	    {
		    public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            public static GUIContent depthSource = new GUIContent("Depth Source", "");
			public static GUIContent intensity = new GUIContent("Intensity", "");
			public static GUIContent radius = new GUIContent("Radius", "");
			public static GUIContent sampleCount = new GUIContent("Sample Count", "");
			public static GUIContent downScale = new GUIContent("Downscale", "");
		}

		// Serialized Properties
		private SerializedProperty m_DepthSource;
		private SerializedProperty m_Intensity;
	    private SerializedProperty m_Radius;
	    private SerializedProperty m_DownScale;
	    private SerializedProperty m_SampleCount;

	    private List<SerializedObject> m_properties = new List<SerializedObject>();

        private void Init(SerializedProperty property)
        {
            m_DepthSource = property.FindPropertyRelative("depthSource");
            m_Intensity = property.FindPropertyRelative("intensity");
            m_Radius = property.FindPropertyRelative("radius");
            m_DownScale = property.FindPropertyRelative("downScale");
            m_SampleCount = property.FindPropertyRelative("sampleCount");
            m_properties.Add(property.serializedObject);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
	    {
            rect.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.BeginChangeCheck();
			EditorGUI.BeginProperty(rect, label, property);
            

            if (!m_properties.Contains(property.serializedObject))
            {
                Init(property);
            }

			EditorGUI.PropertyField(rect, m_DepthSource, Styles.depthSource);
			rect.y += Styles.defaultLineSpace;

			EditorGUI.Slider(rect, m_Intensity, 0f, 4f, Styles.intensity);
			rect.y += Styles.defaultLineSpace;

			EditorGUI.Slider(rect, m_Radius, 0f, 1f, Styles.radius);
			rect.y += Styles.defaultLineSpace;

			EditorGUI.PropertyField(rect, m_DownScale, Styles.downScale);
			rect.y += Styles.defaultLineSpace;

			EditorGUI.IntSlider(rect, m_SampleCount, 0, 32, Styles.sampleCount);
			rect.y += Styles.defaultLineSpace;

			EditorGUI.EndProperty();
			if (EditorGUI.EndChangeCheck())
			{
				property.serializedObject.ApplyModifiedProperties();
			}
	    }

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
			return Styles.defaultLineSpace * 6;
		}

		/*
	    void DoFilters(ref Rect rect)
	    {
		    m_FiltersFoldout.value = EditorGUI.Foldout(rect, m_FiltersFoldout.value, Styles.filtersHeader, true);
		    SaveHeaderBool(m_FiltersFoldout);
		    rect.y += Styles.defaultLineSpace;
		    if (m_FiltersFoldout.value)
		    {
			    EditorGUI.indentLevel++;
			    //Render queue filter
			    EditorGUI.PropertyField(rect, m_RenderQueue, Styles.renderQueueFilter);
			    rect.y += Styles.defaultLineSpace;
			    //Layer mask
			    EditorGUI.PropertyField(rect, m_LayerMask, Styles.layerMask);
			    rect.y += Styles.defaultLineSpace;
			    //Shader pass list
			    EditorGUI.indentLevel--;
			    m_ShaderPassesList.DoList(rect);
			    rect.y += m_ShaderPassesList.GetHeight();
		    }
	    }

	    void DoMaterialOverride(ref Rect rect)
	    {
		    //Override material
		    EditorGUI.PropertyField(rect, m_OverrideMaterial, Styles.overrideMaterial);
		    if (m_OverrideMaterial.objectReferenceValue)
		    {
			    rect.y += Styles.defaultLineSpace;
			    EditorGUI.indentLevel++;
			    EditorGUI.BeginChangeCheck();
			    EditorGUI.PropertyField(rect, m_OverrideMaterialPass, Styles.overrideMaterialPass);
			    if (EditorGUI.EndChangeCheck())
				    m_OverrideMaterialPass.intValue = Mathf.Max(0, m_OverrideMaterialPass.intValue);
			    EditorGUI.indentLevel--;
		    }
	    }

	    void DoDepthOverride(ref Rect rect)
	    {
		    EditorGUI.PropertyField(rect, m_OverrideDepth, Styles.overrideDepth);
		    if (m_OverrideDepth.boolValue)
		    {
			    rect.y += Styles.defaultLineSpace;
			    EditorGUI.indentLevel++;
			    //Write depth
			    EditorGUI.PropertyField(rect, m_WriteDepth, Styles.writeDepth);
			    rect.y += Styles.defaultLineSpace;
			    //Depth testing options
			    EditorGUI.PropertyField(rect, m_DepthState, Styles.depthState);
			    EditorGUI.indentLevel--;
		    }
	    }

	    void DoCameraOverride(ref Rect rect)
	    {
		    EditorGUI.PropertyField(rect, m_OverrideCamera, Styles.overrideCamera);
		    if (m_OverrideCamera.boolValue)
		    {
			    rect.y += Styles.defaultLineSpace;
			    EditorGUI.indentLevel++;
			    //FOV
			    EditorGUI.Slider(rect, m_FOV, 4f, 179f, Styles.cameraFOV);
			    rect.y += Styles.defaultLineSpace;
			    //Offset vector
			    var offset = m_CameraOffset.vector4Value;
			    EditorGUI.BeginChangeCheck();
			    var newOffset = EditorGUI.Vector3Field(rect, Styles.positionOffset, new Vector3(offset.x, offset.y, offset.z));
			    if(EditorGUI.EndChangeCheck())
					m_CameraOffset.vector4Value = new Vector4(newOffset.x, newOffset.y, newOffset.z, 1f);
			    rect.y += Styles.defaultLineSpace;
			    //Restore prev camera projections
			    EditorGUI.PropertyField(rect, m_RestoreCamera, Styles.restoreCamera);
			    rect.y += Styles.defaultLineSpace;

			    EditorGUI.indentLevel--;
		    }
	    }

	    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	    {
		    float height = Styles.defaultLineSpace;

            if (m_properties.Contains(property.serializedObject))
            {
                height += Styles.defaultLineSpace * (m_FiltersFoldout.value ? m_FilterLines : 1);
                height += m_FiltersFoldout.value ? m_ShaderPassesList.GetHeight() : 0;

                height += Styles.defaultLineSpace; // add line for overrides dropdown
                if (m_RenderFoldout.value)
                {
                    height += Styles.defaultLineSpace * (m_OverrideMaterial.objectReferenceValue != null ? m_MaterialLines : 1);
                    height += Styles.defaultLineSpace * (m_OverrideDepth.boolValue ? m_DepthLines : 1);
                    height += EditorGUI.GetPropertyHeight(m_OverrideStencil);
                    height += Styles.defaultLineSpace * (m_OverrideCamera.boolValue ? m_CameraLines : 1);
                }
            }
            return height;
	    }

	    private void SaveHeaderBool(HeaderBool boolObj)
	    {
		    EditorPrefs.SetBool(boolObj.key, boolObj.value);
	    }
        */
		class HeaderBool
	    {
		    public string key;
		    public bool value;

		    public HeaderBool(string _key, bool _default = false)
		    {
			    key = _key;
			    if (EditorPrefs.HasKey(key))
				    value = EditorPrefs.GetBool(key);
			    else
					value = _default;
			    EditorPrefs.SetBool(key, value);
		    }
	    }
    }
}
