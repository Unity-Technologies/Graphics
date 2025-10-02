using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using UnityEngine.VFX;

using Object = UnityEngine.Object;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    interface IPropertyRMProvider
    {
        bool expanded { get; }
        bool expandable { get; }
        bool expandableIfShowsEverything { get; }
        object value { get; set; }
        bool spaceableAndMasterOfSpace { get; }
        VFXSpace space { get; set; }
        bool IsSpaceInherited();
        string name { get; }
        VFXPropertyAttributes attributes { get; }
        object[] customAttributes { get; }
        Type portType { get; }
        int depth { get; }
        bool editable { get; }

        IEnumerable<int> filteredOutEnumerators { get; }
        void RetractPath();
        void ExpandPath();


        void StartLiveModification();
        void EndLiveModification();
    }

    interface IVFXNotifyValueChanged<T> : INotifyValueChanged<T>
    {
        void SetValueWithoutNotify(T typedNewValue, bool force = false);
    }

    class SimplePropertyRMProvider<T> : IPropertyRMProvider
    {
        System.Func<T> m_Getter;
        System.Action<T> m_Setter;
        string m_Name;

        public SimplePropertyRMProvider(string name, System.Func<T> getter, System.Action<T> setter)
        {
            m_Getter = getter;
            m_Setter = setter;
            m_Name = name;
        }

        VFXSpace IPropertyRMProvider.space { get { return VFXSpace.Local; } set { } }

        bool IPropertyRMProvider.IsSpaceInherited() { return false; }

        bool IPropertyRMProvider.spaceableAndMasterOfSpace { get { return false; } }

        bool IPropertyRMProvider.expanded { get { return false; } }
        bool IPropertyRMProvider.expandable { get { return false; } }
        bool IPropertyRMProvider.expandableIfShowsEverything { get { return false; } }
        object IPropertyRMProvider.value
        {
            get
            {
                return m_Getter();
            }

            set
            {
                m_Setter((T)value);
            }
        }

        public virtual IEnumerable<int> filteredOutEnumerators { get { return null; } }

        string IPropertyRMProvider.name
        {
            get { return m_Name; }
        }
        VFXPropertyAttributes IPropertyRMProvider.attributes { get { return new VFXPropertyAttributes(); } }
        object[] IPropertyRMProvider.customAttributes { get { return null; } }
        Type IPropertyRMProvider.portType
        {
            get
            {
                return typeof(T);
            }
        }
        int IPropertyRMProvider.depth { get { return 0; } }
        bool IPropertyRMProvider.editable { get { return true; } }
        void IPropertyRMProvider.RetractPath()
        { }
        void IPropertyRMProvider.ExpandPath()
        { }


        void IPropertyRMProvider.StartLiveModification() { }
        void IPropertyRMProvider.EndLiveModification() { }
    }

    abstract class PropertyRM : VisualElement
    {
        public abstract void SetValue(object obj);
        public abstract object GetValue();
        public virtual void SetMultiplier(object obj) { }

        Clickable m_IconClickable;

        static Texture2D[] m_IconStates;

        public bool m_PropertyEnabled;

        public bool propertyEnabled
        {
            get { return m_PropertyEnabled; }

            set
            {
                m_PropertyEnabled = value;
                UpdateEnabled();
            }
        }
        public bool m_Indeterminate;

        public bool indeterminate
        {
            get { return m_Indeterminate; }

            set
            {
                m_Indeterminate = value;
                UpdateIndeterminate();
            }
        }

        public float labelWidth { get; private set; }
        public virtual bool isDelayed { get; set; }

        protected bool hasChangeDelayed { get; set; }


        public virtual bool IsCompatible(IPropertyRMProvider provider)
        {
            return GetType() == GetPropertyType(provider);
        }

        public const float depthOffset = 10;

        public float GetPreferredLabelWidth()
        {
            if (panel != null && hasLabel && this.Q<Label>() is { } label && (label.resolvedStyle.unityFontDefinition.fontAsset != null || label.resolvedStyle.unityFontDefinition.font != null))
            {
                return label.MeasureTextSize(label.text, -1, MeasureMode.Undefined, 11, MeasureMode.Exactly).x
                       + m_Provider.depth * depthOffset
                       + label.resolvedStyle.paddingLeft
                       + label.resolvedStyle.marginLeft
                       + (provider.spaceableAndMasterOfSpace ? 16f : 0f);
            }

            return 0;
        }

        private bool hasLabel => !string.IsNullOrEmpty(m_Provider.name);
        public abstract float GetPreferredControlWidth();

        public void SetLabelWidth(float width)
        {
            if (hasLabel && this.Q<Label>() is { } label)
            {
                label.style.width = width - m_Provider.depth * depthOffset + 4 - (provider.spaceableAndMasterOfSpace ? 16 : 0) ;
            }
        }

        protected abstract void UpdateEnabled();

        protected abstract void UpdateIndeterminate();

        protected void ValueDragFinished()
        {
            m_Provider.EndLiveModification();
            hasChangeDelayed = false;
            NotifyValueChanged();
        }

        protected void ValueDragStarted()
        {
            m_Provider.StartLiveModification();
        }

        public void ForceUpdate()
        {
            SetValue(m_Provider.value);
            UpdateGUI(true);
        }

        public IPropertyRMProvider provider
        {
            get { return m_Provider; }
        }

        public abstract void UpdateGUI(bool force);


        public void UpdateValue()
        {
            object value = m_Provider.value;
            SetValue(value);
        }

        public void Update()
        {
            Profiler.BeginSample("PropertyRM.Update");

            Profiler.BeginSample("PropertyRM.Update:Angle");
            if (m_Provider.attributes.Is(VFXPropertyAttributes.Type.Angle))
                SetMultiplier(Mathf.PI / 180.0f);
            Profiler.EndSample();


            Profiler.BeginSample("PropertyRM.Update:GetValue:");
            object value = m_Provider.value;
            Profiler.EndSample();
            Profiler.BeginSample("PropertyRM.Update:Regex");

            if (value != null)
            {
                string regex = m_Provider.attributes.ApplyRegex(value);
                if (regex != null)
                    value = m_Provider.value = regex;
            }
            Profiler.EndSample();

            UpdateExpandable();

            Profiler.BeginSample("PropertyRM.Update:SetValue");

            SetValue(value);

            Profiler.EndSample();


            Profiler.BeginSample("PropertyRM.Update:Name");

            if (hasLabel && this.Q<Label>() is { } label)
            {
                var labelText = ObjectNames.NicifyVariableName(m_Provider.name);
                string labelTooltip = null;
                m_Provider.attributes.ApplyToGUI(ref labelText, ref labelTooltip);
                label.text = labelText;
                label.tooltip = labelTooltip;
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

        void UpdateExpandable()
        {
            if (IsExpandable())
            {
                AddToClassList("expandable");
                if (m_Provider.expanded)
                {
                    AddToClassList("icon-expanded");
                }
                else
                {
                    RemoveFromClassList("icon-expanded");
                }
            }
            else
            {
                RemoveFromClassList("expandable");
            }
        }

        public PropertyRM(IPropertyRMProvider provider, float labelWidth)
        {
            this.AddStyleSheetPathWithSkinVariant("VFXControls");
            this.AddStyleSheetPathWithSkinVariant("PropertyRM");

            m_Provider = provider;
            this.labelWidth = labelWidth;
            isDelayed = m_Provider.attributes.Is(VFXPropertyAttributes.Type.Delayed);

            if (provider.attributes.Is(VFXPropertyAttributes.Type.Angle))
                SetMultiplier(Mathf.PI / 180.0f);

            string labelText = provider.name;
            string labelTooltip = null;
            provider.attributes.ApplyToGUI(ref labelText, ref labelTooltip);

            if (provider.depth != 0)
            {
                AddToClassList("hasDepth");
                style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Left, 9 + (provider.depth - 1) * 14 ));
                style.paddingLeft = 0;
                style.marginLeft = 5;
                for (int i = 1; i < provider.depth; ++i)
                {
                    VisualElement line = new VisualElement();
                    line.style.width = 1;
                    line.name = "line";
                    line.style.marginLeft =  13;

                    Add(line);
                }
            }

            if (IsExpandable())
            {
                m_IconClickable = new Clickable(OnExpand);
                this.AddManipulator(m_IconClickable);
            }

            AddToClassList("propertyrm");
            RegisterCallback<MouseDownEvent>(OnCatchMouse);

        }

        bool IsExpandable() => m_Provider.expandable && (m_Provider.expandableIfShowsEverything || !showsEverything);

        void OnCatchMouse(MouseDownEvent e)
        {
            var node = GetFirstAncestorOfType<VFXNodeUI>();
            if (node != null)
            {
                node.OnSelectionMouseDown(e);
            }
            e.StopPropagation();
        }

        static readonly Dictionary<Type, Type> m_TypeDictionary = new Dictionary<Type, Type>
        {
            {typeof(Vector), typeof(VectorPropertyRM)},
            {typeof(Position), typeof(PositionPropertyRM)},
            {typeof(DirectionType), typeof(DirectionPropertyRM)},
            {typeof(bool), typeof(BoolPropertyRM)},
            {typeof(float), typeof(FloatPropertyRM)},
            {typeof(int), typeof(IntPropertyRM)},
            {typeof(uint), typeof(UintPropertyRM)},
            {typeof(FlipBook), typeof(FlipBookPropertyRM)},
            {typeof(Vector2), typeof(Vector2PropertyRM)},
            {typeof(Vector3), typeof(Vector3PropertyRM)},
            {typeof(Vector4), typeof(Vector4PropertyRM)},
            {typeof(Matrix4x4), typeof(Matrix4x4PropertyRM)},
            {typeof(Color), typeof(ColorPropertyRM)},
            {typeof(Gradient), typeof(GradientPropertyRM)},
            {typeof(AnimationCurve), typeof(CurvePropertyRM)},
            {typeof(Object), typeof(ObjectPropertyRM)},
            {typeof(string), typeof(StringPropertyRM)}
        };

        static Type GetPropertyType(IPropertyRMProvider controller)
        {
            Type propertyType = null;
            Type type = controller.portType;

            if (type != null)
            {
                if (controller.customAttributes?.SingleOrDefault(x => x is VFXSettingFieldTypeAttribute) is VFXSettingFieldTypeAttribute fieldTypeAttribute)
                {
                    propertyType = fieldTypeAttribute.type;
                }
                else if (type.IsEnum)
                {
                    propertyType = typeof(EnumPropertyRM);
                }
                else if (controller.spaceableAndMasterOfSpace)
                {
                    if (!m_TypeDictionary.TryGetValue(type, out propertyType))
                    {
                        propertyType = typeof(SpaceablePropertyRM<object>);
                    }
                }
                else
                {
                    while (type != typeof(object) && type != null)
                    {
                        if (!m_TypeDictionary.TryGetValue(type, out propertyType))
                        {
                            /*foreach (var inter in type.GetInterfaces())
                            {
                                if (m_TypeDictionary.TryGetValue(inter, out propertyType))
                                {
                                    break;
                                }
                            }*/
                        }
                        if (propertyType != null)
                        {
                            break;
                        }
                        type = type.BaseType;
                    }
                }
            }
            if (propertyType == null)
            {
                propertyType = typeof(EmptyPropertyRM);
            }

            return propertyType;
        }

        public static PropertyRM Create(IPropertyRMProvider controller, float labelWidth)
        {
            Type propertyType = GetPropertyType(controller);


            Profiler.BeginSample(propertyType.Name + ".CreateInstance");
            PropertyRM result = System.Activator.CreateInstance(propertyType, new object[] { controller, labelWidth }) as PropertyRM;
            Profiler.EndSample();

            return result;
        }

        public virtual object FilterValue(object value)
        {
            return value;
        }

        protected void NotifyValueChanged()
        {
            object value = GetValue();
            value = FilterValue(value);
            m_Provider.value = value;
            hasChangeDelayed = false;
        }

        void OnExpand(EventBase evt)
        {
            // Allow expand/collapse on when clicking over the arrow icon (which can be embedded in the label's background)
            if (evt is PointerUpEvent pointerUpEvent)
            {
                var label = this.Q<Label>();
                if (label != null)
                {
                    if (provider.depth > 0)
                    {
                        if (pointerUpEvent.localPosition.x > label.layout.x + 20)
                            return;
                    }
                    else if (pointerUpEvent.localPosition.x > 20)
                    {
                        return;
                    }
                }
            }

            if (m_Provider.expanded)
            {
                m_Provider.RetractPath();
            }
            else
            {
                m_Provider.ExpandPath();
            }
        }

        protected IPropertyRMProvider m_Provider;

        public abstract bool showsEverything { get; }
    }

    abstract class PropertyRM<T> : PropertyRM
    {
        public PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        { }
        public override void SetValue(object obj)
        {
            if (obj != null)
            {
                if (m_Provider.portType == typeof(Transform) && obj is Matrix4x4)
                {
                    // do nothing
                }
                else
                {
                    try
                    {
                        m_Value = (T)obj;
                    }
                    catch (System.Exception)
                    {
                        Debug.Log("Error Trying to convert" + (obj != null ? obj.GetType().Name : "null") + " to " + typeof(T).Name);
                    }
                }
            }

            UpdateGUI(false);
        }

        public override object GetValue()
        {
            return m_Value;
        }

        protected T m_Value;
    }

    abstract class SimplePropertyRM<T> : PropertyRM<T>
    {
        public abstract ValueControl<T> CreateField();

        public SimplePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Field = CreateField();
            m_Field.AddToClassList("fieldContainer");
            m_Field.OnValueChanged += OnValueChanged;
            Add(m_Field);
        }

        public void OnValueChanged()
        {
            T newValue = m_Field.GetValue();
            if (!newValue.Equals(m_Value))
            {
                m_Value = newValue;
                if (!isDelayed)
                    NotifyValueChanged();
                else
                    hasChangeDelayed = true;
            }
        }

        protected override void UpdateEnabled()
        {
            m_Field.SetEnabled(propertyEnabled);
        }

        protected ValueControl<T> m_Field;
        public override void UpdateGUI(bool force)
        {
            m_Field.SetValue(m_Value);
        }

        public override bool showsEverything { get { return true; } }

        public override void SetMultiplier(object obj)
        {
            try
            {
                m_Field.SetMultiplier((T)obj);
            }
            catch (System.Exception)
            {
            }
        }
    }


    abstract class SimpleUIPropertyRM<T, U> : PropertyRM<T>
    {
        public abstract INotifyValueChanged<U> CreateField();

        public SimpleUIPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Field = CreateField();
            isDelayed = m_Provider.attributes.Is(VFXPropertyAttributes.Type.Delayed);

            VisualElement fieldElement = m_Field as VisualElement;
            fieldElement.AddToClassList("fieldContainer");
            fieldElement.RegisterCallback<ChangeEvent<U>>(OnValueChanged);

            Add(fieldElement);
            SetLabelWidth(labelWidth);
        }

        public virtual T Convert(object value)
        {
            return (T)System.Convert.ChangeType(m_Field.value, typeof(T));
        }

        public void OnValueChanged(ChangeEvent<U> e)
        {
            try
            {
                T newValue = Convert(m_Field.value);
                if (!newValue.Equals(m_Value))
                {
                    m_Value = newValue;
                    if (!isDelayed)
                        NotifyValueChanged();
                    else
                        hasChangeDelayed = true;
                }
                else
                {
                    UpdateGUI(false);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Catching exception to not break graph in OnValueChanged" + ex.Message);
            }
        }

        protected override void UpdateEnabled()
        {
            ((VisualElement)m_Field).SetEnabled(propertyEnabled);
        }

        INotifyValueChanged<U> m_Field;


        protected INotifyValueChanged<U> field
        {
            get { return m_Field; }
        }

        protected virtual bool HasFocus() { return false; }
        public override void UpdateGUI(bool force)
        {
            if (!HasFocus() || force)
            {
                try
                {
                    var value = (U)System.Convert.ChangeType(m_Value, typeof(U));
                    if (m_Field is IVFXNotifyValueChanged<U> vfxNotifyValueChanged)
                        vfxNotifyValueChanged.SetValueWithoutNotify(value, force);
                    else
                        m_Field.value = value;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Catching exception to not break graph in UpdateGUI" + ex.Message);
                }
            }
        }

        public override bool showsEverything { get { return true; } }
    }

    abstract class SimpleVFXUIPropertyRM<T, U> : SimpleUIPropertyRM<U, U> where T : VFXControl<U>
    {
        public SimpleVFXUIPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected IVFXControl fieldControl => (IVFXControl)field;

        protected override void UpdateIndeterminate()
        {
            fieldControl.indeterminate = indeterminate;
        }

        protected override void UpdateEnabled()
        {
            fieldControl.SetEnabled(propertyEnabled);
        }

        public override void UpdateGUI(bool force)
        {
            base.UpdateGUI(force);
            if (force)
                fieldControl.ForceUpdate();
        }
    }

    class EmptyPropertyRM : PropertyRM
    {
        public override float GetPreferredControlWidth()
        {
            return 0;
        }

        public override void SetValue(object obj)
        {
        }

        public override object GetValue()
        {
            return null;
        }

        protected override void UpdateEnabled()
        {
        }

        protected override void UpdateIndeterminate()
        {
        }

        public EmptyPropertyRM(IPropertyRMProvider provider, float labelWidth) : base(provider, labelWidth)
        {
            var label = new Label(ObjectNames.NicifyVariableName(provider.name));
            label.AddToClassList("label");
            Add(label);
        }

        public override void UpdateGUI(bool force)
        {
        }

        public override bool showsEverything { get { return false; } }
    }
}
