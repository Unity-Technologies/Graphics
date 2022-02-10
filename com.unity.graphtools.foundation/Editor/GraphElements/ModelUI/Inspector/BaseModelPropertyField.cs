using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class to display a UI to edit a property or field on a <see cref="IGraphElementModel"/>.
    /// </summary>
    public abstract class BaseModelPropertyField : VisualElement
    {
        static CustomStyleProperty<float> s_LabelWidthRatioProperty = new CustomStyleProperty<float>("--unity-property-field-label-width-ratio");
        static CustomStyleProperty<float> s_LabelExtraPaddingProperty = new CustomStyleProperty<float>("--unity-property-field-label-extra-padding");

        public static readonly string ussClassName = "ge-model-property-field";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string inputUssClassName = ussClassName + "__input";

        static Dictionary<Type, Type> s_CustomPropertyFields;

        /// <summary>
        /// Tries to get an instance of a custom property field provided by an implementation of <see cref="ICustomPropertyField{T}"/>.
        /// </summary>
        /// <param name="customPropertyField">On exit, the custom property field instance.</param>
        /// <typeparam name="T">The type for which to get the custom property field.</typeparam>
        /// <returns>True if a custom property field was found, false otherwise.</returns>
        protected static bool TryGetCustomPropertyField<T>(out ICustomPropertyField<T> customPropertyField)
        {
            if (TryGetCustomPropertyField(typeof(T), out var customPropertyDrawerNonTyped))
            {
                customPropertyField = customPropertyDrawerNonTyped as ICustomPropertyField<T>;
                return true;
            }

            customPropertyField = null;
            return false;
        }

        /// <summary>
        /// Tries to get an instance of a custom property field provided by an implementation of <see cref="ICustomPropertyField{T}"/>.
        /// </summary>
        /// <param name="propertyType">The type for which to get the custom property field.</param>
        /// <param name="customPropertyField">On exit, the custom property field instance.</param>
        /// <returns>True if a custom property field was found, false otherwise.</returns>
        protected static bool TryGetCustomPropertyField(Type propertyType, out ICustomPropertyField customPropertyField)
        {
            if (s_CustomPropertyFields == null)
            {
                s_CustomPropertyFields = new Dictionary<Type, Type>();

                var customPropertyDrawerTypes = TypeCache.GetTypesDerivedFrom<ICustomPropertyField>();
                foreach (var customPropertyDrawerType in customPropertyDrawerTypes)
                {
                    // We only want non-generic non-abstract types that derive from a generic type.
                    if (customPropertyDrawerType.IsGenericType ||
                        customPropertyDrawerType.IsAbstract ||
                        customPropertyDrawerType.BaseType == null ||
                        customPropertyDrawerType.BaseType.GenericTypeArguments.Length == 0)
                        continue;

                    var typeParam = customPropertyDrawerType.BaseType.GenericTypeArguments[0];
                    s_CustomPropertyFields.Add(typeParam, customPropertyDrawerType);
                }
            }

            if (s_CustomPropertyFields.TryGetValue(propertyType, out var propertyDrawerType))
            {
                customPropertyField = Activator.CreateInstance(propertyDrawerType) as ICustomPropertyField;
                return true;
            }

            customPropertyField = null;
            return false;
        }

        float m_LabelWidthRatio;
        float m_LabelExtraPadding;

        /// <summary>
        /// The label for the field.
        /// </summary>
        protected string Label { get; }

        /// <summary>
        ///  The command dispatcher.
        /// </summary>
        public ICommandTarget CommandTargetView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModelPropertyField"/> class.
        /// </summary>
        /// <param name="commandTargetView">The view hosting this field.</param>
        /// <param name="label">The label for the field.</param>
        protected BaseModelPropertyField(ICommandTarget commandTargetView, string label)
        {
            CommandTargetView = commandTargetView;
            Label = label ?? "";

            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Update the value displayed by the custom UI.
        /// </summary>
        public abstract void UpdateDisplayedValue();

        /// <summary>
        /// Configures the field so it is displayed correctly.
        /// </summary>
        /// <param name="field">The field to configure.</param>
        /// <typeparam name="TFieldValue">The type of value displayed by the field.</typeparam>
        /// <returns>The field.</returns>
        // Stolen from PropertyField.
        protected VisualElement ConfigureField<TFieldValue>(BaseField<TFieldValue> field)
        {
            field.label = Label ?? "";

            field.labelElement.AddToClassList(labelUssClassName);
            field.SafeQ(null, BaseField<TFieldValue>.inputUssClassName).AddToClassList(inputUssClassName);

            // Style this like a PropertyField
            AddToClassList(PropertyField.ussClassName);
            field.labelElement.AddToClassList(PropertyField.labelUssClassName);
            field.SafeQ(null, BaseField<TFieldValue>.inputUssClassName).AddToClassList(PropertyField.inputUssClassName);

            // These default values are based off IMGUI
            m_LabelWidthRatio = 0.45f;
            m_LabelExtraPadding = 2.0f;

            field.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            if (!(parent is ModelInspector inspectorElement))
                return field;

            field.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                // Calculate all extra padding from the containing element's contents
                var totalPadding = resolvedStyle.paddingLeft + resolvedStyle.paddingRight +
                    resolvedStyle.marginLeft + resolvedStyle.marginRight;

                // Get inspector element padding next
                totalPadding += inspectorElement.resolvedStyle.paddingLeft +
                    inspectorElement.resolvedStyle.paddingRight +
                    inspectorElement.resolvedStyle.marginLeft +
                    inspectorElement.resolvedStyle.marginRight;

                var labelElement = field.labelElement;

                // Then get label padding
                totalPadding += labelElement.resolvedStyle.paddingLeft + labelElement.resolvedStyle.paddingRight +
                    labelElement.resolvedStyle.marginLeft + labelElement.resolvedStyle.marginRight;

                // Then get base field padding
                totalPadding += field.resolvedStyle.paddingLeft + field.resolvedStyle.paddingRight +
                    field.resolvedStyle.marginLeft + field.resolvedStyle.marginRight;

                // Not all visual input controls have the same padding so we can't base our total padding on
                // that information.  Instead we add a flat value to totalPadding to best match the hard coded
                // calculation in IMGUI
                totalPadding += m_LabelExtraPadding;

                // Formula to follow IMGUI label width settings
                var newWidth = resolvedStyle.width * m_LabelWidthRatio - totalPadding;
                if (Mathf.Abs(labelElement.resolvedStyle.width - newWidth) > Mathf.Epsilon)
                    labelElement.style.width = newWidth;
            });

            return field;
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_LabelWidthRatioProperty, out var labelWidthRatio))
            {
                m_LabelWidthRatio = labelWidthRatio;
            }

            if (evt.customStyle.TryGetValue(s_LabelExtraPaddingProperty, out var labelExtraPadding))
            {
                m_LabelExtraPadding = labelExtraPadding;
            }
        }
    }
}
