using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for an <see cref="IEdgeModel"/>, with user editable control points.
    /// </summary>
    public class EdgeControlPart : BaseModelViewPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="EdgeControlPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="EdgeControlPart"/>.</returns>
        public static EdgeControlPart Create(string name, IModel model, IModelView ownerElement, string parentClassName)
        {
            if (model is IEdgeModel)
            {
                return new EdgeControlPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected EdgeControl m_EdgeControl;

        /// <inheritdoc />
        public override VisualElement Root => m_EdgeControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeControlPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected EdgeControlPart(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            m_EdgeControl = new EdgeControl(m_OwnerElement as Edge) { name = PartName };
            m_EdgeControl.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_EdgeControl.RegisterCallback<MouseEnterEvent>(OnMouseEnterEdge);
            m_EdgeControl.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEdge);

            container.Add(m_EdgeControl);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is IEdgeModel edgeModel)
            {
                m_EdgeControl.OutputOrientation = edgeModel.FromPort?.Orientation ?? (edgeModel.ToPort?.Orientation ?? PortOrientation.Horizontal);
                m_EdgeControl.InputOrientation = edgeModel.ToPort?.Orientation ?? (edgeModel.FromPort?.Orientation ?? PortOrientation.Horizontal);
            }

            m_EdgeControl.UpdateLayout();
            UpdateEdgeControlColors();
            m_EdgeControl.MarkDirtyRepaint();
        }

        protected void UpdateEdgeControlColors()
        {
            var parent = m_OwnerElement as Edge;

            if (parent?.IsSelected() ?? false)
            {
                m_EdgeControl.ResetColor();
            }
            else
            {
                var edgeModel = m_Model as IEdgeModel;
                var inputColor = Color.white;
                var outputColor = Color.white;

                if (edgeModel?.ToPort != null)
                    inputColor = edgeModel.ToPort.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;
                else if (edgeModel?.FromPort != null)
                    inputColor = edgeModel.FromPort.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;

                if (edgeModel?.FromPort != null)
                    outputColor = edgeModel.FromPort.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;
                else if (edgeModel?.ToPort != null)
                    outputColor = edgeModel.ToPort.GetView<Port>(m_OwnerElement.RootView)?.PortColor ?? Color.white;

                if (parent?.IsGhostEdge ?? false)
                {
                    inputColor = new Color(inputColor.r, inputColor.g, inputColor.b, 0.5f);
                    outputColor = new Color(outputColor.r, outputColor.g, outputColor.b, 0.5f);
                }

                m_EdgeControl.SetColor(inputColor, outputColor);
            }
        }

        protected void OnMouseEnterEdge(MouseEnterEvent e)
        {
            if (e.target == m_EdgeControl)
            {
                m_EdgeControl.ResetColor();
            }
        }

        protected void OnMouseLeaveEdge(MouseLeaveEvent e)
        {
            if (e.target == m_EdgeControl)
            {
                UpdateEdgeControlColors();
            }
        }
    }
}
