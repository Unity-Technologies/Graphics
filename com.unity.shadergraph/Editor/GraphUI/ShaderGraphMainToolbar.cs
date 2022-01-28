using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphMainToolbar : MainToolbar
    {
        public ShaderGraphMainToolbar(BaseGraphTool graphTool, GraphView graphView)
            : base(graphTool, graphView)
        {
        }

        protected override void BuildOptionMenu(GenericMenu menu)
        {
            base.BuildOptionMenu(menu);
            /**
             * Additional main toolbar cog-menu items can be added here
             * Example:
             *   menu.AddSeparator("");
             *   MenuToggle("Auto Itemize Constants", BoolPref.AutoItemizeConstants);
             *   MenuToggle("Auto Itemize Variables", BoolPref.AutoItemizeVariables);
             **/
        }
    }
}
