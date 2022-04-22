using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "No input connectors/And")]
    [SeacherHelp("Outputs <i>true</i> if <u>all</u> boolean inputs are <i>true</i>.")]
    public class NoInputConnectorAndOperator : AndOperator
    {
        public override string Title
        {
            get => "And";
            set {}
        }

        protected override void AddInputPorts()
        {
            this.AddNoConnectorInputPort("Port 1", TypeHandle.Bool);
            for (var i = 1; i < InputPortCount; ++i)
                this.AddDataInputPort("Port " + (i + 1), TypeHandle.Bool);
        }
    }
}
