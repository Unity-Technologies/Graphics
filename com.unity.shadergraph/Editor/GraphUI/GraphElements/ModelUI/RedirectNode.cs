using System.Linq;
using System.Text;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
{
    public class RedirectNode : Node
    {
        protected override void BuildPartList()
        {
            PartList.AppendPart(InOutPortContainerPart.Create(portContainerPartName, Model, this, ussClassName));
            AddToClassList("sg-redirect-node");
            this.AddStylesheet("RedirectNode.uss");
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            // TODO: Remove
            evt.menu.InsertAction(0, "Log Source and Destinations", _ =>
            {
                var src = ((RedirectNodeModel) Model).ResolveSource();
                var dsts = ((RedirectNodeModel) Model).ResolveDestinations().ToList();

                var msg = new StringBuilder($"Node {Model.Guid}\n");
                msg.AppendLine(src is null ? "No source" : $"Source: {GetPortDisplayString(src)}");
                msg.AppendLine(!dsts.Any()
                    ? "No destinations"
                    : $"Destinations: \n\t{string.Join(", \n\t", dsts.Select(GetPortDisplayString))}");

                Debug.Log(msg.ToString());
            });
        }

        static string GetPortDisplayString(IPortModel port)
        {
            return $"{port.UniqueName} ({(port.NodeModel is IHasTitle hasTitle ? hasTitle.DisplayTitle : port.NodeModel.Guid)})";
        }
    }
}
