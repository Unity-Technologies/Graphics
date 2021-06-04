using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// This attributes tells a <see cref="VolumeComponentEditor"/> class which type of
    /// <see cref="VolumeComponent"/> it's an editor for.
    /// When you make a custom editor for a component, you need put this attribute on the editor
    /// class.
    /// </summary>
    /// <seealso cref="VolumeComponentEditor"/>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class VolumeComponentEditorAttribute : Attribute
    {
        /// <summary>
        /// A type derived from <see cref="VolumeComponent"/>.
        /// </summary>
        public readonly Type componentType;

        /// <summary>
        /// Creates a new <see cref="VolumeComponentEditorAttribute"/> instance.
        /// </summary>
        /// <param name="componentType">A type derived from <see cref="VolumeComponent"/></param>
        public VolumeComponentEditorAttribute(Type componentType)
        {
            this.componentType = componentType;
        }
    }

    /// <summary>
    /// A custom editor class that draws a <see cref="VolumeComponent"/> in the Inspector. If you do not
    /// provide a custom editor for a <see cref="VolumeComponent"/>, Unity uses the default one.
    /// You must use a <see cref="VolumeComponentEditorAttribute"/> to let the editor know which
    /// component this drawer is for.
    /// </summary>
    /// <example>
    /// Below is an example of a custom <see cref="VolumeComponent"/>:
    /// <code>
    /// using UnityEngine.Rendering;
    ///
    /// [Serializable, VolumeComponentMenu("Custom/Example Component")]
    /// public class ExampleComponent : VolumeComponent
    /// {
    ///     public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    /// }
    /// </code>
    /// And its associated editor:
    /// <code>
    /// using UnityEditor.Rendering;
    ///
    /// [VolumeComponentEditor(typeof(ExampleComponent))]
    /// class ExampleComponentEditor : VolumeComponentEditor
    /// {
    ///     SerializedDataParameter m_Intensity;
    ///
    ///     public override void OnEnable()
    ///     {
    ///         var o = new PropertyFetcher&lt;ExampleComponent&gt;(serializedObject);
    ///         m_Intensity = Unpack(o.Find(x => x.intensity));
    ///     }
    ///
    ///     public override void OnInspectorGUI()
    ///     {
    ///         PropertyField(m_Intensity);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="VolumeComponentEditorAttribute"/>
    public class VolumeComponentEditor
    {
        /// <summary>
        /// Specifies the <see cref="VolumeComponent"/> this editor is drawing.
        /// </summary>
        public VolumeComponent target { get; private set; }

        /// <summary>
        /// A <c>SerializedObject</c> representing the object being inspected.
        /// </summary>
        public SerializedObject serializedObject { get; private set; }

        /// <summary>
        /// The copy of the serialized property of the <see cref="VolumeComponent"/> being
        /// inspected. Unity uses this to track whether the editor is collapsed in the Inspector or not.
        /// </summary>
        public SerializedProperty baseProperty { get; internal set; }

        /// <summary>
        /// The serialized property of <see cref="VolumeComponent.active"/> for the component being
        /// inspected.
        /// </summary>
        public SerializedProperty activeProperty { get; internal set; }

        SerializedProperty m_AdvancedMode;

        /// <summary>
        /// Override this property if your editor makes use of the "More Options" feature.
        /// </summary>
        public virtual bool hasAdvancedMode => false;

        /// <summary>
        /// Checks if the editor currently has the "More Options" feature toggled on.
        /// </summary>
        public bool isInAdvancedMode
        {
            get => m_AdvancedMode != null && m_AdvancedMode.boolValue;
            internal set
            {
                if (m_AdvancedMode != null)
                {
                    m_AdvancedMode.boolValue = value;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>
        /// A reference to the parent editor in the Inspector.
        /// </summary>
        protected Editor m_Inspector;

        List<(GUIContent displayName, int displayOrder, SerializedDataParameter param)> m_Parameters;

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

        /// <summary>
        /// Triggers an Inspector repaint event.
        /// </summary>
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

        void GetFields(object o, List<(FieldInfo, SerializedProperty)> infos, SerializedProperty prop = null)
        {
            if (o == null)
                return;

            var fields = o.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                {
                    if ((field.GetCustomAttributes(typeof(HideInInspector), false).Length == 0) &&
                        ((field.GetCustomAttributes(typeof(SerializeField), false).Length > 0) ||
                         (field.IsPublic && field.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0)))
                        infos.Add((field, prop == null ?
                            serializedObject.FindProperty(field.Name) : prop.FindPropertyRelative(field.Name)));
                }
                else if (!field.FieldType.IsArray && field.FieldType.IsClass)
                    GetFields(field.GetValue(o), infos, prop == null ?
                        serializedObject.FindProperty(field.Name) : prop.FindPropertyRelative(field.Name));
            }
        }

        /// <summary>
        /// Unity calls this method when the object loads.
        /// </summary>
        /// <remarks>
        /// You can safely override this method and not call <c>base.OnEnable()</c> unless you want
        /// Unity to display all the properties from the <see cref="VolumeComponent"/> automatically.
        /// </remarks>
        public virtual void OnEnable()
        {
            // Grab all valid serializable field on the VolumeComponent
            // TODO: Should only be done when needed / on demand as this can potentially be wasted CPU when a custom editor is in use
            var fields = new List<(FieldInfo, SerializedProperty)>();
            GetFields(target, fields);

            m_Parameters = fields
                .Select(t => {
                    var name = "";
                    var order = 0;
                    var attr = (DisplayInfoAttribute[])t.Item1.GetCustomAttributes(typeof(DisplayInfoAttribute), true);
                    if (attr.Length != 0)
                    {
                        name = attr[0].name;
                        order = attr[0].order;
                    }

                    var parameter = new SerializedDataParameter(t.Item2);
                    return (new GUIContent(name), order, parameter);
                })
                .OrderBy(t => t.order)
                .ToList();
        }

        /// <summary>
        /// Unity calls this method when the object goes out of scope.
        /// </summary>
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

        /// <summary>
        /// Unity calls this method everytime it re-draws the Inspector.
        /// </summary>
        /// <remarks>
        /// You can safely override this method and not call <c>base.OnInspectorGUI()</c> unless you
        /// want Unity to display all the properties from the <see cref="VolumeComponent"/>
        /// automatically.
        /// </remarks>
        public virtual void OnInspectorGUI()
        {
            // Display every field as-is
            foreach (var parameter in m_Parameters)
            {
                if (!string.IsNullOrEmpty(parameter.displayName.text))
                    PropertyField(parameter.param, parameter.displayName);
                else
                    PropertyField(parameter.param);
            }
        }

        /// <summary>
        /// Sets the label for the component header. Override this method to provide
        /// a custom label. If you don't, Unity automatically inferres one from the class name.
        /// </summary>
        /// <returns>A label to display in the component header.</returns>
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
            }
        }

        internal void SetAllOverridesTo(bool state)
        {
            Undo.RecordObject(target, "Toggle All");
            target.SetAllOverridesTo(state);
            serializedObject.Update();
        }

        /// <summary>
        /// Generates and auto-populates a <see cref="SerializedDataParameter"/> from a serialized
        /// <see cref="VolumeParameter{T}"/>.
        /// </summary>
        /// <param name="property">A serialized property holding a <see cref="VolumeParameter{T}"/>
        /// </param>
        /// <returns></returns>
        protected SerializedDataParameter Unpack(SerializedProperty property)
        {
            Assert.IsNotNull(property);
            return new SerializedDataParameter(property);
        }

        /// <summary>
        /// Draws a given <see cref="SerializedDataParameter"/> in the editor.
        /// </summary>
        /// <param name="property">The property to draw in the editor</param>
        protected void PropertyField(SerializedDataParameter property)
        {
            var title = EditorGUIUtility.TrTextContent(property.displayName, property.GetAttribute<TooltipAttribute>()?.tooltip);
            PropertyField(property, title);
        }

        /// <summary>
        /// Handles unity built-in decorators (Space, Header, Tooltips, ...) from <see cref="SerializedDataParameter"/> attributes
        /// </summary>
        /// <param name="property">The property to obtain the attributes and handle the decorators</param>
        /// <param name="title">A custom label and/or tooltip that might be updated by <see cref="TooltipAttribute"/> and/or by <see cref="InspectorNameAttribute"/></param>
        void HandleDecorators(SerializedDataParameter property, GUIContent title)
        {
            foreach (var attr in property.attributes)
            {
                if (!(attr is PropertyAttribute))
                    continue;

                switch (attr)
                {
                    case SpaceAttribute spaceAttribute:
                        EditorGUILayout.GetControlRect(false, spaceAttribute.height);
                        break;
                    case HeaderAttribute headerAttribute:
                    {
                        var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
                        EditorGUI.LabelField(rect, headerAttribute.header, EditorStyles.miniLabel);
                        break;
                    }
                    case TooltipAttribute tooltipAttribute:
                    {
                        if (string.IsNullOrEmpty(title.tooltip))
                            title.tooltip = tooltipAttribute.tooltip;
                        break;
                    }
                    case InspectorNameAttribute inspectorNameAttribute:
                        title.text = inspectorNameAttribute.displayName;
                        break;
                }
            }
        }

        /// <summary>
        /// Draws a given <see cref="SerializedDataParameter"/> in the editor using a custom label
        /// and tooltip.
        /// </summary>
        /// <param name="property">The property to draw in the editor.</param>
        /// <param name="title">A custom label and/or tooltip.</param>
        protected void PropertyField(SerializedDataParameter property, GUIContent title)
        {
            HandleDecorators(property, title);

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

        /// <summary>
        /// Draws the override checkbox used by a property in the editor.
        /// </summary>
        /// <param name="property">The property to draw the override checkbox for</param>
        protected void DrawOverrideCheckbox(SerializedDataParameter property)
        {
            var overrideRect = GUILayoutUtility.GetRect(17f, 17f, GUILayout.ExpandWidth(false));
            overrideRect.yMin += 4f;
            property.overrideState.boolValue = GUI.Toggle(overrideRect, property.overrideState.boolValue, EditorGUIUtility.TrTextContent("", "Override this setting for this volume."), CoreEditorStyles.smallTickbox);
        }
    }
}
