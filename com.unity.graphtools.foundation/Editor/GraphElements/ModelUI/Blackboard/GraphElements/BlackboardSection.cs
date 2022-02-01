using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A section of the blackboard. A section contains a group of variables from the graph.
    /// </summary>
    public class BlackboardSection : BlackboardVariableGroup
    {
        /// <summary>
        /// The uss class name for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard-section";

        /// <summary>
        /// The uss class name for the header.
        /// </summary>
        public static readonly string headerUssClassName = ussClassName.WithUssElement("header");

        /// <summary>
        /// The uss class name for the add button.
        /// </summary>
        public static readonly string addButtonUssClassName = ussClassName.WithUssElement("add");

        /// <summary>
        /// The add button.
        /// </summary>
        protected Button m_AddButton;

        /// <inheritdoc />
        protected override BlackboardSection Section => this;

        ISectionModel SectionModel => Model as ISectionModel;

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            AddToClassList(ussClassName);

            base.BuildElementUI();

            m_Title.AddToClassList(headerUssClassName);

            m_AddButton = new Button { text = "+" };
            m_AddButton.AddToClassList(addButtonUssClassName);
            m_Title.Add(m_AddButton);

            m_AddButton.clicked += () =>
            {
                var menu = new GenericMenu();

                var selectedGroup = GraphView.GetSelection().OfType<IGroupModel>().FirstOrDefault(t => t.GetSection() == Model);

                ((Stencil)Model.GraphModel.Stencil)?.PopulateBlackboardCreateMenu(name, menu, View, Model.GraphModel, selectedGroup);
                var menuPosition = new Vector2(m_AddButton.layout.xMin, m_AddButton.layout.yMax);
                menuPosition = m_AddButton.parent.LocalToWorld(menuPosition);
                menu.DropDown(new Rect(menuPosition, Vector2.zero));
            };
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            m_TitleLabel.AddToClassList(ussClassName.WithUssElement(TitlePartName));
        }
    }
}
