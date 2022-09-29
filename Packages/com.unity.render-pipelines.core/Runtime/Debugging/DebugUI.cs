using System;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug UI Class
    /// </summary>
    public partial class DebugUI
    {
        /// <summary>
        /// Flags for Debug UI widgets.
        /// </summary>
        [Flags]
        public enum Flags
        {
            /// <summary>
            /// None.
            /// </summary>
            None = 0,
            /// <summary>
            /// This widget is Editor only.
            /// </summary>
            EditorOnly = 1 << 1,
            /// <summary>
            /// This widget is Runtime only.
            /// </summary>
            RuntimeOnly = 1 << 2,
            /// <summary>
            /// This widget will force the Debug Editor Window refresh.
            /// </summary>
            EditorForceUpdate = 1 << 3,
            /// <summary>
            /// This widget will appear in the section "Frequently Used"
            /// </summary>
            FrequentlyUsed = 1 << 4

        }

        /// <summary>
        /// Base class for all debug UI widgets.
        /// </summary>
        public abstract class Widget
        {
            // Set to null until it's added to a panel, be careful
            /// <summary>
            /// Panels containing the widget.
            /// </summary>
            protected Panel m_Panel;

            /// <summary>
            /// Panels containing the widget.
            /// </summary>
            public virtual Panel panel
            {
                get { return m_Panel; }
                internal set { m_Panel = value; }
            }

            /// <summary>
            /// Parent container.
            /// </summary>
            protected IContainer m_Parent;

            /// <summary>
            /// Parent container.
            /// </summary>
            public virtual IContainer parent
            {
                get { return m_Parent; }
                internal set { m_Parent = value; }
            }

            /// <summary>
            /// Flags for the widget.
            /// </summary>
            public Flags flags { get; set; }

            /// <summary>
            /// Display name.
            /// </summary>
            public string displayName { get; set; }

            /// <summary>
            /// Tooltip.
            /// </summary>
            public string tooltip { get; set; }

            /// <summary>
            /// Path of the widget.
            /// </summary>
            public string queryPath { get; private set; }

            /// <summary>
            /// True if the widget is Editor only.
            /// </summary>
            public bool isEditorOnly => flags.HasFlag(Flags.EditorOnly);

            /// <summary>
            /// True if the widget is Runtime only.
            /// </summary>
            public bool isRuntimeOnly => flags.HasFlag(Flags.RuntimeOnly);

            /// <summary>
            /// True if the widget is inactive in the editor (i.e. widget is runtime only and the application is not 'Playing').
            /// </summary>
            public bool isInactiveInEditor => (isRuntimeOnly && !Application.isPlaying);

            /// <summary>
            /// Optional delegate that can be used to conditionally hide widgets at runtime (e.g. due to state of other widgets).
            /// </summary>
            public Func<bool> isHiddenCallback;

            /// <summary>
            /// If <see cref="isHiddenCallback">shouldHideDelegate</see> has been set and returns true, the widget is hidden from the UI.
            /// </summary>
            public bool isHidden => isHiddenCallback?.Invoke() ?? false;

            internal virtual void GenerateQueryPath()
            {
                queryPath = displayName.Trim();

                if (m_Parent != null)
                    queryPath = m_Parent.queryPath + " -> " + queryPath;
            }

            /// <summary>
            /// Returns the hash code of the widget.
            /// </summary>
            /// <returns>The hash code of the widget.</returns>
            public override int GetHashCode()
            {
                return queryPath.GetHashCode() ^ isHidden.GetHashCode();
            }

            /// <summary>
            /// Helper struct to allow more compact initialization of widgets.
            /// </summary>
            public struct NameAndTooltip
            {
                /// <summary>
                /// The name
                /// </summary>
                public string name;
                /// <summary>
                /// The tooltip
                /// </summary>
                public string tooltip;
            }

            /// <summary>
            /// Helper setter to allow more compact initialization of widgets.
            /// </summary>
            public NameAndTooltip nameAndTooltip
            {
                set
                {
                    displayName = value.name;
                    tooltip = value.tooltip;
                }
            }
        }

        /// <summary>
        /// Interface for widgets that can contain other widgets.
        /// </summary>
        public interface IContainer
        {
            /// <summary>
            /// List of children of the container.
            /// </summary>
            ObservableList<Widget> children { get; }

            /// <summary>
            /// Display name of the container.
            /// </summary>
            string displayName { get; set; }

            /// <summary>
            /// Path of the container.
            /// </summary>
            string queryPath { get; }
        }

        /// <summary>
        /// Any widget that implements this will be considered for serialization (only if the setter is set and thus is not read-only)
        /// </summary>
        public interface IValueField
        {
            /// <summary>
            /// Return the value of the field.
            /// </summary>
            /// <returns>Value of the field.</returns>
            object GetValue();

            /// <summary>
            /// Set the value of the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            void SetValue(object value);

            /// <summary>
            /// Function used to validate the value when setting it.
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            object ValidateValue(object value);
        }

        // Miscellaneous
        /// <summary>
        /// Button widget.
        /// </summary>
        public class Button : Widget
        {
            /// <summary>
            /// Action performed by the button.
            /// </summary>
            public Action action { get; set; }
        }

        /// <summary>
        /// Read only Value widget.
        /// </summary>
        public class Value : Widget
        {
            /// <summary>
            /// Getter for the Value.
            /// </summary>
            public Func<object> getter { get; set; }

            /// <summary>
            /// Refresh rate for the read-only value (runtime only)
            /// </summary>
            public float refreshRate = 0.1f;

            /// <summary>
            /// Optional C# numeric format string, using following syntax: "{0[:numericFormatString]}"
            /// See https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
            /// and https://docs.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting
            /// Example: 123.45678 with formatString "{0:F2} ms" --> "123.45 ms".
            /// </summary>
            public string formatString = null;

            /// <summary>
            /// Constructor.
            /// </summary>
            public Value()
            {
                displayName = "";
            }

            /// <summary>
            /// Returns the value of the widget.
            /// </summary>
            /// <returns>The value of the widget.</returns>
            public virtual object GetValue()
            {
                Assert.IsNotNull(getter);
                return getter();
            }

            /// <summary>
            /// Returns the formatted value string for display purposes.
            /// </summary>
            /// <param name="value">Value to be formatted.</param>
            /// <returns>The formatted value string.</returns>
            public virtual string FormatString(object value)
            {
                return string.IsNullOrEmpty(formatString) ? $"{value}" : string.Format(formatString, value);
            }
        }

        /// <summary>
        /// Progress bar value.
        /// </summary>
        public class ProgressBarValue : Value
        {
            /// <summary>
            /// Minimum value.
            /// </summary>
            public float min = 0f;
            /// <summary>
            /// Maximum value.
            /// </summary>
            public float max = 1f;

            /// <summary>
            /// Get the current progress string, remapped to [0, 1] range, representing the progress between min and max.
            /// </summary>
            /// <param name="value">Value to be formatted.</param>
            /// <returns>Formatted progress percentage string between 0% and 100%.</returns>
            public override string FormatString(object value)
            {
                static float Remap01(float v, float x0, float y0) => (v - x0) / (y0 - x0);

                float clamped = Mathf.Clamp((float)value, min, max);
                float percentage = Remap01(clamped, min, max);
                return $"{percentage:P1}";
            }
        }

        /// <summary>
        /// Tuple of Value widgets for creating tabular UI.
        /// </summary>
        public class ValueTuple : Widget
        {
            /// <summary>
            /// Number of elements in the tuple.
            /// </summary>
            public int numElements
            {
                get
                {
                    Assert.IsTrue(values.Length > 0);
                    return values.Length;
                }
            }

            /// <summary>
            /// Value widgets.
            /// </summary>
            public Value[] values;

            /// <summary>
            /// Refresh rate for the read-only values (runtime only)
            /// </summary>
            public float refreshRate => values.FirstOrDefault()?.refreshRate ?? 0.1f;

            /// <summary>
            /// The currently pinned element index, or -1 if none are pinned.
            /// </summary>
            public int pinnedElementIndex = -1;
        }
    }
}
