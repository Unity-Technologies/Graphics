using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for the <c>RenderObjects</c> renderer feature.
    /// </summary>
    [CustomEditor(typeof(RenderObjects), true)]
    public class ScriptableRendererFeatureEditor : Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject, "m_Script");
        }
    }
}

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(RenderObjects.RenderObjectsSettings), true)]
    internal class RenderObjectsPassFeatureEditor : PropertyDrawer
    {
        internal class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static GUIContent callback = new GUIContent("Event", "Choose at which point this render pass is executed in the frame.");

            //Headers
            public static GUIContent filtersHeader = new GUIContent("Filters", "Settings that control which objects should be rendered.");
            public static GUIContent renderHeader = new GUIContent("Overrides", "Different parts of the rendering that you can choose to override.");

            //Filters
            public static GUIContent renderQueueFilter = new GUIContent("Queue", "Only render objects in the selected render queue range.");
            public static GUIContent layerMask = new GUIContent("Layer Mask", "Only render objects in a layer that match the given layer mask.");
            public static GUIContent shaderPassFilter = new GUIContent("LightMode Tags", "Controls which shader passes to render by filtering by LightMode tag.");

            //Render Options
            public static GUIContent overrideMaterial = new GUIContent("Material", "Choose an override material, every renderer will be rendered with this material.");
            public static GUIContent overrideMaterialPass = new GUIContent("Pass Index", "The pass index for the override material to use.");
            public static GUIContent overrideShader = new GUIContent("Shader", "Choose an override shader, every renderer will be renderered with this shader and it's current material properties");
            public static GUIContent overrideShaderPass = new GUIContent("Pass Index", "The pass index for the override shader to use.");
            public static GUIContent overrideMode = new GUIContent("Override Mode", "Choose the material override mode. Material: override the material and all properties. Shader: override the shader and maintain current properties.");

            //Depth Settings
            public static GUIContent overrideDepth = new GUIContent("Depth", "Select this option to specify how this Renderer Feature affects or uses the values in the Depth buffer.");
            public static GUIContent writeDepth = new GUIContent("Write Depth", "Choose to write depth to the screen.");
            public static GUIContent depthState = new GUIContent("Depth Test", "Choose a new depth test function.");

            //Camera Settings
            public static GUIContent overrideCamera = new GUIContent("Camera", "Override camera matrices. Toggling this setting will make camera use perspective projection.");
            public static GUIContent cameraFOV = new GUIContent("Field Of View", "The camera's view angle measured in degrees along vertical axis.");
            public static GUIContent positionOffset = new GUIContent("Position Offset", "Position offset to apply to the original camera's position.");
            public static GUIContent restoreCamera = new GUIContent("Restore", "Restore to the original camera matrices after the execution of the render passes added by this feature.");
        }

        //Headers and layout
        private HeaderBool m_FiltersFoldout;
        private int m_FilterLines = 3;
        private HeaderBool m_RenderFoldout;
        private int m_MaterialLines = 2;
        private int m_DepthLines = 3;
        private int m_CameraLines = 4;

        // Serialized Properties
        private SerializedProperty m_Callback;
        private SerializedProperty m_PassTag;
        //Filter props
        private SerializedProperty m_FilterSettings;
        private SerializedProperty m_RenderQueue;
        private SerializedProperty m_LayerMask;
        private SerializedProperty m_ShaderPasses;
        //Render props
        private SerializedProperty m_OverrideMaterial;
        private SerializedProperty m_OverrideMaterialPass;
        private SerializedProperty m_OverrideShader;
        private SerializedProperty m_OverrideShaderPass;
        private SerializedProperty m_OverrideMode;
        //Depth props
        private SerializedProperty m_OverrideDepth;
        private SerializedProperty m_WriteDepth;
        private SerializedProperty m_DepthState;
        //Stencil props
        private SerializedProperty m_StencilSettings;
        //Caemra props
        private SerializedProperty m_CameraSettings;
        private SerializedProperty m_OverrideCamera;
        private SerializedProperty m_FOV;
        private SerializedProperty m_CameraOffset;
        private SerializedProperty m_RestoreCamera;

        private List<SerializedObject> m_properties = new List<SerializedObject>();

        static bool FilterRenderPassEvent(int evt) =>
            // Return all events higher or equal than before rendering prepasses
            evt >= (int)RenderPassEvent.BeforeRenderingPrePasses &&
            // filter obsolete events
            typeof(RenderPassEvent).GetField(Enum.GetName(typeof(RenderPassEvent), evt))?.GetCustomAttribute(typeof(ObsoleteAttribute)) == null;

        // Return all render pass event names that match filterRenderPassEvent
        private GUIContent[] m_EventOptionNames = Enum.GetValues(typeof(RenderPassEvent)).Cast<int>()
            .Where(FilterRenderPassEvent)
            .Select(x => new GUIContent(Enum.GetName(typeof(RenderPassEvent), x))).ToArray();

        // Return all render pass event options that match filterRenderPassEvent
        private int[] m_EventOptionValues = Enum.GetValues(typeof(RenderPassEvent)).Cast<int>()
            .Where(FilterRenderPassEvent).ToArray();

        private void Init(SerializedProperty property)
        {
            //Header bools
            var key = $"{this.ToString().Split('.').Last()}.{property.serializedObject.targetObject.name}";
            m_FiltersFoldout = new HeaderBool($"{key}.FiltersFoldout", true);
            m_RenderFoldout = new HeaderBool($"{key}.RenderFoldout");


            m_Callback = property.FindPropertyRelative("Event");
            m_PassTag = property.FindPropertyRelative("passTag");

            //Filter props
            m_FilterSettings = property.FindPropertyRelative("filterSettings");
            m_RenderQueue = m_FilterSettings.FindPropertyRelative("RenderQueueType");
            m_LayerMask = m_FilterSettings.FindPropertyRelative("LayerMask");
            m_ShaderPasses = m_FilterSettings.FindPropertyRelative("PassNames");

            //Render options
            m_OverrideMaterial = property.FindPropertyRelative("overrideMaterial");
            m_OverrideMaterialPass = property.FindPropertyRelative("overrideMaterialPassIndex");
            m_OverrideShader = property.FindPropertyRelative("overrideShader");
            m_OverrideShaderPass = property.FindPropertyRelative("overrideShaderPassIndex");
            m_OverrideMode = property.FindPropertyRelative("overrideMode");

            //Depth props
            m_OverrideDepth = property.FindPropertyRelative("overrideDepthState");
            m_WriteDepth = property.FindPropertyRelative("enableWrite");
            m_DepthState = property.FindPropertyRelative("depthCompareFunction");

            //Stencil
            m_StencilSettings = property.FindPropertyRelative("stencilSettings");

            //Camera
            m_CameraSettings = property.FindPropertyRelative("cameraSettings");
            m_OverrideCamera = m_CameraSettings.FindPropertyRelative("overrideCamera");
            m_FOV = m_CameraSettings.FindPropertyRelative("cameraFieldOfView");
            m_CameraOffset = m_CameraSettings.FindPropertyRelative("offset");
            m_RestoreCamera = m_CameraSettings.FindPropertyRelative("restoreCamera");

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

            var passName = property.serializedObject.FindProperty("m_Name").stringValue;
            if (passName != m_PassTag.stringValue)
            {
                m_PassTag.stringValue = passName;
                property.serializedObject.ApplyModifiedProperties();
            }

            //Forward Callbacks
            EditorGUI.BeginChangeCheck();
            int selectedValue = EditorGUI.IntPopup(rect, Styles.callback, m_Callback.intValue, m_EventOptionNames, m_EventOptionValues);
            if (EditorGUI.EndChangeCheck())
                m_Callback.intValue = selectedValue;
            rect.y += Styles.defaultLineSpace;

            DoFilters(ref rect);

            m_RenderFoldout.value = EditorGUI.Foldout(rect, m_RenderFoldout.value, Styles.renderHeader, true);
            SaveHeaderBool(m_RenderFoldout);
            rect.y += Styles.defaultLineSpace;
            if (m_RenderFoldout.value)
            {
                EditorGUI.indentLevel++;
                //Override material
                DoMaterialOverride(ref rect);
                rect.y += Styles.defaultLineSpace;
                //Override depth
                DoDepthOverride(ref rect);
                rect.y += Styles.defaultLineSpace;
                //Override stencil
                EditorGUI.PropertyField(rect, m_StencilSettings);
                rect.y += EditorGUI.GetPropertyHeight(m_StencilSettings);
                //Override camera
                DoCameraOverride(ref rect);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }

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
                EditorGUI.PropertyField(rect, m_ShaderPasses, Styles.shaderPassFilter, true);
                rect.y += EditorGUI.GetPropertyHeight(m_ShaderPasses);
                EditorGUI.indentLevel--;
            }
        }

        void DoMaterialOverride(ref Rect rect)
        {
            EditorGUI.PropertyField(rect, m_OverrideMode, Styles.overrideMode);
            EditorGUI.indentLevel++;

            switch (m_OverrideMode.intValue)
            {
                case (int)RenderObjects.RenderObjectsSettings.OverrideMaterialMode.None:
                    m_MaterialLines = 1;
                    break;

                case (int)RenderObjects.RenderObjectsSettings.OverrideMaterialMode.Material:
                    m_MaterialLines = 3;

                    rect.y += Styles.defaultLineSpace;
                    EditorGUI.PropertyField(rect, m_OverrideMaterial, Styles.overrideMaterial);
                    rect.y += Styles.defaultLineSpace;

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(rect, m_OverrideMaterialPass, Styles.overrideMaterialPass);
                    if (EditorGUI.EndChangeCheck())
                        m_OverrideMaterialPass.intValue = Mathf.Max(0, m_OverrideMaterialPass.intValue);
                    break;

                case (int)RenderObjects.RenderObjectsSettings.OverrideMaterialMode.Shader:
                    m_MaterialLines = 3;

                    rect.y += Styles.defaultLineSpace;
                    EditorGUI.PropertyField(rect, m_OverrideShader, Styles.overrideShader);
                    rect.y += Styles.defaultLineSpace;
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(rect, m_OverrideShaderPass, Styles.overrideShaderPass);
                    if (EditorGUI.EndChangeCheck())
                        m_OverrideShaderPass.intValue = Mathf.Max(0, m_OverrideShaderPass.intValue);
                    break;
            }

            EditorGUI.indentLevel--;
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
                if (EditorGUI.EndChangeCheck())
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

            Init(property);
            height += Styles.defaultLineSpace * (m_FiltersFoldout.value ? m_FilterLines : 1);
            height += m_FiltersFoldout.value ? EditorGUI.GetPropertyHeight(m_ShaderPasses) : 0;

            height += Styles.defaultLineSpace; // add line for overrides dropdown
            if (m_RenderFoldout.value)
            {
                height += Styles.defaultLineSpace * m_MaterialLines;
                height += Styles.defaultLineSpace * (m_OverrideDepth.boolValue ? m_DepthLines : 1);
                height += EditorGUI.GetPropertyHeight(m_StencilSettings);
                height += Styles.defaultLineSpace * (m_OverrideCamera.boolValue ? m_CameraLines : 1);
            }

            return height;
        }

        private void SaveHeaderBool(HeaderBool boolObj)
        {
            EditorPrefs.SetBool(boolObj.key, boolObj.value);
        }

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
