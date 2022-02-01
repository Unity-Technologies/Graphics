using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Inspector for <see cref="INodeModel"/>.
    /// </summary>
    public class NodeFieldsInspector : FieldsInspector
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NodeFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardHeaderPart"/>.</returns>
        public static NodeFieldsInspector Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is INodeModel)
            {
                return new NodeFieldsInspector(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeFieldsInspector"/> class.
        /// </summary>
        protected NodeFieldsInspector(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            if (m_Model is ICollapsible)
                yield return new ModelPropertyField<bool>(
                    m_OwnerElement.View,
                    m_Model,
                    nameof(ICollapsible.Collapsed),
                    null,
                    typeof(CollapseNodeCommand));

            yield return new ModelPropertyField<ModelState>(
                m_OwnerElement.View,
                m_Model,
                nameof(INodeModel.State),
                null,
                typeof(ChangeNodeStateCommand));
        }
    }
}
