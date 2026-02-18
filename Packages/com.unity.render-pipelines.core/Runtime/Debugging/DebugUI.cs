#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
#if ENABLE_RENDERING_DEBUGGER_UI
using UnityEngine.UIElements;
#endif

namespace UnityEngine.Rendering
{
#if ENABLE_RENDERING_DEBUGGER_UI
    internal static class DebugUIExtensions
    {
        public static List<(Label, VisualElement)> CreatePanels(List<DebugUI.Panel> panels, DebugUI.Context context)
        {
            List<(Label, VisualElement)> returnList = new();
            foreach (var p  in panels)
            {
                var tab = new Label(p.displayName)
                {
                    name = p.displayName + "_Tab",
                    focusable = true
                };
                tab.AddToClassList("debug-window-tab-item");

                var panel = p.Create(context);

                returnList.Add((tab, panel));
            }
            return returnList;
        }

        /// <summary>
        /// Utility to add a scheduled item to the panel's tracker so it can be disabled when the widget is not visible.
        /// </summary>
        /// <param name="widget">Widget that contains the scheduler</param>
        /// <param name="element">VisualElement whose scheduler is used to create the ScheduledItem</param>
        /// <param name="scheduledItemCreator">Callback that creates the ScheduledItem.</param>
        internal static void ScheduleTracked(this DebugUI.Widget widget, VisualElement element,  Func<IVisualElementScheduledItem> scheduledItemCreator)
        {
            // When the element is attached to a panel, we invoke the scheduledItem function to create the scheduler,
            // and add it to DebugManager's tracking list.
            element.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                var scheduledItem = scheduledItemCreator.Invoke();
                DebugManager.instance.schedulerTracker.Add(widget.m_Context, widget, scheduledItem);
                scheduledItem.Pause(); // All schedulers start paused, and are enabled/disabled by DebugManager based on widget visibility.
            });

            // When the element is detached from a panel, all ScheduledItems corresponding to that element are cleaned up.
            element.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                DebugManager.instance.schedulerTracker.Remove(widget.m_Context, widget, element);
            });
        }

        public static bool IsAnyRuntimeContext(this DebugUI.Context context)
        {
            return (context == DebugUI.Context.Runtime) ||
                   (context == DebugUI.Context.RuntimePersistent);
        }
    }
#endif

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
    /// - Specify widget behavior such as "EditorOnly" and "RuntimeOnly".
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
    /// [ExecuteInEditMode]
    /// public class DebugUIExample : MonoBehaviour
    /// {
    ///     void OnEnable()
    ///     {
    ///         // Create a button widget that logs a message when you select it
    ///         DebugUI.Button button = new DebugUI.Button
    ///         {
    ///             displayName = "Log Message Button",
    ///             action = () => Debug.Log("Button selected")
    ///         };
    ///
    ///         // Create a value widget that displays the current time
    ///         DebugUI.Value timeValue = new DebugUI.Value
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
    ///         // Add widgets a new Rendering Debugger panel
    ///         var panel = DebugManager.instance.GetPanel("My Custom Panel", createIfNull: true);
    ///
    ///         // Add the widgets to the panel
    ///         panel.children.Add(button);
    ///         panel.children.Add(timeValue);
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
            [Obsolete("This is no longer used. #from(6000.5)")]
            EditorForceUpdate = 1 << 3,
            /// <summary>
            /// This widget will appear in the section "Frequently Used"
            /// </summary>
            [Obsolete("This is no longer used. #from(6000.5)")]
            FrequentlyUsed = 1 << 4
        }

        /// <summary>
        /// Context in which the widget is used.
        /// </summary>
        public enum Context
        {
            /// <summary>Invalid context</summary>
            Invalid = -1,
            /// <summary>Editor context</summary>
            Editor = 0,
            /// <summary>Runtime context</summary>
            Runtime = 1,
            /// <summary>Runtime persistent context</summary>
            RuntimePersistent = 2,

            /// <summary>Count</summary>
            Count
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

#if ENABLE_RENDERING_DEBUGGER_UI
            /// <summary>
            /// Creates the VisualElement for the widget that will be added to the panel UI.
            /// </summary>
            /// <returns>The widget's VisualElement</returns>
            protected abstract VisualElement Create();

            /// <summary>
            /// Controls the way the widget is hidden.
            /// When set to false (default), the widget is removed from the layout (DisplayStyle.None).
            /// When set to true, the widget is not rendered but takes space in the layout (Visibility.Hidden).
            /// </summary>
            internal bool keepLayoutWhenHidden { private get; set; }

            internal bool m_IsHiddenBySearchFilter;

            internal string m_AdditionalSearchText = string.Empty;

            protected internal bool m_RequiresLegacyStateHandling = false;

            void UpdateElementVisibility()
            {
                if (m_IsHiddenBySearchFilter)
                {
                    m_VisualElement.style.display = DisplayStyle.None;
                    return;
                }

                if (keepLayoutWhenHidden)
                {
                    m_VisualElement.style.display = DisplayStyle.Flex;
                    m_VisualElement.style.visibility = isHidden ? Visibility.Hidden : Visibility.Visible;
                }
                else
                {
                    m_VisualElement.style.display = isHidden ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }

            int ComputeIndentationLevel()
            {
                int indentLevel = 0;
                Container currentParent = parent as Container;
                while (currentParent != null)
                {
                    indentLevel++;
                    currentParent = currentParent.parent as Container;
                }
                return indentLevel;
            }

            // Once the element is attached to the panel, recalculate the base field label width
            // whenever the window is resized, or the tab/panel divider is moved.
            internal void CustomAlignBaseFieldLabelWhenResized(VisualElement label)
            {
                if (m_Context == Context.Editor)
                {
                    label.RegisterCallbackOnce<AttachToPanelEvent>(panelEvt =>
                    {
                        // Use the tab container element (we want to exclude tabs)
                        var contentPanel = panelEvt.destinationPanel.visualTree.Q<VisualElement>("debug-window-tab-container");
                        contentPanel.RegisterCallback<GeometryChangedEvent>(geometryEvt =>
                        {
                            CustomAlignBaseFieldLabel(label, geometryEvt.newRect.width);
                        });
                    });
                }
            }

            // We need to do base field label alignment manually in two cases:
            // 1. In runtime context, where UIE.BaseField<T> alignment logic is disabled
            // 2. In any context, for elements that do not inherit from UIE.BaseField<T>, but we still want
            //    to align them, such as DebugUI.Value and DebugUI.ProgressBar.
            internal void CustomAlignBaseFieldLabel(VisualElement label, float width = 0f)
            {
                if (label != null)
                {
                    int indentationLevel = ComputeIndentationLevel();
                    if (m_Context == Context.Runtime)
                    {
                        // NOTE: In the runtime we don't care about the window width, just use a fixed label width.
                        const float kLabelBaseWidth = 330f;
                        const float kIndentSize = 8f; // Should match .debug-window-container-content margin-left
                        float totalPadding = indentationLevel*kIndentSize;
                        label.style.minWidth = kLabelBaseWidth - totalPadding;
                    }
                    else
                    {
                        // This is trying to match the BaseField field alignment logic as closely as possible.
                        // For the magic numbers & full logic see UIElements/Core/Controls/InputField/BaseField.cs
                        const float kLabelWidthRatio = 0.45f; // Should match UIElements.BaseField m_LabelWidthRatio
                        const float kLabelExtraPadding = 37.0f; // Should match UIElements.BaseField m_LabelExtraPadding
                        const float kIndentSize = 8f;
                        float totalPadding = indentationLevel*kIndentSize + kLabelExtraPadding;
                        label.style.minWidth = Mathf.Ceil(width * kLabelWidthRatio) - totalPadding;
                    }
                }
            }

            internal VisualElement ToVisualElement(Context context)
            {
                m_Context = context;
                m_VisualElement = Create();

                //Debug.Log($"ToVisualElement for {queryPath}");

                if (m_VisualElement == null)
                {
                    Debug.LogWarning($"Unable to create a Visual Element for type {GetType()}");
                    return null;
                }
                m_VisualElement.AddToClassList("unity-inspector-element");


#if UNITY_EDITOR
                // Support for legacy state handling
                if (this is ISupportsLegacyStateHandling legacyStateWidget)
                {
                    m_RequiresLegacyStateHandling = legacyStateWidget.RequiresLegacyStateHandling();
                    //Debug.Log($"LegacyState: {m_RequiresLegacyStateHandling} ({queryPath})");
                }
#endif

                UpdateElementVisibility();
                this.ScheduleTracked(m_VisualElement, () => m_VisualElement.schedule.Execute(UpdateElementVisibility).Every(100));

                // In runtime window, figure out indentation level to ensure field alignment
                if (context == Context.Runtime)
                {
                    m_VisualElement.RegisterCallbackOnce<AttachToPanelEvent>(_ =>
                    {
                        CustomAlignBaseFieldLabel(m_VisualElement.Q<Label>(classes: "unity-base-field__label"));
                    });
                }

                // In runtime UI, attach focus handler for keyboard navigation & pinning
                if (context.IsAnyRuntimeContext())
                {
                    VisualElement focusTarget = m_VisualElement;
                    if (this is Foldout)
                    {
                        focusTarget = m_VisualElement.Q<Toggle>();
                    }

                    focusTarget?.RegisterCallback<FocusInEvent>(evt =>
                    {
                        var targetElement = evt.target as VisualElement;
                        if (DebugManager.instance.m_RuntimeDebugWindow?.IsPopupOpen() ?? false)
                        {
                            if (targetElement != null)
                            {
                                var elementThatGetFocus = evt.target as VisualElement;
                                var focusController = targetElement.panel?.focusController;
                                if (focusController != null)
                                {
                                    // Blur the entering focus to restore it to the previous one.
                                    elementThatGetFocus.Blur();
                                }
                            }
                            evt.StopImmediatePropagation();
                        }
                        else if (targetElement != null)
                        {
                            // Ensure focused element is visible in the scroll view
                            var scrollView = targetElement.GetFirstAncestorOfType<ScrollView>();
                            scrollView.ScrollTo(targetElement);
                        }
                    }, TrickleDown.TrickleDown);

                    focusTarget?.RegisterCallback<FocusEvent>(_ =>
                    {
                        DebugManager.instance.selectedWidget = this;
                    });
                }

                return m_VisualElement;
            }

            // Root visual element of the widget, valid after it has been initialized by calling ToVisualElement.
            internal VisualElement m_VisualElement = null;

            // Context of the widget, valid after it has been initialized by calling ToVisualElement.
            internal Context m_Context = Context.Invalid;
#endif

            /// <summary>
            /// Increment button implementation for numeric fields
            /// </summary>
            /// <param name="fast">Whether to use fast increment</param>
            internal virtual void OnIncrement(bool fast) { }

            /// <summary>
            /// Decrement button implementation for numeric fields
            /// </summary>
            /// <param name="fast">Whether to use fast decrement</param>
            internal virtual void OnDecrement(bool fast) { }

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
        [Obsolete("This interface is no longer used. #from(6000.5)")]
        public interface IValueField
        {
            /// <summary>
            /// Return the value of the field.
            /// </summary>
            /// <returns>Value of the field.</returns>
            [Obsolete("This method is no longer used. #from(6000.5)")]
            object GetValue();

            /// <summary>
            /// Set the value of the field.
            /// </summary>
            /// <param name="value">Input value.</param>
            [Obsolete("This method is no longer used. #from(6000.5)")]
            void SetValue(object value);

            /// <summary>
            /// Function used to validate the value when setting it.
            /// </summary>
            /// <param name="value">Input value.</param>
            /// <returns>Validated value.</returns>
            [Obsolete("This method is no longer used. #from(6000.5)")]
            object ValidateValue(object value);
        }

        // Miscellaneous
        /// <summary>
        /// Button widget.
        /// </summary>
        public class Button : Widget
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var button = new UIElements.Button
                {
                    text = displayName
                };
                button.AddToClassList("debug-window-button");
                button.AddToClassList("debug-window-search-filter-target");
                button.clicked += () => action();
                return button;
            }
#endif

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
            /// If is a header, and it should be displayed with bold font
            /// </summary>
            public bool isHeader { get; set; }

#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var container = new VisualElement();
                container.AddToClassList("debug-window-value-container");

                var nameLabel = new Label() { text = displayName };
                nameLabel.AddToClassList("debug-window-value-name");
                nameLabel.AddToClassList("debug-window-search-filter-target");

                var valueLabel = new Label() { text = FormatString(GetValue()) };
                valueLabel.AddToClassList("debug-window-value-value");
                valueLabel.focusable = true;

                if (isHeader)
                {
                    valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                }

                this.ScheduleTracked(valueLabel, () => valueLabel.schedule.Execute(() =>
                {
                    valueLabel.text = FormatString(GetValue());
                }).Every((long)(refreshRate * 1000.0f)));

                CustomAlignBaseFieldLabelWhenResized(nameLabel);

                container.Add(nameLabel);
                container.Add(valueLabel);

                return container;
            }
#endif

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

#if ENABLE_RENDERING_DEBUGGER_UI
            protected override VisualElement Create()
            {
                var container = new VisualElement();
                container.AddToClassList(UIElements.BaseField<float>.ussClassName);
                container.style.flexDirection = FlexDirection.Row;

                var label = new Label();
                label.AddToClassList(UIElements.BaseField<float>.labelUssClassName);
                label.text = displayName;
                container.Add(label);

                var progressBar = new UIElements.ProgressBar()
                {
                    lowValue = min,
                    highValue = max
                };
                progressBar.style.flexGrow = 1;

                progressBar.schedule.Execute(() =>
                {
                    progressBar.title = FormatString(GetValue());
                    progressBar.value = (float)GetValue();
                }).Every((long)(refreshRate * 1000.0f));

                CustomAlignBaseFieldLabelWhenResized(label);

                container.AddToClassList(UIElements.BaseField<float>.alignedFieldUssClassName);
                container.Add(progressBar);

                 return container;
            }
#endif
        }

        /// <summary>
        /// An array of read-only values that Unity displays in a horizontal row.
        /// </summary>
        public class ValueTuple : Widget
        {
            internal const int k_LabelWidthEditor = 280;
            internal const int k_LabelWidthRuntime = 340;

            internal static int GetLabelWidth(Context ctx) => ctx == Context.Editor ? k_LabelWidthEditor : k_LabelWidthRuntime;

            /// <summary>
            /// If is a header, and it should be displayed with bold font
            /// </summary>
            public bool isHeader { get; set; }

#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            protected override VisualElement Create()
            {
                var valueContainer = new UIElements.VisualElement();
                valueContainer.AddToClassList("debug-window-valuetuple");

                var label = new Label(displayName) { style = { minWidth = ValueTuple.GetLabelWidth(m_Context) }, };
                label.AddToClassList("debug-window-search-filter-target");

                if (isHeader)
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;

                valueContainer.Add(label);

                if (!isHeader)
                    valueContainer.focusable = true;

                foreach (var value in values)
                {
                    value.isHeader = isHeader;
                    var valueVisualElement = value.ToVisualElement(m_Context);
                    var valueLabel = valueVisualElement.Q<Label>(className: "debug-window-value-name");
                    if (valueLabel != null)
                        valueLabel.style.display = DisplayStyle.None; // With ValueTuples we don't want to show individual value names
                    valueContainer.Add(valueVisualElement);
                }

                return valueContainer;
            }
#endif

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
            public float refreshRate
            {
                get
                {
                    if (values != null && values.Length > 0 && values[0] != null)
                    {
                        return values[0].refreshRate;
                    }
                    return 0.1f;
                }
            }

            /// <summary>
            /// The currently pinned element index, or -1 if none are pinned.
            /// </summary>
            public int pinnedElementIndex = -1;
        }
    }
}
