#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assert = UnityEngine.Assertions.Assert;

#if ENABLE_RENDERING_DEBUGGER_UI
using UnityEngine.UIElements;
#endif

namespace UnityEngine.Rendering
{
    internal interface ISupportsLegacyStateHandling
    {
        bool RequiresLegacyStateHandling();
    }

    public partial class DebugUI
    {
        /// <summary>
        /// Generic field.
        /// </summary>
        /// <example>
        /// <code>
        /// public class CustomRectField : DebugUI.Field&lt;Rect&gt;
        /// {
        ///     protected override VisualElement Create()
        ///     {
        ///         var field = new RectField()
        ///         {
        ///             label = displayName,
        ///         };
        ///         return field;
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of data managed by the field.</typeparam>
        public abstract class Field<T> : Widget
#pragma warning disable CS0618 // Type or member is obsolete
            , IValueField
#pragma warning restore CS0618 // Type or member is obsolete
            , ISupportsLegacyStateHandling
        {
            /// <summary>
            /// Getter for this field.
            /// </summary>
            public Func<T> getter { get; set; }
            /// <summary>
            /// Setter for this field.
            /// </summary>
            public Action<T> setter { get; set; }

            // This should be an `event` but they don't play nice with object initializers in the
            // version of C# we use.
            /// <summary>
            /// Callback used when the value of the field changes.
            /// </summary>
            public Action<Field<T>, T> onValueChanged;

            /// <summary>
            /// Function used to validate the value when updating the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            /// <returns>Validated value.</returns>
            object IValueField.ValidateValue(object value)
            {
                return ValidateValue((T)value);
            }

            /// <summary>
            /// Function used to validate the value when updating the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            /// <returns>Validated value.</returns>
            public virtual T ValidateValue(T value)
            {
                return value;
            }

            /// <summary>
            /// Get the value of the field.
            /// </summary>
            /// <returns>Value of the field.</returns>
            object IValueField.GetValue()
            {
                return GetValue();
            }

            /// <summary>
            /// Get the value of the field.
            /// </summary>
            /// <returns>Value of the field.</returns>
            public T GetValue()
            {
                Assert.IsNotNull(getter);
                return getter();
            }

            /// <summary>
            /// Set the value of the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            public void SetValue(object value)
            {
                SetValue((T)value);
            }

            /// <summary>
            /// Set the value of the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            public virtual void SetValue(T value)
            {
                if (setter == null)
                    return;

                var v = ValidateValue(value);

                if (v == null || !v.Equals(getter()))
                {
#if UNITY_EDITOR
                    T previousValue = GetValue();
                    onWidgetValueChangedAnalytic?.Invoke(queryPath, previousValue, v);
#endif

                    setter(v);
                    onValueChanged?.Invoke(this, v);
                }
            }

            internal static Action<string, T, T> onWidgetValueChangedAnalytic;

            // In order to support the legacy DebugState system, we are inspecting the closure of the `getter` lambda, to see if the captured
            // data is using the ISerializedDebugDisplaySettings interface. We know that any data that uses the new interface does not need
            // legacy state handling. This is a temporary solution until we fully migrate to the new system and remove DebugState.
            bool ISupportsLegacyStateHandling.RequiresLegacyStateHandling()
            {
                bool FieldsHaveISerializedDebugDisplaySettings(object obj)
                {
                    if (obj == null)
                        return false;

                    var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var field in fields)
                    {
                        var value = field.GetValue(obj);
                        if (value is ISerializedDebugDisplaySettings)
                            return true;
                    }
                    return false;
                }

                var getterClosure = getter.Target;
                if (getterClosure != null)
                {
                    bool foundISerializedDebugDisplaySettings = FieldsHaveISerializedDebugDisplaySettings(getterClosure);
                    return !foundISerializedDebugDisplaySettings;
                }
                return false;
            }
        }

        /// <summary>
        /// Boolean field.
        /// </summary>
        public class BoolField : Field<bool>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var toggle = new UIElements.Toggle();
                BaseFieldHelper.ConfigureBaseField(this, toggle);
                return toggle;
            }
#endif
        }

        /// <summary>
        /// An array of checkboxes that Unity displays in a horizontal row.
        /// </summary>
        public class HistoryBoolField : BoolField
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var valueContainer = new UIElements.VisualElement();
                valueContainer.AddToClassList("debug-window-historyboolfield");
                valueContainer.Add(new Label(displayName)
                {
                    style = { width = ValueTuple.GetLabelWidth(m_Context) }
                });

                var boolField = new DebugUI.BoolField()
                {
                    displayName = string.Empty,
                    tooltip = tooltip,
                    getter = getter,
                    setter = setter,
                };
                childWidgets.Add(boolField);
                valueContainer.Add(boolField.ToVisualElement(m_Context));

                foreach (var value in historyGetter)
                {
                    var historyBoolField = new DebugUI.BoolField()
                    {
                        displayName = string.Empty,
                        getter = value
                    };
                    childWidgets.Add(historyBoolField);

                    var field = historyBoolField.ToVisualElement(m_Context);
                    field.SetEnabled(false);
                    valueContainer.Add(field);
                }

                valueContainer.AddToClassList(UIElements.BaseField<bool>.alignedFieldUssClassName);

                return valueContainer;
            }
#endif
            internal List<Widget> childWidgets { private set; get; } = new List<Widget>();

            /// <summary>
            /// History getter for this field.
            /// </summary>
            public Func<bool>[] historyGetter { get; set; }
            /// <summary>
            /// Depth of the field's history.
            /// </summary>
            public int historyDepth => historyGetter?.Length ?? 0;
            /// <summary>
            /// Get the value of the field at a certain history index.
            /// </summary>
            /// <param name="historyIndex">Index of the history to query.</param>
            /// <returns>Value of the field at the provided history index.</returns>
            public bool GetHistoryValue(int historyIndex)
            {
                Assert.IsNotNull(historyGetter);
                Assert.IsTrue(historyIndex >= 0 && historyIndex < historyGetter.Length, "out of range historyIndex");
                Assert.IsNotNull(historyGetter[historyIndex]);
                return historyGetter[historyIndex]();
            }
        }

        /// <summary>
        /// A slider for an integer.
        /// </summary>
        public class IntField : Field<int>
        {
            /// <summary>
            /// Minimum value function.
            /// </summary>
            public Func<int> min;
            /// <summary>
            /// Maximum value function.
            /// </summary>
            public Func<int> max;

            // Runtime-only
            /// <summary>
            /// Step increment.
            /// </summary>
            public int incStep = 1;
            /// <summary>
            /// Step increment multiplier.
            /// </summary>
            [Obsolete("Use incStepMult instead #from(6000.5) (UnityUpgradable) -> incStepMult")]
            public int intStepMult = 10;
            /// <summary>
            /// Step increment multiplier.
            /// </summary>
            public int incStepMult = 10;

            /// <summary>
            /// Function used to validate the value when updating the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            /// <returns>Validated value.</returns>
            public override int ValidateValue(int value)
            {
                if (min != null) value = Mathf.Max(value, min());
                if (max != null) value = Mathf.Min(value, max());
                return value;
            }

            internal override void OnDecrement(bool fast)
            {
                int currentValue = GetValue();
                int step = fast ? incStepMult : incStep;
                int minValue = min != null ? min() : int.MinValue;

                // Check if subtraction would cause overflow, set to max value instead of wrapping around
                int newValue = currentValue >= minValue + step ? currentValue - step : minValue;
                SetValue(newValue);
            }

            internal override void OnIncrement(bool fast)
            {
                int currentValue = GetValue();
                int step = fast ? incStepMult : incStep;
                int maxValue = max != null ? max() : int.MaxValue;

                // Check if addition would cause overflow, set to max value instead of wrapping around
                int newValue = currentValue <= maxValue-step ? currentValue + step : maxValue;
                SetValue(newValue);
            }

#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                if (m_Context.IsAnyRuntimeContext())
                {
                    var field = new UIElements.IntegerField();
                    DebugUIStepperHelper.AddStepper(
                        field,
                        SetValue,
                        GetValue,
                        onDecrement: OnDecrement,
                        onIncrement: OnIncrement
                    );
                    BaseFieldHelper.ConfigureBaseField(this, field);
                    return field;
                }
                if (min != null || max != null)
                {
                    var field = new UIElements.SliderInt(min?.Invoke() ?? int.MinValue, max?.Invoke() ?? int.MaxValue);
                    BaseFieldHelper.ConfigureBaseField(this, field);
                    field.showInputField = true;
                    return field;
                }
                else
                {
                    var field = new UIElements.IntegerField();
                    BaseFieldHelper.ConfigureBaseField(this, field);
                    return field;
                }
            }
#endif
        }

        /// <summary>
        /// A slider for a positive integer.
        /// </summary>
        public class UIntField : Field<uint>
        {
            /// <summary>
            /// Minimum value function.
            /// </summary>
            public Func<uint> min;
            /// <summary>
            /// Maximum value function.
            /// </summary>
            public Func<uint> max;

            // Runtime-only
            /// <summary>
            /// Step increment.
            /// </summary>
            public uint incStep = 1u;
            /// <summary>
            /// Step increment multiplier.
            /// </summary>
            [Obsolete("Use incStepMult instead #from(6000.5) (UnityUpgradable) -> incStepMult")]
            public uint intStepMult = 10u;
            /// <summary>
            /// Step increment multiplier.
            /// </summary>
            public uint incStepMult = 10u;

            /// <summary>
            /// Function used to validate the value when updating the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            /// <returns>Validated value.</returns>
            public override uint ValidateValue(uint value)
            {
                if (min != null) value = (uint)Mathf.Max(value, min());
                if (max != null) value = (uint)Mathf.Min(value, max());
                return value;
            }

            internal override void OnDecrement(bool fast)
            {
                uint currentValue = GetValue();
                uint step = fast ? incStepMult : incStep;
                uint minValue = min != null ? min() : uint.MinValue;

                // Check if subtraction would cause overflow, set to max value instead of wrapping around
                uint newValue = currentValue >= minValue + step ? currentValue - step : minValue;
                SetValue(newValue);
            }

            internal override void OnIncrement(bool fast)
            {
                uint currentValue = GetValue();
                uint step = fast ? incStepMult : incStep;
                uint maxValue = max != null ? max() : uint.MaxValue;

                // Check if addition would cause overflow, set to max value instead of wrapping around
                uint newValue = currentValue <= maxValue-step ? currentValue + step : maxValue;
                SetValue(newValue);
            }

#if ENABLE_RENDERING_DEBUGGER_UI

            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var field = new UIElements.UnsignedIntegerField();
                if (m_Context.IsAnyRuntimeContext())
                {
                    field.RegisterCallback<FocusOutEvent>(evt =>
                    {
                        var validatedValue = ValidateValue(field.value);
                        if (validatedValue != field.value)
                        {
                            field.SetValueWithoutNotify(validatedValue);
                            SetValue(validatedValue);
                        }
                    });
                    DebugUIStepperHelper.AddStepper(
                        field,
                        SetValue,
                        GetValue,
                        onDecrement: OnDecrement,
                        onIncrement: OnIncrement
                    );
                }
                BaseFieldHelper.ConfigureBaseField(this, field);
                field.RegisterCallback<ChangeEvent<uint>>((evt) =>
                {
                    field.SetValueWithoutNotify(ValidateValue(evt.newValue));
                });
                return field;
            }
#endif
        }

        /// <summary>
        /// A slider for a float.
        /// </summary>
        public class FloatField : Field<float>
        {
            /// <summary>
            /// Minimum value function.
            /// </summary>
            public Func<float> min;
            /// <summary>
            /// Maximum value function.
            /// </summary>
            public Func<float> max;

            // Runtime-only
            /// <summary>
            /// Step increment.
            /// </summary>
            public float incStep = 0.1f;
            /// <summary>
            /// Step increment multiplier.
            /// </summary>
            public float incStepMult = 10f;
            /// <summary>
            /// Number of decimals.
            /// </summary>
            public int decimals = 3;

            /// <summary>
            /// Function used to validate the value when updating the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            /// <returns>Validated value.</returns>
            public override float ValidateValue(float value)
            {
                if (min != null) value = Mathf.Max(value, min());
                if (max != null) value = Mathf.Min(value, max());
                return value;
            }

#if ENABLE_RENDERING_DEBUGGER_UI
            internal override void OnDecrement(bool fast)
            {
                float currentValue = GetValue();
                float step = fast ? incStepMult : incStep;
                float minValue = min != null ? min() : float.MinValue;

                // Check if subtraction would cause overflow, set to max value instead of wrapping around
                float newValue = currentValue >= minValue + step ? currentValue - step : minValue;

                // Float precision: detect desired number of decimal places based on the step, and round to that
                newValue = DebugUIStepperHelper.RoundToPrecision(newValue, currentValue, step);

                SetValue(newValue);
            }

            internal override void OnIncrement(bool fast)
            {
                float currentValue = GetValue();
                float step = fast ? incStepMult : incStep;
                float maxValue = max != null ? max() : float.MaxValue;

                // Check if addition would cause overflow, set to max value instead of wrapping around
                float newValue = currentValue <= maxValue-step ? currentValue + step : maxValue;

                // Float precision: detect desired number of decimal places based on the step, and round to that
                newValue = DebugUIStepperHelper.RoundToPrecision(newValue, currentValue, step);

                SetValue(newValue);
            }

            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                if (m_Context.IsAnyRuntimeContext())
                {
                    var field = new UIElements.FloatField();
                    DebugUIStepperHelper.AddStepper(
                        field,
                        SetValue,
                        GetValue,
                        onDecrement: OnDecrement,
                        onIncrement: OnIncrement
                    );
                    BaseFieldHelper.ConfigureBaseField(this, field);
                    return field;
                }
                if (min != null || max != null)
                {
                    var field = new UIElements.Slider(min?.Invoke() ?? float.MinValue, max?.Invoke() ?? float.MaxValue);
                    BaseFieldHelper.ConfigureBaseField(this, field);
                    field.showInputField = true;
                    return field;
                }
                else
                {
                    var field = new UIElements.FloatField();
                    BaseFieldHelper.ConfigureBaseField(this, field);
                    return field;
                }
            }
#endif
        }

        /// <summary>
        /// Field that displays <see cref="RenderingLayerMask"/>
        /// </summary>
        public class RenderingLayerField : Field<RenderingLayerMask>, IContainer
        {
            static readonly NameAndTooltip s_RenderingLayerColors = new()
            {
                name = "Layers Color",
                tooltip = "Select the display color for each Rendering Layer"
            };

            private string[] m_RenderingLayersNames = Array.Empty<string>();

            private int m_DefinedRenderingLayersCount = -1;

            private int maxRenderingLayerCount
            {
                get
                {
#if UNITY_EDITOR
                    if (UnityEditor.Rendering.EditorGraphicsSettings.
                        TryGetFirstRenderPipelineSettingsFromInterface<UnityEditor.Rendering.RenderingLayersLimitSettings>(out var settings))
                        return Mathf.Min(settings.maxSupportedRenderingLayers, RenderingLayerMask.GetRenderingLayerCount());
#endif

                    return RenderingLayerMask.GetRenderingLayerCount();
                }
            }

#if ENABLE_RENDERING_DEBUGGER_UI
            protected override VisualElement Create()
            {
                var maskField = new UIElements.MaskField(displayName, new List<string>(m_RenderingLayersNames), 0);
                maskField.RegisterCallback<ChangeEvent<int>>(evt =>
                {
                    SetValue(evt.newValue);
                });
                this.ScheduleTracked(maskField, () => maskField.schedule.Execute(() =>
                {
                    var value = GetValue();
                    maskField.SetValueWithoutNotify(Convert.ToInt32(value));
                })
                .Every(100));
                maskField.AddToClassList(UIElements.BaseField<int>.alignedFieldUssClassName);
                HackPopupHoverColor(maskField, m_Context);

                var content = new VisualElement();
                content.AddToClassList("debug-window-renderinglayerfield__content");
                foreach (var child in children)
                {
                    var childUIElement = child.ToVisualElement(m_Context);
                    if (childUIElement != null)
                    {
                        childUIElement.RemoveFromClassList("debug-window-foldout");
                        content.Add(childUIElement);
                    }
                }

                VisualElement container = new VisualElement();
                container.AddToClassList("unity-inspector-element");
                container.AddToClassList("debug-window-renderinglayerfield");
                container.Add(maskField);
                container.Add(content);

                return container;
            }
#endif

            private void Resize()
            {
                m_DefinedRenderingLayersCount = RenderingLayerMask.GetDefinedRenderingLayerCount();

                // Fill layer names
                m_RenderingLayersNames = new string[maxRenderingLayerCount];
                for (int i = 0; i < maxRenderingLayerCount; i++)
                {
                    var definedLayerName = RenderingLayerMask.RenderingLayerToName(i);
                    if (string.IsNullOrEmpty(definedLayerName))
                        definedLayerName = $"Unused Rendering Layer {i}";
                    m_RenderingLayersNames[i] = definedLayerName;
                }

                // Foldout + Color for each layer
                m_RenderingLayersColors.Clear();
                var layersColor = new DebugUI.Foldout()
                {
                    nameAndTooltip = s_RenderingLayerColors,
                    flags = Flags.EditorOnly,
                    parent = this,
                };
                m_RenderingLayersColors.Add(layersColor);

                for (int i = 0; i < m_RenderingLayersNames.Length; i++)
                {
                    var index = i; // capture the variable for the color field index
                    layersColor.children.Add(new DebugUI.ColorField
                    {
                        displayName = m_RenderingLayersNames[index],
                        getter = () =>
                        {
                            Assert.IsNotNull(getRenderingLayerColor, "Please specify a method for getting the rendering layer color");
                            return getRenderingLayerColor(index);
                        },
                        setter = value =>
                        {
                            Assert.IsNotNull(setRenderingLayerColor, "Please specify a method for setting the rendering layer color");
                            setRenderingLayerColor(value, index);
                        }
                    });
                }

                GenerateQueryPath();
            }

            /// <summary>
            /// Obtains the list of the available rendering layer names
            /// </summary>
            public string[] renderingLayersNames
            {
                get
                {
                    if (m_DefinedRenderingLayersCount != RenderingLayerMask.GetDefinedRenderingLayerCount())
                    {
                        Resize();
                    }

                    return m_RenderingLayersNames;
                }
            }

            private ObservableList<Widget> m_RenderingLayersColors = new ObservableList<Widget>();

            /// <summary>
            /// Gets the list of widgets representing the rendering layer colors.
            /// </summary>
            public ObservableList<Widget> children
            {
                get
                {
                    if (m_DefinedRenderingLayersCount != RenderingLayerMask.GetDefinedRenderingLayerCount())
                    {
                        Resize();
                    }

                    return m_RenderingLayersColors;
                }
            }

            /// <summary>
            /// Obtains the color in a given index
            /// </summary>
            public Func<int, Vector4> getRenderingLayerColor { get; set; }
            /// <summary>
            /// Sets the color for a given index
            /// </summary>
            public Action<Vector4, int> setRenderingLayerColor { get; set; }

            internal override void GenerateQueryPath()
            {
                base.GenerateQueryPath();

                int numChildren = children.Count;
                for (int i = 0; i < numChildren; i++)
                    children[i].GenerateQueryPath();
            }
        }

        /// <summary>
        /// Generic <see cref="EnumField"/> that stores enumNames and enumValues
        /// </summary>
        /// <typeparam name="T">The inner type of the field</typeparam>
        public abstract class EnumField<T> : Field<T>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var field = new UIElements.PopupField<string>()
                {
                    label = displayName,
                    choices = enumNames.Select(e => e.text).ToList()
                };
                field.AddToClassList("debug-window-enumfield");
                field.AddToClassList(UIElements.BaseField<int>.alignedFieldUssClassName);

                this.ScheduleTracked(field, () => field.schedule.Execute(() =>
                {
                    T value = GetValue();
                    var index = Array.IndexOf(enumValues, value);
                    if (index >= 0 && index < enumNames.Length)
                    {
                        var expectedValue = enumNames[index].text;
                        if (field.value != expectedValue)
                        {
                            field.SetValueWithoutNotify(expectedValue);
                        }
                    }
                }).Every(100));

                return field;
            }
#endif

            /// <summary>
            /// List of names of the enumerator entries.
            /// </summary>
            public GUIContent[] enumNames;

            private int[] m_EnumValues;

            /// <summary>
            /// List of values of the enumerator entries.
            /// </summary>
            public int[] enumValues
            {
                get => m_EnumValues;
                set
                {
                    if (value?.Distinct().Count() != value?.Count())
                        Debug.LogWarning($"{displayName} - The values of the enum are duplicated, this might lead to a errors displaying the enum");
                    m_EnumValues = value;
                }
            }


            // Space-delimit PascalCase (https://stackoverflow.com/questions/155303/net-how-can-you-split-a-caps-delimited-string-into-an-array)
            static Regex s_NicifyRegEx = new("([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", RegexOptions.Compiled);

            /// <summary>
            /// Automatically fills the enum names with a given <see cref="Type"/>
            /// </summary>
            /// <param name="enumType">The enum type</param>
            /// <param name="removeZeroElement">Whether the item with the value zero should be removed from enumNames and enumValues</param>
            protected void AutoFillFromType(Type enumType, bool removeZeroElement = false)
            {
                if (enumType == null || !enumType.IsEnum)
                    throw new ArgumentException($"{nameof(enumType)} must not be null and it must be an Enum type");

                using (ListPool<GUIContent>.Get(out var tmpNames))
                using (ListPool<int>.Get(out var tmpValues))
                {
                    var enumEntries = enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Where(fieldInfo => !fieldInfo.IsDefined(typeof(ObsoleteAttribute)) && !fieldInfo.IsDefined(typeof(HideInInspector)));
                    foreach (var fieldInfo in enumEntries)
                    {
                        var description = fieldInfo.GetCustomAttribute<InspectorNameAttribute>();
                        var displayName = new GUIContent(description == null ? s_NicifyRegEx.Replace(fieldInfo.Name, "$1 ") : description.displayName);

                        int fieldValue = (int)Enum.Parse(enumType, fieldInfo.Name);
                        if (removeZeroElement && fieldValue == 0)
                            continue;

                        tmpNames.Add(displayName);
                        tmpValues.Add(fieldValue);
                    }
                    enumNames = tmpNames.ToArray();
                    enumValues = tmpValues.ToArray();
                }
            }
        }

#if ENABLE_RENDERING_DEBUGGER_UI
        private static void HackPopupHoverColor(VisualElement popupField, in DebugUI.Context context)
        {
            if (context == DebugUI.Context.Runtime)
            {
                // For some reason. it seems impossible to override the hover color of the popup field.
                // This works because C# style overrides have higher precedence than any USS stuff.
                Color hoverColor = new Color32(0x66, 0x66, 0x66, 0xFF); // Should match --widget-background-color-hover
                popupField.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    var inputElement = popupField.Q<VisualElement>(className: "unity-base-field__input");
                    if (inputElement != null)
                        inputElement.style.backgroundColor = hoverColor;
                });
                popupField.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    var inputElement = popupField.Q<VisualElement>(className: "unity-base-field__input");
                    if (inputElement != null)
                        inputElement.style.backgroundColor = new StyleColor(StyleKeyword.Null);
                });
            }
        }
#endif

        /// <summary>
        /// A dropdown that contains the values from an enum.
        /// </summary>
        public class EnumField : EnumField<int>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var field = new UIElements.PopupField<string>()
                {
                    label = displayName,
                    choices = enumNames.Select(e => e.text).ToList()
                };
                field.AddToClassList("debug-window-enumfield");
                field.AddToClassList(UIElements.BaseField<int>.alignedFieldUssClassName);

                HackPopupHoverColor(field, m_Context);

                field.RegisterCallback<ChangeEvent<string>>(evt =>
                {
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        if (evt.newValue == enumNames[i].text)
                        {
                            SetValue(enumValues[i]);
                            break;
                        }
                    }
                });

                this.ScheduleTracked(field, () => field.schedule.Execute(() =>
                {
                    if (currentIndex >= 0 && currentIndex < enumNames.Length)
                        field.SetValueWithoutNotify(enumNames[currentIndex].text);
                }).Every(100));

                return field;
            }
#endif

            internal int[] quickSeparators;

            private int[] m_Indexes;
            internal int[] indexes => m_Indexes ??= Enumerable.Range(0, enumNames?.Length ?? 0).ToArray();

            /// <summary>
            /// Get the enumeration value index.
            /// </summary>
            public Func<int> getIndex { get; set; }
            /// <summary>
            /// Set the enumeration value index.
            /// </summary>
            public Action<int> setIndex { get; set; }

            /// <summary>
            /// Current enumeration value index.
            /// </summary>
            public int currentIndex
            {
                get => getIndex();
                set => setIndex(value);
            }

            private Type m_Type;

            /// <summary>
            /// Generates enumerator values and names automatically based on the provided type.
            /// </summary>
            public Type autoEnum
            {
                set
                {
                    if (m_Type != value)
                    {
                        AutoFillFromType(value);
                        InitQuickSeparators();
                        m_Type = value;
                    }
                }
            }

            internal void InitQuickSeparators()
            {
                var enumNamesPrefix = enumNames.Select(x =>
                {
                    string[] splitted = x.text.Split('/');
                    if (splitted.Length == 1)
                        return "";
                    else
                        return splitted[0];
                });
                quickSeparators = new int[enumNamesPrefix.Distinct().Count()];
                string lastPrefix = null;
                for (int i = 0, wholeNameIndex = 0; i < quickSeparators.Length; ++i)
                {
                    var currentTestedPrefix = enumNamesPrefix.ElementAt(wholeNameIndex);
                    while (lastPrefix == currentTestedPrefix)
                    {
                        currentTestedPrefix = enumNamesPrefix.ElementAt(++wholeNameIndex);
                    }
                    lastPrefix = currentTestedPrefix;
                    quickSeparators[i] = wholeNameIndex++;
                }
            }

            /// <summary>
            /// Set the value of the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            public override void SetValue(int value)
            {
                Assert.IsNotNull(setter);
                var validValue = ValidateValue(value);

                // There might be cases that the value does not map the index, look for the correct index
                var newCurrentIndex = Array.IndexOf(enumValues, validValue);

                if (currentIndex != newCurrentIndex && !validValue.Equals(getter()))
                {
#if UNITY_EDITOR
                    int previousValue = GetValue();
                    onWidgetValueChangedAnalytic?.Invoke(queryPath, previousValue, validValue);
#endif

                    setter(validValue);
                    onValueChanged?.Invoke(this, validValue);

                    if (newCurrentIndex > -1)
                        currentIndex = newCurrentIndex;
                }
            }
        }

        /// <summary>
        /// A dropdown that contains a list of Unity objects.
        /// </summary>
        public class ObjectPopupField : Field<Object>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var choices = new List<UnityEngine.Object>() { null };
                choices.AddRange(getObjects());

                var field = new UIElements.PopupField<UnityEngine.Object>()
                {
                    label = displayName,
                    choices = choices,
                    formatListItemCallback = o => o != null ? o.name : "None",
                    formatSelectedValueCallback = o => o != null ? o.name : "None"
                };

                field.AddToClassList("debug-window-objectpopupfield");
                HackPopupHoverColor(field, m_Context);

                field.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt =>
                {
                    SetValue(evt.newValue);
                });

                this.ScheduleTracked(field, () => field.schedule.Execute(() =>
                {
                    field.SetValueWithoutNotify(GetValue());
                }).Every(100));

                field.AddToClassList(UIElements.BaseField<UnityEngine.Object>.alignedFieldUssClassName);
                return field;
            }
#endif

            /// <summary>
            /// Callback to obtain the elemtents of the pop up
            /// </summary>
            public Func<IEnumerable<Object>> getObjects { get; set; }
        }

        /// <summary>
        /// A dropdown that contains a list of cameras
        /// </summary>
        public class CameraSelector : ObjectPopupField
        {
            /// <summary>
            /// A dropdown that contains a list of cameras
            /// </summary>
            public CameraSelector()
            {
                displayName = "Camera";
                getObjects = () => cameras;
            }

            private Camera[] m_CamerasArray;
            private List<Camera> m_Cameras = new List<Camera>();

            IEnumerable<Camera> cameras
            {
                get
                {
                    m_Cameras.Clear();

#if UNITY_EDITOR
                    if (UnityEditor.SceneView.lastActiveSceneView != null)
                    {
                        var sceneCamera = UnityEditor.SceneView.lastActiveSceneView.camera;
                        if (sceneCamera != null)
                            m_Cameras.Add(sceneCamera);
                    }
#endif

                    if (m_CamerasArray == null || m_CamerasArray.Length != Camera.allCamerasCount)
                    {
                        m_CamerasArray = new Camera[Camera.allCamerasCount];
                    }

                    Camera.GetAllCameras(m_CamerasArray);

                    foreach (var camera in m_CamerasArray)
                    {
                        if (camera == null)
                            continue;

                        if (camera.cameraType != CameraType.Preview && camera.cameraType != CameraType.Reflection)
                        {
                            if (camera.TryGetComponent<IAdditionalData>(out _))
                                m_Cameras.Add(camera);
                        }
                    }

                    return m_Cameras;
                }
            }
        }

        /// <summary>
        /// Enumerator field with history.
        /// </summary>
        public class HistoryEnumField : EnumField
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var valueContainer = new UIElements.VisualElement();
                valueContainer.AddToClassList("debug-window-historyenum");
                valueContainer.Add(new Label(displayName)
                {
                    style = { width = ValueTuple.GetLabelWidth(m_Context) }
                });

                var enumField = new DebugUI.EnumField()
                {
                    displayName = string.Empty,
                    tooltip = tooltip,
                    getter = getter,
                    setter = setter,
                    enumNames = enumNames,
                    enumValues = enumValues,
                    getIndex = getIndex,
                    setIndex = setIndex,
                };
                childWidgets.Add(enumField);
                valueContainer.Add(enumField.ToVisualElement(m_Context));

                foreach (var value in historyIndexGetter)
                {
                    var historyEnumField = new DebugUI.EnumField()
                    {
                        displayName = string.Empty,
                        enumNames = enumNames,
                        enumValues = enumValues,
                        getIndex = value,
                        setIndex = i => {},
                        getter = value
                    };
                    childWidgets.Add(historyEnumField);

                    var field = historyEnumField.ToVisualElement(m_Context);
                    field.SetEnabled(false);
                    valueContainer.Add(field);
                }

                return valueContainer;
            }
#endif

            internal List<Widget> childWidgets { private set; get; } = new List<Widget>();

            /// <summary>
            /// History getter for this field.
            /// </summary>
            public Func<int>[] historyIndexGetter { get; set; }
            /// <summary>
            /// Depth of the field's history.
            /// </summary>
            public int historyDepth => historyIndexGetter?.Length ?? 0;
            /// <summary>
            /// Get the value of the field at a certain history index.
            /// </summary>
            /// <param name="historyIndex">Index of the history to query.</param>
            /// <returns>Value of the field at the provided history index.</returns>
            public int GetHistoryValue(int historyIndex)
            {
                Assert.IsNotNull(historyIndexGetter);
                Assert.IsTrue(historyIndex >= 0 && historyIndex < historyIndexGetter.Length, "out of range historyIndex");
                Assert.IsNotNull(historyIndexGetter[historyIndex]);
                return historyIndexGetter[historyIndex]();
            }
        }

        /// <summary>
        /// Bitfield enumeration field.
        /// </summary>
        public class BitField : EnumField<Enum>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var maskField = new UIElements.MaskField(displayName, enumNames.Select(e => e.text).ToList(), 0);
                maskField.AddToClassList("debug-window-bitfield");
                HackPopupHoverColor(maskField, m_Context);

                maskField.RegisterCallback<ChangeEvent<int>>(evt =>
                {
                    var value = Enum.Parse(m_EnumType, evt.newValue.ToString()) as Enum;
                    SetValue(value);
                });

                this.ScheduleTracked(maskField, () => maskField.schedule.Execute(() =>
                {
                    var value = GetValue();
                    maskField.SetValueWithoutNotify(Convert.ToInt32(value));
                })
                .Every(100));

                maskField.AddToClassList(UIElements.BaseField<int>.alignedFieldUssClassName);
                return maskField;
            }
#endif
            Type m_EnumType;

            /// <summary>
            /// Generates bitfield values and names automatically based on the provided type.
            /// </summary>
            public Type enumType
            {
                get => m_EnumType;
                set
                {
                    if (!value.IsEnum ||
                        value.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Length == 0)
                    {
                        throw new ArgumentException($"{nameof(value)} must be an Enum type with the Flags attribute");
                    }

                    m_EnumType = value;

                    // Automatically fill enum names and values. If there is a zero value, remove it because a
                    // "Nothing" value will get automatically added by the MaskField control.
                    AutoFillFromType(value, removeZeroElement: true);
                }
            }
        }

#if ENABLE_RENDERING_DEBUGGER_UI
        public class RuntimeColorField : UIElements.BaseField<Color>
        {
            private VisualElement colorPreview;
            private UIElements.FloatField redField;
            private UIElements.FloatField greenField;
            private UIElements.FloatField blueField;

            public RuntimeColorField(string label, bool showRGB) : base(label, null)
            {
                CreateVisualElements(showRGB);
            }

            private void CreateVisualElements(bool showRGB)
            {
                contentContainer.AddToClassList("debug-window-colorfield-runtime-container");
                contentContainer.style.flexDirection = FlexDirection.Row;

                // RGB fields
                if (showRGB )
                {
                    redField = CreateColorComponentField();
                    greenField = CreateColorComponentField();
                    blueField = CreateColorComponentField();
                    contentContainer.Add(redField);
                    contentContainer.Add(greenField);
                    contentContainer.Add(blueField);
                }

                // Color preview
                colorPreview = new VisualElement();
                colorPreview.AddToClassList("debug-window-colorfield-preview");
                colorPreview.AddToClassList("unity-base-field");
                colorPreview.style.flexBasis = new Length(50, LengthUnit.Percent);
                colorPreview.style.flexGrow = 1;
                colorPreview.style.flexShrink = 0;

                contentContainer.Add(colorPreview);
            }

            private UIElements.FloatField CreateColorComponentField()
            {
                var field = new UIElements.FloatField();
                field.isReadOnly = true;
                field.focusable = false;
                field.style.flexGrow = 0;
                field.style.flexShrink = 0;
                field.formatString = "F2";
                field.label = string.Empty;

                return field;
            }

            public override void SetValueWithoutNotify(Color newValue)
            {
                base.SetValueWithoutNotify(newValue);

                // Update color preview
                colorPreview.style.backgroundColor = newValue;

                // Update RGB fields to display current values
                redField?.SetValueWithoutNotify(newValue.r);
                greenField?.SetValueWithoutNotify(newValue.g);
                blueField?.SetValueWithoutNotify(newValue.b);
            }
        }
#endif

        /// <summary>
        /// Color field.
        /// </summary>
        public class ColorField : Field<Color>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                UIElements.BaseField<Color> field = null;
#if UNITY_EDITOR
                if (m_Context == DebugUI.Context.Editor && showPicker)
                {
                    field = new UnityEditor.UIElements.ColorField()
                    {
                        label = displayName,
                    };
                }
#endif
                if (field == null)
                {
                    field = new RuntimeColorField(displayName, showPicker);
                }

                if (field != null)
                {
                    field.AddToClassList("debug-window-colorfield");
                    field.AddToClassList(UIElements.BaseField<Color>.alignedFieldUssClassName);
                    field.RegisterCallback<ChangeEvent<Color>>(evt => SetValue(evt.newValue));
                    this.ScheduleTracked(field, () => field.schedule.Execute(() =>
                    {
                        field.SetValueWithoutNotify((Color)Convert.ChangeType(GetValue(), typeof(Color)));
                    }).Every(100));
                }

                return field;
            }
#endif

            /// <summary>
            /// HDR color.
            /// </summary>
            public bool hdr = false;
            /// <summary>
            /// Show alpha of the color field.
            /// </summary>
            public bool showAlpha = true;

            // Editor-only
            /// <summary>
            /// Show the color picker.
            /// </summary>
            public bool showPicker = true;

            // Runtime-only
            /// <summary>
            /// Step increment.
            /// </summary>
            public float incStep = 0.025f;
            /// <summary>
            /// Step increment multiplier.
            /// </summary>
            public float incStepMult = 5f;
            /// <summary>
            /// Number of decimals.
            /// </summary>
            public int decimals = 3;

            /// <summary>
            /// Function used to validate the value when updating the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            /// <returns>Validated value.</returns>
            public override Color ValidateValue(Color value)
            {
                if (!hdr)
                {
                    value.r = Mathf.Clamp01(value.r);
                    value.g = Mathf.Clamp01(value.g);
                    value.b = Mathf.Clamp01(value.b);
                    value.a = Mathf.Clamp01(value.a);
                }

                return value;
            }
        }

#if ENABLE_RENDERING_DEBUGGER_UI
        class RuntimeBaseFieldWrapper<T> : UIElements.BaseField<T> where T : struct
        {
            public RuntimeBaseFieldWrapper(VisualElement container, string label) : base(label, container)
            {
            }
        }
#endif

        /// <summary>
        /// Generic base class for vector fields with stepper support for Runtime.
        /// </summary>
        /// <typeparam name="T">The numeric vector type</typeparam>
        public abstract class VectorField<T> : DebugUI.Field<T> where T : struct
        {
            // Runtime-only
            /// <summary>
            /// Step increment.
            /// </summary>
            public float incStep = 0.025f;
            /// <summary>
            /// Step increment multiplier.
            /// </summary>
            public float incStepMult = 10f;
            /// <summary>
            /// Number of decimals.
            /// </summary>
            public int decimals = 3;

#if ENABLE_RENDERING_DEBUGGER_UI
            /// <summary>
            /// Selected component index.
            /// </summary>
            internal int selectedComponent = -1;

            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                UIElements.BaseField<T> vectorField;
                if (m_Context.IsAnyRuntimeContext())
                {
                    vectorField = CreateRuntimeVectorField();
                }
                else
                {
                    vectorField = CreateEditorVectorField();
                }

                BaseFieldHelper.ConfigureBaseField(this, vectorField);
                return vectorField;
            }

            private UIElements.BaseField<T> CreateRuntimeVectorField()
            {
                var fieldsContainer = new VisualElement();
                fieldsContainer.AddToClassList($"debug-window-{GetVectorTypeName()}-runtime");
                fieldsContainer.style.flexDirection = FlexDirection.Column;
                fieldsContainer.delegatesFocus = true;

                // Create component fields based on vector type
                var componentFields = CreateComponentFields();
                foreach (var field in componentFields)
                {
                    fieldsContainer.Add(field);
                }

                // Sync values from data model to UI
                this.ScheduleTracked(fieldsContainer, () => fieldsContainer.schedule.Execute(() =>
                {
                    var value = GetValue();
                    var components = GetVectorComponents(value);
                    for (int i = 0; i < componentFields.Length && i < components.Length; i++)
                    {
                        componentFields[i].SetValueWithoutNotify(components[i]);
                    }
                }).Every(100));

                return new RuntimeBaseFieldWrapper<T>(fieldsContainer, displayName);
            }

            private UIElements.FloatField[] CreateComponentFields()
            {
                var componentCount = GetComponentCount();
                var componentFields = new UIElements.FloatField[componentCount];
                var componentNames = GetComponentNames();


                for (int i = 0; i < componentCount; i++)
                {
                    var componentIndex = i; // Capture for closure
                    var field = new UIElements.FloatField { formatString = $"F{decimals}" };
                    DebugUIStepperHelper.AddStepper(
                        field,
                        (newValue) => UpdateVectorComponent(componentIndex, newValue),
                        () => this[componentIndex],
                        onDecrement: (fast) => { selectedComponent = componentIndex; OnDecrement(fast); },
                        onIncrement: (fast) => { selectedComponent = componentIndex; OnIncrement(fast); }
                    );

                    field.RegisterCallback<FocusInEvent>(evt =>
                    {
                        selectedComponent = componentIndex;
                    });

                    // Add tooltip for component
                    field.focusable = true;
                    field.label = componentNames[i];
                    field.tooltip = $"{displayName} {componentNames[i]}";
                    componentFields[i] = field;

                }

                return componentFields;
            }

            private void UpdateVectorComponent(int componentIndex, float newValue)
            {
                var currentValue = GetValue();
                var newVectorValue = SetVectorComponent(currentValue, componentIndex, newValue);
                SetValue(newVectorValue);
            }

            internal override void OnDecrement(bool fast)
            {
                if (selectedComponent >= 0 && selectedComponent < GetComponentCount())
                {
                    float currentValue = this[selectedComponent];
                    float step = fast ? incStepMult : incStep;
                    float minValue = float.MinValue;

                    // Check if subtraction would cause overflow, set to max value instead of wrapping around
                    float newValue = currentValue >= minValue + step ? currentValue - step : minValue;

                    // Float precision: detect desired number of decimal places based on the step, and round to that
                    newValue = DebugUIStepperHelper.RoundToPrecision(newValue, currentValue, step);

                    UpdateVectorComponent(selectedComponent, newValue);
                }
            }

            internal override void OnIncrement(bool fast)
            {
                if (selectedComponent >= 0 && selectedComponent < GetComponentCount())
                {
                    float currentValue = this[selectedComponent];
                    var step = fast ? incStepMult : incStep;
                    float maxValue = float.MaxValue;

                    // Check if subtraction would cause overflow, set to max value instead of wrapping around
                    float newValue = currentValue <= maxValue - step ? currentValue + step : maxValue;

                    // Float precision: detect desired number of decimal places based on the step, and round to that
                    newValue = DebugUIStepperHelper.RoundToPrecision(newValue, currentValue, step);

                    UpdateVectorComponent(selectedComponent, newValue);
                }
            }

            // Abstract methods to be implemented by specific vector types
            protected abstract float this[int index] { get; }
            protected abstract int GetComponentCount();
            protected abstract string[] GetComponentNames();
            protected abstract string GetVectorTypeName();
            protected abstract float[] GetVectorComponents(T vector);
            protected abstract T SetVectorComponent(T vector, int componentIndex, float value);
            protected abstract UIElements.BaseField<T> CreateEditorVectorField();
#endif
        }

        /// <summary>
        /// Vector2 field.
        /// </summary>
        public class Vector2Field : VectorField<Vector2>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            protected override int GetComponentCount() => 2;
            protected override string[] GetComponentNames() => new[] { "X", "Y" };
            protected override string GetVectorTypeName() => "vector2field";

            protected override float[] GetVectorComponents(Vector2 vector) =>
                new[] { vector.x, vector.y };

            protected override float this[int index]
            {
                get
                {
                    var vector = GetValue();
                    return index switch
                    {
                        0 => vector.x,
                        1 => vector.y,
                        _ => 0
                    };
                }
            }

            protected override Vector2 SetVectorComponent(Vector2 vector, int componentIndex, float value)
            {
                return componentIndex switch
                {
                    0 => new Vector2(value, vector.y),
                    1 => new Vector2(vector.x, value),
                    _ => vector
                };
            }

            protected override UIElements.BaseField<Vector2> CreateEditorVectorField() =>
                new UIElements.Vector2Field();
#endif
        }

        /// <summary>
        /// Vector3 field.
        /// </summary>
        public class Vector3Field : VectorField<Vector3>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            protected override int GetComponentCount() => 3;
            protected override string[] GetComponentNames() => new[] { "X", "Y", "Z" };
            protected override string GetVectorTypeName() => "vector3field";

            protected override float[] GetVectorComponents(Vector3 vector) =>
                new[] { vector.x, vector.y, vector.z };

            protected override float this[int index]
            {
                get
                {
                    var vector = GetValue();
                    return index switch
                    {
                        0 => vector.x,
                        1 => vector.y,
                        2 => vector.z,
                        _ => 0
                    };
                }
            }

            protected override Vector3 SetVectorComponent(Vector3 vector, int componentIndex, float value)
            {
                return componentIndex switch
                {
                    0 => new Vector3(value, vector.y, vector.z),
                    1 => new Vector3(vector.x, value, vector.z),
                    2 => new Vector3(vector.x, vector.y, value),
                    _ => vector
                };
            }

            protected override UIElements.BaseField<Vector3> CreateEditorVectorField() =>
                new UIElements.Vector3Field();
#endif
        }

        /// <summary>
        /// Vector4 field.
        /// </summary>
        public class Vector4Field : VectorField<Vector4>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            protected override int GetComponentCount() => 4;
            protected override string[] GetComponentNames() => new[] { "X", "Y", "Z", "W" };
            protected override string GetVectorTypeName() => "vector4field";

            protected override float[] GetVectorComponents(Vector4 vector) =>
                new[] { vector.x, vector.y, vector.z, vector.w };

            protected override float this[int index]
            {
                get
                {
                    var vector = GetValue();
                    return index switch
                    {
                        0 => vector.x,
                        1 => vector.y,
                        2 => vector.z,
                        3 => vector.w,
                        _ => 0
                    };
                }
            }

            protected override Vector4 SetVectorComponent(Vector4 vector, int componentIndex, float value)
            {
                return componentIndex switch
                {
                    0 => new Vector4(value, vector.y, vector.z, vector.w),
                    1 => new Vector4(vector.x, value, vector.z, vector.w),
                    2 => new Vector4(vector.x, vector.y, value, vector.w),
                    3 => new Vector4(vector.x, vector.y, vector.z, value),
                    _ => vector
                };
            }

            protected override UIElements.BaseField<Vector4> CreateEditorVectorField() =>
                new UIElements.Vector4Field();
#endif
        }

#if ENABLE_RENDERING_DEBUGGER_UI
        private class RuntimeObjectField : UIElements.BaseField<Object>
        {
            private UIElements.Label objectPreview;

            public RuntimeObjectField(string label, UIElements.Label visual) : base(label, visual)
            {
                objectPreview = visual;
            }

            public override void SetValueWithoutNotify(Object newValue)
            {
                base.SetValueWithoutNotify(newValue);
                objectPreview.text = newValue!=null ? newValue.name : "None";
            }
        }
#endif

        /// <summary>
        /// A field for selecting a Unity object.
        /// </summary>
        public class ObjectField : Field<Object>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                UIElements.BaseField<UnityEngine.Object> field = null;
#if UNITY_EDITOR
                if (m_Context == DebugUI.Context.Editor)
                {
                    field = new UnityEditor.UIElements.ObjectField()
                    {
                        label = displayName
                    };
                }
#endif
                if (field == null)
                {
                    var objectPreview = new UIElements.Label();
                    field = new RuntimeObjectField(displayName, objectPreview);
                }

                field.AddToClassList("debug-window-objectfield");
                field.AddToClassList(UIElements.BaseField<Object>.alignedFieldUssClassName);
                field.RegisterCallback<ChangeEvent<Object>>(evt => SetValue(evt.newValue));
                this.ScheduleTracked(field, () => field.schedule.Execute(() =>
                {
                    var selectedObject = GetValue();
                    field.SetValueWithoutNotify(selectedObject);
                }).Every(100));

                return field;
            }
#endif

            /// <summary>
            /// Object type.
            /// </summary>
            public Type type = typeof(Object);
        }

        /// <summary>
        /// A list of fields for selecting Unity objects.
        /// </summary>
        public class ObjectListField : Field<Object[]>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var container = new UIElements.Foldout()
                {
                    text = displayName
                };
                container.AddToClassList("debug-window-objectlistfield");

                // TODO: Allow selection
                foreach (var o in GetValue())
                {
                    var child = new Label(o.name);
                }

                return container;
            }
#endif

            /// <summary>
            /// Objects type.
            /// </summary>
            public Type type = typeof(Object);
        }

        /// <summary>
        /// A read-only message box with an icon.
        /// </summary>
        public class MessageBox : Widget
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var helpBox = new HelpBox(displayName, (HelpBoxMessageType)style);
                helpBox.text = message;
                helpBox.AddToClassList("debug-window-messagebox");

                if (messageCallback != null)
                {
                    this.ScheduleTracked(helpBox, () => helpBox.schedule.Execute(() =>
                    {
                        helpBox.text = message;
                    }).Every(100));
                }

                return helpBox;
            }
#endif

            /// <summary>
            /// Label style defines text color and background.
            /// </summary>
            public enum Style
            {
                /// <summary>
                /// None category - no icon in the message
                /// </summary>
                None,
                /// <summary>
                /// Info category
                /// </summary>
                Info,
                /// <summary>
                /// Warning category
                /// </summary>
                Warning,
                /// <summary>
                /// Error category
                /// </summary>
                Error
            }

            /// <summary>
            /// Style used to render displayName.
            /// </summary>
            public Style style = Style.Info;

            /// <summary>
            /// Message Callback to feed the new message to the widget
            /// </summary>
            public Func<string> messageCallback = null;

            /// <summary>
            /// This obtains the message from the display name or from the message callback if it is not null
            /// </summary>
            public string message => messageCallback == null ? displayName : messageCallback();
        }

        /// <summary>
        /// Widget that will show into the Runtime UI only
        /// Warning the user if the Runtime Debug Shaders variants are being stripped from the build.
        /// </summary>
        public class RuntimeDebugShadersMessageBox : MessageBox
        {
            /// <summary>
            /// Constructs a <see cref="RuntimeDebugShadersMessageBox"/>
            /// </summary>
            public RuntimeDebugShadersMessageBox()
            {
                displayName =
                    "Warning: the debug shader variants are missing. Ensure that the \"Strip Runtime Debug Shaders\" option is disabled in the SRP Graphics Settings.";
                style = DebugUI.MessageBox.Style.Warning;
                isHiddenCallback = () =>
                {
#if !UNITY_EDITOR
                    if (GraphicsSettings.TryGetRenderPipelineSettings<ShaderStrippingSetting>(out var shaderStrippingSetting))
                        return !shaderStrippingSetting.stripRuntimeDebugShaders;
#endif
                    return true;
                };
            }
        }
    }
}


