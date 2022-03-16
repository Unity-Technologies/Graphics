using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The <see cref="GraphView.Layer"/> that contains all placemats.
    /// </summary>
    public class PlacematContainer : GraphView.Layer
    {
        public static readonly string ussClassName = "ge-placemat-container";

        GraphView m_GraphView;

        public static int PlacematsLayer => Int32.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlacematContainer"/> class.
        /// </summary>
        /// <param name="graphView">The parent graph view.</param>
        public PlacematContainer(GraphView graphView)
        {
            m_GraphView = graphView;

            this.AddStylesheet("PlacematContainer.uss");
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
        }

        public bool GetPortCenterOverride(Port port, out Vector2 overriddenPosition)
        {
            var rootNode = port.PortModel.NodeModel.GetView<GraphElement>(port.RootView);
            if (m_GraphView.GraphModel != null && rootNode != null)
            {
                //Find the furthest placemat containing the rootNode and that is collapsed (if any)
                for (var i = 0; i < m_GraphView.GraphModel.PlacematModels.Count; i++)
                {
                    var placematModel = m_GraphView.GraphModel.PlacematModels[i];
                    if (placematModel.Collapsed)
                    {
                        var placemat = placematModel.GetView<Placemat>(m_GraphView);
                        if (placemat?.WillDragNode(rootNode) ?? false)
                        {
                            return placemat.GetPortCenterOverride(port.PortModel, out overriddenPosition);
                        }
                    }
                }
            }

            overriddenPosition = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Sort the placemat visual elements by their model Z order.
        /// </summary>
        public void UpdateElementsOrder()
        {
            Sort((a, b) => ((Placemat)a).PlacematModel.GetZOrder().CompareTo(((Placemat)b).PlacematModel.GetZOrder()));
        }
    }
}
