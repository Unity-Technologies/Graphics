#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

using System;

#if ENABLE_RENDERING_DEBUGGER_UI
using UnityEngine.UIElements;
#endif

namespace UnityEngine.Rendering
{
    public partial class DebugUI
    {
        // Root panel class - we don't want to extend Container here because we need a clear
        // separation between debug panels and actual widgets
        /// <summary>
        /// Root panel class.
        /// </summary>
        public class Panel : IContainer, IComparable<Panel>
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            /// <inheritdoc/>
            public VisualElement Create(DebugUI.Context context)
            {
                VisualElement container = new GroupBox()
                {
                    text = displayName,
                    name = displayName + "_Content"
                };

                var label = container.Q<Label>(className:"unity-group-box__label");
                label.AddToClassList("debug-window-header-title");
                container.AddToClassList("debug-window-tab-content");
                container.AddToClassList("unity-inspector-element");

                var content = new VisualElement();
                foreach (var child in children)
                {
                    if (context == Context.Editor && child.isRuntimeOnly)
                        continue;
                    if (context.IsAnyRuntimeContext() && child.isEditorOnly)
                        continue;

                    var childUIElement = child.ToVisualElement(context);
                    if (childUIElement != null)
                        content.Add(childUIElement);
                }
                container.Add(content);

                return container;
            }
#endif

            /// <summary>
            /// Widget flags for this panel.
            /// </summary>
            public Flags flags { get; set; }
            /// <summary>
            /// Display name of the panel.
            /// </summary>
            public string displayName { get; set; }
            /// <summary>
            /// Group index of the panel.
            /// </summary>
            public int groupIndex { get; set; }
            /// <summary>
            /// Path of the panel.
            /// </summary>
            public string queryPath { get { return displayName; } }

            /// <summary>
            /// Specify if the panel is editor only.
            /// </summary>
            public bool isEditorOnly { get { return (flags & Flags.EditorOnly) != 0; } }
            /// <summary>
            /// Specify if the panel is runtime only.
            /// </summary>
            public bool isRuntimeOnly { get { return (flags & Flags.RuntimeOnly) != 0; } }
            /// <summary>
            /// Returns true if the panel is inactive in the editor.
            /// </summary>
            public bool isInactiveInEditor { get { return (isRuntimeOnly && !Application.isPlaying); } }
            /// <summary>
            /// Returns true if the panel should always be updated.
            /// </summary>
            [Obsolete("This is no longer used. #from(6000.5)")]
            public bool editorForceUpdate => false;

            /// <summary>
            /// List of children.
            /// </summary>
            public ObservableList<Widget> children { get; private set; }
            /// <summary>
            /// Callback used when the panel is set dirty.
            /// </summary>
            public event Action<Panel> onSetDirty = delegate { };

#if UNITY_EDITOR
            public string documentationUrl { get; set; }
#endif

            /// <summary>
            /// Constructor.
            /// </summary>
            public Panel()
            {
                children = new ObservableList<Widget>(0, (widget, widget1) => widget.order.CompareTo(widget1.order));
                children.ItemAdded += OnItemAdded;
                children.ItemRemoved += OnItemRemoved;
            }

            /// <summary>
            /// Callback used when a child is added.
            /// </summary>
            /// <param name="sender">Sender widget.</param>
            /// <param name="e">List of added children.</param>
            protected virtual void OnItemAdded(ObservableList<Widget> sender, ListChangedEventArgs<Widget> e)
            {
                if (e.item != null)
                {
                    e.item.panel = this;
                    e.item.parent = this;
                }

                SetDirty();
            }

            /// <summary>
            /// Callback used when a child is removed.
            /// </summary>
            /// <param name="sender">Sender widget.</param>
            /// <param name="e">List of removed children.</param>
            protected virtual void OnItemRemoved(ObservableList<Widget> sender, ListChangedEventArgs<Widget> e)
            {
                if (e.item != null)
                {
                    e.item.panel = null;
                    e.item.parent = null;
                }

                SetDirty();
            }

            /// <summary>
            /// Set the panel dirty.
            /// </summary>
            public void SetDirty()
            {
                int numChildren = children.Count;
                for (int i = 0; i < numChildren; i++)
                    children[i].GenerateQueryPath();

                onSetDirty(this);
            }

            /// <summary>
            /// Comparison function.
            /// </summary>
            /// <param name="other">Panel to compare to.</param>
            /// <returns>True if the panels share the same group index.</returns>
            int IComparable<Panel>.CompareTo(Panel other) => other == null ? 1 : groupIndex.CompareTo(other.groupIndex);

            internal bool TryFindChild<T>(string childDisplayName, out T foundChild)
                where T : DebugUI.Widget
            {
                foundChild = null;
                if (!string.IsNullOrEmpty(childDisplayName))
                {
                    foreach (var child in children)
                    {
                        if (child.displayName.Equals(childDisplayName) && child is T castedChild)
                        {
                            foundChild = castedChild;
                            break;
                        }
                    }
                }

                return foundChild != null;
            }
        }
    }
}
