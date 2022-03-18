using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// The view model for an inspector section.
    /// </summary>
    [Serializable]
    public class InspectorSectionModel : IInspectorSectionModel
    {
        [SerializeField]
        SerializableGUID m_Guid;

        [SerializeField]
        string m_Title;

        [SerializeField]
        bool m_Collapsible = true;

        [SerializeField]
        SectionType m_SectionType;

        [SerializeField]
        bool m_Collapsed;

        /// <inheritdoc />
        public string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <inheritdoc />
        public string DisplayTitle => Title;

        /// <inheritdoc />
        public SectionType SectionType
        {
            get => m_SectionType;
            set => m_SectionType = value;
        }

        /// <inheritdoc />
        public bool Collapsible
        {
            get => m_Collapsible;
            set => m_Collapsible = value;
        }

        /// <inheritdoc />
        public bool Collapsed
        {
            get => m_Collapsed;
            set => m_Collapsed = value;
        }

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

        /// <inheritdoc />
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }
    }
}
