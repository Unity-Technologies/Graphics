using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.AnimatedValues;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// A custom editor class that draws a <see cref="VolumeComponent"/> in the Inspector. If you do not
    /// provide a custom editor for a <see cref="VolumeComponent"/>, Unity uses the default one.
    /// You must use a <see cref="CustomEditor"/> to let the editor know which
    /// component this drawer is for.
    /// </summary>
    /// <example>
    /// <para>Below is an example of a custom <see cref="VolumeComponent"/>:</para>
    /// <code>
    /// using UnityEngine.Rendering;
    ///
    /// [Serializable, VolumeComponentMenu("Custom/Example Component")]
    /// public class ExampleComponent : VolumeComponent
    /// {
    ///     public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    /// }
    /// </code>
    /// <para>And its associated editor:</para>
    /// <code>
    /// using UnityEditor.Rendering;
    ///
    /// [CustomEditor(typeof(ExampleComponent))]
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
    /// <seealso cref="CustomEditor"/>
    [CustomEditor(typeof(VolumeComponent), true)]
    public class VolumeComponentEditor : Editor
    {
        const string k_KeyPrefix = "CoreRP:VolumeComponent:UI_State:";

        EditorPrefBool m_EditorPrefBool;

        internal string categoryTitle { get; set; }

        /// <summary>
        /// If the editor for this <see cref="VolumeComponent"/> is expanded or not in the inspector
        /// </summary>
        public bool expanded
        {
            get => m_EditorPrefBool.value;
            set => m_EditorPrefBool.value = value;
        }

        internal bool visible { get; private set; }

        static class Styles
        {
            public static readonly GUIContent k_OverrideSettingText = EditorGUIUtility.TrTextContent("", "Override this setting for this volume.");

            public static readonly GUIContent k_AllText =
                EditorGUIUtility.TrTextContent("ALL", "Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need.");

            public static readonly GUIContent k_NoneText = EditorGUIUtility.TrTextContent("NONE", "Toggle all overrides off.");

            public static string toggleAllText { get; } = L10n.Tr("Toggle All");

            public const int overrideCheckboxWidth = 14;
            public const int overrideCheckboxOffset = 9;
        }

        Vector2? m_OverrideToggleSize;

        internal Vector2 overrideToggleSize
        {
            get
            {
                if (!m_OverrideToggleSize.HasValue)
                    m_OverrideToggleSize = CoreEditorStyles.smallTickbox.CalcSize(Styles.k_OverrideSettingText);
                return m_OverrideToggleSize.Value;
            }
        }

        /// <summary>
        /// Specifies the <see cref="VolumeComponent"/> this editor is drawing.
        /// </summary>
        public VolumeComponent volumeComponent => target as VolumeComponent;

        /// <summary>
        /// The copy of the serialized property of the <see cref="VolumeComponent"/> being
        /// inspected. Unity uses this to track whether the editor is collapsed in the Inspector or not.
        /// </summary>
        [Obsolete("Please use expanded property instead. #from(2022.2)", false)]
        public SerializedProperty baseProperty { get; internal set; }

        /// <summary>
        /// The serialized property of <see cref="VolumeComponent.active"/> for the component being
        /// inspected.
        /// </summary>
        public SerializedProperty activeProperty { get; internal set; }

        #region Additional Properties

        List<VolumeParameter> m_VolumeNotAdditionalParameters = new List<VolumeParameter>();

        /// <summary>
        /// Override this property if your editor makes use of the "Additional Properties" feature.
        /// </summary>
        public virtual bool hasAdditionalProperties => volumeComponent.parameterList.Count != m_VolumeNotAdditionalParameters.Count;

        /// <summary>
        /// Set to true to show additional properties.
        /// </summary>
        public bool showAdditionalProperties
        {
            get => AdvancedProperties.enabled;
            set => AdvancedProperties.enabled = value;
        }

        /// <summary>
        /// Start a scope for additional properties.
        /// This will handle the highlight of the background when toggled on and off.
        /// </summary>
        /// <returns>True if the additional content should be drawn.</returns>
        protected bool BeginAdditionalPropertiesScope()
        {
            if (!showAdditionalProperties || !hasAdditionalProperties)
                return false;

            AdvancedProperties.BeginGroup();
            return true;
        }

        /// <summary>
        /// End a scope for additional properties.
        /// </summary>
        protected void EndAdditionalPropertiesScope()
        {
            if (hasAdditionalProperties && showAdditionalProperties)
                AdvancedProperties.EndGroup();
        }

        #endregion

        /// <summary>
        /// A reference to the parent editor in the Inspector.
        /// </summary>
        protected Editor m_Inspector;

        /// <summary>
        /// A reference to the parent editor in the Inspector.
        /// </summary>
        internal Editor inspector
        {
            get => m_Inspector;
            set => m_Inspector = value;
        }

        internal void SetVolume(Volume v)
        {
            volume = v;
        }

        /// <summary>
        /// Obtains the <see cref="Volume"/> that is being edited if editing a scene volume, otherwise null.
        /// </summary>
        protected Volume volume { get; private set; }

        internal void SetVolumeProfile(VolumeProfile p)
        {
            volumeProfile = p;
        }

        /// <summary>
        /// Obtains the <see cref="VolumeProfile"/> that is being edited.
        /// </summary>
        VolumeProfile volumeProfile { get; set; }

        List<(GUIContent displayName, int displayOrder, SerializedDataParameter param)> m_Parameters;

        static Dictionary<Type, VolumeParameterDrawer> s_ParameterDrawers;
        SupportedOnRenderPipelineAttribute m_SupportedOnRenderPipelineAttribute;
        Type[] m_LegacyPipelineTypes;

        static VolumeComponentEditor()
        {
            s_ParameterDrawers = new Dictionary<Type, VolumeParameterDrawer>();
            ReloadDecoratorTypes();
        }

        [DidReloadScripts]
        static void OnEditorReload()
        {
            ReloadDecoratorTypes();
        }

        static void ReloadDecoratorTypes()
        {
            s_ParameterDrawers.Clear();

            foreach (var type in TypeCache.GetTypesDerivedFrom<VolumeParameterDrawer>())
            {
                if (type.IsAbstract)
                    continue;

                var attr = type.GetCustomAttribute<VolumeParameterDrawerAttribute>(false);
                if (attr == null)
                {
                    Debug.LogWarning($"{type} is missing the attribute {nameof(VolumeParameterDrawerAttribute)}");
                    continue;
                }

                s_ParameterDrawers.Add(attr.parameterType, Activator.CreateInstance(type) as VolumeParameterDrawer);
            }
        }

        /// <summary>
        /// Triggers an Inspector repaint event.
        /// </summary>
        public new void Repaint()
        {
            // Volume Component Editors can be shown in the Graphics Settings window (default volume profile)
            // This will force a repaint of the whole window, otherwise, additional properties highlight animation does not work properly.
            SettingsService.RepaintAllSettingsWindow();

            base.Repaint();
        }

        internal static string GetAdditionalPropertiesPreferenceKey(Type type)
        {
            return $"UI_Show_Additional_Properties_{type}";
        }

        internal void InitAdditionalPropertiesPreference()
        {
            string key = GetAdditionalPropertiesPreferenceKey(GetType());
            AdvancedProperties.UpdateShowAdvancedProperties(key, EditorPrefs.HasKey(key) && EditorPrefs.GetBool(key));
        }

        internal void Init()
        {
            activeProperty = serializedObject.FindProperty("active");

            string inspectorKey = string.Empty;
            bool expandedByDefault = true;
            if (!enableOverrides)
            {
                inspectorKey += "default"; // Ensures the default VolumeProfile editor doesn't share expander state with other editors
                expandedByDefault = false;
            }

            m_EditorPrefBool = new EditorPrefBool(k_KeyPrefix + inspectorKey + volumeComponent.GetType().Name, expandedByDefault);

            InitAdditionalPropertiesPreference();

            InitParameters();
            OnEnable();

            var volumeComponentType = volumeComponent.GetType();
            m_SupportedOnRenderPipelineAttribute = volumeComponentType.GetCustomAttribute<SupportedOnRenderPipelineAttribute>();

#pragma warning disable CS0618
            var supportedOn = volumeComponentType.GetCustomAttribute<VolumeComponentMenuForRenderPipeline>();
            m_LegacyPipelineTypes = supportedOn != null ? supportedOn.pipelineTypes : Array.Empty<Type>();
#pragma warning restore CS0618

            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }

        void OnDestroy()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
        }

        internal void DetermineVisibility(Type renderPipelineAssetType, Type renderPipelineType)
        {
            if (renderPipelineAssetType == null)
            {
                visible = false;
                return;
            }

            if (m_SupportedOnRenderPipelineAttribute != null)
            {
                visible = m_SupportedOnRenderPipelineAttribute.GetSupportedMode(renderPipelineAssetType) != SupportedOnRenderPipelineAttribute.SupportedMode.Unsupported;
                return;
            }

            if (renderPipelineType != null && m_LegacyPipelineTypes.Length > 0)
            {
                visible = m_LegacyPipelineTypes.Contains(renderPipelineType);
                return;
            }

            visible = true;
        }

        void InitParameters()
        {
            VolumeComponent.FindParameters(target, m_VolumeNotAdditionalParameters, field => field.GetCustomAttribute<AdditionalPropertyAttribute>() == null);
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
                        infos.Add((field, prop == null ? serializedObject.FindProperty(field.Name) : prop.FindPropertyRelative(field.Name)));
                }
                else if (!field.FieldType.IsArray && field.FieldType.IsClass)
                    GetFields(field.GetValue(o), infos, prop == null ? serializedObject.FindProperty(field.Name) : prop.FindPropertyRelative(field.Name));
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
                .Select(t =>
                {
                    var name = "";
                    var order = 0;
                    var (fieldInfo, serializedProperty) = t;
                    var attr = (DisplayInfoAttribute[])fieldInfo.GetCustomAttributes(typeof(DisplayInfoAttribute), true);
                    if (attr.Length != 0)
                    {
                        name = attr[0].name;
                        order = attr[0].order;
                    }

                    var parameter = new SerializedDataParameter(t.Item2);
                    return (EditorGUIUtility.TrTextContent(name), order, parameter);
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

        internal void AddDefaultProfileContextMenuEntries(
            GenericMenu menu,
            VolumeProfile defaultProfile,
            GenericMenu.MenuFunction copyAction)
        {
            // Host can be either VolumeProfileEditor or VolumeEditor
            var profile = volume
                ? volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile
                : volumeProfile;

            if (defaultProfile != null &&
                profile != null &&
                defaultProfile != profile)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent($"Show Default Volume Profile"), false,
                    () => Selection.activeObject = defaultProfile);
                menu.AddItem(EditorGUIUtility.TrTextContent($"Apply Values to Default Volume Profile"), false, copyAction);
            }
        }

        void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.serializedObject.targetObject != target)
                return;

            var targetComponent = property.serializedObject.targetObject as VolumeComponent;

            AddDefaultProfileContextMenuEntries(menu, VolumeManager.instance.globalDefaultProfile,
                () => VolumeProfileUtils.AssignValuesToProfile(VolumeManager.instance.globalDefaultProfile, targetComponent, property));
        }

        /// <summary>
        /// Unity calls this method after drawing the header for each VolumeComponentEditor
        /// </summary>
        protected virtual void OnBeforeInspectorGUI()
        {
        }

        internal bool OnInternalInspectorGUI()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return false;

            serializedObject.Update();
            using (new EditorGUILayout.VerticalScope())
            {
                OnBeforeInspectorGUI();
                if (enableOverrides)
                    TopRowFields();
                else
                    GUILayout.Space(4);
                OnInspectorGUI();
                EditorGUILayout.Space();
            }

            return serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Unity calls this method each time it re-draws the Inspector.
        /// </summary>
        /// <remarks>
        /// You can safely override this method and not call <c>base.OnInspectorGUI()</c> unless you
        /// want Unity to display all the properties from the <see cref="VolumeComponent"/>
        /// automatically.
        /// </remarks>
        public override void OnInspectorGUI()
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
        /// a custom label. If you don't, Unity automatically obtains one from the class name.
        /// </summary>
        /// <returns>A label to display in the component header.</returns>
        public virtual GUIContent GetDisplayTitle()
        {
            var title = string.IsNullOrEmpty(volumeComponent.displayName) ? ObjectNames.NicifyVariableName(volumeComponent.GetType().Name) : volumeComponent.displayName;
            return EditorGUIUtility.TrTextContent(title, string.Empty);
        }

        void AddToggleState(GUIContent content, bool state)
        {
            bool allOverridesSameState = AreOverridesTo(state);
            if (GUILayout.Toggle(allOverridesSameState, content, CoreEditorStyles.miniLabelButton, GUILayout.ExpandWidth(false)) && !allOverridesSameState)
                SetOverridesTo(state);
        }

        void TopRowFields()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                AddToggleState(Styles.k_AllText, true);
                AddToggleState(Styles.k_NoneText, false);
            }
        }

        /// <summary>
        /// Checks if all the visible parameters have the given state
        /// </summary>
        /// <param name="state">The state to check</param>
        internal bool AreOverridesTo(bool state)
        {
            if (hasAdditionalProperties && showAdditionalProperties)
                return AreAllOverridesTo(state);

            for (int i = 0; i < m_VolumeNotAdditionalParameters.Count; ++i)
            {
                if (m_VolumeNotAdditionalParameters[i].overrideState != state)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the given state to all the visible parameters
        /// </summary>
        /// <param name="state">The state to check</param>
        internal void SetOverridesTo(bool state)
        {
            if (hasAdditionalProperties && showAdditionalProperties)
                SetAllOverridesTo(state);
            else
            {
                Undo.RecordObject(target, Styles.toggleAllText);
                volumeComponent.SetOverridesTo(m_VolumeNotAdditionalParameters, state);
                serializedObject.Update();
            }
        }

        internal bool AreAllOverridesTo(bool state)
        {
            for (int i = 0; i < volumeComponent.parameterList.Count; ++i)
            {
                if (volumeComponent.parameterList[i].overrideState != state)
                    return false;
            }

            return true;
        }

        internal void SetAllOverridesTo(bool state)
        {
            Undo.RecordObject(target, Styles.toggleAllText);
            volumeComponent.SetAllOverridesTo(state);
            serializedObject.Update();
        }

        /// <summary>
        /// Generates and auto-populates a <see cref="SerializedDataParameter"/> from a serialized
        /// <see cref="VolumeParameter{T}"/>.
        /// </summary>
        /// <param name="property">A serialized property holding a <see cref="VolumeParameter{T}"/>
        /// </param>
        /// <returns>A <see cref="SerializedDataParameter"/> that encapsulates the provided serialized property.</returns>
        protected SerializedDataParameter Unpack(SerializedProperty property)
        {
            Assert.IsNotNull(property);
            return new SerializedDataParameter(property);
        }

        /// <summary>
        /// Draws a given <see cref="SerializedDataParameter"/> in the editor.
        /// </summary>
        /// <param name="property">The property to draw in the editor</param>
        /// <returns>true if the property field has been rendered</returns>
        protected bool PropertyField(SerializedDataParameter property)
        {
            var title = EditorGUIUtility.TrTextContent(property.displayName,
                property.GetAttribute<TooltipAttribute>()?.tooltip); // avoid property from getting the tooltip of another one with the same name
            return PropertyField(property, title);
        }

        static readonly Dictionary<string, GUIContent> s_HeadersGuiContents = new Dictionary<string, GUIContent>();

        /// <summary>
        /// Draws a header into the inspector with the given title
        /// </summary>
        /// <param name="header">The title for the header</param>
        protected void DrawHeader(string header)
        {
            if (!s_HeadersGuiContents.TryGetValue(header, out GUIContent content))
            {
                content = EditorGUIUtility.TrTextContent(header);
                s_HeadersGuiContents.Add(header, content);
            }

            EditorGUILayout.Space(4);
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rect, content, EditorStyles.boldLabel);
        }

        /// <summary>
        /// Handles unity built-in decorators (Space, Header, Tooltips, ...) from <see cref="SerializedDataParameter"/> attributes
        /// </summary>
        /// <param name="property">The property to obtain the attributes and handle the decorators</param>
        /// <param name="title">A custom label and/or tooltip that might be updated by <see cref="TooltipAttribute"/> and/or by <see cref="InspectorNameAttribute"/></param>
        internal void HandleDecorators(SerializedDataParameter property, GUIContent title)
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
                        DrawHeader(headerAttribute.header);
                        break;
                    case TooltipAttribute tooltipAttribute:
                        if (string.IsNullOrEmpty(title.tooltip))
                            title.tooltip = tooltipAttribute.tooltip;
                        break;
                    case InspectorNameAttribute inspectorNameAttribute:
                        title.text = inspectorNameAttribute.displayName;
                        break;
                }
            }
        }

        /// <summary>
        /// Get indentation from Indent attribute
        /// </summary>
        /// <param name="property">The property to obtain the attributes</param>
        /// <returns>The relative indent level change</returns>
        int HandleRelativeIndentation(SerializedDataParameter property)
        {
            foreach (var attr in property.attributes)
            {
                if (attr is VolumeComponent.Indent indent)
                    return indent.relativeAmount;
            }

            return 0;
        }

        /// <summary>
        /// Draws a given <see cref="SerializedDataParameter"/> in the editor using a custom label
        /// and tooltip.
        /// </summary>
        /// <param name="property">The property to draw in the editor.</param>
        /// <param name="title">A custom label and/or tooltip.</param>
        /// <returns>true if the property field has been rendered</returns>
        protected bool PropertyField(SerializedDataParameter property, GUIContent title)
        {
            if (VolumeParameter.IsObjectParameter(property.referenceType))
                return DrawEmbeddedField(property, title);
            else
                return DrawPropertyField(property, title);
        }

        /// <summary>
        /// Draws a given <see cref="SerializedDataParameter"/> in the editor using a custom label
        /// and tooltip.
        /// </summary>
        /// <param name="property">The property to draw in the editor.</param>
        /// <param name="title">A custom label and/or tooltip.</param>
        private bool DrawPropertyField(SerializedDataParameter property, GUIContent title)
        {
            using (var scope = new OverridablePropertyScope(property, title, this))
            {
                if (!scope.displayed)
                    return false;

                // Custom drawer
                if (scope.drawer?.OnGUI(property, title) ?? false)
                    return true;

                // Standard Unity drawer
                EditorGUILayout.PropertyField(property.value, title);
            }

            return true;
        }

        /// <summary>
        /// Draws a given <see cref="SerializedDataParameter"/> in the editor using a custom label
        /// and tooltip. This variant is only for embedded class / struct
        /// </summary>
        /// <param name="property">The property to draw in the editor.</param>
        /// <param name="title">A custom label and/or tooltip.</param>
        private bool DrawEmbeddedField(SerializedDataParameter property, GUIContent title)
        {
            bool isAdditionalProperty = property.GetAttribute<AdditionalPropertyAttribute>() != null;
            bool displayed = !isAdditionalProperty || BeginAdditionalPropertiesScope();
            if (!displayed)
                return false;

            // Custom parameter drawer
            s_ParameterDrawers.TryGetValue(property.referenceType, out VolumeParameterDrawer drawer);
            if (drawer != null && !drawer.IsAutoProperty())
                if (drawer.OnGUI(property, title))
                {
                    if (isAdditionalProperty)
                        EndAdditionalPropertiesScope();
                    return true;
                }

            // Standard Unity drawer
            using (new IndentLevelScope())
            {
                bool expanded = property?.value?.isExpanded ?? true;
                expanded = EditorGUILayout.Foldout(expanded, title, true);
                if (expanded)
                {
                    // Not the fastest way to do it but that'll do just fine for now
                    var it = property.value.Copy();
                    var end = it.GetEndProperty();
                    bool first = true;

                    while (it.Next(first) && !SerializedProperty.EqualContents(it, end))
                    {
                        PropertyField(Unpack(it));
                        first = false;
                    }
                }

                property.value.isExpanded = expanded;
            }

            if (isAdditionalProperty)
                EndAdditionalPropertiesScope();
            return true;
        }

        /// <summary>
        /// Draw a Color Field but convert the color to gamma space before displaying it in the shader.
        /// Using SetColor on a material does the conversion, but setting the color as vector3 in a constant buffer doesn't
        /// So we have to do it manually, doing it in the UI avoids having to do a migration step for existing fields
        /// </summary>
        /// <param name="property">The color property</param>
        protected void ColorFieldLinear(SerializedDataParameter property)
        {
            var title = EditorGUIUtility.TrTextContent(property.displayName,
                property.GetAttribute<TooltipAttribute>()?.tooltip);

            using (var scope = new OverridablePropertyScope(property, title, this))
            {
                if (!scope.displayed)
                    return;

                // Standard Unity drawer
                CoreEditorUtils.ColorFieldLinear(property.value, title);
            }
        }

        /// <summary>
        /// Draws the override checkbox used by a property in the editor.
        /// </summary>
        /// <param name="property">The property to draw the override checkbox for</param>
        protected void DrawOverrideCheckbox(SerializedDataParameter property)
        {
            // Create a rect the height + vspacing of the property that is being overriden
            float height = EditorGUI.GetPropertyHeight(property.value) + EditorGUIUtility.standardVerticalSpacing;
            var overrideRect = GUILayoutUtility.GetRect(Styles.k_AllText, CoreEditorStyles.miniLabelButton, GUILayout.Height(height),
                GUILayout.Width(Styles.overrideCheckboxWidth + Styles.overrideCheckboxOffset), GUILayout.ExpandWidth(false));

            // also center vertically the checkbox
            overrideRect.yMin += height * 0.5f - overrideToggleSize.y * 0.5f;
            overrideRect.xMin += Styles.overrideCheckboxOffset;

            property.overrideState.boolValue = GUI.Toggle(overrideRect, property.overrideState.boolValue, Styles.k_OverrideSettingText, CoreEditorStyles.smallTickbox);
        }

        /// <summary>
        /// Scope for property that handle:
        /// - Layout decorator (Space, Header)
        /// - Naming decorator (Tooltips, InspectorName)
        /// - Overridable checkbox if parameter IsAutoProperty
        /// - disabled GUI if Overridable checkbox (case above) is unchecked
        /// - additional property scope
        /// This is automatically used inside PropertyField method
        /// </summary>
        protected struct OverridablePropertyScope : IDisposable
        {
            bool isAdditionalProperty;
            VolumeComponentEditor editor;
            IDisposable disabledScope;
            IDisposable indentScope;
            internal bool haveCustomOverrideCheckbox { get; private set; }
            internal VolumeParameterDrawer drawer { get; private set; }

            /// <summary>
            /// Either the content property will be displayed or not (can varry with additional property settings)
            /// </summary>
            public bool displayed { get; private set; }

            /// <summary>
            /// The title modified regarding attribute used on the field
            /// </summary>
            public GUIContent label { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="property">The property that will be drawn</param>
            /// <param name="label">The label of this property</param>
            /// <param name="editor">The editor that will draw it</param>
            public OverridablePropertyScope(SerializedDataParameter property, GUIContent label, VolumeComponentEditor editor)
            {
                disabledScope = null;
                indentScope = null;
                haveCustomOverrideCheckbox = false;
                drawer = null;
                displayed = false;
                isAdditionalProperty = false;
                this.label = label;
                this.editor = editor;

                Init(property, label, editor);
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="property">The property that will be drawn</param>
            /// <param name="label">The label of this property</param>
            /// <param name="editor">The editor that will draw it</param>
            public OverridablePropertyScope(SerializedDataParameter property, string label, VolumeComponentEditor editor)
            {
                disabledScope = null;
                indentScope = null;
                haveCustomOverrideCheckbox = false;
                drawer = null;
                displayed = false;
                isAdditionalProperty = false;
                this.label = EditorGUIUtility.TrTextContent(label);
                this.editor = editor;

                Init(property, this.label, editor);
            }

            void Init(SerializedDataParameter property, GUIContent label, VolumeComponentEditor editor)
            {
                // Below, 3 is horizontal spacing and there is one between label and field and another between override checkbox and label
                EditorGUIUtility.labelWidth -= Styles.overrideCheckboxWidth + Styles.overrideCheckboxOffset + 3 + 3;

                isAdditionalProperty = property.GetAttribute<AdditionalPropertyAttribute>() != null;
                displayed = !isAdditionalProperty || editor.BeginAdditionalPropertiesScope();

                s_ParameterDrawers.TryGetValue(property.referenceType, out VolumeParameterDrawer vpd);
                drawer = vpd;

                //never draw override for embedded class/struct
                haveCustomOverrideCheckbox = (displayed && !(drawer?.IsAutoProperty() ?? true))
                                             || VolumeParameter.IsObjectParameter(property.referenceType);

                if (displayed)
                {
                    editor.HandleDecorators(property, label);

                    int relativeIndentation = editor.HandleRelativeIndentation(property);

                    int indent = relativeIndentation * 15;
                    if (haveCustomOverrideCheckbox)
                        indent += 15;

                    if (indent != 0)
                        indentScope = new IndentLevelScope(indent);

                    if (!haveCustomOverrideCheckbox)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (editor.enableOverrides)
                            editor.DrawOverrideCheckbox(property);

                        disabledScope = new EditorGUI.DisabledScope(!property.overrideState.boolValue);
                    }
                }
            }

            /// <summary>
            /// Dispose of the class
            /// </summary>
            void IDisposable.Dispose()
            {
                disabledScope?.Dispose();
                indentScope?.Dispose();

                if (!haveCustomOverrideCheckbox && displayed)
                    EditorGUILayout.EndHorizontal();

                if (isAdditionalProperty)
                    editor.EndAdditionalPropertiesScope();

                EditorGUIUtility.labelWidth += Styles.overrideCheckboxWidth + Styles.overrideCheckboxOffset + 3 + 3;
            }
        }

        /// <summary>
        /// Like EditorGUI.IndentLevelScope but this one will also indent the override checkboxes.
        /// </summary>
        protected class IndentLevelScope : GUI.Scope
        {
            int m_Offset;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="offset">[optional] Change the indentation offset</param>
            public IndentLevelScope(int offset = 15)
            {
                m_Offset = offset;

                // When using EditorGUI.indentLevel++, the clicking on the checkboxes does not work properly due to some issues on the C++ side.
                // This scope is a work-around for this issue.
                GUILayout.BeginHorizontal();
                EditorGUILayout.Space(offset, false);
                GUIStyle style = new GUIStyle();
                GUILayout.BeginVertical(style);
                EditorGUIUtility.labelWidth -= m_Offset;
            }

            /// <summary>
            /// Closes the scope
            /// </summary>
            protected override void CloseScope()
            {
                EditorGUIUtility.labelWidth += m_Offset;
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Whether to draw the UI elements related to overrides.
        /// </summary>
        public bool enableOverrides { get; set; } = true;
    }
}
