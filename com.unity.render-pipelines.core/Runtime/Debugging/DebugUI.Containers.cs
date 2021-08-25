using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public partial class DebugUI
    {
        /// <summary>
        /// Base class for "container" type widgets, although it can be used on its own (if a display name is set then it'll behave as a group with a header)
        /// </summary>
        public class Container : Widget, IContainer
        {
            /// <summary>
            /// List of children.
            /// </summary>
            public ObservableList<Widget> children { get; private set; }

            /// <summary>
            /// Panel the container is attached to.
            /// </summary>
            public override Panel panel
            {
                get { return m_Panel; }
                internal set
                {
                    m_Panel = value;

                    // Bubble down
                    foreach (var child in children)
                        child.panel = value;
                }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            public Container()
            {
                displayName = "";
                children = new ObservableList<Widget>();
                children.ItemAdded += OnItemAdded;
                children.ItemRemoved += OnItemRemoved;
            }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="displayName">Display name of the container.</param>
            /// <param name="children">List of attached children.</param>
            public Container(string displayName, ObservableList<Widget> children)
            {
                this.displayName = displayName;
                this.children = children;
                children.ItemAdded += OnItemAdded;
                children.ItemRemoved += OnItemRemoved;
            }

            internal override void GenerateQueryPath()
            {
                base.GenerateQueryPath();

                foreach (var child in children)
                    child.GenerateQueryPath();
            }

            /// <summary>
            /// Method called when a children is added.
            /// </summary>
            /// <param name="sender">Sender widget.</param>
            /// <param name="e">List of added children.</param>
            protected virtual void OnItemAdded(ObservableList<Widget> sender, ListChangedEventArgs<Widget> e)
            {
                if (e.item != null)
                {
                    e.item.panel = m_Panel;
                    e.item.parent = this;
                }

                if (m_Panel != null)
                    m_Panel.SetDirty();
            }

            /// <summary>
            /// Method called when a children is removed.
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

                if (m_Panel != null)
                    m_Panel.SetDirty();
            }

            /// <summary>
            /// Returns the hash code of the widget.
            /// </summary>
            /// <returns>Hash code of the widget.</returns>
            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 23 + queryPath.GetHashCode();

                foreach (var child in children)
                    hash = hash * 23 + child.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Unity-like foldout that can be collapsed.
        /// </summary>
        public class Foldout : Container, IValueField
        {
            /// <summary>
            /// Context menu item.
            /// </summary>
            public struct ContextMenuItem
            {
                /// <summary>
                /// Name of the item displayed in context menu dropdown.
                /// </summary>
                public string displayName;

                /// <summary>
                /// Callback when context menu item is selected.
                /// </summary>
                public Action action;
            }

            /// <summary>
            /// Always false.
            /// </summary>
            public bool isReadOnly { get { return false; } }

            /// <summary>
            /// Opened state of the foldout.
            /// </summary>
            public bool opened;

            /// <summary>
            /// Draw the foldout in full width using a header style.
            /// </summary>
            public bool isHeader;

            /// <summary>
            /// Optional list of context menu items. If the list is not provided, no context menu button will be displayed.
            /// </summary>
            public List<ContextMenuItem> contextMenuItems = null;

            /// <summary>
            /// List of columns labels.
            /// </summary>
            public string[] columnLabels { get; set; } = null;

            /// <summary>
            /// List of columns label tooltips.
            /// </summary>
            public string[] columnTooltips { get; set; } = null;

            /// <summary>
            /// Constructor.
            /// </summary>
            public Foldout() : base() { }
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="displayName">Display name of the foldout.</param>
            /// <param name="children">List of attached children.</param>
            /// <param name="columnLabels">Optional list of column names.</param>
            /// <param name="columnTooltips">Optional list of tooltips for column name labels.</param>
            public Foldout(string displayName, ObservableList<Widget> children, string[] columnLabels = null, string[] columnTooltips = null)
                : base(displayName, children)
            {
                this.columnLabels = columnLabels;
                this.columnTooltips = columnTooltips;
            }

            /// <summary>
            /// Get the opened state of the foldout.
            /// </summary>
            /// <returns>True if the foldout is opened.</returns>
            public bool GetValue() => opened;

            /// <summary>
            /// Get the opened state of the foldout.
            /// </summary>
            /// <returns>True if the foldout is opened.</returns>
            object IValueField.GetValue() => GetValue();

            /// <summary>
            /// Set the opened state of the foldout.
            /// </summary>
            /// <param name="value">True to open the foldout, false to close it.</param>
            public void SetValue(object value) => SetValue((bool)value);

            /// <summary>
            /// Validates the value of the widget before setting it.
            /// </summary>
            /// <param name="value">Input value.</param>
            /// <returns>The validated value.</returns>
            public object ValidateValue(object value) => value;

            /// <summary>
            /// Set the value of the widget.
            /// </summary>
            /// <param name="value">Input value.</param>
            public void SetValue(bool value) => opened = value;
        }

        /// <summary>
        /// Horizontal Layout Container.
        /// </summary>
        public class HBox : Container
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public HBox()
            {
                displayName = "HBox";
            }
        }

        /// <summary>
        /// Vertical Layout Container.
        /// </summary>
        public class VBox : Container
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public VBox()
            {
                displayName = "VBox";
            }
        }

        /// <summary>
        /// Array Container.
        /// </summary>
        public class Table : Container
        {
            /// <summary>Row Container.</summary>
            public class Row : Foldout
            {
                /// <summary>Constructor.</summary>
                public Row() { displayName = "Row"; }
            }

            /// <summary>
            /// True if the table is read only.
            /// </summary>
            public bool isReadOnly = false;

            /// <summary>Constructor.</summary>
            public Table() { displayName = "Array"; }

            /// <summary>
            /// Set column visibility.
            /// </summary>
            /// <param name="index">Index of the column.</param>
            /// <param name="visible">True if the column should be visible.</param>
            public void SetColumnVisibility(int index, bool visible)
            {
#if UNITY_EDITOR
                var header = Header;
                if (index < 0 || index >= m_ColumnCount)
                    return;

                index++;
                if (header.IsColumnVisible(index) != visible)
                {
                    var newVisibleColumns = new System.Collections.Generic.List<int>(header.state.visibleColumns);
                    if (newVisibleColumns.Contains(index))
                    {
                        newVisibleColumns.Remove(index);
                    }
                    else
                    {
                        newVisibleColumns.Add(index);
                        newVisibleColumns.Sort();
                    }
                    header.state.visibleColumns = newVisibleColumns.ToArray();

                    var cols = header.state.columns;
                    for (int i = 0; i < cols.Length; i++)
                        cols[i].width = 50f;
                    header.ResizeToFit();
                }
#else
                var columns = VisibleColumns;
                if (index < 0 || index > columns.Length)
                    return;

                columns[index] = visible;
#endif
            }

            /// <summary>
            /// Get column visibility.
            /// </summary>
            /// <param name="index">Index of the column.</param>
            /// <returns>True if the column is visible.</returns>
            public bool GetColumnVisibility(int index)
            {
#if UNITY_EDITOR
                var header = Header;
                if (index < 0 || index >= m_ColumnCount)
                    return false;

                return header.IsColumnVisible(index + 1);
#else
                var columns = VisibleColumns;
                if (index < 0 || index > columns.Length)
                    return false;

                return columns[index];
#endif
            }

#if UNITY_EDITOR
            /// <summary>
            /// The scroll position of the table.
            /// </summary>
            public Vector2 scroll = Vector2.zero;

            int m_ColumnCount;
            UnityEditor.IMGUI.Controls.MultiColumnHeader m_Header = null;

            /// <summary>
            /// The table header for drawing
            /// </summary>
            public UnityEditor.IMGUI.Controls.MultiColumnHeader Header
            {
                get
                {
                    if (m_Header != null)
                        return m_Header;

                    if (children.Count != 0)
                    {
                        m_ColumnCount = ((Container)children[0]).children.Count;
                        for (int i = 1; i < children.Count; i++)
                        {
                            if (((Container)children[i]).children.Count != m_ColumnCount)
                            {
                                Debug.LogError("All rows must have the same number of children.");
                                return null;
                            }
                        }
                    }

                    UnityEditor.IMGUI.Controls.MultiColumnHeaderState.Column CreateColumn(string name)
                    {
                        var col = new UnityEditor.IMGUI.Controls.MultiColumnHeaderState.Column()
                        {
                            canSort = false,
                            headerTextAlignment = TextAlignment.Center,
                            headerContent = new GUIContent(name),
                        };

                        GUIStyle style = UnityEditor.IMGUI.Controls.MultiColumnHeader.DefaultStyles.columnHeaderCenterAligned;
                        style.CalcMinMaxWidth(col.headerContent, out col.width, out float _);
                        col.width = Mathf.Min(col.width, 50f);
                        return col;
                    }

                    var cols = new UnityEditor.IMGUI.Controls.MultiColumnHeaderState.Column[m_ColumnCount + 1];
                    cols[0] = CreateColumn(displayName);
                    cols[0].allowToggleVisibility = false;
                    for (int i = 0; i < m_ColumnCount; i++)
                        cols[i + 1] = CreateColumn(((Container)children[0]).children[i].displayName);

                    var state = new UnityEditor.IMGUI.Controls.MultiColumnHeaderState(cols);
                    m_Header = new UnityEditor.IMGUI.Controls.MultiColumnHeader(state) { height = 23 };
                    m_Header.ResizeToFit();
                    return m_Header;
                }
            }
#else
            bool[] m_Header = null;

            /// <summary>
            /// The visible columns
            /// </summary>
            public bool[] VisibleColumns
            {
                get
                {
                    if (m_Header != null)
                        return m_Header;

                    int columnCount = 0;
                    if (children.Count != 0)
                    {
                        columnCount = ((Container)children[0]).children.Count;
                        for (int i = 1; i < children.Count; i++)
                        {
                            if (((Container)children[i]).children.Count != columnCount)
                            {
                                Debug.LogError("All rows must have the same number of children.");
                                return null;
                            }
                        }
                    }

                    m_Header = new bool[columnCount];
                    for (int i = 0; i < columnCount; i++)
                        m_Header[i] = true;

                    return m_Header;
                }
            }
#endif

            /// <summary>
            /// Method called when a children is added.
            /// </summary>
            /// <param name="sender">Sender widget.</param>
            /// <param name="e">List of added children.</param>
            protected override void OnItemAdded(ObservableList<Widget> sender, ListChangedEventArgs<Widget> e)
            {
                base.OnItemAdded(sender, e);
                m_Header = null;
            }

            /// <summary>
            /// Method called when a children is removed.
            /// </summary>
            /// <param name="sender">Sender widget.</param>
            /// <param name="e">List of removed children.</param>
            protected override void OnItemRemoved(ObservableList<Widget> sender, ListChangedEventArgs<Widget> e)
            {
                base.OnItemRemoved(sender, e);
                m_Header = null;
            }
        }
    }
}
