using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Inspector for node port default values.
    /// </summary>
    public class NodePortsInspector : FieldsInspector
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NodePortsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="NodePortsInspector"/>.</returns>
        public static NodePortsInspector Create(string name, IModel model, IModelView ownerElement, string parentClassName)
        {
            return new NodePortsInspector(name, model, ownerElement, parentClassName);
        }

        /// <inheritdoc />
        public NodePortsInspector(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (ShouldRebuildFields())
            {
                BuildFields();
                foreach (var modelField in m_Fields)
                {
                    modelField.UpdateDisplayedValue();
                }
            }
            else
            {
                foreach (var modelField in m_Fields)
                {
                    if (!modelField.UpdateDisplayedValue())
                    {
                        BuildFields();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the fields should be completely rebuilt.
        /// </summary>
        /// <returns>True if the fields should be rebuilt.</returns>
        protected virtual bool ShouldRebuildFields()
        {
            var portsToDisplay = GetPortsToDisplay().ToList();

            if (portsToDisplay.Count != m_Fields.Count)
                return true;

            for (var i = 0; i < portsToDisplay.Count; i++)
            {
                if (m_Fields[i] is ConstantField constantField)
                {
                    if (!ReferenceEquals(portsToDisplay[i], constantField.Owner))
                        return true;

                    if (portsToDisplay[i].EmbeddedValue.Type != constantField.ConstantModel.Type)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the ports that should be displayed in the inspector.
        /// </summary>
        /// <returns>An enumerable of ports to display.</returns>
        protected virtual IEnumerable<IPortModel> GetPortsToDisplay()
        {
            var portNodeModel = m_Model as IPortNodeModel;
            return portNodeModel?.Ports.Where(
                p => p.Direction == PortDirection.Input && p.PortType == PortType.Data && p.EmbeddedValue != null);
        }

        /// <inheritdoc />
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            var ports = GetPortsToDisplay();

            if (ports == null)
                yield break;

            foreach (var port in ports)
            {
                yield return InlineValueEditor.CreateEditorForConstant(
                    m_OwnerElement.RootView, port, port.EmbeddedValue, false,
                    (port as IHasTitle)?.DisplayTitle ?? "");
            }
        }
    }
}
