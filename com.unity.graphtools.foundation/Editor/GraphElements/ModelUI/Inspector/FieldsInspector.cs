using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for UI parts that display a list of <see cref="BaseModelPropertyField"/>.
    /// </summary>
    public abstract class FieldsInspector : BaseModelUIPart
    {
        public static readonly string ussClassName = "ge-inspector-fields";

        VisualElement m_Root;
        List<BaseModelPropertyField> m_Fields;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldsInspector"/> class.
        /// </summary>
        protected FieldsInspector(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_Fields = new List<BaseModelPropertyField>();
            foreach (var field in GetFields())
            {
                m_Fields.Add(field);
                m_Root.Add(field);
            }

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            foreach (var modelField in m_Fields)
            {
                modelField.UpdateDisplayedValue();
            }
        }

        /// <summary>
        /// Gets the field to display.
        /// </summary>
        /// <returns>The fields to display.</returns>
        protected abstract IEnumerable<BaseModelPropertyField> GetFields();
    }
}
