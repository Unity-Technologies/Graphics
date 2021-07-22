using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
{
    /// <summary>
    /// A RegistryNode is the view for a RegistryNodeModel. It gets a description of its UI from the ShaderGraph
    /// Registry.
    /// </summary>
    public class RegistryNode : CollapsibleInOutNode
    {
        protected override void BuildPartList()
        {
            base.BuildPartList();

            if (Model is not RegistryNodeModel registryNodeModel) return;

            PartList.InsertPartAfter(titleIconContainerPartName, new RegistryKeyPart("registry-key", Model, this, ussClassName));

            // TODO: Build part list from node definition
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (Model is not RegistryNodeModel registryNodeModel) return;

            // TODO: Build contextual menu from node definition
        }
    }
}
