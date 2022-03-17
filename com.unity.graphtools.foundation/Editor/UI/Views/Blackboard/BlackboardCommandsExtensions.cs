using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class BlackboardCommandsExtensions
    {
        public static void DispatchSelectUnusedVariables(this BlackboardView self)
        {
            List<IGraphElementModel> selectables = new List<IGraphElementModel>();

            foreach (var variable in self.BlackboardViewModel.ParentGraphView.GraphModel.VariableDeclarations)
            {
                if( ! variable.IsUsed())
                    selectables.Add(variable);
            }

            self.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, selectables));
        }
    }
}
