using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// View model for the inspector.
    /// </summary>
    [Serializable]
    public class InspectorModel : IInspectorModel
    {
        const string k_InspectorTitle = "Inspector";
        const string k_GraphInspectorTitle = "Graph Inspector";
        const string k_NodeInspectorTitle = "Node Inspector";

        [SerializeField]
        SerializableGUID m_Guid;

        [SerializeField]
        string m_Title;

        [SerializeReference]
        List<IInspectorSectionModel> m_SectionModels;

        [SerializeField]
        Vector2 m_ScrollOffset;

        /// <inheritdoc />
        public string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <inheritdoc />
        public string DisplayTitle => Title;

        /// <inheritdoc />
        public IReadOnlyList<IInspectorSectionModel> Sections => m_SectionModels;

        /// <inheritdoc />
        public SerializableGUID Guid
        {
            get
            {
                if (!m_Guid.Valid)
                    AssignNewGuid();
                return m_Guid;
            }
            set => m_Guid = value;
        }

        public Vector2 ScrollOffset
        {
            get => m_ScrollOffset;
            set => m_ScrollOffset = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorModel"/> class.
        /// </summary>
        /// <param name="inspectedModel">The model that will be inspected.</param>
        public InspectorModel(IModel inspectedModel)
        {
            SetTitleFromModel(inspectedModel);

            m_SectionModels = new List<IInspectorSectionModel>();

            switch (inspectedModel)
            {
                case INodeModel _:
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = null,
                        Collapsible = false,
                        SectionType = SectionType.Settings
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = "Node Properties",
                        Collapsed = false,
                        SectionType = SectionType.Properties
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = "Advanced Properties",
                        Collapsed = false,
                        SectionType = SectionType.Advanced
                    });
                    break;
                case IVariableDeclarationModel _:
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = null,
                        Collapsible = false,
                        SectionType = SectionType.Settings
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = "Advanced Properties",
                        Collapsed = false,
                        SectionType = SectionType.Advanced
                    });
                    break;
                case IGraphModel _:
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = "Graph Settings",
                        Collapsed = false,
                        SectionType = SectionType.Settings
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = "Advanced Settings",
                        Collapsed = true,
                        SectionType = SectionType.Advanced
                    });
                    break;
            }
        }

        /// <inheritdoc />
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }

        bool SetTitleFromModel(IModel inspectedModel)
        {
            var currentTitle = m_Title;

            if (inspectedModel is IHasTitle hasTitle)
            {
                m_Title = hasTitle.Title;
            }

            if (string.IsNullOrEmpty(m_Title))
            {
                switch (inspectedModel)
                {
                    case INodeModel _:
                        m_Title = k_NodeInspectorTitle;
                        break;
                    case IGraphModel _:
                        m_Title = k_GraphInspectorTitle;
                        break;
                    default:
                        m_Title = k_InspectorTitle;
                        break;
                }
            }

            return m_Title != currentTitle;
        }
    }
}
