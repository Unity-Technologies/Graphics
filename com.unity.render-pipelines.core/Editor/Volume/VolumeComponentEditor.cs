using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class VolumeComponentEditorAttribute : Attribute
    {
        public readonly Type componentType;

        public VolumeComponentEditorAttribute(Type componentType)
        {
            this.componentType = componentType;
        }
    }

    public class VolumeComponentEditor
    {
        public VolumeComponent target { get; private set; }
        public SerializedObject serializedObject { get; private set; }

        public SerializedProperty baseProperty { get; internal set; }
        public SerializedProperty activeProperty { get; internal set; }

        SerializedProperty m_AdvancedMode;

        public virtual bool hasAdvancedMode => false;

        public bool isInAdvancedMode => m_AdvancedMode != null && m_AdvancedMode.boolValue;

        protected Editor m_Inspector;
        List<SerializedDataParameter> m_Parameters;

        static Dictionary<Type, VolumeParameterDrawer> s_ParameterDrawers;

        static VolumeComponentEditor()
        {
            s_ParameterDrawers = new Dictionary<Type, VolumeParameterDrawer>();
            ReloadDecoratorTypes();
        }

        [Callbacks.DidReloadScripts]
        static void OnEditorReload()
        {
            ReloadDecoratorTypes();
        }

        static void ReloadDecoratorTypes()
        {
            s_ParameterDrawers.Clear();

            // Look for all the valid parameter drawers
            var types = CoreUtils.GetAllTypesDerivedFrom<VolumeParameterDrawer>()
                .Where(
                    t => t.IsDefined(typeof(VolumeParameterDrawerAttribute), false)
                    && !t.IsAbstract
                    );

            // Store them
            foreach (var type in types)
            {
                var attr = (VolumeParameterDrawerAttribute)type.GetCustomAttributes(typeof(VolumeParameterDrawerAttribute), false)[0];
                var decorator = (VolumeParameterDrawer)Activator.CreateInstance(type);
                s_ParameterDrawers.Add(attr.parameterType, decorator);
            }
        }

        public void Repaint()
        {
            m_Inspector.Repaint();
        }

        internal void Init(VolumeComponent target, Editor inspector)
        {
            this.target = target;
            m_Inspector = inspector;
            serializedObject = new SerializedObject(target);
            activeProperty = serializedObject.FindProperty("active");
            m_AdvancedMode = serializedObject.FindProperty("m_AdvancedMode");
            OnEnable();
        }

        public virtual void OnEnable()
        {
            m_Parameters = new List<SerializedDataParameter>();

            // Grab all valid serializable field on the VolumeComponent
            // TODO: Should only be done when needed / on demand as this can potentially be wasted CPU when a custom editor is in use
            var fields = target.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t => t.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                .Where(t =>
                    (t.IsPublic && t.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0) ||
                    (t.GetCustomAttributes(typeof(SerializeField), false).Length > 0)
                    )
                .Where(t => t.GetCustomAttributes(typeof(HideInInspector), false).Length == 0)
                .ToList();

            // Prepare all serialized objects for this editor
            foreach (var field in fields)
            {
                var property = serializedObject.FindProperty(field.Name);
                var parameter = new SerializedDataParameter(property);
                m_Parameters.Add(parameter);
            }
        }

        public virtual void OnDisable()
        {
        }

        internal void OnInternalInspectorGUI()
        {
            serializedObject.Update();
            TopRowFields();
            OnInspectorGUI();
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        public virtual void OnInspectorGUI()
        {
            // Display every field as-is
            foreach (var parameter in m_Parameters)
                PropertyField(parameter);
        }

        public virtual string GetDisplayTitle()
        {
            return target.displayName == "" ? ObjectNames.NicifyVariableName(target.GetType().Name) : target.displayName;
        }

        void TopRowFields()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("All", "Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need."), CoreEditorStyles.miniLabelButton, GUILayout.Width(17f), GUILayout.ExpandWidth(false)))
                    SetAllOverridesTo(true);

                if (GUILayout.Button(EditorGUIUtility.TrTextContent("None", "Toggle all overrides off."), CoreEditorStyles.miniLabelButton, GUILayout.Width(32f), GUILayout.ExpandWidth(false)))
                    SetAllOverridesTo(false);

                GUILayout.FlexibleSpace();

                // TODO: Rework the UI to match the 'advanced mode' in regular HDRP components
                if (hasAdvancedMode)
                    m_AdvancedMode.boolValue = GUILayout.Toggle(m_AdvancedMode.boolValue, "Advanced", EditorStyles.miniButton, GUILayout.ExpandWidth(false), GUILayout.Width(70f));
            }
        }

        internal void SetAllOverridesTo(bool state)
        {
            Undo.RecordObject(target, "Toggle All");
            target.SetAllOverridesTo(state);
            serializedObject.Update();
        }

        // Takes a serialized VolumeParameter<T> as input
        protected SerializedDataParameter Unpack(SerializedProperty property)
        {
            Assert.IsNotNull(property);
            return new SerializedDataParameter(property);
        }

        protected void PropertyField(SerializedDataParameter property)
        {
            var title = EditorGUIUtility.TrTextContent(property.displayName);
            PropertyField(property, title);
        }

        protected void PropertyField(SerializedDataParameter property, GUIContent title)
        {
            // Handle unity built-in decorators (Space, Header, Tooltip etc)
            foreach (var attr in property.attributes)
            {
                if (attr is PropertyAttribute)
                {
                    if (attr is SpaceAttribute)
                    {
                        EditorGUILayout.GetControlRect(false, (attr as SpaceAttribute).height);
                    }
                    else if (attr is HeaderAttribute)
                    {
                        var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                        rect.y += 0f;
                        rect = EditorGUI.IndentedRect(rect);
                        EditorGUI.LabelField(rect, (attr as HeaderAttribute).header, EditorStyles.miniLabel);
                    }
                    else if (attr is TooltipAttribute)
                    {
                        if (string.IsNullOrEmpty(title.tooltip))
                            title.tooltip = (attr as TooltipAttribute).tooltip;
                    }
                }
            }

            // Custom parameter drawer
            VolumeParameterDrawer drawer;
            s_ParameterDrawers.TryGetValue(property.referenceType, out drawer);

            bool invalidProp = false;

            if (drawer != null && !drawer.IsAutoProperty())
            {
                if (drawer.OnGUI(property, title))
                    return;

                invalidProp = true;
            }

            // ObjectParameter<T> is a special case
            if (VolumeParameter.IsObjectParameter(property.referenceType))
            {
                bool expanded = property.value.isExpanded;
                expanded = EditorGUILayout.Foldout(expanded, title, true);

                if (expanded)
                {
                    EditorGUI.indentLevel++;

                    // Not the fastest way to do it but that'll do just fine for now
                    var it = property.value.Copy();
                    var end = it.GetEndProperty();
                    bool first = true;

                    while (it.Next(first) && !SerializedProperty.EqualContents(it, end))
                    {
                        PropertyField(Unpack(it));
                        first = false;
                    }

                    EditorGUI.indentLevel--;
                }

                property.value.isExpanded = expanded;
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                // Override checkbox
                DrawOverrideCheckbox(property);

                // Property
                using (new EditorGUI.DisabledScope(!property.overrideState.boolValue))
                {
                    if (drawer != null && !invalidProp)
                    {
                        if (drawer.OnGUI(property, title))
                            return;
                    }

                    // Default unity field
                    EditorGUILayout.PropertyField(property.value, title);
                }
            }
        }

        protected void DrawOverrideCheckbox(SerializedDataParameter property)
        {
            var overrideRect = GUILayoutUtility.GetRect(17f, 17f, GUILayout.ExpandWidth(false));
            overrideRect.yMin += 4f;
            property.overrideState.boolValue = GUI.Toggle(overrideRect, property.overrideState.boolValue, EditorGUIUtility.TrTextContent("", "Override this setting for this volume."), CoreEditorStyles.smallTickbox);
        }
    }
}
