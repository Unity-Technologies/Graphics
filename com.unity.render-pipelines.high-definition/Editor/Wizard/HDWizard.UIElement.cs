using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDWizard : EditorWindow
    {
        #region OBJECT_SELECTOR

        //utility class to show only non scene object selection
        static class ObjectSelector
        {
            static Action<UnityEngine.Object, Type, Action<UnityEngine.Object>> ShowObjectSelector;
            static Func<UnityEngine.Object> GetCurrentObject;
            static Func<int> GetSelectorID;
            static Action<int> SetSelectorID;

            const string ObjectSelectorUpdatedCommand = "ObjectSelectorUpdated";

            static int id;

            static int selectorID { get => GetSelectorID(); set => SetSelectorID(value); }

            public static bool opened
                => Resources.FindObjectsOfTypeAll(typeof(PlayerSettings).Assembly.GetType("UnityEditor.ObjectSelector")).Length > 0;

            // Action to be called with the window is closed
            static Action s_OnClose;

            static ObjectSelector()
            {
                Type playerSettingsType = typeof(PlayerSettings);
                Type objectSelectorType = playerSettingsType.Assembly.GetType("UnityEditor.ObjectSelector");
                var instanceObjectSelectorInfo = objectSelectorType.GetProperty("get", BindingFlags.Static | BindingFlags.Public);
#if UNITY_2020_1_OR_NEWER
                var showInfo = objectSelectorType.GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(UnityEngine.Object), typeof(Type), typeof(UnityEngine.Object), typeof(bool), typeof(List<int>), typeof(Action<UnityEngine.Object>), typeof(Action<UnityEngine.Object>) }, null);
#else
                var showInfo = objectSelectorType.GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(UnityEngine.Object), typeof(Type), typeof(SerializedProperty), typeof(bool), typeof(List<int>), typeof(Action<UnityEngine.Object>), typeof(Action<UnityEngine.Object>) }, null);
#endif
                var objectSelectorVariable = Expression.Variable(objectSelectorType, "objectSelector");
                var objectParameter = Expression.Parameter(typeof(UnityEngine.Object), "unityObject");
                var typeParameter = Expression.Parameter(typeof(Type), "type");
                var onClosedParameter = Expression.Parameter(typeof(Action<UnityEngine.Object>), "onClosed");
                var onChangedObjectParameter = Expression.Parameter(typeof(Action<UnityEngine.Object>), "onChangedObject");
                var showObjectSelectorBlock = Expression.Block(
                    new[] { objectSelectorVariable },
                    Expression.Assign(objectSelectorVariable, Expression.Call(null, instanceObjectSelectorInfo.GetGetMethod())),
#if UNITY_2020_1_OR_NEWER
                    Expression.Call(objectSelectorVariable, showInfo, objectParameter, typeParameter, Expression.Constant(null, typeof(UnityEngine.Object)), Expression.Constant(false), Expression.Constant(null, typeof(List<int>)), Expression.Constant(null, typeof(Action<UnityEngine.Object>)), onChangedObjectParameter)
#else
                    Expression.Call(objectSelectorVariable, showInfo, objectParameter, typeParameter, Expression.Constant(null, typeof(SerializedProperty)), Expression.Constant(false), Expression.Constant(null, typeof(List<int>)), Expression.Constant(null, typeof(Action<UnityEngine.Object>)), onChangedObjectParameter)
#endif
                );
                var showObjectSelectorLambda = Expression.Lambda<Action<UnityEngine.Object, Type, Action<UnityEngine.Object>>>(showObjectSelectorBlock, objectParameter, typeParameter, onChangedObjectParameter);
                ShowObjectSelector = showObjectSelectorLambda.Compile();

                var instanceCall = Expression.Call(null, instanceObjectSelectorInfo.GetGetMethod());
                var objectSelectorIDField = Expression.Field(instanceCall, "objectSelectorID");
                var getSelectorIDLambda = Expression.Lambda<Func<int>>(objectSelectorIDField);
                GetSelectorID = getSelectorIDLambda.Compile();

                var inSelectorIDParam = Expression.Parameter(typeof(int), "value");
                var setSelectorIDLambda = Expression.Lambda<Action<int>>(Expression.Assign(objectSelectorIDField, inSelectorIDParam), inSelectorIDParam);
                SetSelectorID = setSelectorIDLambda.Compile();

                var getCurrentObjectInfo = objectSelectorType.GetMethod("GetCurrentObject");
                var getCurrentObjectLambda = Expression.Lambda<Func<UnityEngine.Object>>(Expression.Call(null, getCurrentObjectInfo));
                GetCurrentObject = getCurrentObjectLambda.Compile();
            }

            public static void Show(UnityEngine.Object obj, Type type, Action<UnityEngine.Object> onChangedObject, Action onClose)
            {
                id = GUIUtility.GetControlID("s_ObjectFieldHash".GetHashCode(), FocusType.Keyboard);
                GUIUtility.keyboardControl = id;
                ShowObjectSelector(obj, type, onChangedObject);
                selectorID = id;
                ObjectSelector.s_OnClose = onClose;
                EditorApplication.update += CheckClose;
            }

            static void CheckClose()
            {
                if (!opened)
                {
                    ObjectSelector.s_OnClose?.Invoke();
                    EditorApplication.update -= CheckClose;
                }
            }

            public static void CheckAssignationEvent<T>(Action<T> assignator)
                where T : UnityEngine.Object
            {
                Event evt = Event.current;
                if (evt.type != EventType.ExecuteCommand)
                    return;
                string commandName = evt.commandName;
                if (commandName != ObjectSelectorUpdatedCommand || selectorID != id)
                    return;
                T current = GetCurrentObject() as T;
                if (current == null)
                    return;
                assignator(current);
                GUI.changed = true;
                evt.Use();
            }
        }

        void CreateOrLoad<T>(Action onCancel, Action<T> onObjectChanged)
            where T : ScriptableObject
        {
            string title;
            string content;
            UnityEngine.Object target;
            if (typeof(T) == typeof(HDRenderPipelineAsset))
            {
                title = Style.hdrpAssetDisplayDialogTitle;
                content = Style.hdrpAssetDisplayDialogContent;
                target = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            }
            else
                throw new ArgumentException("Unknown type used");

            switch (EditorUtility.DisplayDialogComplex(title, content, Style.displayDialogCreate, "Cancel", Style.displayDialogLoad))
            {
                case 0: //create
                    if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                        AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);
                    var asset = ScriptableObject.CreateInstance<T>();
                    asset.name = typeof(T).Name;
                    AssetDatabase.CreateAsset(asset, "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + asset.name + ".asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    if (typeof(T) == typeof(HDRenderPipelineAsset))
                        GraphicsSettings.renderPipelineAsset = asset as HDRenderPipelineAsset;
                    break;
                case 1: //cancel
                    onCancel?.Invoke();
                    break;
                case 2: //Load
                {
                    m_Fixer.Pause();
                    ObjectSelector.Show(target, typeof(T), o => onObjectChanged?.Invoke((T)o), m_Fixer.Unpause);
                    break;
                }

                default:
                    throw new ArgumentException("Unrecognized option");
            }
        }

        #endregion

        #region UIELEMENT

        class ToolbarRadio : UIElements.Toolbar, INotifyValueChanged<int>
        {
            public new class UxmlFactory : UxmlFactory<ToolbarRadio, UxmlTraits> {}
            public new class UxmlTraits : Button.UxmlTraits {}

            List<ToolbarToggle> radios = new List<ToolbarToggle>();

            public new static readonly string ussClassName = "unity-toolbar-radio";

            public int radioLength => radios.Count;

            int m_Value;
            public int value
            {
                get => m_Value;
                set
                {
                    if (value == m_Value)
                        return;

                    if (panel != null)
                    {
                        using (ChangeEvent<int> evt = ChangeEvent<int>.GetPooled(m_Value, value))
                        {
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }

            public ToolbarRadio()
            {
                AddToClassList(ussClassName);
            }

            void AddRadio(string label = null, string tooltip = null)
            {
                var toggle = new ToolbarToggle()
                {
                    text = label,
                    tooltip = tooltip
                };
                toggle.RegisterValueChangedCallback(InnerValueChanged(radioLength));
                toggle.SetValueWithoutNotify(radioLength == 0);
                if (radioLength == 0)
                    toggle.AddToClassList("SelectedRadio");
                radios.Add(toggle);
                Add(toggle);
                toggle.AddToClassList("Radio");
            }

            public void AddRadios((string label, string tooltip)[] tabs)
            {
                if (tabs.Length == 0)
                    return;

                if (radioLength > 0)
                {
                    radios[radioLength - 1].RemoveFromClassList("LastRadio");
                }
                foreach (var(label, tooltip) in tabs)
                    AddRadio(label, tooltip);

                radios[radioLength - 1].AddToClassList("LastRadio");
            }

            EventCallback<ChangeEvent<bool>> InnerValueChanged(int radioIndex)
            {
                return (ChangeEvent<bool> evt) =>
                {
                    if (radioIndex == m_Value)
                    {
                        if (!evt.newValue)
                        {
                            //cannot deselect in a radio
                            radios[m_Value].RemoveFromClassList("SelectedRadio");
                            radios[radioIndex].AddToClassList("SelectedRadio");
                            radios[radioIndex].SetValueWithoutNotify(true);
                        }
                        else
                            value = -1;
                    }
                    else
                        value = radioIndex;
                };
            }

            public void SetValueWithoutNotify(int newValue)
            {
                if (m_Value != newValue)
                {
                    if (newValue < 0 || newValue >= radioLength)
                        throw new System.IndexOutOfRangeException();

                    if (m_Value != newValue)
                    {
                        radios[m_Value].RemoveFromClassList("SelectedRadio");
                        radios[newValue].AddToClassList("SelectedRadio");
                        radios[newValue].SetValueWithoutNotify(true);
                        m_Value = newValue;
                    }
                }
            }
        }

        abstract class VisualElementUpdatable : VisualElement
        {
            protected Func<bool> m_Tester;
            bool m_HaveFixer;
            public bool currentStatus { get; private set; }

            protected VisualElementUpdatable(Func<bool> tester, bool haveFixer)
            {
                m_Tester = tester;
                m_HaveFixer = haveFixer;
            }

            public virtual void CheckUpdate()
            {
                bool wellConfigured = m_Tester();
                if (wellConfigured ^ currentStatus)
                {
                    UpdateDisplay(wellConfigured, m_HaveFixer);
                    currentStatus = wellConfigured;
                }
            }

            protected void Init() => UpdateDisplay(currentStatus, m_HaveFixer);

            protected abstract void UpdateDisplay(bool statusOK, bool haveFixer);
        }

        class HiddableUpdatableContainer : VisualElementUpdatable
        {
            public HiddableUpdatableContainer(Func<bool> tester, bool haveFixer = false) : base(tester, haveFixer) {}

            public override void CheckUpdate()
            {
                base.CheckUpdate();
                if (currentStatus)
                {
                    foreach (VisualElementUpdatable updatable in Children().Where(e => e is VisualElementUpdatable))
                        updatable.CheckUpdate();
                }
            }

            new public void Init() => base.Init();

            protected override void UpdateDisplay(bool visible, bool haveFixer)
                => style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        class ConfigInfoLine : VisualElementUpdatable
        {
            static class Style
            {
                const string k_IconFolder = @"Packages/com.unity.render-pipelines.high-definition/Editor/Wizard/WizardResources/";
                public static readonly Texture ok = CoreEditorUtils.LoadIcon(k_IconFolder, "OK");
                public static readonly Texture error = CoreEditorUtils.LoadIcon(k_IconFolder, "Error");
                public static readonly Texture warning = CoreEditorUtils.LoadIcon(k_IconFolder, "Warning");

                public const int k_IndentStepSize = 15;
            }

            readonly bool m_VisibleStatus;
            readonly bool m_SkipErrorIcon;

            public ConfigInfoLine(string label, string error, MessageType messageType, string resolverButtonLabel, Func<bool> tester, Action resolver, int indent = 0, bool visibleStatus = true, bool skipErrorIcon = false)
                : base(tester, resolver != null)
            {
                m_VisibleStatus = visibleStatus;
                m_SkipErrorIcon = skipErrorIcon;
                var testLabel = new Label(label)
                {
                    name = "TestLabel"
                };
                var fixer = new Button(resolver)
                {
                    text = resolverButtonLabel,
                    name = "Resolver"
                };
                var testRow = new VisualElement() { name = "TestRow" };
                testRow.Add(testLabel);
                if (m_VisibleStatus)
                {
                    var statusOK = new Image()
                    {
                        image = Style.ok,
                        name = "StatusOK"
                    };
                    var statusKO = new Image()
                    {
                        image = Style.error,
                        name = "StatusError"
                    };
                    testRow.Add(statusOK);
                    testRow.Add(statusKO);
                }
                testRow.Add(fixer);

                Add(testRow);
                HelpBox.Kind kind;
                switch (messageType)
                {
                    default:
                    case MessageType.None: kind = HelpBox.Kind.None; break;
                    case MessageType.Error: kind = HelpBox.Kind.Error; break;
                    case MessageType.Warning: kind = HelpBox.Kind.Warning; break;
                    case MessageType.Info: kind = HelpBox.Kind.Info; break;
                }
                Add(new HelpBox(kind, error));

                testLabel.style.paddingLeft = style.paddingLeft.value.value + indent * Style.k_IndentStepSize;

                Init();
            }

            protected override void UpdateDisplay(bool statusOK, bool haveFixer)
            {
                if (!((hierarchy.parent as HiddableUpdatableContainer)?.currentStatus ?? true))
                {
                    if (m_VisibleStatus)
                    {
                        this.Q(name: "StatusOK").style.display = DisplayStyle.None;
                        this.Q(name: "StatusError").style.display = DisplayStyle.None;
                    }
                    this.Q(name: "Resolver").style.display = DisplayStyle.None;
                    this.Q(className: "HelpBox").style.display = DisplayStyle.None;
                }
                else
                {
                    if (m_VisibleStatus)
                    {
                        this.Q(name: "StatusOK").style.display = statusOK ? DisplayStyle.Flex : DisplayStyle.None;
                        this.Q(name: "StatusError").style.display = !statusOK ? (m_SkipErrorIcon ? DisplayStyle.None : DisplayStyle.Flex) : DisplayStyle.None;
                    }
                    this.Q(name: "Resolver").style.display = statusOK || !haveFixer ? DisplayStyle.None : DisplayStyle.Flex;
                    this.Q(className: "HelpBox").style.display = statusOK ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }

        class HelpBox : VisualElement
        {
            public enum Kind
            {
                None,
                Info,
                Warning,
                Error
            }

            readonly Label label;
            readonly Image icon;

            public string text
            {
                get => label.text;
                set => label.text = value;
            }

            Kind m_Kind = Kind.None;
            public Kind kind
            {
                get => m_Kind;
                set
                {
                    if (m_Kind != value)
                    {
                        m_Kind = value;

                        string iconName;
                        switch (kind)
                        {
                            default:
                            case Kind.None:
                                icon.style.display = DisplayStyle.None;
                                return;
                            case Kind.Info:
                                iconName = "console.infoicon";
                                break;
                            case Kind.Warning:
                                iconName = "console.warnicon";
                                break;
                            case Kind.Error:
                                iconName = "console.erroricon";
                                break;
                        }
                        icon.image = EditorGUIUtility.IconContent(iconName).image;
                        icon.style.display = DisplayStyle.Flex;
                    }
                }
            }

            public HelpBox(Kind kind, string message)
            {
                this.label = new Label(message);
                icon = new Image();

                AddToClassList("HelpBox");
                Add(icon);
                Add(this.label);

                this.kind = kind;
            }
        }

        class FixAllButton : VisualElementUpdatable
        {
            public FixAllButton(string label, Func<bool> tester, Action resolver)
                : base(tester, resolver != null)
            {
                Add(new Button(resolver)
                {
                    text = label,
                    name = "FixAll"
                });

                Init();
            }

            protected override void UpdateDisplay(bool statusOK, bool haveFixer)
                => this.Q(name: "FixAll").style.display = statusOK ? DisplayStyle.None : DisplayStyle.Flex;
        }

        class ScopeBox : VisualElementUpdatable
        {
            readonly Label label;
            bool initTitleBackground;

            public ScopeBox(string title) : base(null, false)
            {
                label = new Label(title);
                label.name = "Title";
                AddToClassList("ScopeBox");
                Add(label);
            }

            public override void CheckUpdate()
            {
                foreach (VisualElementUpdatable updatable in Children().Where(e => e is VisualElementUpdatable))
                    updatable.CheckUpdate();
            }

            protected override void UpdateDisplay(bool statusOK, bool haveFixer)
            {}
        }

        #endregion
    }
}
