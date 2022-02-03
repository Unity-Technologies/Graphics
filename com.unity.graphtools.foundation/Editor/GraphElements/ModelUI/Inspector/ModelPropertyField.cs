using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using TextField = UnityEngine.UIElements.TextField;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Class to display a UI to edit a property on a <see cref="IGraphElementModel"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to display.</typeparam>
    public class ModelPropertyField<TValue> : BaseModelPropertyField
    {
        ICustomPropertyField<TValue> m_CustomField;

        /// <summary>
        /// The root element of the UI.
        /// </summary>
        protected VisualElement m_Field;

        /// <summary>
        /// A function to get the current value of the property.
        /// </summary>
        protected Func<TValue> m_ValueGetter;

        /// <summary>
        /// The model that owns the property.
        /// </summary>
        public IGraphElementModel Model { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelPropertyField{TValue}"/> class.
        /// </summary>
        /// <param name="commandTargetView">The view hosting this field.</param>
        /// <param name="model">The model that owns the field.</param>
        /// <param name="propertyName">The name of the property to edit.</param>
        /// <param name="label">The label for the field. If null, the <paramref name="propertyName"/> will be used.</param>
        /// <param name="onValueChanged">The action to execute when the value is edited. If null, nothing will be done.</param>
        /// <param name="valueGetter">A function used to retrieve the current property value on <paramref name="model"/>. If null, the property getter will be used.</param>
        public ModelPropertyField(
            ICommandTarget commandTargetView,
            IGraphElementModel model,
            string propertyName,
            string label,
            Action<TValue, ModelPropertyField<TValue>> onValueChanged = null,
            Func<IGraphElementModel, TValue> valueGetter = null)
            : base(commandTargetView, label ?? ObjectNames.NicifyVariableName(propertyName))
        {
            Model = model;
            m_ValueGetter = valueGetter != null ? () => valueGetter(Model) : MakePropertyValueGetter(Model, propertyName);

            m_Field = CreateFieldFromProperty(propertyName);

            if (onValueChanged != null)
            {
                switch (m_Field)
                {
                    case PopupField<string> _:
                        m_Field.RegisterCallback<ChangeEvent<string>, ModelPropertyField<TValue>>(
                            (e, f) =>
                            {
                                var newValue = (TValue)Enum.Parse(typeof(TValue), e.newValue);
                                onValueChanged(newValue, f);
                            }, this);
                        break;

                    default:
                        m_Field.RegisterCallback<ChangeEvent<TValue>, ModelPropertyField<TValue>>(
                            (e, f) => onValueChanged(e.newValue, f), this);
                        break;
                }
            }

            if (m_Field != null)
                hierarchy.Add(m_Field);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelPropertyField{TValue}"/> class.
        /// </summary>
        /// <param name="commandTargetView">The view hosting this field.</param>
        /// <param name="model">The model that owns the field.</param>
        /// <param name="propertyName">The name of the property to edit.</param>
        /// <param name="label">The label for the field. If null, the <paramref name="propertyName"/> will be used.</param>
        /// <param name="commandType">The command type to dispatch when the value is edited. The type should implement <see cref="ICommand"/> and have a constructor that accepts a <typeparamref name="TValue"/> and a <see cref="IGraphElementModel"/>.</param>
        /// <param name="valueGetter">A function used to retrieve the current property value on <paramref name="model"/>. If null, the property getter will be used.</param>
        public ModelPropertyField(
            ICommandTarget commandTargetView,
            IGraphElementModel model,
            string propertyName,
            string label,
            Type commandType,
            Func<IGraphElementModel, TValue> valueGetter = null)
            : base(commandTargetView, label ?? ObjectNames.NicifyVariableName(propertyName))
        {
            Model = model;
            m_ValueGetter = valueGetter != null ? () => valueGetter(Model) : MakePropertyValueGetter(Model, propertyName);

            m_Field = CreateFieldFromProperty(propertyName);

            if (commandType != null)
            {
                switch (m_Field)
                {
                    case PopupField<string> _:
                        m_Field.RegisterCallback<ChangeEvent<string>, ModelPropertyField<TValue>>((e, f) =>
                        {
                            var value = (TValue)Enum.Parse(typeof(TValue), e.newValue);
                            if (Activator.CreateInstance(commandType, value, f.Model) is ICommand command)
                                f.CommandTargetView.Dispatch(command);
                        }, this);
                        break;

                    default:
                        m_Field.RegisterCallback<ChangeEvent<TValue>, ModelPropertyField<TValue>>((e, f) =>
                        {
                            if (Activator.CreateInstance(commandType, e.newValue, f.Model) is ICommand command)
                                f.CommandTargetView.Dispatch(command);
                        }, this);
                        break;
                }
            }

            if (m_Field != null)
                hierarchy.Add(m_Field);
        }

        static Func<TValue> MakePropertyValueGetter(IGraphElementModel model, string propertyName)
        {
            var getterInfo = model?.GetType().GetProperty(propertyName)?.GetGetMethod();
            if (getterInfo != null)
            {
                Debug.Assert(typeof(TValue) == getterInfo.ReturnType);
                var del = Delegate.CreateDelegate(typeof(Func<TValue>), model, getterInfo);
                return del as Func<TValue>;
            }

            return null;
        }

        /// <inheritdoc />
        public override void UpdateDisplayedValue()
        {
            if (m_ValueGetter != null)
            {
                if (m_CustomField != null)
                {
                    m_CustomField.UpdateDisplayedValue(m_ValueGetter.Invoke());
                }
                else
                {
                    switch (m_Field)
                    {
                        case BaseField<TValue> baseField:
                            baseField.SetValueWithoutNotify(m_ValueGetter.Invoke());
                            break;

                        case PopupField<string> popupField:
                            popupField.SetValueWithoutNotify(m_ValueGetter.Invoke().ToString());
                            break;
                    }
                }
            }
        }

        VisualElement CreateFieldFromProperty(string propertyName)
        {
            // PF TODO Eventually, add support for nested properties, arrays and Enum Flags.

            var propertyType = typeof(TValue);

            if (TryGetCustomPropertyField(out m_CustomField))
            {
                return m_CustomField.Build(CommandTargetView, Label, Model, propertyName);
            }

            //if (EditorGUI.HasVisibleChildFields())
            //    return CreateFoldout();

            //m_ChildrenContainer = null;

            if (propertyType == typeof(long))
            {
                return ConfigureField(new LongField());
            }
            if (propertyType == typeof(int))
            {
                return ConfigureField(new IntegerField());
            }
            if (propertyType == typeof(bool))
            {
                return ConfigureField(new Toggle());
            }
            if (propertyType == typeof(float))
            {
                return ConfigureField(new FloatField());
            }
            if (propertyType == typeof(string))
            {
                return ConfigureField(new TextField());
            }
            if (propertyType == typeof(Color))
            {
                return ConfigureField(new ColorField());
            }
            if (typeof(Object).IsAssignableFrom(propertyType))
            {
                var field = new ObjectField { objectType = propertyType };
                return ConfigureField(field);
            }
            if (propertyType == typeof(LayerMask))
            {
                return ConfigureField(new LayerMaskField());
            }
            if (typeof(Enum).IsAssignableFrom(propertyType))
            {
                /*if (propertyType.IsDefined(typeof(FlagsAttribute), false))
                {
                    var field = new EnumFlagsField();
                    return ConfigureField(field);
                }
                else*/
                {
                    var field = new PopupField<string>(Enum.GetNames(propertyType).ToList(), 0);
                    return ConfigureField(field);
                }
            }
            if (propertyType == typeof(Vector2))
            {
                return ConfigureField(new Vector2Field());
            }
            if (propertyType == typeof(Vector3))
            {
                return ConfigureField(new Vector3Field());
            }
            if (propertyType == typeof(Vector4))
            {
                return ConfigureField(new Vector4Field());
            }
            if (propertyType == typeof(Rect))
            {
                return ConfigureField(new RectField());
            }/*
            if (propertyType is SerializedPropertyType.ArraySize)
            {
                var field = new IntegerField();
                field.SetValueWithoutNotify(property.intValue); // This avoids the OnValueChanged/Rebind feedback loop.
                field.isDelayed = true; // To match IMGUI. Also, focus is lost anyway due to the rebind.
                field.RegisterValueChangedCallback((e) => { UpdateArrayFoldout(e, this, m_ParentPropertyField); });
                return ConfigureField<IntegerField, int>(field, property);
            }*/
            if (propertyType == typeof(char))
            {
                var field = new TextField();
                field.maxLength = 1;
                return ConfigureField(field);
            }
            if (propertyType == typeof(AnimationCurve))
            {
                return ConfigureField(new CurveField());
            }
            if (propertyType == typeof(Bounds))
            {
                return ConfigureField(new BoundsField());
            }
            if (propertyType == typeof(Gradient))
            {
                return ConfigureField(new GradientField());
            }
            if (propertyType == typeof(Vector2Int))
            {
                return ConfigureField(new Vector2IntField());
            }
            if (propertyType == typeof(Vector3Int))
            {
                return ConfigureField(new Vector3IntField());
            }
            if (propertyType == typeof(RectInt))
            {
                return ConfigureField(new RectIntField());
            }
            if (propertyType == typeof(BoundsInt))
            {
                return ConfigureField(new BoundsIntField());
            }

            return null;
        }
    }
}
