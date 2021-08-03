using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
{
    public class GraphDataNode : CollapsibleInOutNode
    {
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.InsertPartAfter(titleIconContainerPartName, new DebugStringPart("registry-key", Model, this,
                ussClassName,
                m =>
                {
                    try
                    {
                        return ((GraphDataNodeModel) NodeModel).registryKey.ToString();
                    }
                    catch
                    {
                        return "Invalid key";
                    }
                }));

            PartList.InsertPartAfter(titleIconContainerPartName, new DebugStringPart("graph-data-path", Model, this,
                ussClassName, m => ((GraphDataNodeModel) m).graphDataName));

            // TODO: Build out fields from node definition
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            // TODO: Build contextual menu from node definition
        }
    }
}
