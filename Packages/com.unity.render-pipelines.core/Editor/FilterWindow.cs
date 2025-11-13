using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// An utility window used to list and filter a set of elements, as seen in the inspector when
    /// clicking on the "Add Component" button.
    /// </summary>
    [InitializeOnLoad]
    public class FilterWindow : EditorWindow
    {
        /// <summary>
        /// The interface to implement to populate the list or tree and traverse its elements.
        /// </summary>
        public interface IProvider
        {
            /// <summary>
            /// The position of the window on screen.
            /// </summary>
            Vector2 position { get; set; }

            /// <summary>
            /// Implement this method to populate the list or tree of elements.
            /// </summary>
            /// <param name="tree">The list to populate.</param>
            void CreateComponentTree(List<Element> tree);

            /// <summary>
            /// Implement this method to define the behavior when an item is selected.
            /// </summary>
            /// <param name="element">The selected element.</param>
            /// <param name="addIfComponent">A flag indicating whether to add the element as a component if applicable.</param>
            /// <returns><c>true</c> if the window should close, <c>false</c> otherwise.</returns>
            bool GoToChild(Element element, bool addIfComponent);
        }

        /// <summary>
        /// The default width for the window.
        /// </summary>
        public static readonly float DefaultWidth = 250f;

        /// <summary>
        /// The default height for the window.
        /// </summary>
        public static readonly float DefaultHeight = 300f;

        #region BaseElements

        /// <summary>
        /// An element from the filtered list or tree.
        /// </summary>
        /// <seealso cref="GroupElement"/>
        public class Element : IComparable
        {
            /// <summary>
            /// The current hierarchical level in the tree.
            /// </summary>
            public int level;

            /// <summary>
            /// The displayed content for the element.
            /// </summary>
            public GUIContent content;

            /// <summary>
            /// The name of the element as displayed in the UI.
            /// </summary>
            public string name
            {
                get { return content.text; }
            }

            /// <summary>
            /// Compares this element to another object.
            /// </summary>
            /// <param name="o">The object to compare to.</param>
            /// <returns><c>true</c> if both objects are the same, <c>false</c> otherwise.</returns>
            public int CompareTo(object o)
            {
                return name.CompareTo((o as Element).name);
            }
        }

        /// <summary>
        /// A meta element used to group several elements in the list or tree.
        /// </summary>
        /// <seealso cref="Element"/>
        [Serializable]
        public class GroupElement : Element
        {
            /// <summary>
            /// The current scroll position in the UI.
            /// </summary>
            public Vector2 scroll;

            /// <summary>
            /// The current selected index in the group.
            /// </summary>
            public int selectedIndex;

            /// <summary>
            /// Requests focus for the element.
            /// </summary>
            public bool WantsFocus { get; protected set; }

            /// <summary>
            /// Returns <c>true</c> if this group and its content should appear disabled in the UI.
            /// </summary>
            public virtual bool ShouldDisable
            {
                get { return false; }
            }

            /// <summary>
            /// Creates a new <see cref="GroupElement"/>
            /// </summary>
            /// <param name="level">The group level.</param>
            /// <param name="name">The display name for the group.</param>
            public GroupElement(int level, string name)
            {
                this.level = level;
                content = new GUIContent(name);
            }

            /// <summary>
            /// Handles custom keyboard events on this group.
            /// </summary>
            /// <param name="evt">The event.</param>
            /// <param name="window">A reference to the parent <see cref="FilterWindow"/>.</param>
            /// <param name="goToParent">The action to execute if a "back" action is triggered in the UI.</param>
            /// <returns><c>true</c> if the builtin events should execute for this group, <c>false</c> otherwise.</returns>
            public virtual bool HandleKeyboard(Event evt, FilterWindow window, Action goToParent)
            {
                return false;
            }

            /// <summary>
            /// A custom drawing method for this group.
            /// </summary>
            /// <param name="sFilterWindow">A reference to the parent <see cref="FilterWindow"/>.</param>
            /// <returns><c>true</c> if the builtin drawing function should execute for this group,
            /// <c>false</c> otherwise.</returns>
            public virtual bool OnGUI(FilterWindow sFilterWindow)
            {
                return false;
            }
        }

        #endregion

        #region Bridge to Advanced Dropdown Item
        class DropDownFilterWindowElement : AdvancedDropdownItemBridge
        {
            public Element element { get; }
            public IProvider provider { get; }

            public DropDownFilterWindowElement(Element element, IProvider provider)
                : base(element.name)
            {
                this.element = element;
                this.provider = provider;
            }
        }

        class DropDownFilterWindow : AdvancedDropdownWindowBridge
        {
            class AdvancedDropdownDataProvider : AdvancedDropdownDataSourceBridge
            {
                IProvider provider { get; }

                public AdvancedDropdownDataProvider(IProvider provider)
                    : base()
                {
                    this.provider = provider;
                }

                /// Recursively builds a tree structure from a flattened list of elements with depth levels
                /// Example Input: [A(0), B(1), C(2), D(1), E(2), F(2), G(3), H(2)]
                /// Resulting Tree:
                /// A
                /// → B
                ///     → C
                /// → D
                ///     → E
                ///     → F
                ///         → G
                ///     → H
                void AddNodes(AdvancedDropdownItem root, int depth, List<Element> tree)
                {
                    while (tree.Count != 0)
                    {
                        var top = tree[0];

                        if (depth == top.level)
                        {
                            // This element is not a child of the given root, is actually a sibling
                            // Stop here and let parent root handle it
                            break;
                        }

                        // Pop the top element
                        tree.RemoveAt(0);

                        // We handle two types: GroupElements and regular Elements
                        // GroupElements act as folders that can contain child elements
                        // Regular Elements are leaf nodes that trigger OnItemSelected when clicked perfoming the user logic
                        if (top is GroupElement groupElement)
                        {
                            var group = new AdvancedDropdownItem(top.name);
                            root.AddChild(group);
                            AddNodes(group, depth + 1, tree);
                        }
                        else
                        {
                            root.AddChild(new DropDownFilterWindowElement(top, provider));
                        }
                    }
                }

                private AdvancedDropdownItem EmptyTree()
                {
                    var root = new AdvancedDropdownItem("");
                    root.AddChild(new AdvancedDropdownItem("No Elements Found"));
                    return root;
                }

                private AdvancedDropdownItem ConvertListToTree(List<Element> tree)
                {
                    if (tree.Count == 0)
                        return EmptyTree();

                    var top = tree[0];
                    var root = new AdvancedDropdownItem(top.name);
                    tree.RemoveAt(0);
                    AddNodes(root, top.level, tree);
                    return root;
                }

                protected override AdvancedDropdownItem FetchData()
                {
                    var tree = new List<Element>();
                    provider.CreateComponentTree(tree);
                    return ConvertListToTree(tree);
                }
            }

            internal static bool Show(Rect rect, IProvider provider)
            {
                return Show<DropDownFilterWindow>(rect, new AdvancedDropdownDataProvider(provider));
            }

            protected override void OnItemSelected(AdvancedDropdownItem item)
            {
                if (item is DropDownFilterWindowElement dropDownFilterWindowElement)
                    dropDownFilterWindowElement.provider.GoToChild(dropDownFilterWindowElement.element, true);
            }
        }
        #endregion

        /// <summary>
        /// Shows the filter window using the given provider.
        /// </summary>
        /// <param name="position">The position to show the filter window at.</param>
        /// <param name="provider">The provider of items for the filter window.</param>
        /// <returns>Returns true if the window is shown, false otherwise.</returns>
        public static bool Show(Vector2 position, IProvider provider) =>
            Show(new Rect( position.x - DefaultWidth / 2f, position.y - 18f - 17f, DefaultWidth, 17f), provider);

        /// <summary>
        /// Shows the filter window using the given provider.
        /// </summary>
        /// <param name="r">The rect to show the filter window below.</param>
        /// <param name="provider">The provider of items for the filter window.</param>
        /// <returns>Returns true if the window is shown, false otherwise.</returns>
        public static bool Show(Rect r, IProvider provider) => DropDownFilterWindow.Show(r, provider);
    }
}
