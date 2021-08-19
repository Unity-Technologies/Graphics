using System;

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
            public bool editorForceUpdate { get { return (flags & Flags.EditorForceUpdate) != 0; } }

            /// <summary>
            /// List of children.
            /// </summary>
            public ObservableList<Widget> children { get; private set; }
            /// <summary>
            /// Callback used when the panel is set dirty.
            /// </summary>
            public event Action<Panel> onSetDirty = delegate { };

            /// <summary>
            /// Constructor.
            /// </summary>
            public Panel()
            {
                children = new ObservableList<Widget>();
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
            /// Returns the hash code of the panel.
            /// </summary>
            /// <returns>Hash code of the panel.</returns>
            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 23 + displayName.GetHashCode();


                int numChildren = children.Count;
                for (int i = 0; i < numChildren; i++)
                    hash = hash * 23 + children[i].GetHashCode();

                return hash;
            }

            /// <summary>
            /// Comparison function.
            /// </summary>
            /// <param name="other">Panel to compare to.</param>
            /// <returns>True if the panels share the same group index.</returns>
            int IComparable<Panel>.CompareTo(Panel other) => other == null ? 1 : groupIndex.CompareTo(other.groupIndex);
        }
    }
}
