#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

using System;

#if ENABLE_RENDERING_DEBUGGER_UI
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    public partial class DebugUI
    {
        // Helper for configuring Field<T> that is implemented using a UIElements.BaseField<T>
        internal static class BaseFieldHelper
        {
            internal static void ConfigureBaseField<T>(Field<T> widget, UIElements.BaseField<T> field)
            {
                field.label = widget.displayName;
                field.style.flexGrow = 0f;
                field.AddToClassList($"debug-window-basefield_{typeof(T).Name.ToLower()}");
                field.labelElement.AddToClassList("debug-window-search-filter-target");

                field.RegisterCallback<ChangeEvent<T>>(evt => widget.SetValue(evt.newValue));

                widget.ScheduleTracked(field, () => field.schedule.Execute(() =>
                {
                    field.SetValueWithoutNotify((T)Convert.ChangeType(widget.GetValue(), typeof(T)));
                }).Every(100));

                field.AddToClassList(UIElements.BaseField<T>.alignedFieldUssClassName);
            }
        }

        // Helper for adding numeric fields increment/decrement buttons for Runtime UI
        internal static class DebugUIStepperHelper
        {
            public static void AddStepper<T>(
                UIElements.TextInputBaseField<T> field,
                Action<T> onValueChanged,
                Func<T> getValue,
                Action<bool> onDecrement = null,
                Action<bool> onIncrement = null)
                where T : struct
            {
                // Create stepper buttons
                var btnDecLarge = new UIElements.Button { text = "<<", focusable = false };
                var btnDecSmall = new UIElements.Button { text = "<", focusable = false };
                var btnIncSmall = new UIElements.Button { text = ">", focusable = false };
                // Need to remove marginRight of latest button to perfectly align with the other fields
                var btnIncLarge = new UIElements.Button { text = ">>", focusable = false, style = { marginRight = 0 } };

                // Add button event handlers
                btnDecLarge.clicked += () =>
                {
                    if (onDecrement == null)
                        return;

                    T validatedValue;
                    onDecrement(true);
                    validatedValue = getValue();

                    field.SetValueWithoutNotify(validatedValue);
                };

                btnDecSmall.clicked += () =>
                {
                    if (onDecrement == null)
                        return;

                    T validatedValue;
                    onDecrement(false);
                    validatedValue = getValue();

                    field.SetValueWithoutNotify(validatedValue);
                };

                btnIncSmall.clicked += () =>
                {
                    if (onIncrement == null)
                        return;

                    T validatedValue;
                    onIncrement(false);
                    validatedValue = getValue();

                    field.SetValueWithoutNotify(validatedValue);
                };

                btnIncLarge.clicked += () =>
                {
                    if (onIncrement == null)
                        return;

                    T validatedValue;
                    onIncrement(true);
                    validatedValue = getValue();

                    field.SetValueWithoutNotify(validatedValue);
                };

                // Make field read-only for stepper control, disable select text
                field.isReadOnly = true;
                // Disable test selection to better readability
                field.selectAllOnFocus = false;
                field.doubleClickSelectsWord = false;
                field.tripleClickSelectsLine = false;

                // Insert buttons into field
                field.Insert(0, btnDecLarge);
                field.Insert(1, btnDecSmall);
                field.Add(btnIncSmall);
                field.Add(btnIncLarge);

                field.AddToClassList("debug-window-stepper-field");

                // Style the buttons
                foreach (var btn in new[] { btnDecLarge, btnDecSmall, btnIncSmall, btnIncLarge })
                {
                    btn.AddToClassList("debug-window-stepper-button");
                }
            }

            static int CountDecimalPlaces(float value)
            {
                value = Mathf.Abs(value);
                if (value == 0f || float.IsNaN(value) || float.IsInfinity(value))
                    return 0;

                int decimals = 0;

                const int kMaxDecimalPlaces = 7;
                const float kEpsilon = 1e-4f;
                // Multiply the original value by 10 until we reach an integer value
                float multipliedValue = value;
                while (decimals < kMaxDecimalPlaces)
                {
                    float nearestInteger = Mathf.Round(multipliedValue);
                    if (Mathf.Abs(multipliedValue - nearestInteger) <= kEpsilon)
                        break;

                    multipliedValue *= 10f;
                    decimals++;
                }

                return decimals;
            }

            // Rounds 'value' to the maximum decimal precision found in 'referenceValues'
            internal static float RoundToPrecision(float value, params float[] referenceValues)
            {
                int decimalPlaces = 0;
                foreach (float refValue in referenceValues)
                {
                    decimalPlaces = Math.Max(decimalPlaces, CountDecimalPlaces(refValue));
                }

                double rounded = Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
                return (float)rounded;
            }
        }
    }
}
#endif
