using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
{
    public class RedirectNode : CollapsibleInOutNode
    {
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(new DebugStringPart("redir-debug-src", Model, this, ussClassName, ge =>
            {
                var src = ((RedirectNodeModel) ge).ResolveSource();

                return src is null ? "No source" : $"Source: {GetPortDisplayString(src)}";
            }));

            PartList.AppendPart(new DebugStringPart("redir-debug-dst", Model, this, ussClassName, ge =>
            {
                var dsts = ((RedirectNodeModel) ge).ResolveDestinations().ToList();

                return !dsts.Any()
                    ? "No destinations"
                    : $"Destinations: \n\t{string.Join(", \n\t", dsts.Select(GetPortDisplayString))}";
            }));
        }

        static string GetPortDisplayString(IPortModel port)
        {
            return $"{port.UniqueName} ({(port.NodeModel is IHasTitle hasTitle ? hasTitle.DisplayTitle : port.NodeModel.Guid)})";
        }
    }
}
