using System;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// The `DebugUI` class defines a collection of debug UI components that are useful for debugging and profiling Rendering features
    /// </summary>
    /// <remarks>
     /// Widgets can be added to the UI, customized, and manipulated at runtime or during editor sessions. The class supports various
    /// widget types, including buttons, read-only value fields, and progress bars. It also provides mechanisms for organizing these
    /// widgets into containers. Each widget has a set of flags to control its visibility and behavior depending on whether it is
    /// used in the editor or at runtime. The widgets can also contain callbacks for conditional visibility at runtime.
    /// Important Notes:
    /// - Widgets can be nested inside containers, allowing for organized groupings of debug UI elements.
    /// - Widgets may be runtime-only, editor-only, or both, allowing them to behave differently depending on the application's
    ///   state (e.g., whether it is in the editor or playing at runtime).
    /// - `DebugUI` also includes helper methods for widget initialization, such as compact initialization using the `NameAndTooltip` struct.
    ///
    /// This API lets you do the following:
    /// - Specify widget behavior such as "EditorOnly", "RuntimeOnly", "EditorForceUpdate", and "FrequentlyUsed".
    /// - Show dynamic data with optional formatting.
    /// - Specify delegate functions to show or hide widgets
    ///
    /// <b>Related Resources:</b>
    /// - [Debug UI Overview](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/features/rendering-debugger.html)
    /// - [Rendering Debugger Controls](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/features/rendering-debugger-add-controls.html).
    /// - [Using Rendering Debugger](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@16.0/manual/use-the-rendering-debugger.html).
    /// </remarks>
    /// <example>
    /// <code>
    /// using UnityEngine;
    /// using UnityEngine.Rendering;
    ///
    /// public class DebugUIExample : MonoBehaviour
    /// {
    ///     private DebugUI.Button button;
    ///     private DebugUI.Value timeValue;
    ///
    ///     void Start()
    ///     {
    ///         // Create a button widget that logs a message when you select it
    ///         button = new DebugUI.Button
    ///         {
    ///             displayName = "Log Message Button",
    ///             action = () => Debug.Log("Button selected"),
    ///             isHiddenCallback = () => true,
    ///         };
    ///
    ///         // Create a value widget that displays the current time
    ///         timeValue = new DebugUI.Value
    ///         {
    ///             // Set the display label
    ///             displayName = "Current Time",
    ///
    ///             // Set the format for the time
    ///             getter = () => System.DateTime.Now.ToString("HH:mm:ss"),
    ///
    ///             // Set the value to refresh every second
    ///             refreshRate = 1f
    ///         };
    ///
    ///         // Add widgets to the UI (assuming a panel or container exists)
    ///         // ....
    ///     }
    /// }
    /// </code>
    /// </example>
    public partial class DebugUI
    {
        /// <summary>
        /// A column of checkboxes for enabling and disabling flags.
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
            /// <summary>
            /// The order of the widget
            /// </summary>
            public int order { get; set; } = 0;

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
            /// <param name="value">Input value.</param>
            /// <returns>Validated value.</returns>
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
        /// A field that displays a read-only value.
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
        /// A progress bar that displays values between 0% and 100%.
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
        /// An array of read-only values that Unity displays in a horizontal row.
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
