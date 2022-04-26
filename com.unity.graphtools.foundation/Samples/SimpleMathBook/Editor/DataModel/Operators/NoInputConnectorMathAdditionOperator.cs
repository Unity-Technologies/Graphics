using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// This Add operator serves as an example to showcase the possibility for a node to have input ports with no connector.
    /// </summary>
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "No input connectors/Add", "operator-add")]
    public class NoInputConnectorMathAdditionOperator : MathAdditionOperator
    {
        public override string Title
        {
            get => "Add";
            set {}
        }

        protected override void AddInputPorts()
        {
            this.AddNoConnectorInputPort("Term 1", OperatorType);
            for (var i = 1; i < InputPortCount; ++i)
                this.AddDataInputPort("Term " + (i + 1), OperatorType);
        }
    }
}
