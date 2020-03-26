using System;
using System.Linq;
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
            public void SetValue(T value)
            {
                Assert.IsNotNull(setter);
                var v = ValidateValue(value);

                if (!v.Equals(getter()))
                {
                    setter(v);

                    if (onValueChanged != null)
                        onValueChanged(this, v);
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
        /// Enumerator field.
        /// </summary>
        public class EnumField : Field<int>
        {
            /// <summary>
            /// List of names of the enumerator entries.
            /// </summary>
            public GUIContent[] enumNames;
            /// <summary>
            /// List of values of the enumerator entries.
            /// </summary>
            public int[] enumValues;

            internal int[] quickSeparators;
            internal int[] indexes;

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
            public int currentIndex { get { return getIndex(); } set { setIndex(value); } }

            /// <summary>
            /// Generates enumerator values and names automatically based on the provided type.
            /// </summary>
            public Type autoEnum
            {
                set
                {
                    enumNames = Enum.GetNames(value).Select(x => new GUIContent(x)).ToArray();

                    // Linq.Cast<T> on a typeless Array breaks the JIT on PS4/Mono so we have to do it manually
                    //enumValues = Enum.GetValues(value).Cast<int>().ToArray();

                    var values = Enum.GetValues(value);
                    enumValues = new int[values.Length];
                    for (int i = 0; i < values.Length; i++)
                        enumValues[i] = (int)values.GetValue(i);

                    InitIndexes();
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

            internal void InitIndexes()
            {
                indexes = new int[enumNames.Length];
                for (int i = 0; i < enumNames.Length; i++)
                {
                    indexes[i] = i;
                }
            }
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
        public class BitField : Field<Enum>
        {
            /// <summary>
            /// List of names of the enumerator entries.
            /// </summary>
            public GUIContent[] enumNames { get; private set; }
            /// <summary>
            /// List of values of the enumerator entries.
            /// </summary>
            public int[] enumValues { get; private set; }

            internal Type m_EnumType;

            /// <summary>
            /// Generates bitfield values and names automatically based on the provided type.
            /// </summary>
            public Type enumType
            {
                set
                {
                    enumNames = Enum.GetNames(value).Select(x => new GUIContent(x)).ToArray();

                    // Linq.Cast<T> on a typeless Array breaks the JIT on PS4/Mono so we have to do it manually
                    //enumValues = Enum.GetValues(value).Cast<int>().ToArray();

                    var values = Enum.GetValues(value);
                    enumValues = new int[values.Length];
                    for (int i = 0; i < values.Length; i++)
                        enumValues[i] = (int)values.GetValue(i);

                    m_EnumType = value;
                }

                get { return m_EnumType; }
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
    }
}
