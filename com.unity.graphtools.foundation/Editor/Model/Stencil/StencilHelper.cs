
namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public static class StencilHelper
    {
        /// <summary>
        /// Returns true if the given node is one of the common nodes of the BasicModel that should authorize copy/paste.
        /// </summary>
        /// <param name="nodeModel">The node model to be tested.</param>
        /// <returns>True if the given node is one of the common nodes of the BasicModel that should be copy/pasted.</returns>
        public static bool IsCommonNodeThatCanBePasted(INodeModel nodeModel)
        {
            return nodeModel is ConstantNodeModel || nodeModel is VariableNodeModel || nodeModel is ContextNodeModel;
        }
    }
}
