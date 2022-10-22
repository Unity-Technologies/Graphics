using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    public partial class DebugUI
    {
        /// <summary>
        /// Generic field - will be serialized in the editor if it's not read-only
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract class Field<T> : Widget, IValueField
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
                Assert.IsNotNull(setter);
                var v = ValidateValue(value);

                if (v == null || !v.Equals(getter()))
                {
                    setter(v);
                    onValueChanged?.Invoke(this, v);
                }
            }
        }

        /// <summary>
        /// Boolean field.
        /// </summary>
        public class BoolField : Field<bool> { }
        /// <summary>
        /// Boolean field with history.
        /// </summary>
        public class HistoryBoolField : BoolField
        {
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
        /// Integer field.
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
            public int intStepMult = 10;

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
        }

        /// <summary>
        /// Unsigned integer field.
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
            public uint intStepMult = 10u;

            /// <summary>
            /// Function used to validate the value when updating the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            /// <returns>Validated value.</returns>
            public override uint ValidateValue(uint value)
            {
                if (min != null) value = (uint)Mathf.Max((int)value, (int)min());
                if (max != null) value = (uint)Mathf.Min((int)value, (int)max());
                return value;
            }
        }

        /// <summary>
        /// Float field.
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
        }

        /// <summary>
        /// Generic <see cref="EnumField"/> that stores enumNames and enumValues
        /// </summary>
        /// <typeparam name="T">The inner type of the field</typeparam>
        public abstract class EnumField<T> : Field<T>
        {
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
            protected void AutoFillFromType(Type enumType)
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
                        tmpNames.Add(displayName);
                        tmpValues.Add((int)Enum.Parse(enumType, fieldInfo.Name));
                    }
                    enumNames = tmpNames.ToArray();
                    enumValues = tmpValues.ToArray();
                }
            }
        }

        /// <summary>
        /// Enumerator field.
        /// </summary>
        public class EnumField : EnumField<int>
        {
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

            /// <summary>
            /// Generates enumerator values and names automatically based on the provided type.
            /// </summary>
            public Type autoEnum
            {
                set
                {
                    AutoFillFromType(value);
                    InitQuickSeparators();
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
                    setter(validValue);
                    onValueChanged?.Invoke(this, validValue);

                    if (newCurrentIndex > -1)
                        currentIndex = newCurrentIndex;
                }
            }
        }

        /// <summary>
        /// Object PopupField
        /// </summary>
        public class ObjectPopupField : Field<Object>
        {
            /// <summary>
            /// Callback to obtain the elemtents of the pop up
            /// </summary>
            public Func<IEnumerable<Object>> getObjects { get; set; }
        }

        /// <summary>
        /// Enumerator field with history.
        /// </summary>
        public class HistoryEnumField : EnumField
        {
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
            Type m_EnumType;

            /// <summary>
            /// Generates bitfield values and names automatically based on the provided type.
            /// </summary>
            public Type enumType
            {
                get => m_EnumType;
                set
                {
                    m_EnumType = value;
                    AutoFillFromType(value);
                }
            }
        }

        /// <summary>
        /// Color field.
        /// </summary>
        public class ColorField : Field<Color>
        {
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

        /// <summary>
        /// Vector2 field.
        /// </summary>
        public class Vector2Field : Field<Vector2>
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
        }

        /// <summary>
        /// Vector3 field.
        /// </summary>
        public class Vector3Field : Field<Vector3>
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
        }

        /// <summary>
        /// Vector4 field.
        /// </summary>
        public class Vector4Field : Field<Vector4>
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
        }

        /// <summary>
        /// Object field.
        /// </summary>
        public class ObjectField : Field<Object>
        {
            /// <summary>
            /// Object type.
            /// </summary>
            public Type type = typeof(Object);
        }

        /// <summary>
        /// Object list field.
        /// </summary>
        public class ObjectListField : Field<Object[]>
        {
            /// <summary>
            /// Objects type.
            /// </summary>
            public Type type = typeof(Object);
        }

        /// <summary>
        /// Simple message box widget, providing a couple of different styles.
        /// </summary>
        public class MessageBox : Widget
        {
            /// <summary>
            /// Label style defines text color and background.
            /// </summary>
            public enum Style
            {
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
        }
    }
}
