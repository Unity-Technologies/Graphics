using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using RenderQueueType = UnityEngine.Rendering.HighDefinition.HDRenderQueue.RenderQueueType;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    abstract class SubTargetPropertyBlock : VisualElement
    {
        // Null/Empty means no title
        protected virtual string title => null;

        protected TargetPropertyGUIContext context;
        protected Action onChange;
        protected Action<String> registerUndo;
        protected SystemData systemData;
        protected BuiltinData builtinData;
        protected LightingData lightingData;
        protected List<string> lockedProperties;

        internal void Initialize(TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo,
            SystemData systemData, BuiltinData builtinData, LightingData lightingData, List<string> lockedProperties)
        {
            this.context = context;
            this.onChange = onChange;
            this.registerUndo = registerUndo;
            this.systemData = systemData;
            this.builtinData = builtinData;
            this.lightingData = lightingData;
            this.lockedProperties = lockedProperties;
        }

        protected Action<bool> CreateLockerFor(string fieldName)
        {
            return (locked) =>
            {
                bool isLocked = lockedProperties.Contains(fieldName);

                if (locked == isLocked)
                    return;

                if (!locked)
                    lockedProperties.Remove(fieldName);
                else
                    lockedProperties.Add(fieldName);

                onChange();
            };
        }

        // Utility function to create UIElement fields:
        private void AddPropertyImpl<Data>(GUIContent displayName, string lockableMaterialPropertyName, Func<Data> getter, Action<Data> setter, int indentLevel)
        {
            // Create UIElement from type:
            BaseField<Data> elem = null;
            BaseField<Enum> elemEnum = null;

            var g = getter();
            switch (g)
            {
                case bool b: elem = new Toggle { value = b, tooltip = displayName.tooltip } as BaseField<Data>; break;
                case int i: elem = new IntegerField { value = i, tooltip = displayName.tooltip } as BaseField<Data>; break;
                case float f: elem = new FloatField { value = f, tooltip = displayName.tooltip } as BaseField<Data>; break;
                case Enum e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip }; break;
                default: throw new Exception($"Can't create UI field for type {getter().GetType()}, please add it if it's relevant. If you can't consider using TargetPropertyGUIContext.AddProperty instead.");
            }

            // Wrap if lockable
            if (lockableMaterialPropertyName != null)
            {
                bool isLocked = lockedProperties.Contains(lockableMaterialPropertyName);
                Action<bool> locker = CreateLockerFor(lockableMaterialPropertyName);
                switch (g)
                {
                    case bool b: elem = new LockableBaseField<Toggle, bool>(elem as Toggle, isLocked, locker) as BaseField<Data>; break;
                    case int i: elem = new LockableBaseField<IntegerField, int>(elem as IntegerField, isLocked, locker) as BaseField<Data>; break;
                    case float f: elem = new LockableBaseField<FloatField, float>(elem as FloatField, isLocked, locker) as BaseField<Data>; break;
                    case Enum e: elemEnum = new LockableBaseField<EnumField, Enum>(elemEnum, isLocked, locker); break;
                }
            }

            if (elem != null)
            {
                context.AddProperty<Data>(displayName.text, indentLevel, elem, (evt) =>
                {
                    if (Equals(getter(), evt.newValue))
                        return;

                    registerUndo(displayName.text);
                    setter(evt.newValue);
                    onChange();
                });

                if (elem is ILockable lockableField)
                    lockableField.InitLockPosition();
            }
            else
            {
                context.AddProperty<Enum>(displayName.text, indentLevel, elemEnum, (evt) =>
                {
                    if (Equals(getter(), evt.newValue))
                        return;

                    registerUndo(displayName.text);
                    setter((Data)(object)evt.newValue);
                    onChange();
                });

                if (elemEnum is ILockable lockableField)
                    lockableField.InitLockPosition();
            }
        }

        protected void AddProperty<Data>(string displayName, Func<Data> getter, Action<Data> setter, int indentLevel = 0)
            => AddProperty<Data>(new GUIContent(displayName), getter, setter, indentLevel);

        protected void AddProperty<Data>(GUIContent displayName, Func<Data> getter, Action<Data> setter, int indentLevel = 0)
            => AddPropertyImpl<Data>(displayName, null, getter, setter, indentLevel);

        protected void AddLockableProperty<Data>(string displayName, string materialPropertyName, Func<Data> getter, Action<Data> setter, int indentLevel = 0)
            => AddLockableProperty<Data>(new GUIContent(displayName), materialPropertyName, getter, setter, indentLevel);

        protected void AddLockableProperty<Data>(GUIContent displayName, string materialPropertyName, Func<Data> getter, Action<Data> setter, int indentLevel = 0)
            => AddPropertyImpl<Data>(displayName, materialPropertyName, getter, setter, indentLevel);

        protected void AddFoldout(string text, Func<bool> getter, Action<bool> setter)
            => AddFoldout(new GUIContent(text), getter, setter);

        protected void AddFoldout(GUIContent content, Func<bool> getter, Action<bool> setter)
        {
            var foldout = new Foldout() {
                value = getter(),
                text = content.text,
                tooltip = content.tooltip
            };

            foldout.RegisterValueChangedCallback((evt) => {
                setter(evt.newValue);
                onChange();
            });

            // Apply padding:
            foldout.style.paddingLeft = context.globalIndentLevel * 15;

            context.Add(foldout);
        }

        protected void AddHelpBox(string message, MessageType type)
        {
            // We don't use UIElement HelpBox because it's width is not dynamic.
            int indentLevel = context.globalIndentLevel;
            var imgui = new IMGUIContainer(() =>
            {
                float indentPadding = indentLevel * 15;
                var rect = EditorGUILayout.GetControlRect(false, 42);
                rect.x += indentPadding;
                rect.width -= indentPadding;
                EditorGUI.HelpBox(rect, message, type);
            });

            context.Add(imgui);
        }

        public void CreatePropertyGUIWithHeader()
        {
            if (!String.IsNullOrEmpty(title))
            {
                int index = foldoutIndex;
                AddFoldout(title,
                    () => (systemData.inspectorFoldoutMask & (1 << index)) != 0,
                    (value) =>
                    {
                        systemData.inspectorFoldoutMask &= ~(1 << index); // Clear
                        systemData.inspectorFoldoutMask |= (value ? 1 : 0) << index; // Set
                    }
                );
                context.globalIndentLevel++;
                if ((systemData.inspectorFoldoutMask & (1 << index)) != 0)
                    CreatePropertyGUI();
                context.globalIndentLevel--;
            }
            else
                CreatePropertyGUI();
        }

        protected abstract void CreatePropertyGUI();

        /// <summary>Warning: this property must have a different value for each property block type!</summary>
        protected abstract int foldoutIndex { get; }
    }

    internal interface ILockable
    {
        void InitLockPosition();
    }

    internal class LockableBaseField<TBaseField, TValueType> : BaseField<TValueType>, ILockable
        where TBaseField : BaseField<TValueType>
    {
        public new static readonly string ussClassName = "unity-lockable";

        BaseField<TValueType> m_ContainedField;
        LockArea m_LockArea;

        public LockableBaseField(BaseField<TValueType> containedField, bool isLocked, Action<bool> locker)
            : base(null, null)
        {
            m_ContainedField = containedField;
            m_LockArea = new LockArea(isLocked, locker);

            Add(m_ContainedField);

            //styling
            this.Q(className: "unity-base-field__input").style.flexGrow = 0;
            style.overflow = Overflow.Visible;
            style.marginLeft = 0;
            style.marginRight = 0;
            m_ContainedField.style.flexGrow = 1;
        }

        public void InitLockPosition()
        {
            //HACK to move the lock into the parent container
            VisualElement lineContainer = this;
            while (lineContainer != null && lineContainer.name != "container")
                lineContainer = lineContainer.hierarchy.parent;
            if (lineContainer == null)
                lineContainer = this;

            lineContainer.Add(m_LockArea);
            m_LockArea.style.position = Position.Absolute;
            m_LockArea.style.left = 0;
        }

        public new virtual TValueType value
        {
            get => m_ContainedField.value;
            set => m_ContainedField.value = value;
        }

        public new Label labelElement => m_ContainedField.labelElement;

        public new string label
        {
            get => m_ContainedField.label;
            set => m_ContainedField.label = value;
        }

        public override void SetValueWithoutNotify(TValueType newValue)
            => m_ContainedField.SetValueWithoutNotify(newValue);
    }

    class LockArea : Image
    {
        public new static readonly string ussClassName = "unity-lock-area";

        bool m_Locked;
        Action<bool> m_Callback;

        public LockArea(bool initValue, Action<bool> callback) : base()
        {
            m_Locked = initValue;
            m_Callback = callback;

            //styling
            this.image = EditorGUIUtility.IconContent("AssemblyLock").image;
            style.height = 15;
            style.minHeight = 15;
            style.maxHeight = 15;
            style.width = 14;
            style.minWidth = 14;
            style.maxWidth = 14;

            UpdateDisplay();
            this.AddManipulator(new ToggleClickManipulator(Toggle));
        }

        void UpdateDisplay()
            => style.opacity = m_Locked ? 1f : 0.25f;

        public bool locked => m_Locked;

        public void Toggle()
        {
            m_Locked ^= true;
            UpdateDisplay();
            m_Callback?.Invoke(m_Locked);
        }

        class ToggleClickManipulator : Manipulator
        {
            Action m_Callback;

            public ToggleClickManipulator(Action callback)
                => m_Callback = callback;

            protected override void RegisterCallbacksOnTarget()
                => target.RegisterCallback<MouseDownEvent>(OnMouseDown);

            protected override void UnregisterCallbacksFromTarget()
                => target.UnregisterCallback<MouseDownEvent>(OnMouseDown);

            void OnMouseDown(MouseDownEvent evt)
            {
                m_Callback();
                evt.StopPropagation();
            }
        }
    }
}
